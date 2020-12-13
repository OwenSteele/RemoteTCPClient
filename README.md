# <ins>Remote TCP Client</ins>
# See also *ReadMe_RemoteTCPClient.ipynb*

##### By Owen Steele 2020

*This repo works alongside the other repo ./RemoteTCPServer.git*
**The /RemoteTCPServer.git repo is set to private, please contact me (OwenSteele) to gain access privileges**

#### <ins>Functionality</ins>
This Client runs on local networks only, multiple clients can be connected at once.

**Requires a local server to be set and running before connection attempt.**

Connects via the local IP and port of the server.

SSL encryption is optional. If the server is SSL enabled, it is still optional for the client to connect with encryption.
If the client connects with SSL encryption, the server name (server certificate key) must also be entered.

### Setup and Running
##### .NET 5.0 runtime is required to execute.
Can be downloaded from MDN here: https://dotnet.microsoft.com/download
Select .NET [core/framework] runtime option and install

<ins>Simple setup through GitHub</ins>
```
1. Download this repo as a '.zip' file, and extract once downloaded.
2. Navigate to /RemoteTCPServer/bin/Debug/net5.
3. Run RemoteTCPClient.exe
```

**Can also be built and run with Visual Studio**

Ensure that your machine has the .NET 5.0 SDK installed

<ins>Simple setup through terminal (windows)</ins>
```
1. cd [your chosen dir]
2. mkdir RemoteTCPClient
3. cd RemoteTCPClient 
4. git init
5. git clone https://github.com/OwenSteele/RemoteTCPClient.git
6. cd /RemoteTCPServer/bin/Debug/net5.0/
7. ./RemoteTCPClient.exe
```

*NOTE: step 5 requires the necessary access privileges*

## Starting a client
The terminal/console will ask you for initial input:
```
* Enter the server local IP: (255.255.255.255)
* Enter the server port number: (0 to 65535)
* Choose to enable SSL encryption:
    If you enable SSL, you will be prompted to provide the server name (and certificate):
```
    
**Clients require the localIP and the port number to connect.**
**Clients are not required to enable SSL to connect - however if enabled, clients also require the server name (key) set.**

## Structure
```
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
```
The client uses asynchronous code to simultaneously cycle through waiting for user input and server messages

## Client - Interface
**Connect:** after entering the server metadata and information, the client will attempt to connect

**Commands:** Once connected, the client can enter commands to be sent to the server, shown by entering:
```
$ help
```
**Login:** Clients can login to predefined (on server) users, default login 'user', password':
```
$ login
User: user
Password: password
```

**Users:** Client can log in to users as above, this data is solely held by the server.

**Commands** The '$ help' commands will show all commands and their syntax. If a command is called incorrectly, the server will return it's 'help' information.
             
Some commands can only be accessed when logged in, or even only by the admin.           

**Disconnect:** to disconnect, close the app window or type:
```
$ !sd
```

<ins> **User commands**</ins>

*After a successful login attempt, clients can access more powerful functions.*

**File Transfer:** Users can send and retrieve files from the server 
                   (requires admin to set the dir path first **!BE CAREFUL THIS SET A PATH TO SAVE FILES ON THE SERVER MACHINE, NOT THE CLIENT MACHINE!**).

**Messaging:** Users can directly message one another, using the 'clientIP:clientHandle' as the identifier:
```
$ userName message list //shows all the available clients
$ owen message 192.168.0.1:1000 message starts after the IP:Handle
```
The client (user: owen) would request to send a message to 192.168.0.1:1000.
the message would be 'message starts after the IP:Handle'.

clients can only message when logged in, and only request to message other client whom are also logged in, an not private.

**Security:** Users can set their visibility and security to three states, 'private', 'prompt on request' and 'auto accept'.
              Private users cannot receive or send anything to other clients, and cannot be seen by other clients.
              Prompted users, must accept a request from another client before receiving data, does not apply to sending data.
              Auto accepting users, automatically accept any data sent to them.

