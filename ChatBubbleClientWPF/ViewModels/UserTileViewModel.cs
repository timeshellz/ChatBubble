using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Input;
using System.Collections.ObjectModel;

using ChatBubble;

namespace ChatBubbleClientWPF.ViewModels
{
    class UserTileViewModel : BaseViewModel
    {
        public enum ContextMenuActions { OpenProfile, OpenPicture, SendMessage, AddFriend, None}

        List<ViewModels.MenuItemViewModel> contextMenuItems = new List<MenuItemViewModel>();

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

        public EventHandler<UserTileInteractionEventArgs> TileActionTriggered;

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


        public UserTileViewModel(Models.User userModel, params ContextMenuActions[] contextMenuActions) : this(userModel)
        {
            FillContextMenu(contextMenuActions);
        }

        public  UserTileViewModel(Models.User userModel, bool useDefaultContextMenu = true)
        {
            UserID = userModel.ID;
            FullName = userModel.FullName;
            Username = userModel.Username;

            if(useDefaultContextMenu) FillContextMenu(new ContextMenuActions[0]);
        }      

        void FillContextMenu(ContextMenuActions[] contextMenuActions)
        {
            ContextMenuItems = new List<MenuItemViewModel>();

            if(contextMenuActions.Length == 0)
            {
                contextMenuActions = new ContextMenuActions[2] { ContextMenuActions.OpenProfile, ContextMenuActions.OpenPicture };
            }

            foreach (ContextMenuActions action in contextMenuActions)
            {
                if (action == ContextMenuActions.AddFriend)
                    ContextMenuItems.Add(new MenuItemViewModel("Send friend request", AddUserAsFriendCommand));
                if (action == ContextMenuActions.OpenPicture)
                    ContextMenuItems.Add(new MenuItemViewModel("Open picture", OpenUserPictureCommand));
                if (action == ContextMenuActions.OpenProfile)
                    ContextMenuItems.Add(new MenuItemViewModel("Open profile", OpenUserProfileCommand));
                if (action == ContextMenuActions.SendMessage)
                    ContextMenuItems.Add(new MenuItemViewModel("Send message", SendMessageCommand));
            }

        }

        void OnOpenProfile()
        {
            TileActionTriggered?.Invoke(this, new UserTileInteractionEventArgs(UserTileInteractionEventArgs.TileAction.OpenProfile, UserID));
        }

        void OnAddUserAsFriend()
        {
            TileActionTriggered?.Invoke(this, new UserTileInteractionEventArgs(UserTileInteractionEventArgs.TileAction.AddFriend, UserID));
        }

        void OnOpenUserPicture()
        {
            TileActionTriggered?.Invoke(this, new UserTileInteractionEventArgs(UserTileInteractionEventArgs.TileAction.OpenPicture, UserID));
        }

        void OnSendMessage()
        {
            TileActionTriggered?.Invoke(this, new UserTileInteractionEventArgs(UserTileInteractionEventArgs.TileAction.SendMessage, UserID));
        }
    }

    class UserTileInteractionEventArgs : EventArgs
    {
        public enum TileAction { OpenProfile, OpenPicture, SendMessage, RemoveFriend, AddFriend, RemoveDialogue, OpenDialogue, Select }

        public TileAction Action { get; private set; }
        public int InteractionID { get; private set; }

        public object InteractionParameters { get; private set; }

        public UserTileInteractionEventArgs(TileAction action, int interactionID)
        {
            Action = action;
            InteractionID = interactionID;
        }

        public UserTileInteractionEventArgs(TileAction action, int interactionID, params object[] parameters) : this(action, interactionID)
        {
            InteractionParameters = parameters;
        }
    }
}
