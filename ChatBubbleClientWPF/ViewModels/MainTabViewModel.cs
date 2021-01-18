using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Reflection;
using System.Windows;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Windows.Input;
using System.Security;
using ChatBubble;

namespace ChatBubbleClientWPF.ViewModels
{
    class MainTabViewModel : BaseViewModel, IUserTileContainer
    {
        readonly MainWindowViewModel mainWindowViewModel;
        private UserTileViewModel pictureTileViewModel;

        public UserTileViewModel PictureTileViewModel
        {
            get { return pictureTileViewModel; }
            set
            {
                pictureTileViewModel = value;
                OnPropertyChanged();
            }
        }

        string userFullName = "n/a";
        string username = "n/a";
        string userStatus = "n/a";
        string userDescription = "n/a";
        string userBubScore = "n/a";

        bool isEditing = false;
        bool isSelf = false;
        bool isFriend = false;

        string oldDescription = "";
        string oldStatus = "";

        Models.User displayedUserData;

        ICommand updateDescriptionCommand;
        ICommand goBackCommand;

        public ICommand UpdateDescriptionCommand
        {
            get
            {
                if (updateDescriptionCommand == null)
                {
                    updateDescriptionCommand = new Command(p => DoOnUpdateDescriptionCommand());
                }
                return updateDescriptionCommand;
            }
        }

        public ICommand GoBackCommand
        {
            get
            {
                if (goBackCommand == null)
                {
                    goBackCommand = new Command(p => DoOnGoBackCommand());
                }
                return goBackCommand;
            }
        }

        public string UserFullName
        {
            get { return userFullName; }
            set
            {
                userFullName = value;
                OnPropertyChanged();
            }
        }

        public string Username
        {
            get { return username; }
            set
            {
                username = value;
                OnPropertyChanged();
            }
        }

        public string UserStatus
        {
            get { return userStatus; }
            set
            {
                userStatus = value;
                OnPropertyChanged();
            }
        }

        public string UserDescription
        {
            get { return userDescription; }
            set
            {
                userDescription = value;
                OnPropertyChanged();
            }
        }

        public string OldDescription
        {
            get { return oldDescription; }
            set
            {
                oldDescription = value;
                OnPropertyChanged();
            }
        }

        public string OldStatus
        {
            get { return oldStatus; }
            set
            {
                oldStatus = value;
                OnPropertyChanged();
            }
        }

        public string UserBubScore
        {
            get { return userBubScore; }
            set
            {
                userBubScore = value;
                OnPropertyChanged();
            }
        }

        public bool IsEditing
        {
            get { return isEditing; }
            set
            {
                isEditing = value;
                OnPropertyChanged();
            }
        }

        public bool IsSelf
        {
            get { return isSelf; }
            set
            {
                isSelf = value;
                OnPropertyChanged();
            }
        }

        public bool IsFriend
        {
            get { return isFriend; }
            set
            {
                isFriend = value;
                OnPropertyChanged();
            }
        }

        public MainTabViewModel(MainWindowViewModel mainWindowViewModel, int userID)
        {
            this.mainWindowViewModel = mainWindowViewModel;

            displayedUserData = new Models.User(userID);

            UserFullName = displayedUserData.FullName;
            Username = displayedUserData.Username;
            UserStatus = displayedUserData.Status;
            UserDescription = displayedUserData.Description;
            UserBubScore = displayedUserData.BubScore.ToString();

            OldDescription = UserDescription;
            OldStatus = UserStatus;

            if (mainWindowViewModel.CurrentUser.ID == userID || userID == 0)
                IsSelf = true;
            else
            {
                IsFriend = CheckIfFriend(userID.ToString());
            }

            if(!IsSelf)
                if (!IsFriend)
                    PictureTileViewModel = new UserTileViewModel(displayedUserData,
                        new UserTileViewModel.ContextMenuActions[3] { UserTileViewModel.ContextMenuActions.OpenPicture,
                    UserTileViewModel.ContextMenuActions.SendMessage,
                    UserTileViewModel.ContextMenuActions.AddFriend });
                else
                    PictureTileViewModel = new FriendTileViewModel(displayedUserData,
                        new UserTileViewModel.ContextMenuActions[2] { UserTileViewModel.ContextMenuActions.OpenPicture,
                    UserTileViewModel.ContextMenuActions.SendMessage});
            else
                PictureTileViewModel = new UserTileViewModel(displayedUserData,
                        new UserTileViewModel.ContextMenuActions[1] { UserTileViewModel.ContextMenuActions.OpenPicture });

            PictureTileViewModel.TileActionTriggered += OnTileAction;
        }
        
        void DoOnUpdateDescriptionCommand()
        {           
            if (isEditing && (OldDescription != UserDescription || OldStatus != UserStatus))
            {
                mainWindowViewModel.CurrentUser.ChangeDescription(UserStatus, UserDescription);

                isEditing = false;
            } 
            else if (!isEditing) isEditing = true;
        }

        void DoOnGoBackCommand()
        {
            if(!IsSelf)
            {
                if (mainWindowViewModel.TabReturnCommand.CanExecute(null))
                    mainWindowViewModel.TabReturnCommand.Execute(null);
            }
        }

        bool CheckIfFriend(string userID)
        {
            string friendListResultString = NetComponents.ClientRequestArbitrary(NetComponents.ConnectionCodes.GetFriendListRequest, "", true, true);

            if (friendListResultString == NetComponents.ConnectionCodes.DatabaseError)      //TO DO: Output an error message here
            {
                return false;
            }

            string[] friendListSplitstrings = { "id=", "login=", "name=" };

            foreach (string friend in friendListResultString.Split(new string[] { "user=" }, StringSplitOptions.RemoveEmptyEntries))
            {
                string[] friendData = friend.Split(friendListSplitstrings, StringSplitOptions.RemoveEmptyEntries);

                if (friendData[0] == userID) return true;
            }

            return false;
        }

        public void OnTileAction(object sender, UserTileInteractionEventArgs e)
        {
            if (sender is UserTileViewModel)
            {
                switch (e.Action)
                {
                    case UserTileInteractionEventArgs.TileAction.OpenPicture:
                        break;
                    case UserTileInteractionEventArgs.TileAction.SendMessage:
                        break;
                    case UserTileInteractionEventArgs.TileAction.AddFriend:

                        mainWindowViewModel.CurrentUser.AddFriend(e.InteractionID);
                        
                        break;
                }

            }

            if(sender is FriendTileViewModel)
            {
                switch (e.Action)
                {
                    case UserTileInteractionEventArgs.TileAction.RemoveFriend:

                        mainWindowViewModel.CurrentUser.RemoveFriend(e.InteractionID);
                 
                        break;
                }
            }
        }
    }
}
