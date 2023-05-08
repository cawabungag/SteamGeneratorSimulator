using Opc.Ua;
using System.Security.Cryptography.X509Certificates;

namespace OPCClientInterface
{
    /// <summary>
    /// ��¼���ܽӿڿ�
    /// </summary>
    public class ClientLogIn
    {
        private OpcuaClient m_OpcUaClient = null;
        public ClientLogIn(OpcuaClient opcUaClient)
        {
            m_OpcUaClient = opcUaClient;
        }
        /// <summary>
        /// �ÿ͵�¼
        /// </summary>
        /// <returns></returns>
        public bool GuestLogin()
        {
            try
            {
                m_OpcUaClient.UserIdentity = new UserIdentity(new AnonymousIdentityToken());
                return true;
            }
            catch
            {
                return false;
            }
        }
        /// <summary>
        /// �û������¼
        /// </summary>
        /// <param name="UserName"></param>
        /// <param name="PassWord"></param>
        /// <returns></returns>
        public bool UserIdentityLogin(string UserName, string PassWord)
        {
            try
            {
                m_OpcUaClient.UserIdentity = new UserIdentity(UserName, PassWord);
                return true;
            }
            catch
            {
                return false;
            }
        }
        /// <summary>
        /// ֤���¼
        /// </summary>
        /// <param name="CertificateFileName"></param>
        /// <param name="PassWord"></param>
        /// <returns></returns>
        public bool CertificateLogin(string CertificateFileName, string PassWord)
        {
            try
            {
                X509Certificate2 certificate = new X509Certificate2(CertificateFileName, PassWord, X509KeyStorageFlags.MachineKeySet | X509KeyStorageFlags.Exportable);
                m_OpcUaClient.UserIdentity = new UserIdentity(certificate);
                return true;
            }
            catch
            {
                return false;
            }
        }
    }
}