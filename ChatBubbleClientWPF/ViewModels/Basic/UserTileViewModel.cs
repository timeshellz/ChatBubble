using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Input;
using System.Collections.ObjectModel;

using ChatBubble;
using ChatBubble.SharedAPI;

using ChatBubbleClientWPF.ViewModels.Windows;
using ChatBubbleClientWPF.ViewModels.Basic;

namespace ChatBubbleClientWPF.ViewModels.Basic
{
    class UserTileViewModel : BaseViewModel, IContextMenuTile
    {
        public enum UserContextMenuActions { OpenProfile, OpenPicture, SendMessage, AddFriend, None}

        List<MenuItemViewModel> contextMenuItems = new List<MenuItemViewModel>();

        public List<MenuItemViewModel> ContextMenuItems
        {
            get { return contextMenuItems; }
            set
            {
                contextMenuItems = value;
                OnPropertyChanged();
            }
        }

        int userID = -1;
        string userFullName = "n/a";
        string username = "n/a";

        internal ICommand openUserProfileCommand;
        internal ICommand openUserPictureCommand;
        internal ICommand sendMessageCommand;
        internal ICommand addUserAsFriendCommand;

        public EventHandler<TileInteractionEventArgs> TileActionTriggered { get; set; }

        public int UserID
        {
            get { return userID; }
            set
            {
                userID = value;
                OnPropertyChanged();
            }
        }

        public string FullName
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

        public ICommand OpenUserProfileCommand
        {
            get
            {
                if (openUserProfileCommand == null)
                {
                    openUserProfileCommand = new Command(p => OnOpenProfile());
                }
                return openUserProfileCommand;
            }
        }

        public ICommand OpenUserPictureCommand
        {
            get
            {
                if (openUserPictureCommand == null)
                {
                    openUserPictureCommand = new Command(p => OnOpenUserPicture());
                }
                return openUserPictureCommand;
            }
        }

        public ICommand SendMessageCommand
        {
            get
            {
                if (sendMessageCommand == null)
                {
                    sendMessageCommand = new Command(p => OnSendMessage());
                }
                return sendMessageCommand;
            }
        }

        public ICommand AddUserAsFriendCommand
        {
            get
            {
                if (addUserAsFriendCommand == null)
                {
                    addUserAsFriendCommand = new Command(p => OnAddUserAsFriend());
                }
                return addUserAsFriendCommand;
            }
        }


        public UserTileViewModel(User userModel, params UserContextMenuActions[] contextMenuActions) : this(userModel)
        {
            FillContextMenu(contextMenuActions);
        }

        public  UserTileViewModel(User userModel, bool useDefaultContextMenu = true)
        {
            UserID = userModel.ID;
            FullName = userModel.FullName;
            Username = userModel.Username;

            if(useDefaultContextMenu) FillContextMenu(new UserContextMenuActions[0]);
        }      

        void FillContextMenu(UserContextMenuActions[] contextMenuActions)
        {
            ContextMenuItems = new List<MenuItemViewModel>();

            if(contextMenuActions.Length == 0)
            {
                contextMenuActions = new UserContextMenuActions[2] { UserContextMenuActions.OpenProfile, UserContextMenuActions.OpenPicture };
            }

            foreach (UserContextMenuActions action in contextMenuActions)
            {
                if (action == UserContextMenuActions.AddFriend)
                    ContextMenuItems.Add(new MenuItemViewModel("Send friend request", AddUserAsFriendCommand));
                if (action == UserContextMenuActions.OpenPicture)
                    ContextMenuItems.Add(new MenuItemViewModel("Open picture", OpenUserPictureCommand));
                if (action == UserContextMenuActions.OpenProfile)
                    ContextMenuItems.Add(new MenuItemViewModel("Open profile", OpenUserProfileCommand));
                if (action == UserContextMenuActions.SendMessage)
                    ContextMenuItems.Add(new MenuItemViewModel("Send message", SendMessageCommand));
            }

        }

        void OnOpenProfile()
        {
            TileActionTriggered?.Invoke(this, new TileInteractionEventArgs(TileInteractionEventArgs.TileAction.OpenProfile, UserID));
        }

        void OnAddUserAsFriend()
        {
            TileActionTriggered?.Invoke(this, new TileInteractionEventArgs(TileInteractionEventArgs.TileAction.AddFriend, UserID));
        }

        void OnOpenUserPicture()
        {
            TileActionTriggered?.Invoke(this, new TileInteractionEventArgs(TileInteractionEventArgs.TileAction.OpenPicture, UserID));
        }

        void OnSendMessage()
        {
            TileActionTriggered?.Invoke(this, new TileInteractionEventArgs(TileInteractionEventArgs.TileAction.OpenDialogue, UserID));
        }       
    }
}
