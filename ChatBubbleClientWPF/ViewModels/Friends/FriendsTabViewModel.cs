using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Collections.ObjectModel;
using System.Collections;

using ChatBubble;
using ChatBubble.SharedAPI;
using ChatBubble.ClientAPI;

using ChatBubbleClientWPF.ViewModels.Windows;
using ChatBubbleClientWPF.ViewModels.Basic;

namespace ChatBubbleClientWPF.ViewModels.Friends
{
    class FriendsTabViewModel : BaseViewModel, IContextMenuTileContainer
    {
        readonly MainWindowViewModel mainWindowViewModel;

        public ObservableCollection<FriendTileViewModel> friendTileViewModelsList;

        public ObservableCollection<FriendTileViewModel> FriendTileViewModels
        {
            get { return friendTileViewModelsList; }
            set
            {
                if (friendTileViewModelsList != value)
                {
                    friendTileViewModelsList = value;
                    OnPropertyChanged();
                }

            }
        }

        public Dictionary<int, User> friendDictionary;

        public FriendsTabViewModel(MainWindowViewModel mainViewModel)
        {
            mainWindowViewModel = mainViewModel;

            ServerFriendListReply serverReply =
                (ServerFriendListReply)ClientRequestManager.SendClientRequest(new GetFriendListRequest(mainWindowViewModel.CurrentUser.Cookie));

            if (serverReply.NetFlag == ConnectionCodes.DatabaseError)      //TO DO: Output an error message here
            {
                return;
            }

            friendDictionary = new Dictionary<int, User>();
            string[] friendListSplitstrings = { "id=", "login=", "name=" };

            foreach (User friend in serverReply.FriendList)
            {
                friendDictionary.Add(friend.ID, friend);
            }

            CreateFriendViewmodels();
        }

        void CreateFriendViewmodels()
        {
            friendTileViewModelsList = new ObservableCollection<FriendTileViewModel>();

            foreach (int id in friendDictionary.Keys)
            {
                FriendTileViewModel friendTile =  new FriendTileViewModel(friendDictionary[id], 
                    UserTileViewModel.UserContextMenuActions.OpenProfile,
                    UserTileViewModel.UserContextMenuActions.OpenPicture,
                    UserTileViewModel.UserContextMenuActions.SendMessage);
                friendTileViewModelsList.Add(friendTile);

                friendTile.TileActionTriggered += OnTileAction;
            }
        }

        public void OnTileAction(object sender, TileInteractionEventArgs e)
        {
            if(sender is FriendTileViewModel friendTileViewModel)
            {
                switch(e.Action)
                {
                    case TileInteractionEventArgs.TileAction.OpenPicture:
                        break;
                    case TileInteractionEventArgs.TileAction.OpenProfile:

                        if (mainWindowViewModel.OpenMainTabCommand.CanExecute(e.InteractionID))
                            mainWindowViewModel.OpenMainTabCommand.Execute(e.InteractionID);

                        break;
                    case TileInteractionEventArgs.TileAction.OpenDialogue:

                        if (mainWindowViewModel.OpenActiveDialogueCommand.CanExecute(e.InteractionID))
                            mainWindowViewModel.OpenActiveDialogueCommand.Execute(e.InteractionID);

                        break;
                    case TileInteractionEventArgs.TileAction.RemoveFriend:

                        mainWindowViewModel.CurrentUser.RemoveFriend(e.InteractionID);
                        FriendTileViewModels.Remove(friendTileViewModel);

                        break;
                }
                
            }
        }
    }

}
