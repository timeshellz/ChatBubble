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

namespace ChatBubbleClientWPF.ViewModels
{
    class MainWindowViewModel : BaseViewModel
    {
        public event EventHandler<TabNavigationEventArgs> TabSwitchPrompted;
        public event EventHandler<EventArgs> TabReturnPrompted;
        ObservableCollection<NotificationViewModel> notificationViewModels;

        public ObservableCollection<NotificationViewModel> NotificationViewModels
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

            this.windowFactory = windowFactory;
            this.pageFactory = pageFactory;

            ResolveTabViewModels();

            this.windowFactory.OpenAssociatedWindow(this);
            this.windowFactory.WindowRendered += DoOnMainViewRendered;

            NotificationViewModels = new ObservableCollection<NotificationViewModel>();

            Application.Current.DispatcherUnhandledException += OnUnhandledException;
        }

        void ResolveTabViewModels()
        {
            Utility.ViewModelResolver resolver = new Utility.ViewModelResolver();

            resolver.MapNewView(typeof(MainTabViewModel), typeof(Tabs.MainTab));
            resolver.MapNewView(typeof(FriendsTabViewModel), typeof(Tabs.FriendsTab));
            resolver.MapNewView(typeof(DialoguesTabViewModel), typeof(Tabs.DialoguesTab));
            resolver.MapNewView(typeof(SearchTabViewModel), typeof(Tabs.SearchTab));
            resolver.MapNewView(typeof(SettingsTabViewModel), typeof(Tabs.SettingsTab));
            resolver.MapNewView(typeof(ActiveDialogueViewModel), typeof(Tabs.ActiveDialogueTab));
        }

        void DoOnMainViewRendered(object sender, EventArgs e)
        {
            OnMainTabPrompted(0);
        }

        void OnMainTabPrompted(object userID)
        {
            bool rememberHistory = false;

            if ((int)userID != CurrentUser.ID && (int)userID != 0) rememberHistory = true;

            TabSwitchPrompted?.Invoke(this, new TabNavigationEventArgs() { PageViewModel = new MainTabViewModel(this, (int)userID),
                PageFactory = pageFactory, RememberHistory = rememberHistory});
        }

        void OnFriendsTabPrompted()
        {
            TabSwitchPrompted?.Invoke(this, new TabNavigationEventArgs() { PageViewModel = new FriendsTabViewModel(this), PageFactory = pageFactory, RememberHistory = false });
        }

        void OnDialoguesTabPrompted()
        {
            TabSwitchPrompted?.Invoke(this, new TabNavigationEventArgs() { PageViewModel = new DialoguesTabViewModel(this), PageFactory = pageFactory, RememberHistory = false });
        }

        void OnActiveDialoguePrompted(object dialogue)
        {
            if(dialogue is Models.Dialogue dialogueModel)
                TabSwitchPrompted?.Invoke(this, new TabNavigationEventArgs() { PageViewModel = new ActiveDialogueViewModel(this, dialogueModel), PageFactory = pageFactory, RememberHistory = false });
        }

        void OnSearchTabPrompted()
        {
            TabSwitchPrompted?.Invoke(this, new TabNavigationEventArgs() { PageViewModel = new SearchTabViewModel(this), PageFactory = pageFactory, RememberHistory = false });
        }

        void OnSettingsTabPrompted()
        {
            TabSwitchPrompted?.Invoke(this, new TabNavigationEventArgs() { PageViewModel = new SettingsTabViewModel(), PageFactory = pageFactory, RememberHistory = false });
        }

        void OnLogOutTabPrompted()
        {
           // TabSwitched?.Invoke(this, new TabNavigationEventArgs() { NewTabType = typeof(MainTabViewModel), PageFactory = pageFactory, RememberHistory = false });
        }

        void OnTabReturnPrompted()
        {
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
                notification = new Models.Notification(Models.Notification.NotificationType.Error, re.ExceptionCode);
            }
            else
                notification = new Models.Notification(Models.Notification.NotificationType.Error, "An error occured");

            e.Handled = true;

            NotificationViewModels.Add(new NotificationViewModel(notification));
            notification.NotificationTimedOut += OnNotificationTimeout;
        }

        void OnNotificationTimeout(object sender, EventArgs e)
        {
            foreach(NotificationViewModel viewModel in NotificationViewModels)
            {
                if (viewModel.NotificationModel == (Models.Notification)sender)
                    Application.Current.Dispatcher.BeginInvoke(DispatcherPriority.Normal, new Action(() => NotificationViewModels.Remove(viewModel)));
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
}
