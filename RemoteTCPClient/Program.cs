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
            int attempts =0;
            while (!_clientSocket.Connected)
            {
                try
                {
                    attempts++;
                    _clientSocket.Connect(IPAddress.Loopback, 9999);
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
