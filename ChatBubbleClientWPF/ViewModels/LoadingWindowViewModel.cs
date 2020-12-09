using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Reflection;
using System.Windows;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Windows.Threading;
using System.Threading;
using ChatBubble;

namespace ChatBubbleClientWPF
{
    class LoadingWindowViewModel : BaseViewModel
    {
        ClientStartup clientStartupModel;

        public event EventHandler<ConnectionEventArgs> ConnectionEstablished;

        string connectionStatusString = String.Empty;

        public string ConnectionStatusString
        {
            get { return connectionStatusString; }
            set
            {
                connectionStatusString = value;
                OnPropertyChanged();
            }
        }

        public LoadingWindowViewModel()
        {
            clientStartupModel = new ClientStartup();
            clientStartupModel.PropertyChanged += new PropertyChangedEventHandler(OnModelPropertyChanged);   
        }

        public void InitializeClientLogic()
        {
            Thread logicThread = new Thread(clientStartupModel.InitiateStartup);
            logicThread.Start();
        }

        void OnModelPropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            switch(e.PropertyName)
            {
                case nameof(clientStartupModel.ConnectionStatus):
                    ModifyConnectionStatus(clientStartupModel.ConnectionStatus);
                    break;
                case nameof(clientStartupModel.ConnectionType):
                    HandleConnectionType(clientStartupModel.ConnectionType);
                    break;
            }
        }

        void ModifyConnectionStatus(ClientStartup.ConnectionStatuses status)
        {
            if(status == ClientStartup.ConnectionStatuses.Connecting)
            {
                if(clientStartupModel.AttemptNumber > 2)
                {
                    ConnectionStatusString = "Connecting, attempt " + clientStartupModel.AttemptNumber + "...";
                }
                else ConnectionStatusString = "Connecting...";
            }
            if(status == ClientStartup.ConnectionStatuses.ConnectionFailed)
            {
                ConnectionStatusString = "Connection failed. Please try again later.";
            }
        }

        void HandleConnectionType(ClientStartup.ConnectionTypes conType)
        {
            if(conType == ClientStartup.ConnectionTypes.Expired)
            {
                //Go to login page
                ConnectionStatusString = "Connected!";
                Thread.Sleep(1000);

                Application.Current.Dispatcher.BeginInvoke(DispatcherPriority.Normal, 
                    new Action(() => OnConnectionEstablished(this, new ConnectionEventArgs() { ConnectionType = ConnectionEventArgs.ConnectionTypes.Expired })));
            }
            if(conType == ClientStartup.ConnectionTypes.Fresh)
            {
                OnConnectionEstablished(this, new ConnectionEventArgs() { ConnectionType = ConnectionEventArgs.ConnectionTypes.Fresh });
                //Go to mainwindow
            }
        }

        void OnConnectionEstablished(object sender, ConnectionEventArgs e)
        {
            ConnectionEstablished?.Invoke(sender, e);
        }
    }

    class ConnectionEventArgs : EventArgs
    {
        public enum ConnectionTypes { Expired, Fresh, None }
        public ConnectionTypes ConnectionType { get; set; } = ConnectionTypes.None;
    }
}
