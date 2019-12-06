using System;
using System.Text;
using System.Net;
using System.Net.Sockets;
using System.Threading;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Runtime;

namespace ChatBubble
{
    /// <summary>
    /// Class containing every method responsible for ChatBubble Client - Server communication and all associated processes
    /// </summary>
    public static class NetComponents
    {
        public static Encoding us_US = Encoding.GetEncoding(20127);

        static Socket mainSocket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
        static Socket auxilarryUDPSocket = new Socket(AddressFamily.InterNetwork, SocketType.Dgram, ProtocolType.Udp);

        static IPAddress serverAddress;
        static IPEndPoint serverIPEndPoint;                     //TO DO: Make the 3 variables on the left be read from server config file at startup

        public static int socketAddress = 8000;
        public static string ipAddress;

        //Server State Variables
        static bool serverListeningState = false;
        public static int liveClientCount;

        public static ConcurrentDictionary<int, string> connectedClientsBlockingCollection = new ConcurrentDictionary<int, string>();
        public static ConcurrentDictionary<int, string> loggedInUsersBlockingCollection = new ConcurrentDictionary<int, string>();

        public static ConcurrentQueue<string> receivedMessagesCollection = new ConcurrentQueue<string>();

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
            serverAddress = IPAddress.Parse(serverAddressString);

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
        /// Serverside method. <para />
        /// Puts the server into listen - accept loop. On accept, creates new thread to handle pending client. Exits from method when
        /// server is no longer listening.
        /// </summary>

        public static void ServerListeningState()
        {
            serverListeningState = true;
            while (serverListeningState == true)
            {
                try
                {
                    mainSocket.Listen(1000);

                    Socket pendingClientSocket = mainSocket.Accept();

                    Thread handshakeReceiveReplyThread = new Thread(ServerHandshakeReception);
                    handshakeReceiveReplyThread.Start(pendingClientSocket);
                }
                catch
                {

                }
            }
        }

        /// <summary>
        /// Serverside method. <para />
        /// Accepts incoming client handshake. Receives and handles incoming cookie token. On successful handshake reception, 
        /// passes the client to request handler.
        /// </summary>
        /// <param name="clientSocket">Pending client socket.</param>
        public static void ServerHandshakeReception(object clientSocket)
        {
            string clientHandshakeToken;
            byte[] streamBytes = new byte[64];


            Socket pendingClientSocket = (Socket)clientSocket;

            pendingClientSocket.Receive(streamBytes);

            clientHandshakeToken = us_US.GetString(streamBytes);
            clientHandshakeToken = clientHandshakeToken.Substring(0, clientHandshakeToken.IndexOf('\0'));

            //Code below compares the stored user persistence cookie signature to the one sent by the user
            if (clientHandshakeToken != "fresh_session" && String.IsNullOrEmpty(clientHandshakeToken) != true &&
                                                                                  IsCookieInDatabase(clientHandshakeToken) == true)
            {
                //If signature is correct, gets user data from database to pass into the login service

                string[] clientHandshakeTokenSubstrings = clientHandshakeToken.Split(new string[2] { "id=", "confirmation=" }, 2, StringSplitOptions.RemoveEmptyEntries);
                string[] userData = GetUserData(clientHandshakeTokenSubstrings[0]);

                if (userData.Length > 1 && userData[0] != "User_not_found")
                {
                    string cookieCredentials = "login=" + userData[1] + "password=" + userData[3];

                    clientHandshakeToken = ServerLogInService(cookieCredentials, pendingClientSocket.RemoteEndPoint);
                }
                else
                {
                    clientHandshakeToken = "session_expr";
                }
            }
            else
            {
                clientHandshakeToken = "session_expr";
            }

            streamBytes = us_US.GetBytes(clientHandshakeToken);
            pendingClientSocket.Send(streamBytes);

            connectedClientsBlockingCollection.TryAdd(connectedClientsBlockingCollection.Count + 1, "ip=" + pendingClientSocket.RemoteEndPoint.ToString());
            ServersideRequestReceiver(pendingClientSocket);
        }
        /// <summary>
        /// Serverside method.
        /// Compares client's cookie with live sessions database to confirm identity.
        /// </summary>
        /// <param name="cookieContent">Contents of the comparable cookie.</param>
        /// <returns></returns>
        public static bool IsCookieInDatabase(string cookieContent)
        {
            string[] clientHandshakeTokenSplitStrings = new string[2] { "id=", "confirmation=" };

            string[] clientHandshakeTokenSubtrings = cookieContent.Split(clientHandshakeTokenSplitStrings, StringSplitOptions.RemoveEmptyEntries);
            //For clientHandshakeTokenSubstrings, index 0 is id, index 1 is confirmation hash

            //For sessionContentSplitstrings, index 0 is hash, index 1 is ip

            FileIOStreamer fileIO = new FileIOStreamer();
            string[] activeUserSessions;
            activeUserSessions = fileIO.GetDirectoryFiles(FileIOStreamer.defaultActiveUsersDirectory, false, false);

            if (Array.Exists(activeUserSessions, pendingUser => pendingUser == "id=" + clientHandshakeTokenSubtrings[0]))
            {
                string sessionHash = fileIO.ReadFromFile(FileIOStreamer.defaultActiveUsersDirectory + "id=" + clientHandshakeTokenSubtrings[0] +
                                                      ".txt");

                if (sessionHash == clientHandshakeTokenSubtrings[1])
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
        public static string[] GetUserData(string searchParameter, bool fastSearch = false)
        {
            FileIOStreamer fileIO = new FileIOStreamer();
            string defaultUsersDirectory = FileIOStreamer.defaultRegisteredUsersDirectory; //TEMPORARY

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

            string[] registeredUserFiles = fileIO.GetDirectoryFiles(defaultUsersDirectory, false, false);
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
                            fileEntries = fileIO.ReadFromFile(defaultUsersDirectory + registeredUserFile + ".txt", "", "password=");
                        }
                        else
                        {
                            fileEntries = fileIO.ReadFromFile(defaultUsersDirectory + registeredUserFile + ".txt");
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
                return (new string[1] { "Error" });
            }
            return (new string[1] { "User_not_found" });
        }

        /// <summary>
        /// Gets the highest user ID found in database.
        /// </summary>
        /// <returns></returns>
        public static int GetMaxUserID()
        {
            FileIOStreamer fileIO = new FileIOStreamer();
            string defaultUsersDirectory = FileIOStreamer.defaultRegisteredUsersDirectory; //TEMPORARY

            string[] registeredUserFilesSplitStrings = new string[3] { "name=", "login=", "password=" };
            string[] registeredUserFiles = fileIO.GetDirectoryFiles(defaultUsersDirectory, false, false);

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
        public static int SearchCompareByName(string userDataString1, string userDataString2)
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
        public static void ServersideRequestReceiver(Socket pendingClientSocket)
        {
            while (pendingClientSocket.Connected == true)
            {
                byte[] streamBytes = new byte[512];

                if (pendingClientSocket.Poll(10, SelectMode.SelectRead) == true) //Ensures that data is available to be read from the socket
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
                        ServerConnectionCloseDictionaryUpdater(pendingClientSocket.RemoteEndPoint.ToString());

                        pendingClientSocket.Close();
                        return;
                    }

                    string clientRequestRaw = us_US.GetString(streamBytes);
                    clientRequestRaw = clientRequestRaw.Substring(0, clientRequestRaw.IndexOf('\0'));

                    if (clientRequestRaw.Length >= 17)
                    {
                        switch (clientRequestRaw.Substring(0, 17))
                        {
                            case "[log_in_request_]":
                                streamBytes = us_US.GetBytes(ServerLogInService(clientRequestRaw.Substring(17), pendingClientSocket.RemoteEndPoint));
                                pendingClientSocket.Send(streamBytes);
                                break;
                            case "[sign_up_request]":
                                streamBytes = us_US.GetBytes(ServerSignUpService(clientRequestRaw.Substring(17)));
                                pendingClientSocket.Send(streamBytes);
                                break;
                            case "[searchs_request]":
                                streamBytes = us_US.GetBytes(ServerSearchService(clientRequestRaw.Substring(17)));
                                pendingClientSocket.Send(streamBytes);
                                break;
                            case "[add_friend_add_]":
                                streamBytes = us_US.GetBytes(ServerAddFriendService(clientRequestRaw.Substring(17)));
                                pendingClientSocket.Send(streamBytes);
                                break;
                            case "[get_friends_lst]":
                                streamBytes = us_US.GetBytes(ServerGetFriendsService(clientRequestRaw.Substring(17)));
                                pendingClientSocket.Send(streamBytes);
                                break;
                            case "[rem_friend_rem_]":
                                streamBytes = us_US.GetBytes(ServerRemoveFriendService(clientRequestRaw.Substring(17)));
                                pendingClientSocket.Send(streamBytes);
                                break;
                            case "[get_user_summar]":
                                streamBytes = us_US.GetBytes(ServerGetUserSummaryService(clientRequestRaw.Substring(17)));
                                pendingClientSocket.Send(streamBytes);
                                break;
                            case "[edt_user_summar]":
                                streamBytes = us_US.GetBytes(ServerEditUserSummaryService(clientRequestRaw.Substring(17)));
                                pendingClientSocket.Send(streamBytes);
                                break;
                            case "[get_pendng_msgs]":
                                streamBytes = us_US.GetBytes(ServerGetPendingMessagesService(clientRequestRaw.Substring(17)));
                                pendingClientSocket.Send(streamBytes);
                                break;
                            case "[send_new_messag]":
                                streamBytes = us_US.GetBytes(ServerPassMessageService(clientRequestRaw.Substring(17)));
                                pendingClientSocket.Send(streamBytes);
                                break;
                            case "[log_out_log_out]":
                                ServerConnectionCloseDictionaryUpdater(pendingClientSocket.RemoteEndPoint.ToString());

                                pendingClientSocket.Close();
                                return;
                        }
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
        public static string ServerLogInService(string clientRequest, EndPoint clientIP)
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
                return ("login_failure");
            }

            if (userData[0] != "User_not_found")
            {
                //For userData,index 0 is user ID, index 1 is user login, index 2 is user name, index 3 is user password, index 4 is user ip
                //For clientRequestSubstrings, index 0 is login, index 1 is password

                if (clientRequestSubstrings[1] == userData[3])
                {
                    Random randomGenerator = new Random();
                    int randomHashSeed = randomGenerator.Next(99999999);

                    ClientSessionHandler(userData[0], randomHashSeed);

                    loggedInUsersBlockingCollection.TryAdd(loggedInUsersBlockingCollection.Count + 1, "id=" + userData[0] + "ip=" + clientIP.ToString());
                    return ("login_success" + "id=" + userData[0] + "hash=" + randomHashSeed.ToString());
                    //Passes user ID and persistence cookie key back for session update purposes
                }
            }
            return ("login_failure");
        }


        /// <summary>
        /// Serverside method. <para />
        /// Handles received "Sign Up" requests. Adds entries to server database accordingly.
        /// </summary>
        /// <param name="clientRequest">Received client request.</param>
        /// <returns></returns>
        public static string ServerSignUpService(string clientRequest)
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
                return ("sign_up_failure_2");
            }

            if (userData[0] == "User_not_found")
            {
                FileIOStreamer fileIO = new FileIOStreamer();
                int maxID = GetMaxUserID();
                fileIO.WriteToFile(defaultUsersDirectory + (maxID + 1).ToString() + "login=" + clientRequestSubstrings[1]
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
                "\nregdateutc=" + dateTime.ToUniversalTime().ToString(), true);

                return ("sign_up_success");
            }
            else if (userData[0] != "Error")
            {
                return ("sign_up_failure_1"); //Returns if a user with this name already exists
            }
            return ("sign_up_failure_2"); //Returns if the database couldn't be parsed
        }

        /// <summary>
        /// Serverside method.<para />
        /// Returns main profile information for the user specified in the request.
        /// </summary>
        /// <param name="clientRequest">Received client request.</param>
        /// <returns></returns>
        public static string ServerGetUserSummaryService(string clientRequest)
        {
            string[] clientRequestSplitStrings = new string[3] { "id=", "confirmation=", "reqid=" };
            FileIOStreamer fileIO = new FileIOStreamer();

            string[] clientRequestSubstrings = clientRequest.Split(clientRequestSplitStrings, StringSplitOptions.RemoveEmptyEntries);
            //For clientRequestSubstrings, client [0] = client ID, [1] = cookie confirmation [2] = ID of requested user

            //Ensures user authenticity
            if (IsCookieInDatabase("id=" + clientRequestSubstrings[0] + "confirmation=" + clientRequestSubstrings[1]) != true)
            {
                return ("authsn_not_passed");
            }

            if (clientRequestSubstrings[2] == "self")
            {
                //If no ID given, return requesting user data
                clientRequestSubstrings[2] = clientRequestSubstrings[0];
            }

            string[] userData = GetUserData(clientRequestSubstrings[2]);

            if (userData[0] != "User_not_found" && userData[0] != "Error" && userData[6].Length > 0)
            {
                string[] summarySubstrings = userData[6].Split(new string[] { "status=", "main=" }, StringSplitOptions.RemoveEmptyEntries);

                return ("id=" + userData[0] + "login=" + userData[1] + "name=" + userData[2] +
                    "status=" + summarySubstrings[0] + "main=" + summarySubstrings[1] + "bubscore=" + userData[7]);
            }
            else
            {
                return ("database__error__");
            }
        }

        /// <summary>
        /// Serverside method.<para />
        /// Updates main profile information for the specified user. Replaces all occurences of "=" with "[eql_sgn]" to prevent database errors.
        /// </summary>
        /// <param name="clientRequest"></param>
        /// <returns></returns>
        public static string ServerEditUserSummaryService(string clientRequest)
        {
            string[] clientRequestSplitStrings = new string[3] { "id=", "confirmation=", "newsummary=" };
            string defaultUsersDirectory = FileIOStreamer.defaultRegisteredUsersDirectory; //TEMPORARY
            FileIOStreamer fileIO = new FileIOStreamer();

            string[] clientRequestSubstrings = clientRequest.Split(clientRequestSplitStrings, StringSplitOptions.RemoveEmptyEntries);
            //For clientRequestSubstrings, client [0] = client ID, [1] = cookie confirmation [2] = new summary

            //Ensures user authenticity
            if (IsCookieInDatabase("id=" + clientRequestSubstrings[0] + "confirmation=" + clientRequestSubstrings[1]) != true)
            {
                return ("authsn_not_passed");
            }

            //Reform newsummary= and replace '=' with '[eqlsgn]' to prevent injection attacks
            //This step can't be done using string.split since split may remove separator injected by user
            clientRequestSubstrings[2] = clientRequestSubstrings[2].Replace("\n", "");

            string clientNewSummaryStatusSubstring = clientRequestSubstrings[2].Substring(clientRequestSubstrings[2].IndexOf("status=") + 7);
            clientNewSummaryStatusSubstring = clientNewSummaryStatusSubstring.Substring(0, clientNewSummaryStatusSubstring.IndexOf("main=")).Replace("=", "[eqlsgn]");
            string clientNewSummaryMainSubstring = clientRequestSubstrings[2].Substring(clientRequestSubstrings[2].IndexOf("main=") + 5).Replace("=", "[eqlsgn]");

            clientRequestSubstrings[2] = "\nstatus=" + clientNewSummaryStatusSubstring + "\nmain=" + clientNewSummaryMainSubstring + "\n";

            string[] userData = GetUserData(clientRequestSubstrings[0]);

            if (userData[0] != "User_not_found" && userData[0] != "Error" && userData[6].Length > 0)
            {
                fileIO.RemoveFileEntry(defaultUsersDirectory + userData[0] + "login=" + userData[1] + ".txt", "summary==", userData[6], true, true);

                if (clientRequestSubstrings[2].Substring(clientRequestSubstrings[2].IndexOf("status=") + 7, clientRequestSubstrings[2].IndexOf("main=") - (clientRequestSubstrings[2].IndexOf("status=") + 7)) == "\n")
                {
                    clientRequestSubstrings[2] = clientRequestSubstrings[2].Insert(clientRequestSubstrings[2].IndexOf("status=") + 7, "null");
                }

                if (clientRequestSubstrings[2].Substring(clientRequestSubstrings[2].IndexOf("main=") + 5, clientRequestSubstrings[2].Length - (clientRequestSubstrings[2].IndexOf("main=") + 5)) == "\n")
                {
                    clientRequestSubstrings[2] = clientRequestSubstrings[2].Insert(clientRequestSubstrings[2].IndexOf("main=") + 5, "null");
                }

                fileIO.WriteToFile(defaultUsersDirectory + userData[0] + "login=" + userData[1] + ".txt", clientRequestSubstrings[2], false, "summary==");
                return ("descript_chng_suc");
            }
            else
            {
                return ("database__error__");
            }
        }

        /// <summary>
        /// Serverside method. <para />
        /// Organizes database search and returns results based on the search parameter.
        /// </summary>
        /// <param name="searchParameter"></param>
        /// <returns></returns>
        public static string ServerSearchService(string searchParameter)
        {
            if (searchParameter == "")
            {
                return ("=no_match=");
            }

            string defaultUsersDirectory = FileIOStreamer.defaultRegisteredUsersDirectory; //TEMPORARY

            FileIOStreamer fileIO = new FileIOStreamer();

            List<string> registeredUsersList = new List<string>(fileIO.GetDirectoryFiles(defaultUsersDirectory, false, false));
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
        public static string ServerAddFriendService(string clientRequest)
        {
            string[] clientRequestSplitStrings = new string[3] { "id=", "confirmation=", "addid=" };
            string defaultUsersDirectory = FileIOStreamer.defaultRegisteredUsersDirectory; //TEMPORARY
            FileIOStreamer fileIO = new FileIOStreamer();

            string[] clientRequestSubstrings = clientRequest.Split(clientRequestSplitStrings, StringSplitOptions.RemoveEmptyEntries);
            //For clientRequestSubstrings, client [0] = client ID, [1] = cookie confirmation [2] = prospective friend ID

            //Ensures user authenticity
            if (IsCookieInDatabase("id=" + clientRequestSubstrings[0] + "confirmation=" + clientRequestSubstrings[1]) != true)
            {
                return ("authsn_not_passed");
            }

            string[] userData = GetUserData(clientRequestSubstrings[0]);

            if (userData[0] != "User_not_found" && userData[0] != "Error")
            {
                //Check if user already has this friend (probably going to be made redundant in the future)
                string[] fIDSubstrings = userData[8].Split(new string[] { "fid=" }, StringSplitOptions.RemoveEmptyEntries);

                foreach (string fid in fIDSubstrings)
                {
                    if (fid == clientRequestSubstrings[2])
                    {
                        return ("friend_already_added");
                    }
                }

                fileIO.WriteToFile(defaultUsersDirectory + userData[0] + "login=" + userData[1] + ".txt", "\nfid=" + clientRequestSubstrings[2] + "=", true, "friends==");
                return ("friend_add_succes");
            }
            else
            {
                return ("database__error__");
            }
        }

        /// <summary>
        /// Serverside method.<para/>
        /// Returns the list of all friends as a string in the friendlist of a specified user.
        /// </summary>
        /// <param name="clientRequest"></param>
        /// <returns></returns>
        public static string ServerGetFriendsService(string clientRequest)
        {
            string[] clientRequestSplitStrings = new string[2] { "id=", "confirmation=" };
            string[] clientRequestSubstrings = clientRequest.Split(clientRequestSplitStrings, StringSplitOptions.RemoveEmptyEntries);
            //For clientRequestSubstrings, client [0] = client ID, [1] = cookie confirmation

            //Ensures user authenticity
            if (IsCookieInDatabase("id=" + clientRequestSubstrings[0] + "confirmation=" + clientRequestSubstrings[1]) != true)
            {
                return ("authsn_not_passed");
            }

            string[] userData = GetUserData(clientRequestSubstrings[0]);

            if (userData.Length > 11 && userData[0] != "User_not_found" && userData[0] != "Error" && userData[8].Contains("null") == false)
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
                return ("database__error__");
            }
        }

        /// <summary>
        /// Serverside method.<para/>
        /// Removes specified friend id from the specified user's friendlist.
        /// </summary>
        /// <param name="clientRequest"></param>
        /// <returns></returns>
        public static string ServerRemoveFriendService(string clientRequest)
        {
            string[] clientRequestSplitStrings = new string[3] { "id=", "confirmation=", "fid=" };
            string defaultUsersDirectory = FileIOStreamer.defaultRegisteredUsersDirectory; //TEMPORARY
            FileIOStreamer fileIO = new FileIOStreamer();

            string[] clientRequestSubstrings = clientRequest.Split(clientRequestSplitStrings, StringSplitOptions.RemoveEmptyEntries);
            //For clientRequestSubstrings, client [0] = client ID, [1] = cookie confirmation, [2] fid=id

            //Ensures user authenticity
            if (IsCookieInDatabase("id=" + clientRequestSubstrings[0] + "confirmation=" + clientRequestSubstrings[1]) != true)
            {
                return ("authsn_not_passed");
            }

            string[] userData = GetUserData(clientRequestSubstrings[0]);

            if (userData[0] != "User_not_found" && userData[0] != "Error" && clientRequestSubstrings.Length > 2 && clientRequestSubstrings[2] != "")
            {
                fileIO.RemoveFileEntry(defaultUsersDirectory + userData[0] + "login=" + userData[1] + ".txt", "friends==", "\nfid=" + clientRequestSubstrings[2] + "=");
                return ("friend_rem_succes");
            }
            else
            {
                return ("database__error__");
            }
        }

        /// <summary>
        /// Serverside method.<para />
        /// Returns a list of all pending chat messages addressed to the requesting user as a string.
        /// </summary>
        /// <param name="clientRequest"></param>
        /// <returns></returns>
        public static string ServerGetPendingMessagesService(string clientRequest)
        {
            string[] clientRequestSplitStrings = new string[2] { "id=", "confirmation=" };
            string defaultPendingMessagesDirectory = FileIOStreamer.defaultPendingMessagesDirectory; //TEMPORARY
            string[] messageHandleSplitstrings = new string[3] { "msgid=", "sender=", "rcpnt=" };
            FileIOStreamer fileIO = new FileIOStreamer();

            string[] clientRequestSubstrings = clientRequest.Split(clientRequestSplitStrings, StringSplitOptions.RemoveEmptyEntries);
            //For clientRequestSubstrings, client [0] = client ID, [1] = cookie confirmation

            //Ensures user authenticity
            if (IsCookieInDatabase("id=" + clientRequestSubstrings[0] + "confirmation=" + clientRequestSubstrings[1]) != true)
            {
                return ("authsn_not_passed");
            }

            string[] allPendingMessages = fileIO.GetDirectoryFiles(defaultPendingMessagesDirectory, false, false);
            string serverReplyString = "";

            foreach (string pendingMessage in allPendingMessages)
            {
                string[] pendingMessageSubstrings = pendingMessage.Split(messageHandleSplitstrings, StringSplitOptions.RemoveEmptyEntries);
                //For message handle splits, [0] - server msgid, [1] = sender id, [2] = recipient id

                if (pendingMessageSubstrings[2] == clientRequestSubstrings[0])
                {
                    string messageContent = fileIO.ReadFromFile(defaultPendingMessagesDirectory + pendingMessage + ".txt");
                    string[] messageContentSubstrings = messageContent.Split(new string[] { "time=", "content=" }, StringSplitOptions.RemoveEmptyEntries);
                    //[0] - message time, [1] - message content

                    serverReplyString += "msg=" + "sender=" + pendingMessageSubstrings[1] + "time=" + messageContentSubstrings[0] +
                        "message=" + messageContentSubstrings[1];

                    fileIO.RemoveFile(defaultPendingMessagesDirectory + pendingMessage + ".txt");
                }
            }

            if (serverReplyString == "")
            {
                return ("[no_pndg_msgs]");
            }
            else
            {
                return (serverReplyString);
            }
        }

        public static void ServerMakeMessagePending(string sender, string recepient, string content)
        {
            FileIOStreamer fileIO = new FileIOStreamer();
            string defaultPendingMessagesDirectory = "D:\\ChatBubblePendingMessagesFolder\\";
            DateTime currentServerTime = DateTime.Now.ToUniversalTime();

            int chatID = fileIO.GetDirectoryFiles(defaultPendingMessagesDirectory, false, false).Length + 1;

            fileIO.WriteToFile(defaultPendingMessagesDirectory + "chatid=" + chatID + "sender=" + sender + "rcpnt=" + recepient + ".txt",
                "time=" + currentServerTime.ToString("dddd, dd MMMM yyyy HH: mm:ss") + "\ncontent=" + content);
        }

        public static string ServerPassMessageService(string clientRequest)
        {
            string[] clientRequestSplitStrings = new string[] { "id=", "confirmation=", "rcpnt=", "content=" };

            string[] clientRequestSubstrings = clientRequest.Split(clientRequestSplitStrings, StringSplitOptions.RemoveEmptyEntries);
            //For clientRequestSubstrings, client [0] = client ID, [1] = cookie confirmation, [2] = recepient id, [3] = message content

            //Ensures user authenticity
            if (IsCookieInDatabase("id=" + clientRequestSubstrings[0] + "confirmation=" + clientRequestSubstrings[1]) != true)
            {
                return ("authsn_not_passed");
            }

            //Check if user texted himself to prevent server action
            {
                if(clientRequestSubstrings[2] == clientRequestSubstrings[0])
                {
                    return ("msg_sent_to_self");
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

                    string messagePullRequest = "[pndg_msgs_av]";

                    streamBytes = us_US.GetBytes(messagePullRequest);

                    IPAddress recepientIPAddress = IPAddress.Parse(ipPortSubstrings[0]);
                    IPEndPoint recepientEndPoint = new IPEndPoint(recepientIPAddress, Convert.ToInt32(ipPortSubstrings[1]));

                    auxilarryUDPSocket.SendTo(streamBytes, recepientEndPoint);
                }
            }

            return ("msg_sent");
        }

        /// <summary>
        /// Serverside method. <para />
        /// [work in progress] Generates hash from randomly created seed for the specified user ID. Generates new live session file for
        /// pending user connection.
        /// </summary>
        /// <param name="userID">Pending log in user ID.</param>
        /// <param name="hashSeed">Hash generator seed.</param>
        public static void ClientSessionHandler(string userID, int hashSeed)
        {
            FileIOStreamer fileIO = new FileIOStreamer();

            //PERSISTENCE COOKIE HASH GENERATOR WILL BE HERE
            string hashString = hashSeed.ToString();

            //PERSONAL HASH WOULD BE WRITTEN IN THE USER SESSION FILE BELOW
            fileIO.ClearFile(FileIOStreamer.defaultActiveUsersDirectory + "id=" + userID + ".txt");
            fileIO.WriteToFile(FileIOStreamer.defaultActiveUsersDirectory + "id=" + userID + ".txt", hashString, true);

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
            FileIOStreamer fileIO = new FileIOStreamer();

            //Sets live session file timeout span here V
            TimeSpan timeOutSpan = new TimeSpan(1, 0, 0);

            while (serverListeningState == true)
            {
                string[] activeUsersArray = new string[loggedInUsersBlockingCollection.Count];
                loggedInUsersBlockingCollection.Values.CopyTo(activeUsersArray, 0);

                fileIO.FileTimeOutComparator(FileIOStreamer.defaultActiveUsersDirectory, activeUsersArray, timeOutSpan);

                Thread.Sleep(1000);
            }
        }

        public static void ServerConnectionCloseDictionaryUpdater(string userIP)
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
            FileIOStreamer fileIO = new FileIOStreamer();
            string handshakeReplyString;
            string localCookieContents = fileIO.ReadFromFile(FileIOStreamer.defaultLocalCookiesDirectory + "persistenceCookie.txt");

            if (String.IsNullOrEmpty(localCookieContents) == true)
            {
                localCookieContents = "fresh_session";
            }

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

                return ("connection_failure");
            }

            if (handshakeReplyString == "session_expr")
            {
                return ("session_expr");
            }
            else if (handshakeReplyString.Substring(0, 13) == "login_success")
            {
                return (handshakeReplyString);
            }
            else
            {
                return ("connection_fatal_error");
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

            string loginPasswordRaw = "[log_in_request_]login=" + login + "password=" + password;

            streamLogPassBytes = us_US.GetBytes(loginPasswordRaw);
            mainSocket.Send(streamLogPassBytes);

            Array.Clear(streamLogPassBytes, 0, streamLogPassBytes.Length);

            try
            {
                mainSocket.Receive(streamLogPassBytes);
            }
            catch
            {
                return ("server_closed");
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
            string signUpRequestRaw = "[sign_up_request]name=" + name + "login=" + login + "password=" + password;

            streamSignUpBytes = us_US.GetBytes(signUpRequestRaw);
            mainSocket.Send(streamSignUpBytes);

            Array.Clear(streamSignUpBytes, 0, streamSignUpBytes.Length);

            try
            {
                mainSocket.Receive(streamSignUpBytes);
            }
            catch
            {
                return ("server_closed");
            }

            return (us_US.GetString(streamSignUpBytes));
        }

        /// <summary>
        /// This method is used to send an arbitrary, user-defined request with it's own flag and request body to the server.
        /// </summary>
        /// <param name="flag">Request type flag.</param>
        /// <param name="request">Request body.</param>
        /// <returns></returns>
        public static string ClientRequestArbitrary(string flag, string request, bool waitReceive = true, bool sendConfirmation = false)
        {
            byte[] streamBytes;
            string localCookieContents = "";

            if (sendConfirmation == true)
            {
                FileIOStreamer fileIO = new FileIOStreamer();
                localCookieContents= fileIO.ReadFromFile(FileIOStreamer.defaultLocalCookiesDirectory + "persistenceCookie.txt");
            }

            string clientRequestRaw = flag + localCookieContents + request;

            streamBytes = us_US.GetBytes(clientRequestRaw);
            try
            {
                mainSocket.Send(streamBytes);
            }
            catch
            {
                return ("send_failure");
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
                return ("server_closed");
            }

            if (mainSocket.Available > 0)
            {
                int oldLength = streamBytes.Length;
                Array.Resize(ref streamBytes, oldLength + mainSocket.Available);

                mainSocket.Receive(streamBytes, oldLength, mainSocket.Available, SocketFlags.None);
            }

            //string test = us_US.GetString(streamBytes);

            return (us_US.GetString(streamBytes));
        }

        public static void ClientSendMessage(string chatID, string content)
        {
            FileIOStreamer fileIO = new FileIOStreamer();

            NetComponents.ClientRequestArbitrary("[send_new_messag]", "rcpnt=" + chatID + "content=" + content, true, true);

            fileIO.WriteToFile(FileIOStreamer.defaultLocalUserDialoguesDirectory + "chatid=" + chatID + ".txt",
                    "message==" + "\ntime=" + DateTime.Now.ToUniversalTime().ToString("dddd, dd MMMM yyyy HH: mm:ss") + "\nstatus=sent" +
                    "\ncontent=" + content + "\n==message\n", false);
        }

        /// <summary>
        /// Clientside method.<para/>
        /// Organizes a request to pull all pending messages from the server and save them on the client's device.
        /// </summary>
        /// <returns></returns>
        public static string ClientPendingMessageManager()
        {
            string pendingMessagesString = NetComponents.ClientRequestArbitrary("[get_pendng_msgs]", "", true, true);

            if (pendingMessagesString.Contains("[no_pndg_msgs]"))
                return "";

            string[] pendingMessages = pendingMessagesString.Split(new string[] { "msg=" }, StringSplitOptions.RemoveEmptyEntries);
            string[] messageSplitstrings = new string[] { "sender=", "time=", "message=" };
            FileIOStreamer fileIO = new FileIOStreamer();

            foreach(string message in pendingMessages)
            {
                string[] messageSubstrings = message.Split(messageSplitstrings, StringSplitOptions.RemoveEmptyEntries);
                //[0] - senderid, [1] - message time, [2] - message contents

                string currentDialogueContents = "";
                
                //ChatID is same as senderID
                fileIO.WriteToFile(FileIOStreamer.defaultLocalUserDialoguesDirectory + "chatid=" + messageSubstrings[0] + ".txt",
                    "message==" + "\ntime=" + messageSubstrings[1] + "\nstatus=unread" + 
                    "\ncontent=" + messageSubstrings[2] + "\n==message\n", false);
            }

            receivedMessagesCollection.Enqueue(pendingMessagesString);

            return (pendingMessagesString);
            
        }

        public static void ClientServerFlagListener()
        {
            byte[] streamBytes;

            while (mainSocket.Connected == true)
            {
                streamBytes = new byte[64];

                auxilarryUDPSocket.Receive(streamBytes);

                string serverMessage = us_US.GetString(streamBytes);
                serverMessage = serverMessage.Substring(0, serverMessage.IndexOf('\0'));

                if (serverMessage == "[pndg_msgs_av]")
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
            return ("Stopped listening on port " + socketAddress + ".\n");
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
                return ("Server not bound. Use 'unbind' before binding server again.\n");
            }

            return ("Server bound on IP " + ipAddress + ":" + socketAddress + "\n");
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
                liveClientCount = 0;
            }
        }

        public static void DisconnectMainSocket()
        {
            mainSocket.Shutdown(SocketShutdown.Both);
            mainSocket.Close();

            mainSocket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
            //mainSocket.Close();
        }
    }
}