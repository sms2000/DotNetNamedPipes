using System.ComponentModel;
using System.Reflection;
using System.Reflection.Emit;
using System.Text;
using System.ServiceModel;
using Interfaces;
using DataExchangeNET6.Exchange.Dynamic;
using DataExchangeNET6.Performance;

namespace DataExchangeNET6.Exchange
{
    /// <summary>
    /// "Implements" a T-interface 
    /// </summary>
    /// <typeparam name="T"></typeparam>
    public class DynamicObjectImplementor<T> : IDynamicObject where T : class
    {
        #region emits
        private static readonly MethodInfo m_methodBuildSerialized = TransferRecord.GetBuildSerializedMethodInfo();
        private static readonly MethodInfo m_methodProxyDelegate = ProxyStatics.GetProxyDelegateMethodInfo();

        private static readonly HashSet<Type> m_internalTypes = new()
        {
            typeof(char),
            typeof(short),
            typeof(int),
            typeof(long),
            typeof(bool),
            typeof(float),
            typeof(double),
        };

        private static readonly Dictionary<string, Type> m_primitives = new()
        {
            { "Int64&", typeof(long) },
            { "Int32&", typeof(int) },
            { "Int16&", typeof(short) },
            { "Int8&", typeof(byte) },
            { "Int&", typeof(int) },
            { "Char&", typeof(char) },

            { "Boolean&", typeof(bool) },

            { "Double&", typeof(double) },
            { "Float&", typeof(float) },

            { "System.Int64&", typeof(long) },
            { "System.Int32&", typeof(int) },
            { "System.Int16&", typeof(short) },
            { "System.Int8&", typeof(byte) },
            { "System.Int&", typeof(int) },
            { "System.Char&", typeof(char) },

            { "System.Boolean&", typeof(bool) },

            { "System.Double&", typeof(double) },
            { "System.Float&", typeof(float) }
        };

        private static readonly Dictionary<string, OpCode> m_loadByRef = new()
        {
            { "Int64&", OpCodes.Ldind_I8 },
            { "Int32&", OpCodes.Ldind_I4 },
            { "Int16&", OpCodes.Ldind_I2 },
            { "Int8&", OpCodes.Ldind_I1 },
            { "Int&", OpCodes.Ldind_I },

            { "Double&", OpCodes.Ldind_R8 },
            { "Float&", OpCodes.Ldind_R4 },

            { "UnsignedInt32&", OpCodes.Ldind_U4 },  // TBD
            { "UnsignedInt16&", OpCodes.Ldind_U2 },  // TBD
            { "UnsignedInt8&", OpCodes.Ldind_U1 }    // TBD
        };

        private static readonly OpCode m_defLoadByRef = OpCodes.Ldind_Ref;

        
        private static readonly Dictionary<string, OpCode[]> m_storeByRef = new()
        {
            { "Int64&", new [] { OpCodes.Ldc_I4_0, OpCodes.Conv_I8, OpCodes.Stind_I8} },
            { "Int32&", new [] { OpCodes.Ldc_I4_0, OpCodes.Stind_I4 } },
            { "Int16&", new [] { OpCodes.Ldc_I4_0, OpCodes.Stind_I2 } },
            { "Int8&", new [] { OpCodes.Ldc_I4_0, OpCodes.Stind_I2 } },
            { "Int&", new [] { OpCodes.Ldc_I4_0, OpCodes.Stind_I } },

            { "Double&", new [] { OpCodes.Ldc_R8, OpCodes.Stind_R8 } },
            { "Float&", new [] { OpCodes.Ldc_R4, OpCodes.Stind_R4 } },
        };
        
        private static readonly OpCode[] m_defStoreByRef = { OpCodes.Ldnull, OpCodes.Stind_Ref };

        private static readonly Dictionary<string, OpCode[]> m_reloadByRef_1 = new()
        {
            { "Int64&", new [] { OpCodes.Unbox_Any } },
            { "Int32&", new [] { OpCodes.Unbox_Any } },
            { "Int16&", new [] { OpCodes.Unbox_Any } },
            { "Int8&", new [] { OpCodes.Unbox_Any } },
            { "Int&", new [] { OpCodes.Unbox_Any } },

            { "Double&", new [] { OpCodes.Unbox_Any } },
            { "Float&", new [] { OpCodes.Unbox_Any } },
        };

        private static readonly OpCode[] m_defReloadByRef_1 = { OpCodes.Castclass };

        private static readonly Dictionary<string, OpCode[]> m_reloadByRef_2 = new()
        {
            { "Int64&", new [] { OpCodes.Stind_I8 } },
            { "Int32&", new [] { OpCodes.Stind_I4 } },
            { "Int16&", new [] { OpCodes.Stind_I2 } },
            { "Int8&", new [] { OpCodes.Stind_I2 } },
            { "Int&", new [] { OpCodes.Stind_I } },

            { "Double&", new [] { OpCodes.Stind_R8 } },
            { "Float&", new [] { OpCodes.Stind_R4 } },
        };

        private static readonly OpCode[] m_defReloadByRef_2 = { OpCodes.Stind_Ref };

        private static readonly OpCode m_boxOpcode = OpCodes.Box;
        #endregion emits

        private static readonly string m_defaultProxyAssembly = "ProxyAssembly";

        private Type? m_callbackType;
        private readonly T m_implemented;
        private readonly ICommonPipe m_clientPipeConnection;
        private readonly bool m_async;

        public Type? CallbackType => m_callbackType;

        public DynamicObjectImplementor(ICommonPipe clientPipeConnection, long connectTimeoutMs)
        {
            m_clientPipeConnection = clientPipeConnection;
            m_async = clientPipeConnection is IClientPipeAsync;

            processContract();
            m_implemented = implementInterface();

            if (!Initialize(connectTimeoutMs))
            {
                throw new InvalidOperationException("Failed to connect");
            }
        }

        public T GetImplemented()
        {
            return m_implemented!;
        }

        /// <summary>
        /// Dispose of the object
        /// </summary>
        public void Dispose()
        {
            GC.SuppressFinalize(this);

            // Kill pipe (mind the call order)
            m_clientPipeConnection?.Close();
            m_clientPipeConnection?.Dispose();
        }

        /// <summary>
        /// Two-way synchronous exchange
        /// </summary>
        /// <param name="encoded">Binary blob</param>
        /// <returns>Optional returned object</returns>
        public object? SendAndWaitAnswer(byte[] encoded)
        {
            if (m_clientPipeConnection == null)
            {
                throw new Exception("ClientPipeConnection isn't initialized");
            }

            try
            {
                byte[]? returned;

                if (m_async)
                {
                    var token = new CancellationTokenRegistration().Token;
                    var pipe = m_clientPipeConnection as IClientPipeAsync;
                    pipe?.WriteBytes(encoded, token, out var timeSpentInWriteMs);
                    returned = pipe?.ReadBytes(token, out var timeSpentInReadMs);
                }
                else
                {
                    var pipe = m_clientPipeConnection as IClientPipe;
                    pipe?.WriteBytes(encoded, out var timeSpentInWriteMs);
                    returned = pipe?.ReadBytes(out var timeSpentInReadMs);
                }

                if (returned == null)
                {
                    // Log
                    return null;
                }

                var str = Encoding.UTF8.GetString(returned);

                // Deserialize and proceed
                var returnValue = MethodCall.DeserializeReturnValue(str);
                if (returnValue != null)
                {
                    // Log - non-Void (legitimate)
                    var millisecondsReceive = (DateTime.UtcNow - returnValue.UtcSerializedTimestamp).TotalMilliseconds;
                    return returnValue;
                }

                // Log - void (also legitimate)
                return null;
            }
            catch (Exception ex)
            {
                // Log
                throw new InvalidOperationException("Nested: " + ex.Message + "," + Environment.NewLine + "Stack: " + ex.StackTrace);
            }
        }

        protected bool Initialize(long connectTimeoutMs)
        {
            if (m_clientPipeConnection != null)
            {
                try
                {
                    if (m_clientPipeConnection.Connect(connectTimeoutMs))
                    {
                        // Log
                        return true;
                    }
                }
                catch (Exception /* ex */)
                {
                    // Log
                }
            }

            // Log
            return false;
        }

#region private
        private void processContract()
        {
            var contract = false;
            var interfaceAttributes = Attribute.GetCustomAttributes(typeof(T));

            foreach (var interfaceAttribute in interfaceAttributes)
            {
                if (interfaceAttribute is not ServiceContractAttribute serviceContractAttribute)
                {
                    continue;
                }

                contract = true;

                if (serviceContractAttribute.CallbackContract is { FullName: { } })
                {
                    var typeName = serviceContractAttribute.CallbackContract.FullName;

                    foreach (var assembly in AppDomain.CurrentDomain.GetAssemblies())
                    {
                        if (assembly.FullName == null || assembly.FullName.StartsWith("System."))
                        {
                            continue;
                        }

                        m_callbackType = assembly.GetType(typeName);
                        if (m_callbackType != null)
                        {
                            break;
                        }
                    }
                }

                break;
            }

            if (!contract)
            {
                throw new ArgumentException("Interface " + typeof(T) + " should be a contract");
            }
        }

        /// <summary>
        /// Implements protocol methods fot the interface T
        /// </summary>
        /// <returns></returns>
        /// <exception cref="ArgumentException"></exception>
        private T implementInterface()
        {
            var assemblyName = new AssemblyName(m_defaultProxyAssembly);
            var moduleBuilder = AssemblyBuilder.DefineDynamicAssembly(assemblyName, AssemblyBuilderAccess.RunAndCollect).DefineDynamicModule(assemblyName.FullName);
            var typeBuilder = moduleBuilder.DefineType(typeof(T).Name, TypeAttributes.Class);
            typeBuilder.AddInterfaceImplementation(typeof(T));

            // Add property
            addDynamicPropertyToType(typeBuilder, new DynamicProperty(IPCHelper.DynamicPropertyName, IPCHelper.DynamicPropertyName));

            // GetImplemented only methods and only those of Interface. Not stubs for properties.
            var methods = typeof(T).GetMethods(BindingFlags.Public | BindingFlags.Instance);

            foreach (var method in methods)
            {
                if (method.GetCustomAttributes().OfType<OperationContractAttribute>().Any())
                {
                    implementMethod(typeBuilder, method);
                }
            }

            var tp = typeBuilder.CreateType();
            if (tp != null)
            {
                var inst = Activator.CreateInstance(tp);
                if (inst is T retObj)
                {
                    return retObj;
                }
            }

            throw new ArgumentException("Cannot create an instance of the interface " + typeof(T).Name);
        }

        private static void implementMethod(TypeBuilder typeBuilder, MethodInfo method)
        {
            var paramInfo = method.GetParameters();
            var paramTypes = new Type[paramInfo.Length];
            var paramTypesExpanded = new Type[ProxyStatics.ServiceParameters + paramInfo.Length];
            var retType = method.ReturnType;

            paramTypesExpanded[ProxyStatics.ServiceParameterThis] = typeof(IDynamicObject);
            paramTypesExpanded[ProxyStatics.ServiceParameterHeader] = typeof(TransferRecord);

            for (var i = 0; i < paramInfo.Length; i++)
            {
                paramTypes[i] = paramInfo[i].ParameterType;
                paramTypesExpanded[ProxyStatics.ServiceParameters + i] = paramInfo[i].ParameterType;
            }

            var meb = typeBuilder.DefineMethod(method.Name, 
                                               MethodAttributes.Public | MethodAttributes.Virtual | MethodAttributes.NewSlot, 
                                               method.ReturnType, paramTypes);

            meb.SetImplementationFlags(MethodImplAttributes.NoOptimization);
            var il = meb.GetILGenerator();
            var localParamArray = il.DeclareLocal(typeof(object).MakeArrayType());
            var localRetVal = method.ReturnType != typeof(void) ? il.DeclareLocal(method.ReturnType) : default;


#if DEBUG_OPCODES
            Console.WriteLine("\nCIL code for the method: {0}" + method.Name);
#endif

            for (var i = 0; i < paramInfo.Length; i++)
            {
                if (!paramInfo[i].IsOut)
                {
                    continue;
                }

                applyLdargByIndex(il, (short)(i + 1));
                emitOutParamInit(il, paramTypes[i]);

#if DEBUG_OPCODES
                Console.WriteLine();
#endif
            }

            applyLdcI4ByIndex(il, (byte)(ProxyStatics.ServiceParameters + paramInfo.Length));
            emit(il, OpCodes.Newarr, typeof(object));
            emit(il, OpCodes.Stloc, localParamArray);
#if DEBUG_OPCODES
            Console.WriteLine();
#endif

            // 'wcfDynamicObject'
            emit(il, OpCodes.Ldloc, localParamArray);
            applyLdcI4ByIndex(il, 0);
            applyLdargByIndex(il, 0);
            emit(il, OpCodes.Stelem_Ref);
#if DEBUG_OPCODES
            Console.WriteLine();
#endif

            // 'Method info'
            emit(il, OpCodes.Ldloc, localParamArray);
            applyLdcI4ByIndex(il, 1);
            emit(il, OpCodes.Ldstr, method.Name);
            emitCall(il, OpCodes.Call, m_methodBuildSerialized, new[] { typeof(string) });
            emit(il, OpCodes.Stelem_Ref);
#if DEBUG_OPCODES
            Console.WriteLine();
#endif

            for (var i = 0; i < paramInfo.Length; i++)
            {
                emit(il, OpCodes.Ldloc, localParamArray);

                applyLdcI4ByIndex(il, (byte)(i + ProxyStatics.ServiceParameters));

                // Load arg
                applyLdargByIndex(il, (short)(i + ProxyStatics.ServiceParameters - 1));

                // Special part
                emitIfLoad(il, paramTypes[i]);
                emitIfBoxing(il, paramTypes[i]);

                emit(il, OpCodes.Stelem_Ref);
#if DEBUG_OPCODES
                Console.WriteLine();
#endif
            }

            // Call
            emit(il, OpCodes.Ldloc, localParamArray);
            emitCall(il, OpCodes.Call, m_methodProxyDelegate!, paramTypesExpanded);

            // Return cast (or just pop if of Void)
            if (localRetVal != default)
            {
                applyRetVal(il, method.ReturnType, localRetVal);
            }
            else
            {
                emit(il, OpCodes.Pop);
            }

#if DEBUG_OPCODES
            Console.WriteLine();
#endif

            for (var i = 0; i < paramInfo.Length; i++)
            {
                if (!paramInfo[i].IsOut)
                {
                    continue;
                }

                applyLdargByIndex(il, (short)(i + ProxyStatics.ServiceParameters - 1));
                emit(il, OpCodes.Ldloc, localParamArray);
                applyLdcI4ByIndex(il, (byte)(i + ProxyStatics.ServiceParameters));
                emit(il, OpCodes.Ldelem_Ref);

                emitOutParamReload(il, paramTypes[i]);

#if DEBUG_OPCODES
                Console.WriteLine();
#endif
            }

            // Reload returned value if any
            if (localRetVal != default)
            {
                emit(il, OpCodes.Ldloc, localRetVal);
            }

            // Ret
            emit(il, OpCodes.Ret);

#if DEBUG_OPCODES
            Console.WriteLine("CIL code for the method: {0} finished.\n" + method.Name);
#endif
        }

        private static void addDynamicPropertyToType(TypeBuilder typeBuilder, DynamicProperty dynamicProperty)
        {
            var propertyType = dynamicProperty.GetSystemType() ?? throw new ArgumentException("System.Type == 'null'");
            var propertyName = dynamicProperty.PropertyName;
            var fieldName = $"_{propertyName.ToCamelCase()}";

            FieldBuilder fieldBuilder = typeBuilder.DefineField(fieldName, propertyType, FieldAttributes.Private);

            // The property set and get methods require a special set of attributes.
            MethodAttributes getSetAttributes = MethodAttributes.Public | MethodAttributes.SpecialName | MethodAttributes.HideBySig;

            // Define the 'get' accessor method.
            MethodBuilder getMethodBuilder = typeBuilder.DefineMethod($"get_{propertyName}", getSetAttributes, propertyType, Type.EmptyTypes);
            ILGenerator il = getMethodBuilder.GetILGenerator();
            emit(il, OpCodes.Ldarg_0);
            emit(il, OpCodes.Ldfld, fieldBuilder);
            emit(il, OpCodes.Ret);

            // Define the 'set' accessor method.
            MethodBuilder setMethodBuilder = typeBuilder.DefineMethod($"set_{propertyName}", getSetAttributes, null, new Type[] { propertyType });
            il = setMethodBuilder.GetILGenerator();
            emit(il, OpCodes.Ldarg_0);
            emit(il, OpCodes.Ldarg_1);
            emit(il, OpCodes.Stfld, fieldBuilder);
            emit(il, OpCodes.Ret);

            // Lastly, we must map the two methods created above to a PropertyBuilder and their corresponding behaviors, 'get' and 'set' respectively.
            PropertyBuilder propertyBuilder = typeBuilder.DefineProperty(propertyName, PropertyAttributes.HasDefault, propertyType, null);
            propertyBuilder.SetGetMethod(getMethodBuilder);
            propertyBuilder.SetSetMethod(setMethodBuilder);

            // Add a 'DisplayName' attribute.
            var attributeType = typeof(DisplayNameAttribute);
            var constructed = attributeType.GetConstructor(new Type[] { typeof(string) });

            if (constructed == null) throw new Exception("Failed to construct");

            var attributeBuilder = new CustomAttributeBuilder(constructed, new object[] { dynamicProperty.DisplayName },
                                                              Array.Empty<PropertyInfo>(), Array.Empty<object>());

            propertyBuilder.SetCustomAttribute(attributeBuilder);
        }

        private static void emitOutParamInit(ILGenerator il, Type type)
        {
            if (!m_storeByRef.TryGetValue(type.Name, out var opcodes))
            {
                opcodes = m_defStoreByRef;
            }

            foreach (var opcode in opcodes)
            {
                emit(il, opcode);
            }
        }

        private static void emitOutParamReload(ILGenerator il, Type type)
        {
            if (!m_reloadByRef_1.TryGetValue(type.Name, out var opcodes))
            {
                opcodes = m_defReloadByRef_1;
            }

            foreach (var opcode in opcodes)
            {
                emit(il, opcode, getReferedType(type));
            }

            if (!m_reloadByRef_2.TryGetValue(type.Name, out opcodes))
            {
                opcodes = m_defReloadByRef_2;
            }

            foreach (var opcode in opcodes)
            {
                emit(il, opcode);
            }
        }

        private static Type getReferedType(Type type)
        {
            var fullName = type.FullName;
            if (fullName != null && fullName.EndsWith("&"))
            {
                fullName = fullName.Substring(0, fullName.Length - 1);
                var dereferedType = TypeCache.Instance.GetType(fullName);
                if (dereferedType != null)
                {
                    return dereferedType;
                }
            }

            throw new ArgumentException("Type " + type + " must be refered type");
        }

        private static void emitIfLoad(ILGenerator il, Type type)
        {
            if (!type.IsByRef)
            {
                return;
            }

            if (!m_loadByRef.TryGetValue(type.Name, out var opcode))
            {
                opcode = m_defLoadByRef;
            }

            emit(il, opcode);
        }

        private static void emitIfBoxing(ILGenerator il, Type type)
        {
            if (type.IsPrimitive)
            {
                // Boxing the primitive
                emit(il, m_boxOpcode, type);
            } 
            else if (type.FullName != default && m_primitives.TryGetValue(type.FullName, out var derefType))
            {
                emit(il, m_boxOpcode, derefType);
            }
        }
        
        private static void applyLdargByIndex(ILGenerator il, short index)
        {
            switch (index)
            {
                case 0:     emit(il, OpCodes.Ldarg_0);          break;
                case 1:     emit(il, OpCodes.Ldarg_1);          break;
                case 2:     emit(il, OpCodes.Ldarg_2);          break;
                case 3:     emit(il, OpCodes.Ldarg_3);          break;              
                default:    emit(il, OpCodes.Ldarg_S, index);   break;
            }
        }
        
        private static void applyLdcI4ByIndex(ILGenerator il, byte index)
        {
            switch (index)
            {
                case 0:     emit(il, OpCodes.Ldc_I4_0);         break;
                case 1:     emit(il, OpCodes.Ldc_I4_1);         break;
                case 2:     emit(il, OpCodes.Ldc_I4_2);         break;
                case 3:     emit(il, OpCodes.Ldc_I4_3);         break;
                case 4:     emit(il, OpCodes.Ldc_I4_4);         break;
                case 5:     emit(il, OpCodes.Ldc_I4_5);         break;              
                case 6:     emit(il, OpCodes.Ldc_I4_6);         break;
                case 7:     emit(il, OpCodes.Ldc_I4_7);         break;
                case 8:     emit(il, OpCodes.Ldc_I4_8);         break;
                default:    emit(il, OpCodes.Ldc_I4_S, index);  break;
            }
        }


        private static void applyRetVal(ILGenerator il, Type type, LocalBuilder local)
        {
            // TODO: castclass for those types without Def Constructor
            //       isinst - for those with Def Constructor (or may be enough to have castclass?)

            if (m_internalTypes.Contains(type))
            {
                emit(il, OpCodes.Castclass, type);
            }
            else
            {
                emit(il, OpCodes.Isinst, type);
            }

            emit(il, OpCodes.Stloc, local);
        }

        private static void emit(ILGenerator il, OpCode opcode)
        {
            il.Emit(opcode);

#if DEBUG_OPCODES
            Console.WriteLine("OpCode: {0}", opcode.ToString());
#endif

        }

        private static void emit(ILGenerator il, OpCode opcode, FieldBuilder buffer)
        {
            il.Emit(opcode, buffer);

#if DEBUG_OPCODES
            Console.WriteLine("OpCode: {0}, FieldBuffer: {1}", opcode.ToString(), buffer.ToString());
#endif
        }

        private static void emit(ILGenerator il, OpCode opcode, LocalBuilder local)
        {
            il.Emit(opcode, local);

#if DEBUG_OPCODES
            Console.WriteLine("OpCode: {0}, Local var: {1}", opcode.ToString(), local.ToString());
#endif
        }

        private static void emit(ILGenerator il, OpCode opcode, Type type)
        {
            il.Emit(opcode, type);
#if DEBUG_OPCODES
            Console.WriteLine("OpCode: {0} Type: {1}", opcode.ToString(), type);
#endif
        }

        private static void emit(ILGenerator il, OpCode opcode, byte index)
        {
            il.Emit(opcode, index);
#if DEBUG_OPCODES
            Console.WriteLine("OpCode: {0} Byte: {1}", opcode.ToString(), index);
#endif
        }

        private static void emit(ILGenerator il, OpCode opcode, short index)
        {
            il.Emit(opcode, index);
#if DEBUG_OPCODES
            Console.WriteLine("OpCode: {0} Short: {1}", opcode.ToString(), index);
#endif
        }

        private static void emit(ILGenerator il, OpCode opcode, string str)
        {
            il.Emit(opcode, str);
#if DEBUG_OPCODES
            Console.WriteLine("OpCode: {0} String: {1}", opcode.ToString(), str);
#endif
        }

        private static void emitCall(ILGenerator il, OpCode opcode, MethodInfo methodInfo, Type[]? parameters)
        {
            il.EmitCall(opcode, methodInfo, parameters);
#if DEBUG_OPCODES
            Console.WriteLine("OpCode: {0} Method: {1}", opcode.ToString(), methodInfo.Name);
#endif
        }
        #endregion private
    }
}
