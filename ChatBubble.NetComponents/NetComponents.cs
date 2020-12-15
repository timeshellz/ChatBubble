using System;
using System.Text;
using System.Net;
using System.Net.Sockets;
using System.Threading;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Runtime;
using System.Globalization;

namespace ChatBubble
{
    /// <summary>
    /// Class containing every method responsible for ChatBubble Client - Server communication and all associated processes
    /// </summary>
    public static class NetComponents
    {
        public static Encoding us_US = Encoding.Unicode;

        static Socket mainSocket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
        static Socket auxilarryUDPSocket = new Socket(AddressFamily.InterNetwork, SocketType.Dgram, ProtocolType.Udp);

        static IPAddress serverAddress;
        static IPEndPoint serverIPEndPoint;                     //TO DO: Make the 3 variables on the left be read from server config file at startup

        public static int socketAddress = 8000;
        public static string ipAddress;

        //Server State Variables
        static bool serverListeningState = false;
        public static int[] handshakeSuccessCount = new int[4]; //Max connection attempts per time interval

        public static DateTime serverSessionStartTime;
        public static DateTime serverListeningStartTime;
        public static TimeSpan[] updateTimeSpans = new TimeSpan[3] { new TimeSpan(0, 10, 0), new TimeSpan(1, 0, 0), new TimeSpan(24, 0, 0) };
        public static int[] connectionAttemptsCount = new int[4]; //Max connected count per time interval //Current connection attempts per time interval


        public static ConcurrentDictionary<int, string> connectedClientsBlockingCollection = new ConcurrentDictionary<int, string>();
        public static ConcurrentDictionary<int, string> loggedInUsersBlockingCollection = new ConcurrentDictionary<int, string>();

        public static ConcurrentQueue<string> receivedMessagesCollection = new ConcurrentQueue<string>();

        /// <summary>
        /// Delegate dictionary of Connection Flag - Net Method pairs.
        /// <p>Stores all possible ChatBubble protocol methods according to their flag.</p>
        /// </summary>
        public static Dictionary<string, Delegate> RequestDictionary = new Dictionary<string, Delegate>()
        {
            [ConnectionCodes.LogInRequest] = new Func<string, EndPoint, string>(ServerLogInService),
            [ConnectionCodes.SignUpRequest] = new Func<string, string>(ServerSignUpService),
            [ConnectionCodes.SearchRequest] = new Func<string, string>(ServerSearchService),
            [ConnectionCodes.AddFriendRequest] = new Func<string, string>(ServerAddFriendService),
            [ConnectionCodes.GetFriendListRequest] = new Func<string, string>(ServerGetFriendsService),
            [ConnectionCodes.RemoveFriendRequest] = new Func<string, string>(ServerRemoveFriendService),
            [ConnectionCodes.GetUserSummaryRequest] = new Func<string, string>(ServerGetUserSummaryService),
            [ConnectionCodes.EditUserSummaryRequest] = new Func<string, string>(ServerEditUserSummaryService),
            [ConnectionCodes.SendNewMessageRequest] = new Func<string, string>(ServerPassMessageService),
            [ConnectionCodes.GetPendingMessageRequest] = new Func<string, string>(ServerGetPendingMessagesService),
            [ConnectionCodes.ChangePasswdRequest] = new Func<string, string>(ServerChangePasswordService),
            [ConnectionCodes.ChangeNameRequest] = new Func<string, string>(ServerChangeNameService),
            [ConnectionCodes.LogOutCall] = new Action<string>(ServerConnectionCloseDictionaryUpdater),
        };

        /// <summary>
        /// Class that represents and generates Connection Flags based on all existing error, success and status conditions, defined as fields.
        /// </summary>
        public static class ConnectionCodes
        {
            public static readonly string LogInRequest, SignUpRequest, SearchRequest, AddFriendRequest, GetFriendListRequest, RemoveFriendRequest,
            GetUserSummaryRequest, EditUserSummaryRequest, GetPendingMessageRequest, SendNewMessageRequest, ChangeNameRequest, ChangePasswdRequest, 
            FreshSessionStatus, ExpiredSessionStatus, MsgToSelfStatus, AvailablePendingMessagesStatus, NoPendingMessagesStatus, ConnectionTimeoutStatus, 
            LoginSuccess, SignUpSuccess, FriendAddSuccess, FriendRemSuccess, DescEditSuccess, MsgSendSuccess, PswdChgSuccess, NmChgSuccess, LoginFailure, 
            SignUpFailure, FriendAddFailure, SendFailure, AuthFailure, ConnectionFailure, PswdChgFailure, NmChgFailure, NotFoundError, DatabaseError, 
            RestrictedError, LogOutCall, ConnectionSignature, InvalidSignature, InvalidRequest;

            public static int DefaultFlagLength { get; private set; }

            static ConnectionCodes()
            {
                DefaultFlagLength = 8;

                System.Reflection.FieldInfo[] fields = typeof(ConnectionCodes).GetFields();

                for (int i = 0; i < fields.Length; i++)
                {
                    switch (fields[i].Name)
                    {
                        case "LogOutCall":
                            fields[i].SetValue(new object(), "[LOGOUT]");
                            break;
                        case "ConnectionSignature":
                            fields[i].SetValue(new object(), "[CBLSIG]");
                            break;
                        case "InvalidSignature":
                            fields[i].SetValue(new object(), "[INVSIG]");
                            break;
                        case "InvalidRequest":
                            fields[i].SetValue(new object(), "[INVREQ]");
                            break;
                        default:
                            string flagLabel = "A";

                            if (fields[i].Name.Contains("Request"))
                            {
                                flagLabel = "R";
                            }
                            if(fields[i].Name.Contains("Error"))
                            {
                                flagLabel = "E";
                            }
                            if(fields[i].Name.Contains("Status"))
                            {
                                flagLabel = "S";
                            }
                            if(fields[i].Name.Contains("Failure"))
                            {
                                flagLabel = "F";
                            }
                            if(fields[i].Name.Contains("Success"))
                            {
                                flagLabel = "C";
                            }

                            int code = i + 1;
                            string finalFlag = code.ToString() + flagLabel;
                            int initialLength = finalFlag.Length;

                            for (int j = initialLength; j < DefaultFlagLength - 2; j++)
                            {
                                finalFlag = "0" + finalFlag;
                            }

                            finalFlag = "[" + finalFlag + "]";

                            fields[i].SetValue(new object(), finalFlag);
                            break;
                    }
                }
            }

            /// <summary>
            /// Gets all currently available Connection Codes for ChatBubble protocol.
            /// </summary>
            /// <returns></returns>
            public static string GetAllErrorCodes()
            {
                string output = "Currently used error codes:\n";
                System.Reflection.FieldInfo[] fields = typeof(ConnectionCodes).GetFields();
                object obj = new object();

                for(int i = 0; i < fields.Length; i++)
                {
                    output += fields[i].Name + " - " + fields[i].GetValue(obj).ToString() + "\n";
                }

                return output;
            }

            public static bool Exists(string connectionFlag)
            {
                System.Reflection.FieldInfo[] fields = typeof(ConnectionCodes).GetFields();
                object obj = new object();
                for (int i = 0; i < fields.Length; i++) if ((string)fields[i].GetValue(obj) == connectionFlag) return true;

                return false;
            }

            /// <summary>
            /// Checks whether request signature matches valid ConnectionCodes default signature.
            /// </summary>
            /// <param name="request"></param>
            /// <returns>True if signature matches, otherwise false.</returns>
            public static bool IsSignatureValid(string request)
            {
                if (request.Length < ConnectionCodes.DefaultFlagLength)
                {
                    return false;
                }

                string signature = request.Substring(0, ConnectionCodes.DefaultFlagLength);

                if (signature == ConnectionCodes.ConnectionSignature) return true;
                return false;
            }
        }

        /// <summary>
        /// Scans for the local machine address.
        /// </summary>
        /// <returns>Local machine IP address string</returns>
        public static string ScanIP()
        {
            string localMachineName = Dns.GetHostName();

            IPHostEntry localMachineIP = Dns.GetHostByName(localMachineName);
            IPAddress[] ipAddressMass = localMachineIP.AddressList;

            ipAddress = ipAddressMass[ipAddressMass.Length - 1].ToString();

            return (ipAddress);
        }

        /// <summary>
        /// Clientside method. <para />
        /// Sets caller's remote EndPoints to the determined server address.
        /// </summary>
        /// <param name="serverAddressString">Server address.</param>
        /// <param name="serverSocketAddressInt">Server main socket address.</param>
        public static void ClientSetServerEndpoints(string serverAddressString, int serverSocketAddressInt)
        {
            try
            {
                serverAddress = IPAddress.Parse(serverAddressString);
            }
            catch
            {
                serverAddress = Dns.GetHostAddresses(serverAddressString)[0];
            }

            serverIPEndPoint = new IPEndPoint(serverAddress, serverSocketAddressInt);
        }

        /// <summary>
        /// Serverside method. <para />
        /// Checks if server's local endpoint is null. If the endpoint is null, then socket is not bound and vice versa.
        /// </summary>
        /// <returns>Empty string if not null. Error message if null.</returns>
        public static string ServerSocketBoundCheck()
        {
            //State Availability Check
            if (serverIPEndPoint == null)
            {
                return ("Server not bound. Listening not possible.\n");
            }
            return ("");
        }
        /// <summary>
        /// Checks if the main socket is currently connected.
        /// </summary>
        /// <returns></returns>
        public static string SocketConnectedCheck()
        {
            if (mainSocket.Connected == false)
            {
                return ("Not connected");
            }
            return ("");
        }

        /// <summary>
        /// Records all concurrent connections for statistics keeping.
        /// </summary>
        static void StatRecordConnection()
        {
            for (int i = 0; i < connectionAttemptsCount.Length; i++)
            {
                connectionAttemptsCount[i]++;
            }
        }

        /// <summary>
        /// Records all current handshake attempts for statistics keeping.
        /// </summary>
        static void StatRecordHandshake()
        {
            for (int i = 0; i < handshakeSuccessCount.Length; i++)
            {
                handshakeSuccessCount[i]++;
            }
        }

        /// <summary>
        /// Serverside method.<para/>
        /// Updates server statistics max values per given intervals, and for entire runtime.<para/>
        /// 
        /// </summary>
        static public void ServerStatUpdater()
        {
            //Regulating update times for every possible update interval
            //Updates maximum number of connections reached per interval

            int updateCount = 0;

            while (serverListeningState)
            {
                Thread.Sleep((int)updateTimeSpans[0].TotalMilliseconds);
                updateCount++;

                //Update every [i] interval
                for (int i = 0; i < updateTimeSpans.Length; i++)
                {
                    if (updateCount * updateTimeSpans[0].Minutes % updateTimeSpans[i].TotalMinutes == 0)
                    {
                        connectionAttemptsCount[i] = 0;
                        handshakeSuccessCount[i] = 0;
                    }
                }

                if(updateCount * updateTimeSpans[0].Minutes % updateTimeSpans[1].TotalMinutes == 0)
                {
                    FileIOStreamer.LogWriter("Regular server statistics update:\n----------------------------------------------------------------------\n"
                        + GetServerSessionStats() + "----------------------------------------------------------------------");
                }
            }
        }

        /// <summary>
        /// Gets current server session statistics.
        /// </summary>
        /// <returns>String containing all avaiable server statistics with timestamps.</returns>
        public static string GetServerSessionStats()
        {
            string outputFormat;          
            string[] outputTypeModifiers = new string[2] { "Connections attempted ", "Successful handshakes performed " };
            string outputTimeNameModifier;

            string output = "Server statistics:\n\n";

            for (int j = 0; j <= 1; j++)
            {
                outputFormat = "";
                outputTimeNameModifier = "";

                for (int i = 0; i < updateTimeSpans.Length; i++)
                {
                    if (updateTimeSpans[i].Minutes > 0)
                    {
                        if (updateTimeSpans[i].Minutes / 10 > 0) outputFormat = "mm";
                        else outputFormat = "%m";
                        outputTimeNameModifier = " minutes(s): ";
                    }
                    if (updateTimeSpans[i].Hours > 0)
                    {
                        if (updateTimeSpans[i].Hours / 10 > 0) outputFormat = "hh";
                        else outputFormat = "%h";
                        outputTimeNameModifier = " hour(s): ";
                    }
                    if (updateTimeSpans[i].Days > 0)
                    {
                        if (updateTimeSpans[i].Days / 10 > 0) outputFormat = "dd";
                        else outputFormat = "%d";
                        outputTimeNameModifier = " day(s): ";
                    }

                    output += outputTypeModifiers[j] + "in " + updateTimeSpans[i].ToString(outputFormat) + outputTimeNameModifier;

                    if (j == 0) output += connectionAttemptsCount[i] + "\n";
                    else output += handshakeSuccessCount[i] + "\n";
                }

                if (j == 0) output += outputTypeModifiers[j] + "since start of session: "
                    + connectionAttemptsCount[connectionAttemptsCount.Length - 1] + "\n";
                else output += outputTypeModifiers[j] + "since start of session: "
                        + handshakeSuccessCount[handshakeSuccessCount.Length - 1] + "\n";
            }

            output += "\nTime since session start: " + (DateTime.Now - serverSessionStartTime).TotalHours.ToString("hh") + 
                (DateTime.Now - serverSessionStartTime).ToString(@"\:mm\:ss") + "\n";

            if(serverListeningStartTime != DateTime.MinValue)
            {
                output += "Time since listening start: " + (DateTime.Now - serverListeningStartTime).TotalHours.ToString("hh") + (DateTime.Now - serverListeningStartTime).ToString(@"\:mm\:ss") + "\n";
            }

            return output;
        }

        /// <summary>
        /// Serverside method. <para />
        /// Puts the server into listen - accept loop. On accept, creates new thread to handle pending client. Exits from method when
        /// server is no longer listening.
        /// </summary>

        public static void ServerListeningState()
        {
            serverListeningState = true;
            serverListeningStartTime = DateTime.Now;

            while (serverListeningState == true)
            {
                try
                {
                    mainSocket.Listen(1000);

                    Socket pendingClientSocket = mainSocket.Accept();

                    FileIOStreamer.LogWriter("New connection with " + pendingClientSocket.RemoteEndPoint.ToString() + " established.");

                    Thread handshakeReceiveReplyThread = new Thread(ServerHandshakeReception);
                    handshakeReceiveReplyThread.Start(pendingClientSocket);

                    StatRecordConnection();
                }
                catch
                {
                    FileIOStreamer.LogWriter("Attempted connection failure occured.");
                }
            }

            serverListeningStartTime = DateTime.MinValue;
        }

        /// <summary>
        /// Serverside method. <para />
        /// Accepts incoming client handshake. Receives and handles incoming cookie token. On successful handshake reception, 
        /// passes the client to request handler.
        /// </summary>
        /// <param name="clientSocket">Pending client socket.</param>
        static void ServerHandshakeReception(object clientSocket)
        {
            string clientHandshakeToken;
            byte[] streamBytes = new byte[64];

            Socket pendingClientSocket = (Socket)clientSocket;

            try
            {
                pendingClientSocket.Receive(streamBytes);
            }
            catch
            {
                FileIOStreamer.LogWriter("Connection with " + pendingClientSocket.RemoteEndPoint.ToString() + " ended abruptly.");
                pendingClientSocket.Close();

                return;
            }

            clientHandshakeToken = us_US.GetString(streamBytes);

            if (clientHandshakeToken.Contains("\0"))
            {
                clientHandshakeToken = clientHandshakeToken.Substring(0, clientHandshakeToken.IndexOf('\0'));
            }

            if (!ConnectionCodes.IsSignatureValid(clientHandshakeToken))
            {
                FileIOStreamer.LogWriter("Invalid connection signature detected. Disconnecting " + pendingClientSocket.RemoteEndPoint.ToString() + ".");

                streamBytes = us_US.GetBytes(ConnectionCodes.InvalidRequest);
                pendingClientSocket.Send(streamBytes);

                pendingClientSocket.Close();
                return;
            }

            string remoteEndPointLogString = pendingClientSocket.RemoteEndPoint.ToString();
            FileIOStreamer.LogWriter("Received " + clientHandshakeToken + " token from " + remoteEndPointLogString);

            clientHandshakeToken = clientHandshakeToken.Substring(ConnectionCodes.DefaultFlagLength);
            string[] clientHandshakeTokenSubstrings = clientHandshakeToken.Split(new string[2] { "id=", "confirmation=" }, 2, StringSplitOptions.RemoveEmptyEntries);

            //Code below compares the stored user persistence cookie signature to the one sent by the user
            if (clientHandshakeToken != ConnectionCodes.FreshSessionStatus && String.IsNullOrEmpty(clientHandshakeToken) != true &&
                                                                                  IsCookieInDatabase(clientHandshakeTokenSubstrings[0], clientHandshakeTokenSubstrings[1]) == true)
            {
                //If signature is correct, gets user data from database to pass into the login service
                
                string[] userData = GetUserData(clientHandshakeTokenSubstrings[0]);

                if (userData.Length > 1 && userData[0] != ConnectionCodes.NotFoundError)
                {
                    string cookieCredentials = "login=" + userData[1] + "password=" + userData[3];

                    FileIOStreamer.LogWriter("Cookie received from " + remoteEndPointLogString);
                    FileIOStreamer.LogWriter("Handling fresh session handshake for " + remoteEndPointLogString);

                    clientHandshakeToken = ServerLogInService(cookieCredentials, pendingClientSocket.RemoteEndPoint);
                }
                else
                {
                    clientHandshakeToken = ConnectionCodes.ExpiredSessionStatus;

                    FileIOStreamer.LogWriter("Handling expired session handshake for " + remoteEndPointLogString);
                }
            }
            else
            {
                clientHandshakeToken = ConnectionCodes.ExpiredSessionStatus;

                FileIOStreamer.LogWriter("Handling expired session handshake for " + remoteEndPointLogString);
            }
            
            try
            {
                streamBytes = us_US.GetBytes(clientHandshakeToken);
                pendingClientSocket.Send(streamBytes);              
            }   
            catch
            {
                FileIOStreamer.LogWriter("Handshake failed for " + remoteEndPointLogString);
                return;
            }

            FileIOStreamer.LogWriter("Handshake established with " + remoteEndPointLogString);
            StatRecordHandshake();

            connectedClientsBlockingCollection.TryAdd(connectedClientsBlockingCollection.Count + 1, "ip=" + pendingClientSocket.RemoteEndPoint.ToString());
            ServersideRequestReceiver(pendingClientSocket);
        }

        /// <summary>
        /// Serverside method.
        /// Compares client's cookie with live sessions database to confirm identity.
        /// </summary>
        /// <param name="id">User ID</param>
        /// <param name="confirmation">User cookie</param>
        /// <returns>True if session matches server database, otherwise false.</returns>
        static bool IsCookieInDatabase(string id, string confirmation)
        {
            //For sessionContentSplitstrings, index 0 is hash, index 1 is ip

            string[] activeUserSessions;
            activeUserSessions = FileIOStreamer.GetDirectoryFiles(FileIOStreamer.defaultActiveUsersDirectory, false, false);

            if (Array.Exists(activeUserSessions, pendingUser => pendingUser == "id=" + id))
            {
                string sessionHash = FileIOStreamer.ReadFromFile(FileIOStreamer.defaultActiveUsersDirectory + "id=" + id +
                                                      ".txt");

                if (sessionHash == confirmation)
                {
                    return (true);
                }
            }
            return (false);
        }

        /// <summary>
        /// Serverside method.<para />
        /// Gets data for specified user ID or username from database.<para />
        /// Returns string array, where [0] = User ID, [1] = Login, [2] = Name, [3] = Password.<para/>
        /// If fastSearch = true, gets every entry for specific user. Otherwise, only returns user ID, login and name.<para/>
        /// Returns "User_not_found" if no user with specified parameter exists.
        /// </summary>
        /// <param name="searchParameter">Search parameter. Can be either user ID or username.</param>
        /// <returns></returns>
        static string[] GetUserData(string searchParameter, bool fastSearch = false)
        {
            string defaultUsersDirectory = FileIOStreamer.defaultRegisteredUsersDirectory;

            string[] registeredUserFilesSplitStrings = new string[15]
            {
                "login=",                       //[1]
                "name=",                        //[2]
                "password=",                    //[3]
                "email=",                       //[4]
                "picture=",                     //[5]
                "summary==", "==summary",       //[6]
                "bubscore=",                    //[7]
                "friends==", "==friends",       //[8]
                "pfriends==","==pfriends",      //[9]
                "blacklist==", "==blacklist",   //[10]
                "regdate=",                     //[11]
            };

            string[] registeredUserFiles = FileIOStreamer.GetDirectoryFiles(defaultUsersDirectory, false, false);
            try
            {
                foreach (string registeredUserFile in registeredUserFiles)
                {
                    //Gets the substrings from registered user file names
                    string[] registeredIDUsername = registeredUserFile.Split(registeredUserFilesSplitStrings, StringSplitOptions.RemoveEmptyEntries);

                    if (registeredIDUsername[0] == searchParameter || registeredIDUsername[1] == searchParameter)
                    {

                        //If registered user found, get information from their file
                        //If fastSearch true, gets full information about user
                        string fileEntries;
                        if (fastSearch == true)
                        {
                            fileEntries = FileIOStreamer.ReadFromFile(defaultUsersDirectory + registeredUserFile + ".txt", "", "password=");
                        }
                        else
                        {
                            fileEntries = FileIOStreamer.ReadFromFile(defaultUsersDirectory + registeredUserFile + ".txt");
                        }

                        string[] fileEntriesSubstrings
                            = fileEntries.Split(registeredUserFilesSplitStrings, StringSplitOptions.RemoveEmptyEntries);

                        //For registeredIDUsername substrings, array index 0 is for ID, array index 1 is for login
                        //For fileEntriesSubstrings, array index 0 is for name, index 1 is for password

                        //This forms the userData array.
                        List<string> userData = new List<string>(registeredIDUsername);

                        foreach (string fileEntry in fileEntriesSubstrings)
                        {
                            userData.Add(fileEntry);
                        }

                        return (userData.ToArray());
                    }
                }
            }
            catch
            {
                return (new string[1] { ConnectionCodes.DatabaseError });
            }
            return (new string[1] { ConnectionCodes.NotFoundError });
        }

        /// <summary>
        /// Gets the highest user ID found in database.
        /// </summary>
        /// <returns></returns>
        static int GetMaxUserID()
        {
            string defaultUsersDirectory = FileIOStreamer.defaultRegisteredUsersDirectory;

            string[] registeredUserFilesSplitStrings = new string[3] { "name=", "login=", "password=" };
            string[] registeredUserFiles = FileIOStreamer.GetDirectoryFiles(defaultUsersDirectory, false, false);

            int maxUserID = 0;

            try
            {
                foreach (string registeredUserFile in registeredUserFiles)
                {
                    string[] registeredIDUsername = registeredUserFile.Split(registeredUserFilesSplitStrings, StringSplitOptions.RemoveEmptyEntries);

                    if (Convert.ToInt32(registeredIDUsername[0]) >= maxUserID)
                    {
                        maxUserID = Convert.ToInt32(registeredIDUsername[0]);
                    }

                }
            }
            catch
            {
                return (0);
            }
            return (maxUserID);
        }

        /// <summary>
        /// Search sorting parameter. Sorts alphabetically by name, non case-sensitive.<para/>
        /// Takes string of form "id=[id]login=[login]name=[name]
        /// </summary>
        /// <param name="userDataString1"></param>
        /// <param name="userDataString2"></param>
        /// <returns></returns>
        static int SearchCompareByName(string userDataString1, string userDataString2)
        {
            string[] userDataSplitStrings = new string[3] { "id=", "login=", "name=" };
            string[] userData1Substrings = userDataString1.Split(userDataSplitStrings, 3, StringSplitOptions.RemoveEmptyEntries);
            string[] userData2Substrings = userDataString2.Split(userDataSplitStrings, 3, StringSplitOptions.RemoveEmptyEntries);

            return (userData1Substrings[2].CompareTo(userData2Substrings[2]));
        }

        /// <summary>
        /// Serverside method. <para />
        /// Receives incoming client requests and dispatches them to their respective destinations.
        /// </summary>
        /// <param name="pendingClientSocket">Pending client socket.</param>
        static void ServersideRequestReceiver(Socket pendingClientSocket)
        {
            while (pendingClientSocket.Connected == true)
            {
                byte[] streamBytes = new byte[1024];

                if (pendingClientSocket.Poll(-1, SelectMode.SelectRead) == true) //Ensures that data is available to be read from the socket
                {
                    try
                    {
                        if (pendingClientSocket.Available > 0)
                        {
                            pendingClientSocket.Receive(streamBytes);
                        }
                        else
                        {
                            //If poll returned true, but no data is available, that means that the client disconnected abruptly
                            //Hence it's necessary to abort the thread
                            throw new SocketException();
                        }
                    }
                    catch
                    {
                        string remoteEndPoint = pendingClientSocket.RemoteEndPoint.ToString();
                        ServerConnectionCloseDictionaryUpdater(remoteEndPoint);

                        pendingClientSocket.Close();

                        FileIOStreamer.LogWriter("Connection with " + remoteEndPoint + " ended abruptly.");
                        return;
                    }

                    string clientRequestRaw = us_US.GetString(streamBytes);

                    if (clientRequestRaw.Contains("\0"))
                    {
                        clientRequestRaw = clientRequestRaw.Substring(0, clientRequestRaw.IndexOf('\0'));
                    }

                    FileIOStreamer.LogWriter("Remote request " + clientRequestRaw + " received from " + pendingClientSocket.RemoteEndPoint.ToString() + ".");                 

                    if (clientRequestRaw.Length >= ConnectionCodes.DefaultFlagLength*2)
                    {
                        if(!ConnectionCodes.IsSignatureValid(clientRequestRaw))
                        {
                            FileIOStreamer.LogWriter("Invalid connection signature detected. Disconnecting " + pendingClientSocket.RemoteEndPoint.ToString() + ".");
                            streamBytes = us_US.GetBytes(ConnectionCodes.InvalidRequest);

                            pendingClientSocket.Send(streamBytes);
                            pendingClientSocket.Close();
                            return;
                        }

                        clientRequestRaw = clientRequestRaw.Substring(ConnectionCodes.DefaultFlagLength);
                        string requestType = clientRequestRaw.Substring(0, ConnectionCodes.DefaultFlagLength);
                        string requestBody = clientRequestRaw.Substring(ConnectionCodes.DefaultFlagLength);
                        string serverReply = "";

                        if (RequestDictionary.ContainsKey(requestType))
                        {
                            if (requestType == ConnectionCodes.LogInRequest)
                            {
                                serverReply = (string)RequestDictionary[requestType].DynamicInvoke(requestBody, pendingClientSocket.RemoteEndPoint);
                            }
                            else if(requestType != ConnectionCodes.LogOutCall)
                            {
                                serverReply = (string)RequestDictionary[requestType].DynamicInvoke(requestBody);
                            }    
                            else
                            {
                                RequestDictionary[requestType].DynamicInvoke(pendingClientSocket.RemoteEndPoint.ToString());
                            }
                        }
                        else
                        {
                            serverReply = ConnectionCodes.InvalidRequest;
                        }

                        streamBytes = us_US.GetBytes(serverReply);
                        pendingClientSocket.Send(streamBytes);                
                    }
                }
            }
        }

        /// <summary>
        /// Serverside method. <para />
        /// Handles received "Log In" requests, generates persistence hash, calls for ClientSessionHandler() method. <para/>
        /// On successfull login, records user IP and ID into blocking collection.
        /// </summary>
        /// <param name="clientRequest">Received client request.<para/>Follows the following format:<para/>
        /// login=[login]password=[password]</param>
        /// <returns></returns>
        static string ServerLogInService(string clientRequest, EndPoint clientIP)
        {
            string[] clientRequestSubstrings;
            string[] clientRequestSplitStrings = new string[3] { "name=", "login=", "password=" };
            string[] userData;

            clientRequestSubstrings = clientRequest.Split(clientRequestSplitStrings, StringSplitOptions.RemoveEmptyEntries);

            try
            {
                userData = GetUserData(clientRequestSubstrings[0]);
            }
            catch
            {
                FileIOStreamer.LogWriter("User id=" + clientRequestSubstrings[0] + " login attempt failed. Database error.");
                return (ConnectionCodes.DatabaseError);
            }

            if (userData[0] != ConnectionCodes.NotFoundError)
            {
                //For userData, index 0 is user ID, index 1 is user login, index 2 is user name, index 3 is user password, index 4 is user ip
                //For clientRequestSubstrings, index 0 is login, index 1 is password

                if (clientRequestSubstrings[1] == userData[3])
                {
                    Random randomGenerator = new Random();
                    int randomHashSeed = randomGenerator.Next(99999999);

                    ClientSessionHandler(userData[0], randomHashSeed);

                    loggedInUsersBlockingCollection.TryAdd(loggedInUsersBlockingCollection.Count + 1, "id=" + userData[0] + "ip=" + clientIP.ToString());

                    FileIOStreamer.LogWriter("User id=" + userData[0] + " successful login detected.");
                    return (ConnectionCodes.LoginSuccess + "id=" + userData[0] + "hash=" + randomHashSeed.ToString());
                    //Passes user ID and persistence cookie key back for session update purposes
                }
            }

            FileIOStreamer.LogWriter("User id=" + userData[0] +" log in attempt failed. Incorrect credentials.");
            return (ConnectionCodes.LoginFailure);
        }


        /// <summary>
        /// Serverside method. <para />
        /// Handles received "Sign Up" requests. Adds entries to server database accordingly.
        /// </summary>
        /// <param name="clientRequest">Received client request.</param>
        /// <returns></returns>
        static string ServerSignUpService(string clientRequest)
        {
            DateTime dateTime = DateTime.Now;

            string[] clientRequestSubstrings;
            string[] clientRequestSplitStrings = new string[3] { "name=", "login=", "password=" };
            string defaultUsersDirectory = FileIOStreamer.defaultRegisteredUsersDirectory; //TEMPORARY
            string[] userData;
            //FOR SAFETY AND AGILITY REASONS, MAKE IT SO THAT DEFAULT USERS LOCATION WOULD BE READ FROM SETTINGS FILE IN THE FUTURE

            clientRequestSubstrings = clientRequest.Split(clientRequestSplitStrings, 3, StringSplitOptions.RemoveEmptyEntries);

            try
            {
                userData = GetUserData(clientRequestSubstrings[1]);
            }
            catch
            {
                FileIOStreamer.LogWriter("New user sign up attempt failed. Database error.");
                return (ConnectionCodes.DatabaseError);
            }

            if (userData[0] == ConnectionCodes.NotFoundError)
            {
                int maxID = GetMaxUserID();
                FileIOStreamer.WriteToFile(defaultUsersDirectory + (maxID + 1).ToString() + "login=" + clientRequestSubstrings[1]
                + ".txt",
                "name=" + clientRequestSubstrings[0] +
                "\npassword=" + clientRequestSubstrings[2] +
                "\nemail=null" +
                "\npicture=null" +
                "\nsummary==" +
                "\nstatus=null" +
                "\nmain=null" +
                "\n==summary" +
                "\nbubscore=0" +
                "\nfriends==null\n==friends" +
                "\npfriends==null\n==pfriends" +
                "\nblacklist==null\n==blacklist" +
                "\nregdateutc=" + dateTime.ToUniversalTime().ToString(CultureInfo.InvariantCulture), true);

                FileIOStreamer.LogWriter("New user id=" + maxID + "signed up.");
                return (ConnectionCodes.SignUpSuccess);
            }
            else if (userData[0] != "Error")
            {
                FileIOStreamer.LogWriter("New user sign up attempt failed. Name already exists.");
                return (ConnectionCodes.SignUpFailure); //Returns if a user with this name already exists
            }

            FileIOStreamer.LogWriter("New user sign up attempt failed. Database error.");
            return (ConnectionCodes.DatabaseError); //Returns if the database couldn't be parsed
        }

        /// <summary>
        /// Serverside method.<para />
        /// Returns main profile information for the user specified in the request.
        /// </summary>
        /// <param name="clientRequest">Received client request.</param>
        /// <returns></returns>
        static string ServerGetUserSummaryService(string clientRequest)
        {
            string[] clientRequestSplitStrings = new string[3] { "id=", "confirmation=", "reqid=" };

            string[] clientRequestSubstrings = clientRequest.Split(clientRequestSplitStrings, StringSplitOptions.RemoveEmptyEntries);
            //For clientRequestSubstrings, client [0] = client ID, [1] = cookie confirmation [2] = ID of requested user

            //Ensures user authenticity
            if (IsCookieInDatabase(clientRequestSubstrings[0], clientRequestSubstrings[1]) != true)
            {
                return (ConnectionCodes.AuthFailure);
            }

            if (clientRequestSubstrings[2] == "self")
            {
                //If no ID given, return requesting user data
                clientRequestSubstrings[2] = clientRequestSubstrings[0];
            }

            string[] userData = GetUserData(clientRequestSubstrings[2]);

            if (userData[0] != ConnectionCodes.NotFoundError && userData[0] != ConnectionCodes.DatabaseError && userData[6].Length > 0)
            {
                string[] summarySubstrings = userData[6].Split(new string[] { "status=", "main=" }, StringSplitOptions.RemoveEmptyEntries);

                return ("id=" + userData[0] + "login=" + userData[1] + "name=" + userData[2] +
                    "status=" + summarySubstrings[0] + "main=" + summarySubstrings[1] + "bubscore=" + userData[7]);
            }
            else
            {
                return (ConnectionCodes.DatabaseError);
            }
        }

        /// <summary>
        /// Serverside method.<para />
        /// Updates main profile information for the specified user. Replaces all occurences of "=" with "[eql_sgn]" to prevent database errors.
        /// </summary>
        /// <param name="clientRequest"></param>
        /// <returns></returns>
        static string ServerEditUserSummaryService(string clientRequest)
        {
            string[] clientRequestSplitStrings = new string[3] { "id=", "confirmation=", "newsummary=" };
            string defaultUsersDirectory = FileIOStreamer.defaultRegisteredUsersDirectory; //TEMPORARY

            string[] clientRequestSubstrings = clientRequest.Split(clientRequestSplitStrings, StringSplitOptions.RemoveEmptyEntries);
            //For clientRequestSubstrings, client [0] = client ID, [1] = cookie confirmation [2] = new summary

            //Ensures user authenticity
            if (IsCookieInDatabase(clientRequestSubstrings[0], clientRequestSubstrings[1]) != true)
            {
                return (ConnectionCodes.AuthFailure);
            }

            //Reform newsummary= and replace '=' with '[eqlsgn]' to prevent injection attacks
            //This step can't be done using string.split since split may remove separator injected by user
            clientRequestSubstrings[2] = clientRequestSubstrings[2].Replace("\n", "");

            string clientNewSummaryStatusSubstring = clientRequestSubstrings[2].Substring(clientRequestSubstrings[2].IndexOf("status=") + 7);
            clientNewSummaryStatusSubstring = clientNewSummaryStatusSubstring.Substring(0, clientNewSummaryStatusSubstring.IndexOf("main=")).Replace("=", "[eqlsgn]");
            string clientNewSummaryMainSubstring = clientRequestSubstrings[2].Substring(clientRequestSubstrings[2].IndexOf("main=") + 5).Replace("=", "[eqlsgn]");

            clientRequestSubstrings[2] = "\nstatus=" + clientNewSummaryStatusSubstring + "\nmain=" + clientNewSummaryMainSubstring + "\n";

            string[] userData = GetUserData(clientRequestSubstrings[0]);

            if (userData[0] != ConnectionCodes.NotFoundError && userData[0] != ConnectionCodes.DatabaseError && userData[6].Length > 0)
            {
                FileIOStreamer.RemoveFileEntry(defaultUsersDirectory + userData[0] + "login=" + userData[1] + ".txt", "summary==", userData[6], true, true);

                if (clientRequestSubstrings[2].Substring(clientRequestSubstrings[2].IndexOf("status=") + 7, clientRequestSubstrings[2].IndexOf("main=") - (clientRequestSubstrings[2].IndexOf("status=") + 7)) == "\n")
                {
                    clientRequestSubstrings[2] = clientRequestSubstrings[2].Insert(clientRequestSubstrings[2].IndexOf("status=") + 7, "null");
                }

                if (clientRequestSubstrings[2].Substring(clientRequestSubstrings[2].IndexOf("main=") + 5, clientRequestSubstrings[2].Length - (clientRequestSubstrings[2].IndexOf("main=") + 5)) == "\n")
                {
                    clientRequestSubstrings[2] = clientRequestSubstrings[2].Insert(clientRequestSubstrings[2].IndexOf("main=") + 5, "null");
                }

                FileIOStreamer.WriteToFile(defaultUsersDirectory + userData[0] + "login=" + userData[1] + ".txt", clientRequestSubstrings[2], false, "summary==");
                return (ConnectionCodes.DescEditSuccess);
            }
            else
            {
                return (ConnectionCodes.DatabaseError);
            }
        }

        /// <summary>
        /// Serverside method. <para />
        /// Organizes database search and returns results based on the search parameter.
        /// </summary>
        /// <param name="searchParameter"></param>
        /// <returns></returns>
        static string ServerSearchService(string searchParameter)
        {
            if (searchParameter == "")
            {
                return ("=no_match=");
            }

            string defaultUsersDirectory = FileIOStreamer.defaultRegisteredUsersDirectory; //TEMPORARY

            List<string> registeredUsersList = new List<string>(FileIOStreamer.GetDirectoryFiles(defaultUsersDirectory, false, false));
            List<string[]> registeredUsersData = new List<string[]>();
            List<string> matchingUsersData = new List<string>();

            foreach (string registeredUser in registeredUsersList)
            {
                string[] registeredUserSubstrings = registeredUser.Split(new string[] { "login=" }, StringSplitOptions.RemoveEmptyEntries);

                //Gets all infromation about every found user, using ID as searchParameter

                registeredUsersData.Add(GetUserData(registeredUserSubstrings[0], true));
            }

            foreach (string[] userData in registeredUsersData)
            {
                for (int i = 1; i <= 2; i++)
                {
                    //Compares the searchQuery to every entry found for every user. Data of any closest matching users is added to list
                    //Results depend on queryLength, which is used to widen or shorten the range of possible outcomes.
                    //Could be made simpler using String.Equals and sorting, but makes no particular difference atm
                    if (userData[i].Length >= searchParameter.Length && userData[i].Substring(0, searchParameter.Length).Equals(searchParameter, StringComparison.OrdinalIgnoreCase))
                    {
                        matchingUsersData.Add("id=" + userData[0] + "login=" + userData[1] + "name=" + userData[2]);
                        break;
                    }
                }
            }

            matchingUsersData.Sort(SearchCompareByName);

            string matchingUsersDataString = String.Join("user=", matchingUsersData);

            if (matchingUsersDataString == "")
            {
                matchingUsersDataString = "=no_match=";
            }

            return (matchingUsersDataString);
        }

        /// <summary>
        /// Serverside method <para/>
        /// Updates user friendlist for the specified user.
        /// </summary>
        /// <param name="clientRequest"></param>
        /// <returns></returns>
        static string ServerAddFriendService(string clientRequest)
        {
            string[] clientRequestSplitStrings = new string[3] { "id=", "confirmation=", "addid=" };
            string defaultUsersDirectory = FileIOStreamer.defaultRegisteredUsersDirectory; //TEMPORARY

            string[] clientRequestSubstrings = clientRequest.Split(clientRequestSplitStrings, StringSplitOptions.RemoveEmptyEntries);
            //For clientRequestSubstrings, client [0] = client ID, [1] = cookie confirmation [2] = prospective friend ID

            //Ensures user authenticity
            if (IsCookieInDatabase(clientRequestSubstrings[0], clientRequestSubstrings[1]) != true)
            {
                return (ConnectionCodes.AuthFailure);
            }

            string[] userData = GetUserData(clientRequestSubstrings[0]);

            if (userData[0] != ConnectionCodes.NotFoundError && userData[0] != ConnectionCodes.DatabaseError)
            {
                //Check if user already has this friend (probably going to be made redundant in the future)
                string[] fIDSubstrings = userData[8].Split(new string[] { "fid=" }, StringSplitOptions.RemoveEmptyEntries);

                foreach (string fid in fIDSubstrings)
                {
                    if (fid == clientRequestSubstrings[2] + "=")
                    {
                        return (ConnectionCodes.FriendAddFailure);
                    }
                }

                FileIOStreamer.WriteToFile(defaultUsersDirectory + userData[0] + "login=" + userData[1] + ".txt", "\nfid=" + clientRequestSubstrings[2] + "=", true, "friends==");
                return (ConnectionCodes.FriendAddSuccess);
            }
            else
            {
                return (ConnectionCodes.DatabaseError);
            }
        }

        /// <summary>
        /// Serverside method.<para/>
        /// Returns the list of all friends as a string in the friendlist of a specified user.
        /// </summary>
        /// <param name="clientRequest"></param>
        /// <returns></returns>
        static string ServerGetFriendsService(string clientRequest)
        {
            string[] clientRequestSplitStrings = new string[2] { "id=", "confirmation=" };
            string[] clientRequestSubstrings = clientRequest.Split(clientRequestSplitStrings, StringSplitOptions.RemoveEmptyEntries);
            //For clientRequestSubstrings, client [0] = client ID, [1] = cookie confirmation

            //Ensures user authenticity
            if (IsCookieInDatabase(clientRequestSubstrings[0], clientRequestSubstrings[1]) != true)
            {
                return (ConnectionCodes.AuthFailure);
            }

            string[] userData = GetUserData(clientRequestSubstrings[0]);

            if (userData.Length > 11 && userData[0] != ConnectionCodes.NotFoundError && userData[0] != ConnectionCodes.DatabaseError && userData[8].Contains("null") == false)
            {
                string[] friendsListArray;

                userData[8] = userData[8].Replace("=", "");
                friendsListArray = userData[8].Split(new string[] { "fid" }, StringSplitOptions.RemoveEmptyEntries);

                List<string> friendUserDataList = new List<string>();

                foreach (string friend in friendsListArray)
                {
                    string[] friendUserData = (GetUserData(friend, true));
                    //[0] = id, [1] = username, [2] = name

                    friendUserDataList.Add("id=" + friendUserData[0] + "login=" + friendUserData[1] + "name=" + friendUserData[2]);
                }

                friendUserDataList.Sort(SearchCompareByName);

                string friendsUserDataString = String.Join("user=", friendUserDataList);

                return (friendsUserDataString);
            }
            else
            {
                return (ConnectionCodes.DatabaseError);
            }
        }

        /// <summary>
        /// Serverside method.<para/>
        /// Removes specified friend id from the specified user's friendlist.
        /// </summary>
        /// <param name="clientRequest"></param>
        /// <returns></returns>
        static string ServerRemoveFriendService(string clientRequest)
        {
            string[] clientRequestSplitStrings = new string[3] { "id=", "confirmation=", "fid=" };
            string defaultUsersDirectory = FileIOStreamer.defaultRegisteredUsersDirectory;

            string[] clientRequestSubstrings = clientRequest.Split(clientRequestSplitStrings, StringSplitOptions.RemoveEmptyEntries);
            //For clientRequestSubstrings, client [0] = client ID, [1] = cookie confirmation, [2] fid=id

            //Ensures user authenticity
            if (IsCookieInDatabase(clientRequestSubstrings[0], clientRequestSubstrings[1]) != true)
            {
                return (ConnectionCodes.AuthFailure);
            }

            string[] userData = GetUserData(clientRequestSubstrings[0]);

            if (userData[0] != ConnectionCodes.NotFoundError && userData[0] != ConnectionCodes.DatabaseError && clientRequestSubstrings.Length > 2 && clientRequestSubstrings[2] != "")
            {
                FileIOStreamer.RemoveFileEntry(defaultUsersDirectory + userData[0] + "login=" + userData[1] + ".txt", "friends==", "\nfid=" + clientRequestSubstrings[2] + "=");
                return (ConnectionCodes.FriendRemSuccess);
            }
            else
            {
                return (ConnectionCodes.DatabaseError);
            }
        }

        /// <summary>
        /// Serverside method.<para />
        /// Returns a list of all pending chat messages addressed to the requesting user as a string.
        /// </summary>
        /// <param name="clientRequest"></param>
        /// <returns></returns>
        static string ServerGetPendingMessagesService(string clientRequest)
        {
            string[] clientRequestSplitStrings = new string[2] { "id=", "confirmation=" };
            string defaultPendingMessagesDirectory = FileIOStreamer.defaultPendingMessagesDirectory; //TEMPORARY
            string[] messageHandleSplitstrings = new string[3] { "msgid=", "sender=", "rcpnt=" };

            string[] clientRequestSubstrings = clientRequest.Split(clientRequestSplitStrings, StringSplitOptions.RemoveEmptyEntries);
            //For clientRequestSubstrings, client [0] = client ID, [1] = cookie confirmation

            //Ensures user authenticity
            if (IsCookieInDatabase(clientRequestSubstrings[0], clientRequestSubstrings[1]) != true)
            {
                return (ConnectionCodes.AuthFailure);
            }

            string[] allPendingMessages = FileIOStreamer.GetDirectoryFiles(defaultPendingMessagesDirectory, false, false);
            string serverReplyString = "";

            foreach (string pendingMessage in allPendingMessages)
            {
                string[] pendingMessageSubstrings = pendingMessage.Split(messageHandleSplitstrings, StringSplitOptions.RemoveEmptyEntries);
                //For message handle splits, [0] - server msgid, [1] = sender id, [2] = recipient id

                if (pendingMessageSubstrings[2] == clientRequestSubstrings[0])
                {
                    string messageContent = FileIOStreamer.ReadFromFile(defaultPendingMessagesDirectory + pendingMessage + ".txt");
                    string[] messageContentSubstrings = messageContent.Split(new string[] { "time=", "content=" }, StringSplitOptions.RemoveEmptyEntries);
                    //[0] - message time, [1] - message content

                    serverReplyString += "msg=" + "sender=" + pendingMessageSubstrings[1] + "time=" + messageContentSubstrings[0] +
                        "message=" + messageContentSubstrings[1];

                    FileIOStreamer.RemoveFile(defaultPendingMessagesDirectory + pendingMessage + ".txt");
                }
            }

            if (serverReplyString == "")
            {
                return (ConnectionCodes.NoPendingMessagesStatus);
            }
            else
            {
                return (serverReplyString);
            }
        }

        /// <summary>
        /// Receives message sender, recepient and content, formulates it into a message and loades it into pending messages database.
        /// </summary>
        /// <param name="sender">Message sender</param>
        /// <param name="recepient">Message receipient</param>
        /// <param name="content">Message content</param>
        static void ServerMakeMessagePending(string sender, string recepient, string content)
        {
            string defaultPendingMessagesDirectory = FileIOStreamer.defaultPendingMessagesDirectory;
            DateTime currentServerTime = DateTime.Now.ToUniversalTime();

            int chatID = FileIOStreamer.GetDirectoryFiles(defaultPendingMessagesDirectory, false, false).Length + 1;

            FileIOStreamer.WriteToFile(defaultPendingMessagesDirectory + "chatid=" + chatID + "sender=" + sender + "rcpnt=" + recepient + ".txt",
                "time=" + currentServerTime.ToString("dddd, dd MMMM yyyy HH: mm:ss") + "\ncontent=" + content);
        }

        /// <summary>
        /// Passes message from sender to server and attempts to notify the receipient of a new message.
        /// </summary>
        /// <param name="clientRequest"></param>
        /// <returns></returns>
        static string ServerPassMessageService(string clientRequest)
        {
            string[] clientRequestSplitStrings = new string[] { "id=", "confirmation=", "rcpnt=", "content=" };

            string[] clientRequestSubstrings = clientRequest.Split(clientRequestSplitStrings, StringSplitOptions.RemoveEmptyEntries);
            //For clientRequestSubstrings, client [0] = client ID, [1] = cookie confirmation, [2] = recepient id, [3] = message content

            //Ensures user authenticity
            if (IsCookieInDatabase(clientRequestSubstrings[0], clientRequestSubstrings[1]) != true)
            {
                return (ConnectionCodes.AuthFailure);
            }

            //Check if user texted himself to prevent server action
            {
                if(clientRequestSubstrings[2] == clientRequestSubstrings[0])
                {
                    return (ConnectionCodes.MsgToSelfStatus);
                }
            }

            //Record message into pending messages
            ServerMakeMessagePending(clientRequestSubstrings[0], clientRequestSubstrings[2], clientRequestSubstrings[3]);

            //Finds user in logged in list of logged in users to attempt sending pending message call
            foreach (KeyValuePair<int, string> userRecord in loggedInUsersBlockingCollection)
            {
                if (userRecord.Value.Substring(0, userRecord.Value.IndexOf("ip=")) == "id=" + clientRequestSubstrings[2])
                {
                    byte[] streamBytes;
                    string[] ipPortSubstrings = userRecord.Value.Substring(userRecord.Value.IndexOf("ip=") + 3).Split(new char[] { ':' }, StringSplitOptions.RemoveEmptyEntries);

                    string messagePullRequest = ConnectionCodes.AvailablePendingMessagesStatus;

                    streamBytes = us_US.GetBytes(messagePullRequest);

                    IPAddress recepientIPAddress = IPAddress.Parse(ipPortSubstrings[0]);
                    IPEndPoint recepientEndPoint = new IPEndPoint(recepientIPAddress, Convert.ToInt32(ipPortSubstrings[1]));

                    auxilarryUDPSocket.SendTo(streamBytes, recepientEndPoint);
                }
            }

            return (ConnectionCodes.MsgSendSuccess);
        }

        /// <summary>
        /// Manages client password change requests.
        /// </summary>
        /// <param name="clientRequest"></param>
        /// <returns></returns>
        public static string ServerChangePasswordService(string clientRequest)
        {          
            string[] clientRequestSplitStrings = new string[] { "id=", "confirmation=", "oldpass=", "newpass=" };
            string defaultUsersDirectory = FileIOStreamer.defaultRegisteredUsersDirectory;

            string[] clientRequestSubstrings = clientRequest.Split(clientRequestSplitStrings, StringSplitOptions.RemoveEmptyEntries);
            //For clientRequestSubstrings, client [0] = client ID, [1] = cookie confirmation, [2] = old password, [3] = new password

            //Ensures user authenticity
            if (IsCookieInDatabase(clientRequestSubstrings[0], clientRequestSubstrings[1]) != true)
            {
                return (ConnectionCodes.AuthFailure);
            }

            string[] userData = GetUserData(clientRequestSubstrings[0]);

            if (userData[0] != ConnectionCodes.NotFoundError && userData[0] != ConnectionCodes.DatabaseError)
            {
                if (userData[3] == clientRequestSubstrings[2])
                {
                    FileIOStreamer.SwapFileEntry(defaultUsersDirectory + userData[0] + "login=" + userData[1] + ".txt", "password=", "", clientRequestSubstrings[3], false, true);

                    return (ConnectionCodes.PswdChgSuccess);
                }
                else
                {
                    return (ConnectionCodes.PswdChgFailure);
                }
            }
            else
            {
                return (ConnectionCodes.DatabaseError);
            }
        }

        /// <summary>
        /// Manages client name change requests.
        /// </summary>
        /// <param name="clientRequest"></param>
        /// <returns></returns>
        public static string ServerChangeNameService(string clientRequest)
        {
            string[] clientRequestSplitStrings = new string[] { "id=", "confirmation=", "newname=" };
            string defaultUsersDirectory = FileIOStreamer.defaultRegisteredUsersDirectory;

            string[] clientRequestSubstrings = clientRequest.Split(clientRequestSplitStrings, StringSplitOptions.RemoveEmptyEntries);
            //For clientRequestSubstrings, client [0] = client ID, [1] = cookie confirmation, [2] = new name

            //Ensures user authenticity
            if (IsCookieInDatabase(clientRequestSubstrings[0], clientRequestSubstrings[1]) != true)
            {
                return (ConnectionCodes.AuthFailure);
            }

            string[] userData = GetUserData(clientRequestSubstrings[0]);

            if (userData[0] != ConnectionCodes.NotFoundError && userData[0] != ConnectionCodes.DatabaseError)
            {
                FileIOStreamer.SwapFileEntry(defaultUsersDirectory + userData[0] + "login=" + userData[1] + ".txt", "name=", "", clientRequestSubstrings[2], false, true);

                return (ConnectionCodes.NmChgSuccess);
            }
            else
            {
                return (ConnectionCodes.NmChgFailure);
            }
        }

        /// <summary>
        /// Serverside method. <para />
        /// [work in progress] Generates hash from randomly created seed for the specified user ID. Generates new live session file for
        /// pending user connection.
        /// </summary>
        /// <param name="userID">Pending log in user ID.</param>
        /// <param name="hashSeed">Hash generator seed.</param>
        static void ClientSessionHandler(string userID, int hashSeed)
        {
            //PERSISTENCE COOKIE HASH GENERATOR WILL BE HERE
            string hashString = hashSeed.ToString();

            //PERSONAL HASH WOULD BE WRITTEN IN THE USER SESSION FILE BELOW
            FileIOStreamer.ClearFile(FileIOStreamer.defaultActiveUsersDirectory + "id=" + userID + ".txt");
            FileIOStreamer.WriteToFile(FileIOStreamer.defaultActiveUsersDirectory + "id=" + userID + ".txt", hashString, true);

            //THIS METHOD WOULD RETURN HASH IN THE FUTURE
        }

        /// <summary>
        /// Serverside method.<para />
        /// Times out dead session files and deletes them from database. <para />
        /// Entries for every client that called itself out during the past timeOutSpan seconds stay intact/get refreshed.<para />
        /// Client sessions that haven't called themselves out (appeared in the blocking collection) are removed from database.
        /// </summary>
        public static void SessionTimeOutCheck()
        {
            //Sets live session file timeout span here V
            TimeSpan timeOutSpan = new TimeSpan(1, 0, 0);

            while (serverListeningState == true)
            {
                string[] activeUsersArray = new string[loggedInUsersBlockingCollection.Count];
                loggedInUsersBlockingCollection.Values.CopyTo(activeUsersArray, 0);

                FileIOStreamer.FileTimeOutComparator(FileIOStreamer.defaultActiveUsersDirectory, activeUsersArray, timeOutSpan);

                Thread.Sleep(1000);
            }
        }

        /// <summary>
        /// Updates currently connected and logged in users collection.
        /// </summary>
        /// <param name="userIP"></param>
        static void ServerConnectionCloseDictionaryUpdater(string userIP)
        {
            foreach (var connectedClient in connectedClientsBlockingCollection)
            {
                if (connectedClient.Value.Contains("ip=" + userIP))
                {
                    connectedClientsBlockingCollection.TryRemove(connectedClient.Key, out userIP);
                }
            }

            foreach (var loggedInUser in loggedInUsersBlockingCollection)
            {
                if (loggedInUser.Value.Contains(userIP))
                {
                    loggedInUsersBlockingCollection.TryRemove(loggedInUser.Key, out userIP);
                }
            }
        }

        /// <summary>
        /// Clientside method. Initiates first handshake with server, checking if the client has an active cookie.<para />
        /// </summary>
        /// <returns></returns>
        public static string InitialHandshakeClient()
        {
            string handshakeReplyString;
            string localCookieContents = FileIOStreamer.ReadFromFile(FileIOStreamer.defaultLocalCookiesDirectory + "persistenceCookie.txt");

            if (String.IsNullOrEmpty(localCookieContents) == true)
            {
                localCookieContents = ConnectionCodes.FreshSessionStatus;
            }

            localCookieContents = ConnectionCodes.ConnectionSignature + localCookieContents;

            byte[] handshakeBytes = us_US.GetBytes(localCookieContents);

            try
            {
                mainSocket.Connect(serverIPEndPoint);
                mainSocket.Send(handshakeBytes);

                mainSocket.ReceiveTimeout = 5000;
                handshakeBytes = new byte[64];

                mainSocket.Receive(handshakeBytes);

                handshakeReplyString = us_US.GetString(handshakeBytes);
                handshakeReplyString = handshakeReplyString.Substring(0, handshakeReplyString.IndexOf('\0'));

                auxilarryUDPSocket.Bind(mainSocket.LocalEndPoint);
            }

            catch
            {

                return (ConnectionCodes.ConnectionTimeoutStatus);
            }

            if (handshakeReplyString == ConnectionCodes.ExpiredSessionStatus)
            {
                return (ConnectionCodes.ExpiredSessionStatus);
            }
            else if (handshakeReplyString.Length >= ConnectionCodes.DefaultFlagLength && 
                handshakeReplyString.Substring(0, ConnectionCodes.DefaultFlagLength) == ConnectionCodes.LoginSuccess)
            {
                return (handshakeReplyString);
            }
            else
            {
                return (ConnectionCodes.ConnectionFailure);
            }

        }

        /// <summary>
        /// Clientside method. <para />
        /// Sends "Log In" request, paired with login and password, to remote server address.
        /// </summary>
        /// <param name="login">User's input username.</param>
        /// <param name="password">User's input password.</param>
        /// <returns></returns>
        public static string LogPasRequestClientside(string login, string password)
        {
            //HERE WILL BE THE ENCRYPTION MECHANISM
            byte[] streamLogPassBytes;

            string loginPasswordRaw = ConnectionCodes.ConnectionSignature + ConnectionCodes.LogInRequest + "login=" + login + "password=" + password;

            streamLogPassBytes = us_US.GetBytes(loginPasswordRaw);
            mainSocket.Send(streamLogPassBytes);

            Array.Clear(streamLogPassBytes, 0, streamLogPassBytes.Length);

            try
            {
                mainSocket.Receive(streamLogPassBytes);
            }
            catch
            {
                return (ConnectionCodes.ConnectionFailure);
            }

            return (us_US.GetString(streamLogPassBytes));
        }

        /// <summary>
        /// Clientside method. <para />
        /// Sends "Sign Up" request, paired with name, username and password, to remote server address.
        /// </summary>
        /// <param name="login">User's input username.</param>
        /// <param name="password">User's input password.</param>
        /// <param name="name">User's input name.</param>
        /// <returns></returns>
        public static string SignUpRequestClientside(string name, string login, string password)
        {
            byte[] streamSignUpBytes;
            string signUpRequestRaw = ConnectionCodes.ConnectionSignature + ConnectionCodes.SignUpRequest + "name=" + name + "login=" + login + "password=" + password;

            streamSignUpBytes = us_US.GetBytes(signUpRequestRaw);
            mainSocket.Send(streamSignUpBytes);

            Array.Clear(streamSignUpBytes, 0, streamSignUpBytes.Length);

            try
            {
                mainSocket.Receive(streamSignUpBytes);
            }
            catch
            {
                return (ConnectionCodes.ConnectionFailure);
            }

            return (us_US.GetString(streamSignUpBytes));
        }

        /// <summary>
        /// This method is used to send an arbitrary, user-defined request with it's own flag and request body to the server.
        /// </summary>
        /// <param name="flag">Request type flag.</param>
        /// <param name="request">Request arguments.</param>
        /// <returns></returns>
        public static string ClientRequestArbitrary(string flag, string request, bool waitReceive = true, bool sendConfirmation = false, bool sendConnectionSignature = true)
        {
            byte[] streamBytes;
            string localCookieContents = "";

            if (sendConfirmation)
            {
                localCookieContents = FileIOStreamer.ReadFromFile(FileIOStreamer.defaultLocalCookiesDirectory + "persistenceCookie.txt");
            }

            string clientRequestRaw = flag + localCookieContents + request;

            if (sendConnectionSignature)
            {
                clientRequestRaw = ConnectionCodes.ConnectionSignature + clientRequestRaw;
            }

            streamBytes = us_US.GetBytes(clientRequestRaw);
            try
            {
                mainSocket.Send(streamBytes);
            }
            catch
            {
                return (ConnectionCodes.SendFailure);
            }

            Array.Clear(streamBytes, 0, streamBytes.Length);

            if (waitReceive == false)
            {
                return ("");
            }

            try
            {
                mainSocket.Receive(streamBytes);
            }
            catch
            {
                return (ConnectionCodes.ConnectionFailure);
            }

            if (mainSocket.Available > 0)
            {
                int oldLength = streamBytes.Length;
                Array.Resize(ref streamBytes, oldLength + mainSocket.Available);

                mainSocket.Receive(streamBytes, oldLength, mainSocket.Available, SocketFlags.None);
            }

            string result = us_US.GetString(streamBytes);

            if(result.Contains("\0"))
            {
                result = result.Substring(0, result.IndexOf('\0'));
            }

            return (result);
        }

        /// <summary>
        /// Sends a request to pass a new message to the server, and records message on the client PC.
        /// </summary>
        /// <param name="chatID"></param>
        /// <param name="content"></param>
        public static void ClientSendMessage(string chatID, string content)
        {
            NetComponents.ClientRequestArbitrary(ConnectionCodes.SendNewMessageRequest, "rcpnt=" + chatID + "content=" + content, true, true);

            FileIOStreamer.WriteToFile(FileIOStreamer.defaultLocalUserDialoguesDirectory + "chatid=" + chatID + ".txt",
                    "message==" + "\ntime=" + DateTime.Now.ToUniversalTime().ToString("dddd, dd MMMM yyyy HH: mm:ss", CultureInfo.InvariantCulture) + "\nstatus=sent" +
                    "\ncontent=" + content + "\n==message\n", false);
        }

        /// <summary>
        /// Clientside method.<para/>
        /// Organizes a request to pull all pending messages from the server and save them on the client's device.
        /// </summary>
        /// <returns></returns>
        public static string ClientPendingMessageManager()
        {
            string pendingMessagesString = NetComponents.ClientRequestArbitrary(ConnectionCodes.GetPendingMessageRequest, "", true, true);

            if (pendingMessagesString.Contains(ConnectionCodes.NoPendingMessagesStatus))
                return "";

            string[] pendingMessages = pendingMessagesString.Split(new string[] { "msg=" }, StringSplitOptions.RemoveEmptyEntries);
            string[] messageSplitstrings = new string[] { "sender=", "time=", "message=" };

            foreach(string message in pendingMessages)
            {
                string[] messageSubstrings = message.Split(messageSplitstrings, StringSplitOptions.RemoveEmptyEntries);
                //[0] - senderid, [1] - message time, [2] - message contents

                //ChatID is same as senderID
                FileIOStreamer.WriteToFile(FileIOStreamer.defaultLocalUserDialoguesDirectory + "chatid=" + messageSubstrings[0] + ".txt",
                    "message==" + "\ntime=" + messageSubstrings[1] + "\nstatus=unread" + 
                    "\ncontent=" + messageSubstrings[2] + "\n==message\n", false);
            }

            receivedMessagesCollection.Enqueue(pendingMessagesString);

            return (pendingMessagesString);
            
        }

        /// <summary>
        /// Listens for UDP server calls.
        /// </summary>
        public static void ClientServerFlagListener()
        {
            byte[] streamBytes;

            while (mainSocket.Connected == true)
            {
                streamBytes = new byte[64];

                try
                {
                    auxilarryUDPSocket.Receive(streamBytes);
                }
                catch
                { return; }

                string serverMessage = us_US.GetString(streamBytes);
                serverMessage = serverMessage.Substring(0, serverMessage.IndexOf('\0'));

                if (serverMessage == ConnectionCodes.AvailablePendingMessagesStatus)
                {
                    NetComponents.ClientPendingMessageManager();
                }
            }
        }

        /// <summary>
        /// Serverside method. <para />
        /// Tries to disconnect server's main socket and stop its listening state.
        /// </summary>
        /// <returns></returns>
        public static string ServerStopListen()
        {
            try
            {
                mainSocket.Disconnect(true);
            }
            catch
            {

            }
            serverListeningState = false;
            return ("Stopped listening on port " + socketAddress);
        }

        /// <summary>
        /// Serverside method. <para />
        /// Sets server's IP address.
        /// </summary>
        /// <param name="input">Server IP address.</param>
        /// <returns></returns>
        public static string ServerSetIPAddress(string input)
        {
            if (serverIPEndPoint != null)
            {
                return ("Can't change IP while server is already bound\n");
            }

            if (input == "")
            {
                return ("Wrong argument. Please try again.\n");
            }

            ipAddress = input;

            return("New server IP address set on " + ipAddress + "\n");
        }

        /// <summary>
        /// Serverside method <para />
        /// Sets server's socket address.
        /// </summary>
        /// <param name="socketNumber"></param>
        /// <returns></returns>
        public static string ServerSetSocket(string socketNumber)
        {
            if (serverIPEndPoint != null)
            {
                return ("Can't change socket address while the server is already bound\n");
            }

            try
            {
                socketAddress = Convert.ToInt32(socketNumber);
            }
            catch
            {
                return ("Wrong argument. Please try again.\n");
            }

            return("New socket address has been set at " + socketAddress + "\n");
        }

        /// <summary>
        /// Serverside method. <para />
        /// Binds IP address to socket address, creating new IPEndPoint.
        /// </summary>
        /// <param name="ipAddress"></param>
        /// <returns></returns>
        public static string ServerBind(string ipAddress)
        {
            try
            {
                serverAddress = IPAddress.Parse(ipAddress);
            }
            catch
            {
                return ("Failed to parse");
            }

            serverIPEndPoint = new IPEndPoint(serverAddress, socketAddress);

            try
            {
                mainSocket.SetSocketOption(SocketOptionLevel.Socket, SocketOptionName.ReuseAddress, true);
                mainSocket.Bind(serverIPEndPoint);

                auxilarryUDPSocket.Bind(serverIPEndPoint);
            }
            catch
            {
                return ("Server not bound. Use 'unbind' before binding server again.");
            }

            return ("Server bound on IP " + ipAddress + ":" + socketAddress);
        }

        /// <summary>
        /// Closes main socket and releases all associated resources.
        /// </summary>
        public static void BreakBind(bool reuse)
        {
            mainSocket.Close();
            auxilarryUDPSocket.Close();

            serverAddress = null;
            serverIPEndPoint = null;

            if(reuse == true)
            {
                mainSocket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
                auxilarryUDPSocket = new Socket(AddressFamily.InterNetwork, SocketType.Dgram, ProtocolType.Udp);
            }
        }

        /// <summary>
        /// Closes main socket and recreates a new one.
        /// </summary>
        public static void DisconnectMainSocket()
        {
            mainSocket.Shutdown(SocketShutdown.Both);
            mainSocket.Close();

            mainSocket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
            //mainSocket.Close();
        }
    }
}