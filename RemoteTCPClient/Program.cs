using System;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Security.Cryptography.X509Certificates;
using System.Net.Security;
using System.Security.Authentication;
using System.Collections;
using System.Threading;
using System.Collections.Generic;

namespace RemoteTCPClient
{
    sealed class Client
    {
        private static Socket _clientSocket = new(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);

        private static Hashtable _certificateErrors = new();

        static void Main()
        {
            Console.ForegroundColor = ConsoleColor.Gray;
            Console.BackgroundColor = ConsoleColor.Black;
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
                    if (headers) Console.Write("$ ");
                    Console.ForegroundColor = ConsoleColor.Gray;
                    string msg;
                    while (true)
                    {
                        msg = Console.ReadLine();
                        if (!String.IsNullOrWhiteSpace(msg)) break;
                    }
                    SendMessage(msg + functionTag);

                    string strData = RecieveMessage();

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
                    if (headers) Console.Write(" >> ");
                    Console.ForegroundColor = ConsoleColor.White;
                    Console.BackgroundColor = ConsoleColor.DarkBlue;
                    if (functionTag == null) Console.WriteLine(strData);
                    else MultipleRequests(strData, functionTag); 
                    Console.ForegroundColor = ConsoleColor.Gray;
                    Console.BackgroundColor = ConsoleColor.Black;
                    headers = true;
                    functionTag = null;
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
            Console.ForegroundColor = ConsoleColor.Gray;
            Console.BackgroundColor = ConsoleColor.Black;
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
        private static void SendMessage(string message)
        {
            byte[] sendBuffer = Encoding.ASCII.GetBytes(message);
            _clientSocket.Send(sendBuffer);
        }
        private static string RecieveMessage()
        {
            byte[] receivedBuffer = new byte[2048];
            int recieved = _clientSocket.Receive(receivedBuffer);
            byte[] data = new byte[recieved];
            Array.Copy(receivedBuffer, data, recieved);
            return Encoding.ASCII.GetString(data);
        }
        private static void MultipleRequests(string data, string functionTag)
        {
            List<string> mutliLines = new();

            if (data.Contains("#/") && data.Contains("/#"))
            {
                List<int[]> hashPositions = new();
                bool lastHashFound = false;
                int strPos = 0;
                do
                {
                    if (data.Substring(strPos, data.Length - strPos).Contains("#/") && data.Substring(strPos, data.Length - strPos).Contains("/#"))
                    {
                        int[] hash = new int[2];
                        hash[0] = data.Substring(strPos, data.Length - strPos).IndexOf("#/") + strPos;
                        hash[1] = data.Substring(strPos, data.Length - strPos).IndexOf("/#") + 1 + strPos;

                        int lnLen = hash[0] - strPos;
                        if (lnLen < 0) lnLen = 0;
                        mutliLines.Add(data.Substring(strPos, lnLen));
                        mutliLines.Add(data.Substring(hash[0], (hash[1] - hash[0]) + 1));
                        strPos = hash[1] + 1;

                        hashPositions.Add(hash);
                    }
                    else lastHashFound = true;

                } while (!lastHashFound);

                string messageToServer = null;
                if (mutliLines.Count > 0)
                {
                    for (int i = 0; i < mutliLines.Count; i++)
                    {
                        if (mutliLines[i].StartsWith("#/"))
                        {
                            switch (mutliLines[i].Substring(2, mutliLines[i].Length - 4))
                            {
                                case "C.RL":
                                    string temp = Console.ReadLine();
                                    if (!String.IsNullOrWhiteSpace(messageToServer)) messageToServer += "|";
                                    messageToServer += temp;
                                    break;
                                case "C.NL":
                                    Console.WriteLine();
                                    break;
                            }
                        }
                        else Console.Write(mutliLines[i]);
                    }
                    if (messageToServer != null) SendMessage(functionTag + messageToServer);
                    Console.WriteLine(RecieveMessage());
                }
            }
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
