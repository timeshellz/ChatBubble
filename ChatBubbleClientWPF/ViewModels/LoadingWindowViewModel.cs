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
using System.Windows.Input;

using ChatBubble;
using ChatBubble.SharedAPI;

namespace ChatBubbleClientWPF.ViewModels
{
    class LoadingWindowViewModel : BaseViewModel
    {
        Models.ClientStartup clientStartupModel;

        public event EventHandler<Utility.ConnectionEventArgs> ConnectionFailed;      

        string connectionStatusString = String.Empty;

        ICommand closeViewModelCommand;

        public ICommand CloseViewModelCommand
        {
            get
            {
                if (closeViewModelCommand == null)
                {
                    closeViewModelCommand = new Command(p => OnViewModelClosing());
                }
                return closeViewModelCommand;
            }
        }

        public string ConnectionStatusString
        {
            get { return connectionStatusString; }
            set
            {
                connectionStatusString = value;
                OnPropertyChanged();
            }
        }

        public LoadingWindowViewModel(Utility.IWindowFactory windowFactory)
        {
            this.windowFactory = windowFactory;

            clientStartupModel = new Models.ClientStartup();
            clientStartupModel.PropertyChanged += new PropertyChangedEventHandler(OnModelPropertyChanged);

            this.windowFactory.OpenAssociatedWindow(this);

            InitializeClientLogic();
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

        void ModifyConnectionStatus(Models.ClientStartup.ConnectionStatuses status)
        {
            if(status == Models.ClientStartup.ConnectionStatuses.Connecting)
            {
                if(clientStartupModel.AttemptNumber > 2)
                {
                    ConnectionStatusString = "Connecting, attempt " + clientStartupModel.AttemptNumber + "...";
                }
                else ConnectionStatusString = "Connecting...";
            }
            if(status == Models.ClientStartup.ConnectionStatuses.ConnectionFailed)
            {
                ConnectionStatusString = "Connection failed. Please try again later.";
                OnConnectionFailed(this, new Utility.ConnectionEventArgs());
            }
        }

        void HandleConnectionType(Models.ClientStartup.ConnectionTypes conType)
        {
            if(conType == Models.ClientStartup.ConnectionTypes.Expired)
            {
                //Go to login page
                ConnectionStatusString = "Connected!";
                Thread.Sleep(1000);

                Application.Current.Dispatcher.BeginInvoke(DispatcherPriority.Normal, new Action(() => CreateLoginViewModel()));

            }
            if(conType == Models.ClientStartup.ConnectionTypes.Fresh)
            {
                ConnectionStatusString = "Logged in!";
                Thread.Sleep(1000);

                Cookie newCookie;

                RecordFreshSession(out newCookie);
                Application.Current.Dispatcher.BeginInvoke(DispatcherPriority.Normal, new Action(() => CreateMainViewModel(newCookie)));
                //Go to mainwindow
            }
        }

        void RecordFreshSession(out Cookie cookie)
        {
            Models.ClientFrontDoor clientFront = new Models.ClientFrontDoor();
            clientFront.HandleLoginReply(clientStartupModel.ServerReply);
            cookie = clientFront.LoggedInUserCookie;
        }

        void CreateLoginViewModel()
        {
            windowFactory.WindowRendered += (o, e) => OnViewModelClosing();

            LoginWindowViewModel loginWindowViewModel = new LoginWindowViewModel(windowFactory);
        }

        void CreateMainViewModel(Cookie userCookie)
        {
            windowFactory.WindowRendered += (o, e) => OnViewModelClosing();

            MainWindowViewModel mainWindowViewModel = new MainWindowViewModel(windowFactory, new Utility.PageFactory(), userCookie);
        }

        void OnConnectionFailed(object sender, Utility.ConnectionEventArgs e)
        {
            ConnectionFailed?.Invoke(sender, e);
        }
    }
}
