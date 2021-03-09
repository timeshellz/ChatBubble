using System;
using System.Text;
using System.Net;
using System.Net.Sockets;
using System.Threading;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Runtime;
using System.Globalization;
using ProtoBuf;

using ChatBubble.SharedAPI;

namespace ChatBubble.ServerAPI
{
    public static class ServerRequestManager
    {
        //Server State Variables
        static bool serverListeningState = false;
        public static int[] handshakeSuccessCount = new int[4]; //Max connection attempts per time interval

        public static DateTime serverSessionStartTime;
        public static DateTime serverListeningStartTime;
        public static TimeSpan[] updateTimeSpans = new TimeSpan[3] { new TimeSpan(0, 10, 0), new TimeSpan(1, 0, 0), new TimeSpan(24, 0, 0) };
        public static int[] connectionAttemptsCount = new int[4]; //Max connected count per time interval //Current connection attempts per time interval


        public static ConcurrentDictionary<int, EndPoint> connectedClientEndpoints = new ConcurrentDictionary<int, EndPoint>();
        public static ConcurrentDictionary<int, EndPoint> loggedInUserEndpoints = new ConcurrentDictionary<int, EndPoint>();

        public static ConcurrentQueue<string> receivedMessagesCollection = new ConcurrentQueue<string>();

        /// <summary>
        /// Delegate dictionary of Connection Flag - Net Method pairs.
        /// <p>Stores all possible ChatBubble protocol methods according to their flag.</p>
        /// </summary>
        public static Dictionary<string, Delegate> RequestDictionary = new Dictionary<string, Delegate>()
        {
            [ConnectionCodes.LoginRequest] = new Func<LoginRequest, EndPoint, GenericServerReply>(ServerLoginService),
            [ConnectionCodes.SignUpRequest] = new Func<SignupRequest, GenericServerReply>(ServerSignupService),
            [ConnectionCodes.SearchRequest] = new Func<SearchRequest, GenericServerReply>(ServerSearchService),
            [ConnectionCodes.AddFriendRequest] = new Func<AddFriendRequest, GenericServerReply>(ServerAddFriendService),
            [ConnectionCodes.GetFriendListRequest] = new Func<GetFriendListRequest, GenericServerReply>(ServerGetFriendsService),
            [ConnectionCodes.RemoveFriendRequest] = new Func<RemoveFriendRequest, GenericServerReply>(ServerRemoveFriendService),
            [ConnectionCodes.GetUserSummaryRequest] = new Func<GetUserRequest, GenericServerReply>(ServerGetUserService),
            [ConnectionCodes.EditUserSummaryRequest] = new Func<EditSummaryRequest, GenericServerReply>(ServerEditUserSummaryService),
            [ConnectionCodes.SendNewMessageRequest] = new Func<SendMessageRequest, GenericServerReply>(ServerSendMessageService),
            [ConnectionCodes.GetPendingMessageRequest] = new Func<GetPendingMessagesRequest, GenericServerReply>(ServerGetPendingMessagesService),
            [ConnectionCodes.ChangePasswdRequest] = new Func<ChangePasswordRequest, GenericServerReply>(ServerChangePasswordService),
            [ConnectionCodes.ChangeNameRequest] = new Func<ChangeNameRequest, GenericServerReply>(ServerChangeNameService),
            [ConnectionCodes.ChangeDialogueStatusRequest] = new Func<ChangeDialogueStatusRequest, GenericServerReply>(ServerSetDialogueStatusService),
            [ConnectionCodes.GetDialogueStatusRequest] = new Func<GetDialogueStatusRequest, GenericServerReply>(ServerGetDialogueStatusService),
            [ConnectionCodes.LogOutCall] = new Action<EndPoint>(UpdateConnectionDictionary),
        };

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

                if (updateCount * updateTimeSpans[0].Minutes % updateTimeSpans[1].TotalMinutes == 0)
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

            if (serverListeningStartTime != DateTime.MinValue)
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
                    SharedNetworkConfiguration.MainSocket.Listen(1000);

                    Socket pendingClientSocket = SharedNetworkConfiguration.MainSocket.Accept();

                    FileIOStreamer.LogWriter("New connection with " + pendingClientSocket.RemoteEndPoint.ToString() + " established.");

                    Thread handshakeReceiveReplyThread = new Thread(HandshakeReceiver);
                    handshakeReceiveReplyThread.Start(pendingClientSocket);

                    StatRecordConnection();
                }
                catch (Exception e)
                {
                    FileIOStreamer.LogWriter("Attempted connection failure occured: " + e.Message);
                }
            }

            serverListeningStartTime = DateTime.MinValue;
        }

        static ClientRequest ReceiveClientRequest(Socket clientSocket)
        {
            if (clientSocket.Poll(-1, SelectMode.SelectRead))
            {
                if (clientSocket.Available == 0)
                {
                    throw new SocketException(Convert.ToInt32(SocketError.HostDown));
                }

                int bytesRead = 0;
                int totalBytesRead = 0;

                byte[] receiveBuffer = new byte[2048];
                byte[] signedSerializedRequest = new byte[receiveBuffer.Length];

                while (clientSocket.Available > 0)
                {
                    bytesRead = clientSocket.Receive(receiveBuffer);

                    if (totalBytesRead + bytesRead >= signedSerializedRequest.Length)
                        Array.Resize(ref signedSerializedRequest, totalBytesRead + bytesRead);

                    Array.Copy(receiveBuffer, 0, signedSerializedRequest, totalBytesRead, bytesRead);
                    totalBytesRead += bytesRead;
                }

                if (signedSerializedRequest.Length != totalBytesRead)
                    Array.Resize(ref signedSerializedRequest, totalBytesRead);

                int encodedSignatureLength = SharedNetworkConfiguration.Encoding.GetByteCount(ConnectionCodes.ConnectionSignature);

                byte[] connectionSignature = new byte[encodedSignatureLength];
                Array.Copy(signedSerializedRequest, connectionSignature, encodedSignatureLength);

                if (!ConnectionCodes.IsSignatureValid(SharedNetworkConfiguration.Encoding.GetString(connectionSignature)))
                    throw new RequestException(ConnectionCodes.InvalidSignature);

                byte[] unsignedSerializedRequest = new byte[signedSerializedRequest.Length - encodedSignatureLength];
                Array.ConstrainedCopy(signedSerializedRequest, encodedSignatureLength, unsignedSerializedRequest, 0, unsignedSerializedRequest.Length);

                ClientRequest request;

                try
                {
                    request = (ClientRequest)NetTransferObject.DeserializeNetObject(unsignedSerializedRequest);
                }
                catch
                {
                    throw new RequestException(ConnectionCodes.InvalidRequest);
                }

                return request;
            }
            else
            {
                throw new SocketException(Convert.ToInt32(SocketError.HostDown));
            }
        }

        /// <summary>
        /// Serverside method. <para />
        /// Accepts incoming client handshake. Receives and handles incoming cookie token. On successful handshake reception, 
        /// passes the client to request handler.
        /// </summary>
        /// <param name="clientSocket">Pending client socket.</param>
        static void HandshakeReceiver(object clientSocket)
        {
            Socket pendingClientSocket = (Socket)clientSocket;
            HandshakeRequest handshakeRequest;

            try
            {
                handshakeRequest = (HandshakeRequest)ReceiveClientRequest(pendingClientSocket);
            }
            catch(RequestException e)
            {
                FileIOStreamer.LogWriter("Connection with " + pendingClientSocket.RemoteEndPoint.ToString() + " failed. Error code: " + e.ExceptionCode);

                pendingClientSocket.Close();
                return;
            }
            catch
            {
                FileIOStreamer.LogWriter("Connection with " + pendingClientSocket.RemoteEndPoint.ToString() + " ended abruptly.");

                pendingClientSocket.Close();
                return;
            }

            string remoteEndPointLogString = pendingClientSocket.RemoteEndPoint.ToString();
            FileIOStreamer.LogWriter("Received handshake request from " + remoteEndPointLogString);

            GenericServerReply serverReply;

            if (handshakeRequest.Type == HandshakeRequest.HandshakeType.OngoingSession)
            {
                FileIOStreamer.LogWriter("Cookie received from " + remoteEndPointLogString);
                FileIOStreamer.LogWriter("Handling fresh session handshake for " + remoteEndPointLogString);

                LoginRequest loginRequest = new LoginRequest(handshakeRequest.Cookie);

                try
                {
                    serverReply = ServerLoginService(loginRequest, pendingClientSocket.RemoteEndPoint);
                }
                catch
                { serverReply = new GenericServerReply(ConnectionCodes.AuthFailure); }
            }
            else
            {
                serverReply = new GenericServerReply(ConnectionCodes.ExpiredSessionStatus);

                FileIOStreamer.LogWriter("Handling expired session handshake for " + remoteEndPointLogString);
            }

            try
            {
                pendingClientSocket.Send(NetTransferObject.SerializeNetObject(serverReply));
            }
            catch(Exception e)
            {
                FileIOStreamer.LogWriter("Handshake failed for " + remoteEndPointLogString + ". Reason: " + e.Message + e.StackTrace);

                pendingClientSocket.Close();
                return;
            }

            FileIOStreamer.LogWriter("Handshake established with " + remoteEndPointLogString);
            StatRecordHandshake();

            int i = connectedClientEndpoints.Count;

            //Try adding the client session until final session id is found
            while(!connectedClientEndpoints.TryAdd(i, pendingClientSocket.RemoteEndPoint))
                i++;

            ClientRequestDispatcher(pendingClientSocket);
        }

        /// <summary>
        /// Serverside method.
        /// Compares client's cookie with live sessions database to confirm identity.
        /// </summary>
        /// <param name="id">User ID</param>
        /// <param name="confirmation">User cookie</param>
        /// <returns>True if session matches server database, otherwise false.</returns>
        static bool IsCookieInDatabase(SharedAPI.Cookie cookie)
        {
            //For sessionContentSplitstrings, index 0 is hash, index 1 is ip

            string[] activeUserSessions;
            activeUserSessions = FileIOStreamer.GetDirectoryFiles(FileIOStreamer.defaultActiveUsersDirectory, false, false);

            if (Array.Exists(activeUserSessions, pendingUser => pendingUser == "id=" + cookie.ID.ToString()))
            {
                string sessionHash = FileIOStreamer.ReadFromFile(FileIOStreamer.defaultActiveUsersDirectory + "id=" + cookie.ID.ToString() +
                                                      ".txt");

                if (sessionHash == cookie.Hash)
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
               throw new RequestException(ConnectionCodes.DatabaseError);
            }
            throw new RequestException(ConnectionCodes.NotFoundError);
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
        static void ClientRequestDispatcher(Socket pendingClientSocket)
        {
            while (pendingClientSocket.Connected == true)
            {
                GenericServerReply serverReply;
                ClientRequest newRequest = null;

                try
                {
                    newRequest = ReceiveClientRequest(pendingClientSocket);
                }
                catch (RequestException e)
                {
                    pendingClientSocket.Send(NetTransferObject.SerializeNetObject(new GenericServerReply(e.ExceptionCode)));
                    FileIOStreamer.LogWriter("Request from " + pendingClientSocket.RemoteEndPoint.ToString()
                        + " could not be processed. Error code: " + e.ExceptionCode);

                    return;
                }
                catch(Exception e)
                {
                    FileIOStreamer.LogWriter("Connection with " + pendingClientSocket.RemoteEndPoint.ToString() + " ended abruptly. Reason: " + e.Message + e.StackTrace);
                    UpdateConnectionDictionary(pendingClientSocket.RemoteEndPoint);
                    pendingClientSocket.Close();

                    return;
                }

                string requestFlag = newRequest.NetFlag;

                FileIOStreamer.LogWriter("Remote request received from " + pendingClientSocket.RemoteEndPoint.ToString() + ".");

                if (RequestDictionary.ContainsKey(requestFlag))
                {
                    try
                    {
                        if (requestFlag == ConnectionCodes.LoginRequest)
                        {
                            LoginRequest loginRequest = (LoginRequest)newRequest;

                            serverReply = (GenericServerReply)RequestDictionary[requestFlag].DynamicInvoke(loginRequest, pendingClientSocket.RemoteEndPoint);
                        }
                        else if (requestFlag != ConnectionCodes.LogOutCall && requestFlag != ConnectionCodes.SignUpRequest)
                        {
                            serverReply = (GenericServerReply)RequestDictionary[requestFlag].DynamicInvoke(newRequest);
                        }
                        else
                        {
                            if (!IsCookieInDatabase(newRequest.Cookie))
                                throw new RequestException(ConnectionCodes.AuthFailure);

                            serverReply = (GenericServerReply)RequestDictionary[requestFlag].DynamicInvoke(pendingClientSocket.RemoteEndPoint.ToString());
                        }
                    }
                    catch
                    {
                        pendingClientSocket.Send(NetTransferObject.SerializeNetObject(new GenericServerReply(ConnectionCodes.InvalidRequest)));
                        FileIOStreamer.LogWriter("Request from " + pendingClientSocket.RemoteEndPoint.ToString() + " of type " + requestFlag
                            + " not recognized.");

                        return;
                    }
                }
                else
                {
                    serverReply = new GenericServerReply(ConnectionCodes.InvalidRequest);
                }

                byte[] streamBytes = NetTransferObject.SerializeNetObject(serverReply);

                pendingClientSocket.Send(streamBytes);
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
        static GenericServerReply ServerLoginService(LoginRequest loginRequest, EndPoint clientEndpoint)
        {
            bool credentialCheckPassed = false;
            int userID = 0;

            GenericServerReply serverReply;

            if (loginRequest.NetFlag == ConnectionCodes.CookieLoginRequest)
            {
                if (IsCookieInDatabase(loginRequest.Cookie))
                {
                    credentialCheckPassed = true;
                    userID = loginRequest.Cookie.ID;
                }
            }
            else
            {
                string[] userData;

                try
                {
                    userData = GetUserData(loginRequest.Login);
                }
                catch
                {
                    return new GenericServerReply(ConnectionCodes.DatabaseError);
                }

                if (userData[0] != ConnectionCodes.NotFoundError)
                {
                    if (loginRequest.Password == userData[3])
                    {
                        credentialCheckPassed = true;
                        userID = Convert.ToInt32(userData[0]);
                    }
                }
            }

            if (credentialCheckPassed)
            {
                SharedAPI.Cookie newSessionCookie = ClientSessionHandler(userID);

                loggedInUserEndpoints.TryAdd(userID, clientEndpoint);

                FileIOStreamer.LogWriter("User id=" + userID.ToString() + " successful login detected.");

                serverReply = new ServerLoginReply(ConnectionCodes.LoginSuccess, newSessionCookie);

                return serverReply;
                //Passes user ID and persistence cookie key back for session update purposes
            }
            else
            {
                serverReply = new GenericServerReply(ConnectionCodes.AuthFailure);

                return serverReply;
            }
        }

        /// <summary>
        /// Serverside method. <para />
        /// Handles received "Sign Up" requests. Adds entries to server database accordingly.
        /// </summary>
        /// <param name="clientRequest">Received client request.</param>
        /// <returns></returns>
        static GenericServerReply ServerSignupService(SignupRequest signUpRequest)
        {         
            string defaultUsersDirectory = FileIOStreamer.defaultRegisteredUsersDirectory; //TEMPORARY
            string[] userData;
            //FOR SAFETY AND AGILITY REASONS, MAKE IT SO THAT DEFAULT USERS LOCATION WOULD BE READ FROM SETTINGS FILE IN THE FUTURE

            GenericServerReply serverReply;
            bool userAlreadyExists = false;

            try
            {
                userData = GetUserData(signUpRequest.Login);
            }
            catch(RequestException e)
            {
                if (e.ExceptionCode == ConnectionCodes.NotFoundError)
                    userAlreadyExists = true;
            }

            if (!userAlreadyExists)
            {
                DateTime dateTime = DateTime.Now;

                int maxID = GetMaxUserID();
                FileIOStreamer.WriteToFile(defaultUsersDirectory + (maxID + 1).ToString() + "login=" + signUpRequest.Login
                + ".txt",
                "name=" + signUpRequest.Name +
                "\npassword=" + signUpRequest.Password +
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
                serverReply = new GenericServerReply(ConnectionCodes.SignUpSuccess);

                return serverReply;
            }

            serverReply = new GenericServerReply(ConnectionCodes.SignUpFailure);

            return serverReply;
        }

        /// <summary>
        /// Serverside method.<para />
        /// Returns main profile information for the user specified in the request.
        /// </summary>
        /// <param name="clientRequest">Received client request.</param>
        /// <returns></returns>
        static GenericServerReply ServerGetUserService(GetUserRequest userSummaryRequest)
        {
            string[] userData = GetUserData(userSummaryRequest.ID.ToString());

            if (userData[0] == ConnectionCodes.NotFoundError)
                return new GenericServerReply(ConnectionCodes.NotFoundError);
            if (userData[0] == ConnectionCodes.DatabaseError)
                return new GenericServerReply(ConnectionCodes.DatabaseError);

            try
            {
                string[] summarySubstrings = userData[6].Split(new string[] { "status=", "main=" }, StringSplitOptions.RemoveEmptyEntries);

                User userResult =
                    new User(Convert.ToInt32(userData[0]), userData[2], userData[1], summarySubstrings[0], summarySubstrings[1], Convert.ToInt32(userData[7]));

                return new ServerGetUserReply(ConnectionCodes.DataRequestSuccess, userResult);
            }
            catch
            {
                return new GenericServerReply(ConnectionCodes.DatabaseError);
            }
        }

        /// <summary>
        /// Serverside method.<para />
        /// Updates main profile information for the specified user. Replaces all occurences of "=" with "[eql_sgn]" to prevent database errors.
        /// </summary>
        /// <param name="clientRequest"></param>
        /// <returns></returns>
        static GenericServerReply ServerEditUserSummaryService(EditSummaryRequest editRequest)
        {
            string newStatus = editRequest.NewUserStatus;
            newStatus = newStatus.Replace("\n", "").Replace("=", "[eqlsgn]");

            string newDescription = editRequest.NewDescription;
            newDescription = newDescription.Replace("\n", "").Replace("=", "[eqlsgn]");

            string summaryString = "\nstatus=" + newStatus + "\nmain=" + newDescription + "\n";

            string[] userData = GetUserData(editRequest.Cookie.ID.ToString());

            FileIOStreamer.RemoveFileEntry(FileIOStreamer.defaultRegisteredUsersDirectory + userData[0] + "login=" + userData[1] + ".txt", "summary==", userData[6], true, true);

            if (summaryString.Substring(summaryString.IndexOf("status=") + 7, summaryString.IndexOf("main=") - (summaryString.IndexOf("status=") + 7)) == "\n")
            {
                    summaryString = summaryString.Insert(summaryString.IndexOf("status=") + 7, "null");
            }

            if (summaryString.Substring(summaryString.IndexOf("main=") + 5, summaryString.Length - (summaryString.IndexOf("main=") + 5)) == "\n")
            {
                    summaryString = summaryString.Insert(summaryString.IndexOf("main=") + 5, "null");
            }

            FileIOStreamer.WriteToFile(FileIOStreamer.defaultRegisteredUsersDirectory + userData[0] + "login=" + userData[1] + ".txt", summaryString, false, "summary==");

            return new GenericServerReply(ConnectionCodes.DescEditSuccess);
        }

        /// <summary>
        /// Serverside method. <para />
        /// Organizes database search and returns results based on the search parameter.
        /// </summary>
        /// <param name="searchParameter"></param>
        /// <returns></returns>
        static GenericServerReply ServerSearchService(SearchRequest searchRequest)
        {
            if (searchRequest.SearchParameter == String.Empty)
            {
                return new GenericServerReply(ConnectionCodes.NotFoundError);
            }

            string defaultUsersDirectory = FileIOStreamer.defaultRegisteredUsersDirectory; //TEMPORARY

            List<string> registeredUsersList = new List<string>(FileIOStreamer.GetDirectoryFiles(defaultUsersDirectory, false, false));
            List<string[]> registeredUsersData = new List<string[]>();
            List<User> matchingUsersData = new List<User>();

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
                    if (userData[i].Length >= searchRequest.SearchParameter.Length 
                        && userData[i].Substring(0, searchRequest.SearchParameter.Length).Equals(searchRequest.SearchParameter, StringComparison.OrdinalIgnoreCase))
                    {
                        matchingUsersData.Add(new User(Convert.ToInt32(userData[0]), userData[2], userData[1]));
                        
                        break;
                    }
                }
            }

            if (matchingUsersData.Count == 0)
                return new GenericServerReply(ConnectionCodes.NotFoundError);

            ServerSearchReply serverReply = new ServerSearchReply(ConnectionCodes.DataRequestSuccess, matchingUsersData);

            return serverReply;
        }

        /// <summary>
        /// Serverside method <para/>
        /// Updates user friendlist for the specified user.
        /// </summary>
        /// <param name="clientRequest"></param>
        /// <returns></returns>
        static GenericServerReply ServerAddFriendService(AddFriendRequest friendRequest)
        {
            string defaultUsersDirectory = FileIOStreamer.defaultRegisteredUsersDirectory; //TEMPORARY

            string[] userData = GetUserData(friendRequest.Cookie.ID.ToString());

            //Check if user already has this friend (probably going to be made redundant in the future)
            string[] fIDSubstrings = userData[8].Split(new string[] { "fid=" }, StringSplitOptions.RemoveEmptyEntries);

            foreach (string fid in fIDSubstrings)
            {
                if (fid == friendRequest.ID.ToString() + "=")
                {
                    return new GenericServerReply(ConnectionCodes.FriendAddFailure);
                }
            }

            FileIOStreamer.WriteToFile(defaultUsersDirectory + userData[0] + "login=" + userData[1] + ".txt", "\nfid=" + friendRequest.ID.ToString() + "=", true, "friends==");
            return new GenericServerReply(ConnectionCodes.FriendAddSuccess);
        }

        /// <summary>
        /// Serverside method.<para/>
        /// Returns the list of all friends as a string in the friendlist of a specified user.
        /// </summary>
        /// <param name="clientRequest"></param>
        /// <returns></returns>
        static GenericServerReply ServerGetFriendsService(GetFriendListRequest friendListRequest)
        {
            string[] userData = GetUserData(friendListRequest.ID.ToString());

            if (!userData[8].Contains("null"))
            {
                string[] friendsListArray;

                userData[8] = userData[8].Replace("=", "");
                friendsListArray = userData[8].Split(new string[] { "fid" }, StringSplitOptions.RemoveEmptyEntries);

                List<User> friendUserDataList = new List<User>();

                foreach (string friend in friendsListArray)
                {
                    string[] friendUserData = (GetUserData(friend, true));
                    //[0] = id, [1] = username, [2] = name

                    friendUserDataList.Add(new User(Convert.ToInt32(friendUserData[0]), friendUserData[2], friendUserData[1]));
                }

                ServerFriendListReply serverReply = new ServerFriendListReply(ConnectionCodes.DataRequestSuccess, friendUserDataList);

                return serverReply;
            }
            else
            {
                return new GenericServerReply(ConnectionCodes.NotFoundError);
            }
        }

        /// <summary>
        /// Serverside method.<para/>
        /// Removes specified friend id from the specified user's friendlist.
        /// </summary>
        /// <param name="clientRequest"></param>
        /// <returns></returns>
        static GenericServerReply ServerRemoveFriendService(RemoveFriendRequest removeFriendRequest)
        {
            string[] userData = GetUserData(removeFriendRequest.Cookie.ID.ToString());

            FileIOStreamer.RemoveFileEntry(FileIOStreamer.defaultRegisteredUsersDirectory + userData[0] + "login=" + userData[1] + ".txt", "friends==", "\nfid=" + removeFriendRequest.FriendID.ToString() + "=");

            return new GenericServerReply(ConnectionCodes.FriendRemSuccess);
        }

        /// <summary>
        /// Serverside method.<para />
        /// Returns a list of all pending chat messages addressed to the requesting user as a string.
        /// </summary>
        /// <param name="clientRequest"></param>
        /// <returns></returns>
        static GenericServerReply ServerGetPendingMessagesService(GetPendingMessagesRequest messagesRequest)
        {
            string[] allPendingMessages = FileIOStreamer.GetDirectoryFiles(FileIOStreamer.defaultPendingMessagesDirectory, false, false);
            string[] messageHandleSplitstrings = new string[3] { "chatid=", "sender=", "rcpnt=" };

            Dictionary<int, List<Message>> dialogues = new Dictionary<int, List<Message>>();

            foreach (string pendingMessage in allPendingMessages)
            {
                string[] pendingMessageSubstrings = pendingMessage.Split(messageHandleSplitstrings, StringSplitOptions.RemoveEmptyEntries);
                //For message handle splits, [0] - server msgid, [1] = sender id, [2] = recipient id

                if (pendingMessageSubstrings[2] == messagesRequest.Cookie.ID.ToString())
                {
                    string messageContent = FileIOStreamer.ReadFromFile(FileIOStreamer.defaultPendingMessagesDirectory + pendingMessage + ".txt");
                    string[] messageContentSubstrings = messageContent.Split(new string[] { "time=", "content=" }, StringSplitOptions.RemoveEmptyEntries);
                    //[0] - message time, [1] - message content

                    Message message
                        = new Message(Convert.ToInt32(pendingMessageSubstrings[0]),
                        messageContentSubstrings[0],Message.MessageStatus.Pending, messageContentSubstrings[1]);

                    if (!dialogues.ContainsKey(Convert.ToInt32(pendingMessageSubstrings[1])))
                        dialogues.Add(Convert.ToInt32(pendingMessageSubstrings[1]), new List<Message>());

                    dialogues[Convert.ToInt32(pendingMessageSubstrings[1])].Add(message);

                    FileIOStreamer.RemoveFile(FileIOStreamer.defaultPendingMessagesDirectory + pendingMessage + ".txt");
                }
            }

            if (dialogues.Count == 0)
            {
                return new GenericServerReply(ConnectionCodes.NoPendingMessagesStatus);
            }
            else
            {
                return new ServerPendingMessagesReply(ConnectionCodes.MessagesPendingStatus, dialogues);
            }
        }

        /// <summary>
        /// Receives message sender, recepient and content, formulates it into a message and loades it into pending messages database.
        /// </summary>
        /// <param name="sender">Message sender</param>
        /// <param name="recepient">Message receipient</param>
        /// <param name="content">Message content</param>
        static void ServerMakeMessagePending(Message message, int senderID, int recepientID)
        {
            string defaultPendingMessagesDirectory = FileIOStreamer.defaultPendingMessagesDirectory;

            try
            {
                int chatID = FileIOStreamer.GetDirectoryFiles(defaultPendingMessagesDirectory, false, false).Length + 1;

                FileIOStreamer.WriteToFile(defaultPendingMessagesDirectory + "chatid=" + chatID + "sender=" + senderID.ToString() + "rcpnt=" + recepientID.ToString() + ".txt",
                    "time=" + DateTime.UtcNow + "\ncontent=" + message.Content);
            }
            catch
            {
                throw new RequestException(ConnectionCodes.MessageSendFailure);
            }
        }

        static void SetDialogueStatus(string newStatus, int senderID, int recipientID)
        {
            string defaultMessageStatusDirectory = FileIOStreamer.defaultMessageStatusDirectory;

            if (newStatus != ConnectionCodes.MessagesReadStatus &&
                newStatus != ConnectionCodes.MessagesReceivedStatus &&
                newStatus != ConnectionCodes.MessagesPendingStatus)
                throw new RequestException(ConnectionCodes.InvalidRequest);

            try
            {                
                FileIOStreamer.WriteToFile(defaultMessageStatusDirectory + "sender=" + senderID.ToString() + "rcpnt=" + recipientID.ToString() + ".txt",
                    "status=" + newStatus.ToString());
            }
            catch
            {
                throw new RequestException(ConnectionCodes.DatabaseError);
            }
        }

        static string GetDialogueStatus(int senderID, int recipientID)
        {
            string defaultMessageStatusDirectory = FileIOStreamer.defaultMessageStatusDirectory;

            try
            {
                string status = FileIOStreamer.ReadFromFile(defaultMessageStatusDirectory + "sender=" + senderID.ToString() + "rcpnt=" + recipientID.ToString() + ".txt");

                if (status.Contains("status="))
                    status = status.Replace("status=", "");

                //if (status == ConnectionCodes.MessagesReadStatus)
                  //  FileIOStreamer.RemoveFile(defaultMessageStatusDirectory + "sender=" + senderID.ToString() + "rcpnt=" + recipientID.ToString() + ".txt");
                return status;
            }
            catch
            {
                throw new RequestException(ConnectionCodes.DatabaseError);
            }
        }

        /// <summary>
        /// Passes message from sender to server and attempts to notify the receipient of a new message.
        /// </summary>
        /// <param name="clientRequest"></param>
        /// <returns></returns>
        static GenericServerReply ServerSendMessageService(SendMessageRequest messageRequest)
        {
            //Check if user texted himself to prevent further server action
            {
                if (messageRequest.MessageSenderID == messageRequest.MessageRecipientID)
                {
                    return new GenericServerReply(ConnectionCodes.MsgToSelfStatus);
                }
            }

            //Record message into pending messages
            try
            {
                ServerMakeMessagePending(messageRequest.Message, messageRequest.MessageSenderID, messageRequest.MessageRecipientID);
                SetDialogueStatus(ConnectionCodes.MessagesPendingStatus, messageRequest.MessageSenderID, messageRequest.MessageRecipientID);
            }
            catch(RequestException e)
            {
                return new GenericServerReply(e.ExceptionCode);
            }

            //Tries finding user in logged in list of logged in users to attempt sending pending message call
            if(loggedInUserEndpoints.ContainsKey(messageRequest.MessageRecipientID))
            {
                ServerUDPRequest serverRequest = new ServerUDPRequest(ConnectionCodes.MessagesPendingStatus, messageRequest.MessageSenderID);
                byte[] serializedRequest = NetTransferObject.SerializeNetObject(serverRequest);

                EndPoint recepientEndPoint = loggedInUserEndpoints[messageRequest.MessageRecipientID];

                SharedNetworkConfiguration.AuxilarryUDPSocket.SendTo(serializedRequest, recepientEndPoint);
            }

            return new GenericServerReply(ConnectionCodes.MsgSendSuccess);
        }

        static GenericServerReply ServerSetDialogueStatusService(ChangeDialogueStatusRequest request)
        {
            if(request.NewDialogueStatus != ConnectionCodes.RecipientFormingReplyStatus &&
                request.NewDialogueStatus != ConnectionCodes.RecipientStoppedFormingReplyStatus)
                SetDialogueStatus(request.NewDialogueStatus, request.MessageSenderID, request.Cookie.ID);

            try
            {
                if (loggedInUserEndpoints.ContainsKey(request.MessageSenderID))
                {
                    ServerUDPRequest serverRequest = new ServerUDPRequest(request.NewDialogueStatus, request.Cookie.ID);
                    byte[] serializedRequest = NetTransferObject.SerializeNetObject(serverRequest);

                    EndPoint recepientEndPoint = loggedInUserEndpoints[request.MessageSenderID];

                    SharedNetworkConfiguration.AuxilarryUDPSocket.SendTo(serializedRequest, recepientEndPoint);
                }                
            }
            catch
            {
                return new GenericServerReply(ConnectionCodes.DatabaseError);
            }

            return new GenericServerReply(ConnectionCodes.DataRequestSuccess);
        }

        static GenericServerReply ServerGetDialogueStatusService(GetDialogueStatusRequest request)
        {
            try
            {
                return new GenericServerReply(GetDialogueStatus(request.Cookie.ID, request.MessageSenderID));
            }
            catch(RequestException e)
            {
                return new GenericServerReply(e.ExceptionCode);
            }
        }

        /// <summary>
        /// Manages client password change requests.
        /// </summary>
        /// <param name="clientRequest"></param>
        /// <returns></returns>
        public static GenericServerReply ServerChangePasswordService(ChangePasswordRequest changePasswordRequest)
        {
            string[] userData = GetUserData(changePasswordRequest.Cookie.ID.ToString());

            if (userData[3] == changePasswordRequest.OldPassword)
            {
                FileIOStreamer.SwapFileEntry(FileIOStreamer.defaultRegisteredUsersDirectory + userData[0] + "login=" + userData[1] + ".txt", "password=", "", changePasswordRequest.NewPassword, false, true);

                return new GenericServerReply(ConnectionCodes.PswdChgSuccess);
            }
            else
            {
                return new GenericServerReply(ConnectionCodes.PswdChgFailure);
            }
        }

        /// <summary>
        /// Manages client name change requests.
        /// </summary>
        /// <param name="clientRequest"></param>
        /// <returns></returns>
        public static GenericServerReply ServerChangeNameService(ChangeNameRequest changeNameRequest)
        {
            string[] userData = GetUserData(changeNameRequest.Cookie.ID.ToString());

            try
            {
                FileIOStreamer.SwapFileEntry(FileIOStreamer.defaultRegisteredUsersDirectory + userData[0] + "login=" + userData[1] + ".txt", "name=", "", changeNameRequest.NewName, false, true);
            }
            catch
            {
                return new GenericServerReply(ConnectionCodes.PswdChgFailure);
            }

            return new GenericServerReply(ConnectionCodes.NmChgSuccess);
        }

        /// <summary>
        /// Serverside method. <para />
        /// [work in progress] Generates hash from randomly created seed for the specified user ID. Generates new live session file for
        /// pending user connection.
        /// </summary>
        /// <param name="userID">Pending log in user ID.</param>
        /// <param name="hashSeed">Hash generator seed.</param>
        static SharedAPI.Cookie ClientSessionHandler(int userID)
        {
            Random randomGenerator = new Random();
            int randomHash = randomGenerator.Next(99999999);

            //PERSISTENCE COOKIE HASH GENERATOR WILL BE HERE

            //PERSONAL HASH WOULD BE WRITTEN IN THE USER SESSION FILE BELOW
            FileIOStreamer.ClearFile(FileIOStreamer.defaultActiveUsersDirectory + "id=" + userID.ToString() + ".txt");
            FileIOStreamer.WriteToFile(FileIOStreamer.defaultActiveUsersDirectory + "id=" + userID.ToString() + ".txt", randomHash.ToString(), true);

            return new SharedAPI.Cookie(userID, randomHash.ToString());
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
                int[] activeUserIDsArray = new int[loggedInUserEndpoints.Count];
                loggedInUserEndpoints.Keys.CopyTo(activeUserIDsArray, 0);

                FileIOStreamer.FileTimeOutComparator(FileIOStreamer.defaultActiveUsersDirectory, activeUserIDsArray, timeOutSpan);

                Thread.Sleep(1000);
            }
        }

        /// <summary>
        /// Updates currently connected and logged in users collection.
        /// </summary>
        /// <param name="userIP"></param>
        static void UpdateConnectionDictionary(EndPoint userEndpoint)
        {
            EndPoint outEndPoint;

            foreach (var connectedClient in connectedClientEndpoints)
            {
                if (connectedClient.Value == userEndpoint)
                {
                    connectedClientEndpoints.TryRemove(connectedClient.Key, out outEndPoint);
                }
            }

            foreach (var loggedInUser in loggedInUserEndpoints)
            {
                if (loggedInUser.Value == userEndpoint)
                {
                    loggedInUserEndpoints.TryRemove(loggedInUser.Key, out userEndpoint);
                }
            }
        }
    }
}
