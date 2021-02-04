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

using ChatBubble;
using ChatBubble.FileManager;
using ChatBubble.SharedAPI;
using ChatBubble.ClientAPI;

namespace ChatBubbleClientWPF.Models
{
    class ClientStartup : INotifyPropertyChanged
    {
        static readonly Configuration configFile = ConfigurationManager.OpenExeConfiguration(Assembly.GetExecutingAssembly().Location);
        public event PropertyChangedEventHandler PropertyChanged;

        public enum ConnectionStatuses { NotConnected, Connecting, Connected, ConnectionFailed}
        public enum ConnectionTypes { None, Expired, Fresh }

        ConnectionStatuses connectionStatus = ConnectionStatuses.NotConnected;
        ConnectionTypes connectionType = ConnectionTypes.None;

        int attemptNumber = 2;
        ServerLoginReply serverReply;

        FileManager fileManager;

        public ClientStartup()
        {
            fileManager = new FileManager();
        }

        public ConnectionStatuses ConnectionStatus
        {
            get { return connectionStatus; }
            private set
            {
                connectionStatus = value;
                OnPropertyChanged();
            }
        }

        public ConnectionTypes ConnectionType
        {
            get { return connectionType; }
            private set
            {
                connectionType = value;
                OnPropertyChanged();
            }
        }
        
        public int AttemptNumber
        {
            get { return attemptNumber; }
            private set
            {
                attemptNumber = value;
                OnPropertyChanged();
            }
        }

        public ServerLoginReply ServerReply
        {
            get { return serverReply; }
            private set
            {
                serverReply = value;
                OnPropertyChanged();
            }
        }

        public void InitiateStartup()
        {
            Dictionary<int, object> fileContents = new Dictionary<int, object>();
            HandshakeRequest handshakeRequest;

            string cookiePath = ClientDirectories.DirectoryDictionary[ClientDirectories.DirectoryType.Cookies]
                    + @"cookie" + FileExtensions.GetExtensionForFileType(FileExtensions.FileType.Cookie);

            try
            {
                fileContents = fileManager.ReadFromFile(cookiePath);

                handshakeRequest = new HandshakeRequest((Cookie)fileContents.Values.First());
            }
            catch
            {
                handshakeRequest = new HandshakeRequest();
            }   

            while (ConnectionStatus != ConnectionStatuses.Connected)
            {
                GenericServerReply handshakeResult = null;

                try
                {
                    handshakeResult = ClientRequestManager.PerformHandshake(handshakeRequest);
                }
                catch(RequestException e)
                {
                    if (e.ExceptionCode == ConnectionCodes.ConnectionFailure)
                        ConnectionStatus = ConnectionStatuses.ConnectionFailed;
                    if (e.ExceptionCode == ConnectionCodes.ConnectionTimeoutStatus)
                    {
                        ConnectionStatus = ConnectionStatuses.Connecting;
                        attemptNumber++;
                    }

                    if (attemptNumber >= 50)
                        ConnectionStatus = ConnectionStatuses.ConnectionFailed;
                }

                if (ConnectionStatus == ConnectionStatuses.ConnectionFailed)
                {
                    try
                    {
                        ClientNetworkConfigurator networkConfigurator = new ClientNetworkConfigurator();
                        networkConfigurator.DisconnectSockets(false);
                    }
                    catch { }

                    return;
                }

                if (handshakeResult != null)
                {
                    if (handshakeResult.NetFlag == ConnectionCodes.ExpiredSessionStatus || handshakeResult.NetFlag == ConnectionCodes.AuthFailure)
                    {
                        ConnectionStatus = ConnectionStatuses.Connected;
                        ConnectionType = ConnectionTypes.Expired;
                    }
                    else if (handshakeResult.NetFlag == ConnectionCodes.LoginSuccess && handshakeResult is ServerLoginReply loginReply)
                    {
                        ServerReply = loginReply;
                        ConnectionStatus = ConnectionStatuses.Connected;
                        ConnectionType = ConnectionTypes.Fresh;
                    }
                }
            }
        }

        protected void OnPropertyChanged([CallerMemberName] string propertyName="")
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}
