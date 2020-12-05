using System;
using System.Net.Security;
using System.Net.Sockets;
using System.Security.Authentication;
using System.Security.Cryptography.X509Certificates;
using System.Text;

namespace RemoteTCPClient
{
    public static class SSLCertification
    {
        public static bool HandShake(Socket clientSocket, string serverName)
        {
            TcpClient client = new();
            client.Client = clientSocket;
            SslStream sslStream = new(client.GetStream(), false,
                new RemoteCertificateValidationCallback(SSLCertification.ValidateServerCertificate), null);

            try { sslStream.AuthenticateAsClient(serverName); }
            catch (AuthenticationException e)
            {
                Console.WriteLine($"Exception: {e.Message}");
                if (e.InnerException != null) Console.WriteLine($"Inner exception: {e.InnerException.Message}");
                Console.WriteLine("Authentication failed - closing the connection.");
                client.Close();

                return false;
            }

            byte[] messageToServer = Encoding.ASCII.GetBytes("New client initial contact with server.");
            sslStream.Write(messageToServer);
            sslStream.Flush();

            string messageFromServer = SSLCertification.ReadMessage(sslStream);
            Console.WriteLine($"Message from server: {messageFromServer}");
            client.Close();

            return true;
        }
        private static string ReadMessage(SslStream sslStream)
        {
            byte[] buffer = new byte[2048];
            StringBuilder messageData = new StringBuilder();
            int bytes = -1;
            do
            {
                bytes = sslStream.Read(buffer, 0, buffer.Length);

                Decoder decoder = Encoding.UTF8.GetDecoder();
                char[] chars = new char[decoder.GetCharCount(buffer, 0, bytes)];
                decoder.GetChars(buffer, 0, bytes, chars, 0);
                messageData.Append(chars);
                // Check for EOF.
                if (messageData.ToString().IndexOf("<EOF>") != -1) break;
            } while (bytes != 0);

            return messageData.ToString();
        }
        private static bool ValidateServerCertificate(
              object sender,
              X509Certificate certificate,
              X509Chain chain,
              SslPolicyErrors sslPolicyErrors)
        {
            if (sslPolicyErrors == SslPolicyErrors.None) return true;

            Console.WriteLine("Certificate error: {0}", sslPolicyErrors);                    
            return false; // Do not allow this client to communicate with unauthenticated servers.
        }
    }
}
