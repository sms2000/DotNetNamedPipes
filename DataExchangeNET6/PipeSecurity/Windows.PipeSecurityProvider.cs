using System.IO.Pipes;
using System.Security.AccessControl;
using System.Security.Principal;

namespace PipeSecurityHelper
{
    public class PipeSecurityProvider
    {
        private readonly PipeSecurity? m_pipeSecurity;
        private readonly bool m_currentUserOnly = true;

        public int InBufferSize { get { return 4096; } }
        public int OutBufferSize { get { return 4096; } }

        /// <summary>
        /// Builds the default User level accessible pipe provider
        /// </summary>
        public PipeSecurityProvider() : this(SecurityLimitations.User)
        {
        }

        /// <summary>
        /// Builds the pipe provider with the specified level of access
        /// </summary>
        /// <param name="limitations"></param>
        public PipeSecurityProvider(SecurityLimitations limitations)
        {
            if (OperatingSystem.IsWindows())
            {
                var pipeSecurity = new PipeSecurity();
                SecurityIdentifier? sid;

                switch (limitations)
                {
                    case SecurityLimitations.Everyone:
                        sid = new SecurityIdentifier(WellKnownSidType.WorldSid, null);
                        m_currentUserOnly = false;
                        break;

                    case SecurityLimitations.User:
                    default:
                        sid = new SecurityIdentifier(WellKnownSidType.AuthenticatedUserSid, null);
                        break;

#if FULL_WINDOWS_SECURITY
                    case SecurityLimitations.Admin:
                        sid = new SecurityIdentifier(WellKnownSidType.BuiltinAdministratorsSid, null);
                        break;

                    case SecurityLimitations.Service:
                        sid = new SecurityIdentifier(WellKnownSidType.ServiceSid, null);
                        break;

                    case SecurityLimitations.System:
                        sid = new SecurityIdentifier(WellKnownSidType.WinSystemLabelSid, null);
                        break;
#endif
                }

                pipeSecurity.SetAccessRule(new PipeAccessRule(sid.Translate(typeof(NTAccount)), PipeAccessRights.ReadWrite | PipeAccessRights.CreateNewInstance, AccessControlType.Allow));
                m_pipeSecurity = pipeSecurity;
            }
            else
            {
                switch (limitations)
                {
                    case SecurityLimitations.Everyone:
                        m_currentUserOnly = false;
                        break;

                    default:
                        break;
                }

                m_pipeSecurity = null;
            }
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="constructedPipeName"></param>
        /// <param name="instances"></param>
        /// <returns></returns>
        public NamedPipeServerStream CreateSecureServerStreamAsync(string constructedPipeName, int instances)
        {
            if (OperatingSystem.IsWindows())
            {
                var serverPipe = NamedPipeServerStreamAcl.Create(constructedPipeName, PipeDirection.InOut, instances, PipeTransmissionMode.Byte,
                                    PipeOptions.Asynchronous | PipeOptions.WriteThrough, InBufferSize, OutBufferSize, m_pipeSecurity);

                // Log
                return serverPipe;
            }
            else
            {
                PipeOptions currentUserOnly = m_currentUserOnly ? PipeOptions.CurrentUserOnly : 0;

                var serverPipe = new NamedPipeServerStream(constructedPipeName, PipeDirection.InOut, instances, PipeTransmissionMode.Byte,
                                    PipeOptions.Asynchronous | PipeOptions.WriteThrough | currentUserOnly, InBufferSize, OutBufferSize);

                // Log
                return serverPipe;
            }
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="constructedPipeName"></param>
        /// <param name="serverName"></param>
        /// <returns></returns>
        public static NamedPipeClientStream CreateSecureClientStream(string pipeName, string serverName = @".")
        {
            return new NamedPipeClientStream(serverName, pipeName, PipeDirection.InOut);
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="constructedPipeName"></param>
        /// <param name="serverName"></param>
        /// <returns></returns>
        public static NamedPipeClientStream CreateSecureClientStreamAsync(string pipeName, string serverName = @".")
        {
            return new NamedPipeClientStream(serverName, pipeName, PipeDirection.InOut, PipeOptions.WriteThrough | PipeOptions.Asynchronous);
        }

        public enum SecurityLimitations 
        {
#if FULL_WINDOWS_SECURITY
            System,
            Service,
            Admin,
#endif
            User,
            Everyone
        }
    }
}
