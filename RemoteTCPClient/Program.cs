using System;
using System.Net;
using System.Net.Sockets;
using System.Text;

namespace RemoteTCPClient
{
    class Program
    {
        private static Socket _clientSocket = new(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
        static void Main()
        {
            Console.Title = "OS CLIENT";
            LoopConnect();
            SendLoop();
            Console.ReadLine();
        }
        private static void SendLoop()
        {
            while (true)
            {
                Console.Write("Enter server request: ");
                byte[] sendBuffer = Encoding.ASCII.GetBytes(Console.ReadLine());
                _clientSocket.Send(sendBuffer);

                byte[] receivedBuffer = new byte[1024];
                int recieved = _clientSocket.Receive(receivedBuffer);
                byte[] data = new byte[recieved];
                Array.Copy(receivedBuffer, data, recieved);
                Console.WriteLine("Received: " + Encoding.ASCII.GetString(data));
            }
        }
        private static void LoopConnect()
        {
            IPAddress serverIP;
            int serverPort = 0;
            Console.Write("Enter Server External IP: ");
            
            while (true)
            {
                string inputIP = Console.ReadLine();
                if (IPAddress.TryParse(inputIP, out serverIP)) break;
                else Console.Write("\r Invalid input, try again: ");
            }

            Console.Write("Enter Server Port Number: ");
            while (true)
            {
                string inputPort = Console.ReadLine();
                if (Int32.TryParse(inputPort, out serverPort))
                {
                    if (serverPort < 0 || serverPort > 65535) Console.Write("\r Value out of legal port bound (0 to 65535), try again: ");
                    else break;
                }
                else Console.Write("\r Invalid input, try again: ");
            }
            int attempts = 0;
            while (!_clientSocket.Connected)
            {
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
            Console.WriteLine("\nConnected.");
        }
    }
}
