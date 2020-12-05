using System;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Security.Cryptography.X509Certificates;
using System.Net.Security;
using System.Security.Authentication;
using System.Collections;
using System.Threading;

namespace RemoteTCPClient
{
    sealed class Client
    {
        private static Socket _clientSocket = new(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);

        private static Hashtable _certificateErrors = new();

        static void Main()
        {    
            Console.Title = "OS CLIENT";
            LoopConnect();
            SendLoop();
            Console.ReadLine();
        }
        private static void SendLoop()
        {
            bool headers = true;
            string functionTag = null;

            Console.WriteLine("\n        CONNECTED! \n\n       WAIT.");
            Thread.Sleep(1000);
            GetServerInfo();
            try 
            { 
                while (true)
                {
                    Console.ForegroundColor = ConsoleColor.Green;
                    if (headers) Console.Write("Enter server request: ");
                    Console.ForegroundColor = ConsoleColor.Gray;
                    string msg;
                    while (true)
                    {
                        msg = Console.ReadLine();
                        if (!String.IsNullOrWhiteSpace(msg)) break;
                    }
                    byte[] sendBuffer = Encoding.ASCII.GetBytes(msg+functionTag);
                    _clientSocket.Send(sendBuffer);

                    byte[] receivedBuffer = new byte[2048];
                    int recieved = _clientSocket.Receive(receivedBuffer);
                    byte[] data = new byte[recieved];
                    Array.Copy(receivedBuffer, data, recieved);
                    string strData = Encoding.ASCII.GetString(data);
                    if (strData.Contains("<NoH>"))
                    {
                        headers = false;
                        strData = strData.Substring(5);
                    }
                    else headers = true;
                    if (strData.Contains("<<") && strData.Contains(">>"))
                    {
                        functionTag = strData.Substring(strData.IndexOf("<<"), (strData.IndexOf(">>")-strData.IndexOf("<<"))+2);
                        strData = strData.Substring(0,strData.IndexOf("<<"));
                    }
                    else functionTag = null;

                    Console.ForegroundColor = ConsoleColor.Yellow;
                    Console.BackgroundColor = ConsoleColor.DarkBlue;
                    if (headers) Console.Write("Received: ");
                    Console.ForegroundColor = ConsoleColor.White;
                    Console.BackgroundColor = ConsoleColor.DarkBlue;
                    Console.WriteLine(strData);
                    Console.ForegroundColor = ConsoleColor.Gray;
                    Console.BackgroundColor = ConsoleColor.Black;
                }
            }
            catch (SocketException ex)
            {
                Console.WriteLine(ex.Message);
            }
            catch (ObjectDisposedException ex)
            {
                Console.WriteLine(ex.Message);
            }
        }
        private static void LoopConnect()
        {
            IPAddress serverIP;
            int serverPort = 0;
            string serverName = null;
            bool ssl = false;
            Console.Write("Enter Server External IP: ");
            
            while (true)
            {
                string inputIP = Console.ReadLine();
                if (IPAddress.TryParse(inputIP, out serverIP)) break;
                else Console.Write("\rInvalid input, try again: ");
            }

            Console.Write("Enter Server Port Number: ");
            while (true)
            {
                string inputPort = Console.ReadLine();
                if (Int32.TryParse(inputPort, out serverPort))
                {
                    if (serverPort < 0 || serverPort > 65535) Console.Write("\rValue out of legal port bound (0 to 65535), try again: ");
                    else break;
                }
                else Console.Write("\rInvalid input, try again: ");
            }

            Console.Write("SSL required? [Y/N]: ");
            while (true)
            {
                ConsoleKey key = Console.ReadKey().Key;
                if (key == ConsoleKey.Y)
                {
                    ssl = true;
                    Console.Write("Enter Server name exactly: ");
                    while (true)
                    {
                        string inputName = Console.ReadLine();
                        if (!String.IsNullOrWhiteSpace(inputName))
                        {
                            serverName = inputName;
                            break;
                        }
                        else Console.Write("\rInvalid input, try again: ");
                    }
                    break;
                }
                else if (key == ConsoleKey.N) break;
                else Console.Write("\rInvalid input, try again [Y/N]: ");
            }

            int attempts = 0;
            while (!_clientSocket.Connected)
            {
                Console.WriteLine($"Attempting to connect");
                try
                {
                    attempts++;
                    //change from IPAddress.Loopback to a remote IP to connect remotely
                    _clientSocket.Connect(serverIP, serverPort);
                }
                catch (SocketException)
                {                    
                    Console.Write($"\rConnection attempts : {attempts.ToString()}");
                }
            }
            Console.WriteLine("\rConnected.");

            if (ssl) Console.WriteLine($"\nServer SSL authentication " +
                $"{(SSLCertification.HandShake(_clientSocket, serverName) ? "successful" : "failed")}.");
        }
        private static void GetServerInfo()
        {
            _clientSocket.Send(Encoding.ASCII.GetBytes("###`INITclientINFOrequest`###"));

            byte[] receivedBuffer = new byte[1024];
            int recieved = _clientSocket.Receive(receivedBuffer);
            byte[] data = new byte[recieved];
            Array.Copy(receivedBuffer, data, recieved);
            Console.Clear();
            Console.WriteLine("You are now connected to the server.\n");
            Console.ForegroundColor = ConsoleColor.Cyan;
            Console.WriteLine(Encoding.ASCII.GetString(data));
            Console.ForegroundColor = ConsoleColor.Gray;
        }
    }
}
