using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Configuration;
using System.Reflection;
using System.Windows;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Security;
using System.Runtime.InteropServices;
using ChatBubble;

namespace ChatBubbleClientWPF.Models
{
    class ClientFrontDoor : INotifyPropertyChanged
    {
        public event PropertyChangedEventHandler PropertyChanged;

        public enum SuccessStatuses { LoginSuccess, SignupSuccess, GenericFailure, CredentialFailure,
            IncorrectNameFailure, IncorrectUsernameFailure, IncorrectPasswordFailure, PasswordMismatchFailure,
            UsernameExistsFailure }

        string loggedInUserID;
        SuccessStatuses successStatus;

        public SuccessStatuses SuccessStatus
        {
            get { return successStatus; }
            set
            {
                successStatus = value;
                OnPropertyChanged();
            }
        }      

        public string LoggedInUserID
        {
            get { return loggedInUserID; }
            set
            {
                loggedInUserID = value;
                OnPropertyChanged();
            }
        }

        char[] restrictedSymbols = new char[1] { '=', };

        bool ContainsRestrictedSymbols(string text)
        {
            foreach (char symbol in restrictedSymbols)
            {
                if (text.Contains(symbol))
                {
                    return (true);
                }
            }

            return (false);
        }

        bool ContainsRestrictedSymbols(SecureString text)
        {
            string nonsecureText = text.ToString();
            foreach (char symbol in restrictedSymbols)
            {
                if (nonsecureText.Contains(symbol))
                {
                    return (true);
                }
            }

            return (false);
        }

        public void AttemptLogin(string username, SecureString securePassword)
        {
            string password = ConvertSecureString(securePassword);

            if (ContainsRestrictedSymbols(username))
            {
                SuccessStatus = SuccessStatuses.IncorrectUsernameFailure;
                return;
            }
            if (ContainsRestrictedSymbols(password))
            {
                SuccessStatus = SuccessStatuses.IncorrectPasswordFailure;
                return;
            }

            if (username == "" || password.Length == 0)
            {
                return;
            }

            string serverReply;
            serverReply = NetComponents.LogPasRequestClientside(username, password);

            serverReply = serverReply.Substring(0, serverReply.IndexOf('\0'));
            serverReply = LoginReplyHandler(serverReply);

            if (serverReply == NetComponents.ConnectionCodes.LoginFailure)
            {
                SuccessStatus = SuccessStatuses.CredentialFailure;
            }
            if(serverReply == NetComponents.ConnectionCodes.ConnectionFailure)
            {
                SuccessStatus = SuccessStatuses.GenericFailure;
            }
            if(serverReply == NetComponents.ConnectionCodes.LoginSuccess)
            {
                SuccessStatus = SuccessStatuses.LoginSuccess;
            }
        }

        public string LoginReplyHandler(string serverReply)
        {
            string[] serverReplySplitStrings = new string[2] { "id=", "hash=" };

            //For serverReplySubstrings, index 0 is server flag, index 1 is ID, index 2 is hash
            string[] serverReplySubstrings = serverReply.Split(serverReplySplitStrings, 3, StringSplitOptions.RemoveEmptyEntries);

            if (serverReplySubstrings[0] == NetComponents.ConnectionCodes.LoginSuccess)
            {
                LoggedInUserID = serverReplySubstrings[1];

                //Set local user directory for logged in user
                FileIOStreamer.SetLocalUserDirectory(serverReplySubstrings[1]);

                //Create a cookie in local client directory to keep user logged in
                FileIOStreamer.ClearFile(FileIOStreamer.defaultLocalCookiesDirectory + "persistenceCookie.txt");
                FileIOStreamer.WriteToFile(FileIOStreamer.defaultLocalCookiesDirectory + "persistenceCookie.txt", "id=" +
                    serverReplySubstrings[1] + "confirmation=" + serverReplySubstrings[2], true);

                NetComponents.ClientPendingMessageManager();

                Thread messageReceiverThread = new Thread(NetComponents.ClientServerFlagListener);
                messageReceiverThread.Start();

                return (NetComponents.ConnectionCodes.LoginSuccess);
            }
            else if (serverReply == NetComponents.ConnectionCodes.LoginFailure)
            {
                return (NetComponents.ConnectionCodes.LoginFailure);
            }
            else
            {
                return (NetComponents.ConnectionCodes.ConnectionFailure);

            }
        }


        public void AttemptSignup(string name, string username, SecureString securePassword, SecureString secureRepeatPassword)
        {
            string password = ConvertSecureString(securePassword);
            string repeatPassword = ConvertSecureString(secureRepeatPassword);

            if (ContainsRestrictedSymbols(name))
            {
                SuccessStatus = SuccessStatuses.IncorrectNameFailure;
                return;
            }
            if (ContainsRestrictedSymbols(username))
            {
                SuccessStatus = SuccessStatuses.IncorrectUsernameFailure;
                return;
            }
            if (ContainsRestrictedSymbols(password))
            {
                SuccessStatus = SuccessStatuses.IncorrectPasswordFailure;
                return;
            }
            if (password != repeatPassword)
            {
                SuccessStatus = SuccessStatuses.PasswordMismatchFailure;
                return;
            }

            if (username == "" || password.Length == 0 || name == "")
            {
                return;
            }

            string serverReply = NetComponents.SignUpRequestClientside(name, username, password);
            string[] serverReplySplitStrings = new string[2] { "id=", "hash=" };

            serverReply = serverReply.Substring(0, serverReply.IndexOf('\0'));
            string[] serverReplySubstrings = serverReply.Split(serverReplySplitStrings, 2, StringSplitOptions.RemoveEmptyEntries);
            //It isn't necessary to split strings at this point of functionality, but it might be needed in the future

            if (serverReplySubstrings[0] == NetComponents.ConnectionCodes.SignUpFailure)
            {
                SuccessStatus = SuccessStatuses.UsernameExistsFailure;
                return;
            }
            if (serverReplySubstrings[0] == NetComponents.ConnectionCodes.DatabaseError)
            {
                SuccessStatus = SuccessStatuses.GenericFailure;
                return;
            }
            if (serverReplySubstrings[0] == NetComponents.ConnectionCodes.SignUpSuccess)
            {
                SuccessStatus = SuccessStatuses.SignupSuccess;
                return;
            }

            return;
        }

        protected void OnPropertyChanged([CallerMemberName] string propertyName = "")
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        private string ConvertSecureString(SecureString sString)
        {
            IntPtr valuePtr = IntPtr.Zero;
            try
            {
                valuePtr = Marshal.SecureStringToGlobalAllocUnicode(sString);
                return Marshal.PtrToStringUni(valuePtr);
            }
            finally
            {
                Marshal.ZeroFreeGlobalAllocUnicode(valuePtr);
            }
        }
    }
}
