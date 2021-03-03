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
using ChatBubble.FileManager;
using ChatBubble.SharedAPI;
using ChatBubble.ClientAPI;

namespace ChatBubbleClientWPF.Models
{
    class ClientFrontDoor : INotifyPropertyChanged
    {
        public event PropertyChangedEventHandler PropertyChanged;
        FileManager fileManager = new FileManager();
        public enum SuccessStatuses { LoginSuccess, SignupSuccess, GenericFailure, CredentialFailure,
            IncorrectNameFailure, IncorrectUsernameFailure, IncorrectPasswordFailure, PasswordMismatchFailure,
            UsernameExistsFailure }

        Cookie loggedInUserCookie;
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

        public Cookie LoggedInUserCookie
        {
            get { return loggedInUserCookie; }
            set
            {
                loggedInUserCookie = value;
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

            LoginRequest clientRequest = new LoginRequest(username, password);
            GenericServerReply serverReply = ClientRequestManager.SendClientRequest(clientRequest);

            if (serverReply is ServerLoginReply loginReply)
            {
                if (loginReply.NetFlag == ConnectionCodes.LoginFailure)
                {
                    SuccessStatus = SuccessStatuses.CredentialFailure;
                }
                if (loginReply.NetFlag == ConnectionCodes.ConnectionFailure)
                {
                    SuccessStatus = SuccessStatuses.GenericFailure;
                }
                if (loginReply.NetFlag == ConnectionCodes.LoginSuccess)
                {
                    HandleLoginReply(loginReply);
                    SuccessStatus = SuccessStatuses.LoginSuccess;
                }
            }
            else
                SuccessStatus = SuccessStatuses.GenericFailure;
        }

        public void HandleLoginReply(ServerLoginReply serverReply)
        {
            LoggedInUserCookie = serverReply.NewCookie;

            string cookiePath = ClientDirectories.DirectoryDictionary[ClientDirectories.DirectoryType.Cookies]
            + @"\cookie" + FileExtensions.GetExtensionForFileType(FileExtensions.FileType.Cookie);

            fileManager.TryDeleteFile(cookiePath);
            fileManager.AppendToFile(cookiePath, serverReply.NewCookie, 0);           
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

            SignupRequest clientRequest = new SignupRequest(name, username, password);
            GenericServerReply serverReply = ClientRequestManager.SendClientRequest(clientRequest);

            if (serverReply.NetFlag == ConnectionCodes.SignUpFailure)
            {
                SuccessStatus = SuccessStatuses.UsernameExistsFailure;
                return;
            }
            if (serverReply.NetFlag == ConnectionCodes.DatabaseError)
            {
                SuccessStatus = SuccessStatuses.GenericFailure;
                return;
            }
            if (serverReply.NetFlag == ConnectionCodes.SignUpSuccess)
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
