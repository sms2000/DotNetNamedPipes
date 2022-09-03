using System.Collections.Concurrent;
using System.Runtime.CompilerServices;
using Interfaces;
using DataExchangeNET6.Processing;
using DataExchangeNET6.Client;
using DataExchangeNET6.Exchange.Dynamic;
using PipeSecurityHelper;
using static PipeSecurityHelper.PipeSecurityProvider;

namespace DataExchangeNET6.Exchange
{
    public static class IPCHelper
    {
        /// <summary>
		/// Holds the list of all hosts. Look-up for the callback channels
		/// </summary>
		private static ConcurrentDictionary<IServerManager, object> m_contextMap = new();

		/// <summary>
		/// Holds the list of all channels in both objective and dynamic form for easy look-up
		/// </summary>
		private static ConcurrentDictionary<object, ChannelLookup> m_channelMap = new();

        /// <summary>
		/// Retyurns the dynamic property name
		/// </summary>
        public static string DynamicPropertyName => "WcfDynamicObject";

		/// <summary>
		/// Creates the server side connection
		/// </summary>
		/// <typeparam name="T">Interface of the data exchange</typeparam>
		/// <param name="url">Pipe name</param>
		/// <param name="hostProcessor">Set of the actual methods</param>
		/// <param name="maxConcurrentClients">Maximum number of clients to connect simultaneously</param>
		/// <param name="securityLimitations">Controls the access</param>
		/// <returns>Ref to the channel manager</returns>
		/// <exception cref="ArgumentNullException"></exception>
		public static IServerManager CreateAndOpenServiceHost<T>(string url, T hostProcessor,
																 int maxConcurrentClients = 1,
																 SecurityLimitations securityLimitations = SecurityLimitations.User) where T : class
        {
			if (url == null)
			{
				throw new ArgumentNullException(nameof(url));
			}

			if (hostProcessor == null)
			{
				throw new ArgumentNullException(nameof(hostProcessor));
			}

			IDataExchangeProcessorAsync dataExchangeManager = new DataExchangeProcessorAsync();

			try
			{
				var pipeManager = new AsyncServerManager<T>(hostProcessor, url, new PipeSecurityProvider(securityLimitations), dataExchangeManager, maxInstances : maxConcurrentClients);
				m_contextMap[pipeManager] = hostProcessor;
				pipeManager.Start();

				// Log
				return pipeManager;
			}
			catch (Exception ex)
            {
				// Log
#if DEBUG
				Console.WriteLine("Exception: " + ex.Message);
#endif
				throw ex.InnerException ?? ex;
            }
        }

		/// <summary>
		/// Creates the client side connection
		/// </summary>
		/// <typeparam name="T">Interface of the data exchange</typeparam>
		/// <param name="url">Pipe name</param>
		/// <param name="callbackProcessor">Actual callback processor</param>
		/// <param name="connectTimeoutMs">Optional connection timeout</param>
		/// <param name="securityLimitations">Controls the access</param>
		/// <returns>The stub interface</returns>
		/// <exception cref="ArgumentNullException"></exception>
		/// <exception cref="ArgumentException"></exception>
		public static T CreateDuplexChannel<T>(string url, object? callbackProcessor, long connectTimeoutMs = 0, 
						  					   SecurityLimitations securityLimitations = SecurityLimitations.User) where T : class
		{
            if (string.IsNullOrEmpty(url))
            {
                throw new ArgumentNullException(nameof(url));
            }

            if (callbackProcessor == null)
            {
                // Log - no callback
            }

            // The code does little to formally process attributes, contract elements etc.
            var pipeConnection = new ClientPipeAsync<T>(url);

            // Create the owner
            var wcfDynamicObject = new DynamicObjectImplementor<T>(pipeConnection, connectTimeoutMs);

            // Create callback (optional)
            IServerManager? callbackServiceHost = null;
            var callbackPipeName = string.Empty;

            if (callbackProcessor != null && wcfDynamicObject.CallbackType != null)
            {
                callbackPipeName = buildCallbackPipeName(url);

                callbackServiceHost = createAndOpenServiceHostForCallback(callbackPipeName, callbackProcessor, securityLimitations);

                // Log
            }

            // Impersonate
            var converted = wcfDynamicObject.GetImplemented();
            if (converted == null)
            {
                throw new ApplicationException("'GetImplemented' failed");
            }

            m_channelMap[converted] = new ChannelLookup(converted, wcfDynamicObject, callbackServiceHost);

            if (callbackServiceHost != null)
            {
                // Inform the host about the callback channel activated
                new CallbackRegistrationSender(pipeConnection).Send(callbackPipeName);
            }

            // Hide the Wcf object inside a dedicated dynamic property of 'converted'
            DynamicPropertiesHelper.SetDynamicProperty(converted, DynamicPropertyName, wcfDynamicObject);

            return converted;
		}

		/// <summary>
		/// Create a raw accessing client to test security and other issues
		/// </summary>
		/// <param name="url"></param>
		/// <param name="connectTimeoutMs"></param>
		/// <returns></returns>
		/// <exception cref="ArgumentNullException"></exception>
		public static IClientPipeAsync CreateRawDuplexChannel(string url, long connectTimeoutMs = 0)
		{
			if (url == null)
			{
				throw new ArgumentNullException(nameof(url));
			}

			// The code does little to formally process attributes, contract elements etc.
			var pipe = new ClientPipeAsync<IRawClientProtocol>(url);
            if (pipe.Connect(connectTimeoutMs))
            {
                return pipe;
            }

            pipe.Close();
            pipe.Dispose();
            throw new InvalidOperationException("Failed to connect to: " + url);
        }

		/// <summary>
		/// Creates the client side connection
		/// </summary>
		/// <typeparam name="T">Interface of the data exchange</typeparam>
		/// <param name="url">Pipe name</param>
		/// <param name="callbackProcessor">Actual callback processor</param>
		/// <param name="connectTimeoutMs">Optional connection timeout</param>
		/// <param name="securityLimitations">Controls the access</param>
		/// <returns>The stub interface</returns>
		/// <exception cref="ArgumentNullException"></exception>
		/// <exception cref="ArgumentException"></exception>
		public static T CreateDuplexChannelWithImplementor<T>(string url, object? callbackProcessor, long connectTimeoutMs = 0,
															  SecurityLimitations securityLimitations = SecurityLimitations.User) where T : class
		{
			if (string.IsNullOrEmpty(url))
			{
				throw new ArgumentNullException(nameof(url));
			}

			if (callbackProcessor == null)
			{
				// Log - no callback
			}

			// The code does little to formally process attributes, contract elements etc.
			var pipeConnection = new ClientPipeAsync<T>(url);

			// Create the owner
			var wcfDynamicObject = new DynamicObjectImplementor<T>(pipeConnection, connectTimeoutMs);

			// Create callback (optional)
			IServerManager? callbackServiceHost = null;
			var callbackPipeName = string.Empty;

			if (callbackProcessor != null && wcfDynamicObject.CallbackType != null)
			{
				callbackPipeName = buildCallbackPipeName(url);

				callbackServiceHost = createAndOpenServiceHostForCallback(callbackPipeName, callbackProcessor, securityLimitations);

				// Log
			}

			// Impersonate
			var converted = wcfDynamicObject.GetImplemented();
            if (converted == null)
            {
                throw new ApplicationException("'GetImplemented' failed");
            }

			m_channelMap[converted] = new ChannelLookup(converted, wcfDynamicObject, callbackServiceHost);

			if (callbackServiceHost != null)
			{
				// Inform the host about the callback channel activated
				new CallbackRegistrationSender(pipeConnection).Send(callbackPipeName);
			}

            // Hide the Wcf object inside a dedicated dynamic property of 'converted'
            DynamicPropertiesHelper.SetDynamicProperty(converted, DynamicPropertyName, wcfDynamicObject);

            return converted;
		}

		/// <summary>
		/// Closes the host connection
		/// </summary>
		/// <param name="host">Ref to the channel manager</param>
		/// <exception cref="ArgumentNullException"></exception>
		[MethodImpl(MethodImplOptions.Synchronized)]
		public static void Close(IServerManager? host)
		{
			if (host == null)
			{
				throw new ArgumentNullException(nameof(host));
			}

			if (m_contextMap.ContainsKey(host))
			{
				host.Dispose();
				m_contextMap.Remove(host, out _);
				// Log
			}
			else
			{
				// Log
			}
		}


		/// <summary>
		/// Closes the client connection
		/// </summary>
		/// <typeparam name="T">Interface of the data exchange</typeparam>
		/// <param name="channel">Client side channel</param>
		/// <exception cref="InvalidOperationException"></exception>
		[MethodImpl(MethodImplOptions.Synchronized)]
		public static void Close<T>(T channel)
		{
			if (channel == null)
            {
				throw new InvalidOperationException(nameof(channel));
            }

			if (m_channelMap.ContainsKey(channel))
            {
				var lookUp = m_channelMap[channel];
				var wcfDynamicObject = lookUp.DynamicObject;
				wcfDynamicObject?.Dispose();
    
				var callbackChannelManager = lookUp.CallbackChannelManager;
				if (callbackChannelManager != null) 
				{
					Close(callbackChannelManager);
				}

				m_channelMap.Remove(channel, out _);
				
				// Log
			}
			else
            {
				// Log
            }
		}

		/// <summary>
		/// Closes the raw pipe
		/// </summary>
		/// <param name="pipe"></param>
		[MethodImpl(MethodImplOptions.Synchronized)]
		public static void Close(IClientPipeAsync pipe)
		{
			pipe.Close();
			pipe.Dispose();
		}
		
		#region private
		public static IServerManager createAndOpenServiceHostForCallback<T>(string url, T hostProcessor, SecurityLimitations securityLimitations) where T : class
		{
			IDataExchangeProcessorAsync dataExchangeManager = new DataExchangeProcessorAsync();

			try
			{
				var pipeManager = new AsyncServerManager<T>(hostProcessor, url, new PipeSecurityProvider(securityLimitations), dataExchangeManager);
				m_contextMap[pipeManager] = hostProcessor;
				pipeManager.Start();

				// Log
				return pipeManager;
			}
			catch (Exception)
			{
				// Log
				throw;
			}
		}

		private static string buildCallbackPipeName(string name)
		{
			var pid = Environment.ProcessId;
			var tid = Environment.CurrentManagedThreadId;
			var callbackPipeName = string.Format("{0}_{1}.{2}", name, pid, tid);

			return callbackPipeName;
		}

		private class ChannelLookup
		{
			public object? Converted;
			public IDynamicObject? DynamicObject;
			public IServerManager? CallbackChannelManager;

			public ChannelLookup(object converted, IDynamicObject? wcfDynamicObject, IServerManager? callbackChannelManager)
			{
				Converted = converted;
				DynamicObject = wcfDynamicObject;
				CallbackChannelManager = callbackChannelManager;
			}
		}

		#endregion
	}
}
