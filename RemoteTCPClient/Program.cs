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
using System.IO;
using System.Threading.Tasks;

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
            Console.WriteLine("CLIENT INIT>>");
            while (true)
            {
                LoopConnect();
                SendLoop();
            }
        }
        public static bool pauseInputOutput = false;
        private static void SendLoop()
        {
            bool headers = true;
            bool multipleRequests = false;
            string functionTag = null;
            bool AcceptMessage = false;

            Console.WriteLine("\n        CONNECTED! \n\n       WAIT.");
            Thread.Sleep(1000);
            GetServerInfo();
            try 
            { 
                while (true)
                {
                    string strData = null;
                    while (strData == null)
                    {
                        CancellationTokenSource cts = new();
                        var task = Task.Run(() => {
                            try
                            {                                
                                cts.CancelAfter(5000);
                                if (!pauseInputOutput) UserInput(headers, functionTag);
                            }
                            catch (TaskCanceledException) { }
                        });
                        
                        strData = RecieveMessage();
                    }

                    //headers for client console to print    
                    if (strData.Contains("<NoH>"))
                    {
                        headers = false;
                        strData = strData.Substring(5);
                    }
                    else headers = true;

                    //server function tag when sent back
                    if (strData.Contains("<<") && strData.Contains(">>"))
                    {
                        int fTagPos = strData.IndexOf("<<");
                        functionTag = strData.Substring(fTagPos, (strData.IndexOf(">>")- fTagPos) +2);

                        if (functionTag.Contains(".MR"))
                        {
                            multipleRequests = true; 
                            functionTag = functionTag.Replace(".MR", "");
                        }

                        if (functionTag.Contains("client[") && functionTag.Contains("|request"))
                        {                            
                            AcceptMessage = AcceptClientMessage(functionTag, strData);
                        }

                        if (fTagPos != 0) strData = strData.Replace(functionTag, "");
                    }
                    else functionTag = null;

                    strData = CheckFunctionTag(strData);

                    //Request from server for action from this client

                    Console.ForegroundColor = ConsoleColor.Yellow;
                    Console.BackgroundColor = ConsoleColor.DarkBlue;
                    if (headers && !AcceptMessage) Console.Write("\n >> ");
                    Console.ForegroundColor = ConsoleColor.White;
                    Console.BackgroundColor = ConsoleColor.DarkBlue;


                    if (!strData.Contains("fileTransfer"))
                        if (multipleRequests) MultipleRequests(strData, functionTag);
                        else if (!AcceptMessage)
                        {
                            if(strData.Contains(" '"))
                            {
                                int msgStart = strData.IndexOf(" '");
                                int msgEnd = strData.LastIndexOf("'");
                                Console.Write(strData.Substring(0, msgStart - 1));
                                Console.ForegroundColor = ConsoleColor.Magenta;
                                Console.Write(strData.Substring(msgStart, (msgEnd - msgStart)+1));
                                Console.ForegroundColor = ConsoleColor.Gray;
                                Console.WriteLine(strData.Substring(msgEnd, strData.Length - msgEnd));
                            }
                            else Console.WriteLine(strData);
                        }
                    Console.ForegroundColor = ConsoleColor.Gray;
                    Console.BackgroundColor = ConsoleColor.Black;
                    headers = true;
                    functionTag = null;
                    multipleRequests = false;
                    AcceptMessage = false;
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
        private static void UserInput(bool headers,string functionTag)
        {
            Console.ForegroundColor = ConsoleColor.Green;
            Console.BackgroundColor = ConsoleColor.Black;
            Console.Write("                                                                                                                                                                                                              ");
            if (headers) Console.Write("\r$ ");
            Console.ForegroundColor = ConsoleColor.Gray;
            string msg;
            while (true)
            {
                Console.ForegroundColor = ConsoleColor.Gray;
                Console.BackgroundColor = ConsoleColor.Black;
                msg = Console.ReadLine();
                if (!String.IsNullOrWhiteSpace(msg)) break;
            }

            SendMessage(msg + functionTag);
        }
        private static void LoopConnect()
        {
            Thread.Sleep(1000);
            Console.Clear();

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
                else Console.Write("\rInvalid input, try again [y/n]: ");
            }

            int attempts = 0;
            while (!_clientSocket.Connected)
            {
                Console.WriteLine($"\nAttempting to connect");
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
            if (!pauseInputOutput)
            {
                if (message == "!sd") Disconnect();
                if (message.Contains(" sendfile ")) message = ContainsFileFromPath(message);
                if (message.Contains(" getfile "))
                {
                    string[] data = message.Split(' ');
                    if (data.Length < 4) { Console.WriteLine("ERROR: message not sent to server, infomation missing."); return; }
                    if (!Directory.Exists(data[3])) { Console.WriteLine("ERROR: Directory does not exist on this machine."); return; }
                }
                byte[] sendBuffer = Encoding.ASCII.GetBytes(message);
                _clientSocket.Send(sendBuffer);
                Console.ForegroundColor = ConsoleColor.Gray;
                Console.BackgroundColor = ConsoleColor.Black;
            }
        }
        private static string RecieveMessage()
        {
            byte[] receivedBuffer = new byte[4096];
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
                    pauseInputOutput = true;
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
                    pauseInputOutput = false;
                    if (messageToServer != null) SendMessage(functionTag + messageToServer);
                    Console.WriteLine(RecieveMessage());
                }
            }
        }
        private static string ContainsFileFromPath(string message)
        {
            int pos = -1;
            string[] parts = message.Split(' ');
            for (int i = 0; i < parts.Length; i++) if (File.Exists(parts[i]))
                {
                    pos = i;
                    break;
                }
            
            if (pos != -1)
            {
                string path = parts[pos];
                int lastSlash;
                if (path.Contains('/')) lastSlash = path.LastIndexOf('/');
                else lastSlash = path.LastIndexOf('\\');

                string fileName = path.Substring(lastSlash + 1, path.Length - (lastSlash + 1));
                byte[] fileBytes = File.ReadAllBytes(path);

                return message.Replace(path,$"{fileName} {Encoding.ASCII.GetString(fileBytes)}");
            }
            return message;
        }
        private static bool AcceptClientMessage(string functionTag, string message)
        {
            string ip = functionTag.Substring(functionTag.IndexOf('[') + 1, functionTag.Length - (functionTag.IndexOf(']') - 1));
            message = message.Replace(functionTag, "");
            message = CheckFunctionTag(message);

        Console.Write($"Client[{ip}] has sent you a message. Accept [y/n]? ");
            pauseInputOutput = true;
            while (true)
            {
                ConsoleKey key = Console.ReadKey().Key;
                if (key == ConsoleKey.Y)
                {
                    PrintAcceptedMessage(message);
                    pauseInputOutput = false;
                    return true;
                }
                else if (key == ConsoleKey.N)
                {
                    Console.WriteLine();
                    pauseInputOutput = false;
                    return false;
                }
                else Console.Write("\rInvalid input, try again [y/n]: ");                
            }
        }
        private static void PrintAcceptedMessage(string message)
        {
            Console.ForegroundColor = ConsoleColor.Yellow;
            Console.BackgroundColor = ConsoleColor.DarkBlue;
            Console.Write(" >> ");
            Console.ForegroundColor = ConsoleColor.White;
            Console.BackgroundColor = ConsoleColor.DarkBlue;
            Console.WriteLine(message);
            Console.ForegroundColor = ConsoleColor.Gray;
            Console.BackgroundColor = ConsoleColor.Black;
        }
        private static string CheckFunctionTag(string data)
        {
            string[] dataArr = data.Split(' ');
            if (dataArr[0].Contains("fileTransfer"))
            {
                if (dataArr.Length < 3) return "Server returned invalid info.";
                try
                {
                    File.WriteAllBytes(dataArr[1], Encoding.ASCII.GetBytes(dataArr[2]));
                    if (!File.Exists(dataArr[1])) return "File could be created - path syntax error";
                    return "File saved";
                }
                catch (Exception ex)
                {
                    Console.WriteLine(ex.Message);
                }
                return "ERROR: file not saved";
            }
            if (dataArr[0].Contains("].message"))
            {
                if (dataArr.Length < 2) return "Server returned invalid info.";
                string ip = dataArr[0].Substring(dataArr[0].IndexOf('[') + 1, dataArr[0].IndexOf(']') - (dataArr[0].IndexOf('[') + 1));

                string message = null;
                for (int i = 1; i < dataArr.Length; i++) message += $"{dataArr[i]} ";
                return $"{ip} sent: '{message}' <EOF>";
            }

            return data;
        }
        private static void GetServerInfo()
        {
            SendMessage($"###`CLIENTINFO`###  {Environment.MachineName} {GetLocalIPAddress()}");

            string reply = RecieveMessage();

            Console.Clear();
            Console.WriteLine("You are now connected to the server.\n");
            Console.ForegroundColor = ConsoleColor.Cyan;
            Console.WriteLine(reply);
            Console.ForegroundColor = ConsoleColor.Gray;
        }
        private static IPAddress GetLocalIPAddress()
        {
            foreach (var ip in Dns.GetHostEntry(Dns.GetHostName()).AddressList)
                if (ip.AddressFamily == AddressFamily.InterNetwork) return ip;
            throw new Exception("No network adapters with an IPv4 address in the system!");
        }
        private static void Disconnect()
        {
            _clientSocket.Dispose(); //fix reconnect dispose error
            _clientSocket.Disconnect(false);
        }
    }
}
