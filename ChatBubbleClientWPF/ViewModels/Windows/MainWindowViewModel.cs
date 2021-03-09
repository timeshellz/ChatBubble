using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Reflection;
using System.Windows;
using System.Windows.Threading;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Windows.Input;
using System.Security;

using ChatBubble;
using ChatBubble.SharedAPI;
using ChatBubble.ClientAPI;

using ChatBubbleClientWPF.Models;
using ChatBubbleClientWPF.ViewModels.Basic;

namespace ChatBubbleClientWPF.ViewModels.Windows
{
    class MainWindowViewModel : BaseViewModel
    {
        public event EventHandler<TabNavigationEventArgs> TabSwitchPrompted;
        public event EventHandler<EventArgs> TabReturnPrompted;

        ObservableCollection<Basic.NotificationViewModel> notificationViewModels;
        BaseViewModel currentTabViewModel;


        public ServerFlagReceiver FlagReceiver { get; private set; }
        public event EventHandler<ServerFlagEventArgs> MessageFlagReceived;

        public ObservableCollection<Basic.NotificationViewModel> NotificationViewModels
        {
            get { return notificationViewModels; }
            set
            {
                if (notificationViewModels != value)
                {
                    notificationViewModels = value;
                    OnPropertyChanged();
                }
            }
        }

        public BaseViewModel CurrentTabViewModel
        {
            get { return currentTabViewModel; }
            set
            {
                currentTabViewModel = value;
                OnPropertyChanged();
            }
        }

        Models.CurrentUser currentUser;

        Utility.IPageFactory pageFactory;

        ICommand openMainTabCommand;
        ICommand openFriendsTabCommand;
        ICommand openDialoguesTabCommand;
        ICommand openActiveDialogueCommand;
        ICommand openSearchTabCommand;
        ICommand openSettingsTabCommand;
        ICommand openLogOutTabCommand;

        ICommand tabReturnCommand;

        ICommand closeSessionCommand;
        ICommand minimizeWindowCommand;
        ICommand maximizeWindowCommand;

        public ICommand OpenMainTabCommand
        {
            get
            {
                if (openMainTabCommand == null)
                {
                    openMainTabCommand = new Command(p => OnMainTabPrompted(p));
                }
                return openMainTabCommand;
            }
        }
        
        public ICommand OpenFriendsTabCommand
        {
            get
            {
                if (openFriendsTabCommand == null)
                {
                    openFriendsTabCommand = new Command(p => OnFriendsTabPrompted());
                }
                return openFriendsTabCommand;
            }
        }

        public ICommand OpenDialoguesTabCommand
        {
            get
            {
                if (openDialoguesTabCommand == null)
                {
                    openDialoguesTabCommand = new Command(p => OnDialoguesTabPrompted());
                }
                return openDialoguesTabCommand;
            }
        }

        public ICommand OpenActiveDialogueCommand
        {
            get
            {
                if (openActiveDialogueCommand == null)
                {
                    openActiveDialogueCommand = new Command(p => OnActiveDialoguePrompted(p));
                }
                return openActiveDialogueCommand;
            }
        }

        public ICommand OpenSearchTabCommand
        {
            get
            {
                if (openSearchTabCommand == null)
                {
                    openSearchTabCommand = new Command(p => OnSearchTabPrompted());
                }
                return openSearchTabCommand;
            }
        }

        public ICommand OpenSettingsTabCommand
        {
            get
            {
                if (openSettingsTabCommand == null)
                {
                    openSettingsTabCommand = new Command(p => OnSettingsTabPrompted());
                }
                return openSettingsTabCommand;
            }
        }

        public ICommand OpenLogOutCommand
        {
            get
            {
                if (openLogOutTabCommand == null)
                {
                    openLogOutTabCommand = new Command(p => OnLogOutTabPrompted());
                }
                return openLogOutTabCommand;
            }
        }

        public ICommand TabReturnCommand
        {
            get
            {
                if (tabReturnCommand == null)
                {
                    tabReturnCommand = new Command(p => OnTabReturnPrompted());
                }
                return tabReturnCommand;
            }
        }

        public ICommand CloseSessionCommand
        {
            get
            {
                if (closeSessionCommand == null)
                {
                    closeSessionCommand = new Command(p => OnSessionClosure());
                }
                return closeSessionCommand;
            }
        }

        public Models.CurrentUser CurrentUser
        {
            get { return currentUser; }
            set
            {
                currentUser = value;
                OnPropertyChanged();
            }
        }

        public MainWindowViewModel(Utility.IWindowFactory windowFactory, Utility.IPageFactory pageFactory, Cookie userCookie)
        {
            CurrentUser = new Models.CurrentUser(userCookie);
            CurrentTabViewModel = new MainTab.MainTabViewModel(this, 0);

            this.windowFactory = windowFactory;
            this.pageFactory = pageFactory;

            ResolveTabViewModels();

            this.windowFactory.OpenAssociatedWindow(this);
            this.windowFactory.WindowRendered += DoOnMainViewRendered;

            NotificationViewModels = new ObservableCollection<Basic.NotificationViewModel>();

            Application.Current.DispatcherUnhandledException += OnUnhandledException;

            FlagReceiver = new ServerFlagReceiver();
            FlagReceiver.ServerFlagReceived += OnFlagReceived;

            ManagePendingMessages();            
        }

        void ResolveTabViewModels()
        {
            Utility.ViewModelResolver resolver = new Utility.ViewModelResolver();

            resolver.MapNewView(typeof(MainTab.MainTabViewModel), typeof(Tabs.MainTab));
            resolver.MapNewView(typeof(Friends.FriendsTabViewModel), typeof(Tabs.FriendsTab));
            resolver.MapNewView(typeof(Dialogue.DialoguesTabViewModel), typeof(Tabs.DialoguesTab));
            resolver.MapNewView(typeof(Search.SearchTabViewModel), typeof(Tabs.SearchTab));
            resolver.MapNewView(typeof(Settings.SettingsTabViewModel), typeof(Tabs.SettingsTab));
            resolver.MapNewView(typeof(ActiveDialogue.ActiveDialogueViewModel), typeof(Tabs.ActiveDialogueTab));
        }

        void DoOnMainViewRendered(object sender, EventArgs e)
        {
            OnMainTabPrompted(0);
        }

        void OnMainTabPrompted(object userID)
        {
            currentTabViewModel.Dispose();
            bool rememberHistory = false;

            if ((int)userID != CurrentUser.ID && (int)userID != 0) rememberHistory = true;

            TabSwitchPrompted?.Invoke(this, new TabNavigationEventArgs() { PageViewModel = new MainTab.MainTabViewModel(this, (int)userID),
                PageFactory = pageFactory, RememberHistory = rememberHistory});
        }

        void OnFriendsTabPrompted()
        {
            currentTabViewModel.Dispose();
            TabSwitchPrompted?.Invoke(this, new TabNavigationEventArgs() { PageViewModel = new Friends.FriendsTabViewModel(this), PageFactory = pageFactory, RememberHistory = false });
        }

        void OnDialoguesTabPrompted()
        {
            currentTabViewModel.Dispose();
            TabSwitchPrompted?.Invoke(this, new TabNavigationEventArgs() { PageViewModel = new Dialogue.DialoguesTabViewModel(this), PageFactory = pageFactory, RememberHistory = false });
        }

        void OnActiveDialoguePrompted(object dialogue)
        {
            currentTabViewModel.Dispose();
            if (dialogue is Models.Dialogue dialogueModel)
                TabSwitchPrompted?.Invoke(this, new TabNavigationEventArgs() { PageViewModel = new ActiveDialogue.ActiveDialogueViewModel(this, dialogueModel), PageFactory = pageFactory, RememberHistory = false });
            else if (dialogue is int userID)
            {
                User recipient = ((ServerGetUserReply)ClientRequestManager.SendClientRequest(new GetUserRequest(currentUser.Cookie, userID))).User;
                TabSwitchPrompted?.Invoke(this, new TabNavigationEventArgs()
                { PageViewModel = new ActiveDialogue.ActiveDialogueViewModel(this, new Models.Dialogue(recipient, CurrentUser)), PageFactory = pageFactory, RememberHistory = false });
            }
        }

        void OnSearchTabPrompted()
        {
            currentTabViewModel.Dispose();
            TabSwitchPrompted?.Invoke(this, new TabNavigationEventArgs() { PageViewModel = new Search.SearchTabViewModel(this), PageFactory = pageFactory, RememberHistory = false });
        }

        void OnSettingsTabPrompted()
        {
            currentTabViewModel.Dispose();
            TabSwitchPrompted?.Invoke(this, new TabNavigationEventArgs() { PageViewModel = new Settings.SettingsTabViewModel(), PageFactory = pageFactory, RememberHistory = false });
        }

        void OnLogOutTabPrompted()
        {
            currentTabViewModel.Dispose();
            // TabSwitched?.Invoke(this, new TabNavigationEventArgs() { NewTabType = typeof(MainTabViewModel), PageFactory = pageFactory, RememberHistory = false });
        }

        void OnTabReturnPrompted()
        {
            currentTabViewModel.Dispose();
            TabReturnPrompted?.Invoke(this, new EventArgs());
        }

        void OnSessionClosure()
        {
            ClientNetworkConfigurator networkConfigurator = new ClientNetworkConfigurator();
            networkConfigurator.DisconnectSockets(false);
        }

        void OnUnhandledException(object sender, DispatcherUnhandledExceptionEventArgs e)
        {
            Models.Notification notification;

            if (e.Exception is RequestException re)
            {
                SpawnNotification(new Notification(Models.Notification.NotificationType.Error, re.ExceptionCode));
            }
            else
                SpawnNotification(new Notification(Models.Notification.NotificationType.Error, e.Exception.Message));

            e.Handled = true;
        }

        void SpawnNotification(Notification notification)
        {
            NotificationViewModels.Add(new Basic.NotificationViewModel(notification));
            notification.NotificationTimedOut += OnNotificationTimeout;
        }

        void OnNotificationTimeout(object sender, EventArgs e)
        {
            foreach(Basic.NotificationViewModel viewModel in NotificationViewModels)
            {
                if (viewModel.NotificationModel == (Models.Notification)sender)
                    Application.Current.Dispatcher.BeginInvoke(DispatcherPriority.Normal, new Action(() => NotificationViewModels.Remove(viewModel)));
            }
        }

        void OnFlagReceived(object sender, ServerFlagEventArgs e)
        {
            switch (e.FlagType)
            {
                case ServerFlagEventArgs.FlagTypes.MessagesPending:
                    ManagePendingMessages();
                    MessageFlagReceived?.Invoke(sender, e);
                    break;
                case ServerFlagEventArgs.FlagTypes.MessageStatusRead:
                case ServerFlagEventArgs.FlagTypes.MessageStatusReceived:
                    MessageFlagReceived?.Invoke(sender, e);
                    break;
            }    
        }

        void ManagePendingMessages()
        {
            Dictionary<int, List<Message>> newMessages = GetPendingMessages();

            if(newMessages.Count > 0)
            {
                RecordPendingMessages(newMessages);      
                
                foreach(int key in newMessages.Keys)
                {
                    ClientRequestManager.SendClientRequest(new ChangeDialogueStatusRequest(currentUser.Cookie, key, ConnectionCodes.MessagesReceivedStatus));
                }

                if (CurrentTabViewModel.GetType() != typeof(ActiveDialogue.ActiveDialogueViewModel))
                {
                    Application.Current.Dispatcher.BeginInvoke(DispatcherPriority.Normal, new Action(() =>
                    {
                        foreach (KeyValuePair<int, List<Message>> pair in newMessages)
                        {
                            SpawnNotification(new Notification(Notification.NotificationType.NewMessage, pair.Value.Last().Content));
                        }
                    }));                   
                }
            }           
        }


        Dictionary<int, List<Message>> GetPendingMessages()
        {
            GenericServerReply serverReply = ClientRequestManager.SendClientRequest(new GetPendingMessagesRequest(CurrentUser.Cookie));

            if (serverReply is ServerPendingMessagesReply messagesReply)
            {
                foreach(List<Message> dialogue in messagesReply.PendingMessages.Values)
                {
                    dialogue.Sort((Message a, Message b) =>
                    {
                        if (a.ID < b.ID) return -1;
                        if (a.ID > b.ID) return 1;
                        return 0;
                    });
                }

                return messagesReply.PendingMessages;
            }
            else
            {
                return new Dictionary<int, List<Message>>();
            }
        }

        void RecordPendingMessages(Dictionary<int, List<Message>> newMessages)
        {
            foreach (KeyValuePair<int, List<Message>> pair in newMessages)
            {
                Models.Dialogue.RecordMessagesLocally(pair.Value, pair.Key, CurrentUser.Cookie);
            }
        }

    }

    class TabNavigationEventArgs : EventArgs
    {
        public BaseViewModel PageViewModel { get; set; }
        public Utility.IPageFactory PageFactory { get; set; }
        public bool RememberHistory { get; set; }
        public object[] Arguments { get; set; }
    }

    class TabProcessingEventArgs : EventArgs
    {

    }
}
