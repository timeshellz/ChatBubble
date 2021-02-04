using System;

namespace ChatBubble.SharedAPI
{
    /// <summary>
    /// Class that represents and generates Connection Flags based on all existing error, success and status conditions, defined as fields.
    /// </summary>
    public static class ConnectionCodes
    {
        public static readonly string HandshakeRequest, LoginRequest, CookieLoginRequest, SignUpRequest, SearchRequest, AddFriendRequest, GetFriendListRequest, RemoveFriendRequest,
        GetUserSummaryRequest, EditUserSummaryRequest, GetPendingMessageRequest, SendNewMessageRequest, ChangeNameRequest, ChangePasswdRequest,
        FreshSessionStatus, ExpiredSessionStatus, MsgToSelfStatus, AvailablePendingMessagesStatus, NoPendingMessagesStatus, ConnectionTimeoutStatus,
        DataRequestSuccess, LoginSuccess, SignUpSuccess, FriendAddSuccess, FriendRemSuccess, DescEditSuccess, MsgSendSuccess, PswdChgSuccess, NmChgSuccess, LoginFailure,
        SignUpFailure, FriendAddFailure, MessageSendFailure, AuthFailure, ConnectionFailure, PswdChgFailure, NmChgFailure, NotFoundError, DatabaseError,
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
                        if (fields[i].Name.Contains("Error"))
                        {
                            flagLabel = "E";
                        }
                        if (fields[i].Name.Contains("Status"))
                        {
                            flagLabel = "S";
                        }
                        if (fields[i].Name.Contains("Failure"))
                        {
                            flagLabel = "F";
                        }
                        if (fields[i].Name.Contains("Success"))
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

            for (int i = 0; i < fields.Length; i++)
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
}