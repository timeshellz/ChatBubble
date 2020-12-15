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

namespace ChatBubbleClientWPF
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
        string serverReply = String.Empty;

        public ClientStartup()
        {
            //InitiateStartup();
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

        public string ServerReply
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
            string[] serverAddress = configFile.AppSettings.Settings["serverAddress"].Value.Split(':');

            NetComponents.ClientSetServerEndpoints(serverAddress[0], Convert.ToInt32(serverAddress[1]));

            while (connectionStatus != ConnectionStatuses.Connected)
            {
                string handshakeResult = NetComponents.InitialHandshakeClient();

                if (attemptNumber >= 50 || handshakeResult == NetComponents.ConnectionCodes.ConnectionFailure)
                {
                    ConnectionStatus = ConnectionStatuses.ConnectionFailed;

                    try
                    {
                        NetComponents.BreakBind(false);
                    }
                    catch { }

                    return;
                }

                if (handshakeResult == NetComponents.ConnectionCodes.ExpiredSessionStatus)
                {
                    ConnectionStatus = ConnectionStatuses.Connected;
                    ConnectionType = ConnectionTypes.Expired;
                }
                else if (handshakeResult.Substring(0, NetComponents.ConnectionCodes.DefaultFlagLength) == NetComponents.ConnectionCodes.LoginSuccess)
                {
                    ConnectionStatus = ConnectionStatuses.Connected;
                    ConnectionType = ConnectionTypes.Fresh;
                    ServerReply = handshakeResult;
                }
                else
                {
                    ConnectionStatus = ConnectionStatuses.Connecting;
                    attemptNumber++;
                }
            }
        }

        protected void OnPropertyChanged([CallerMemberName] string propertyName="")
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}
