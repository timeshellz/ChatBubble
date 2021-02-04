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

namespace ChatBubbleClientWPF.ViewModels
{
    class FriendsTabViewModel : BaseViewModel, IUserTileContainer
    {
        readonly MainWindowViewModel mainWindowViewModel;

        public ObservableCollection<ViewModels.FriendTileViewModel> friendTileViewModelsList;

        public ObservableCollection<ViewModels.FriendTileViewModel> FriendTileViewModels
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
                ViewModels.FriendTileViewModel friendTile =  new ViewModels.FriendTileViewModel(friendDictionary[id], 
                    UserTileViewModel.ContextMenuActions.OpenProfile,
                    UserTileViewModel.ContextMenuActions.OpenPicture,
                    UserTileViewModel.ContextMenuActions.SendMessage);
                friendTileViewModelsList.Add(friendTile);

                friendTile.TileActionTriggered += OnTileAction;
            }
        }

        public void OnTileAction(object sender, UserTileInteractionEventArgs e)
        {
            if(sender is ViewModels.FriendTileViewModel friendTileViewModel)
            {
                switch(e.Action)
                {
                    case UserTileInteractionEventArgs.TileAction.OpenPicture:
                        break;
                    case UserTileInteractionEventArgs.TileAction.OpenProfile:

                        if (mainWindowViewModel.OpenMainTabCommand.CanExecute(e.InteractionID))
                            mainWindowViewModel.OpenMainTabCommand.Execute(e.InteractionID);

                        break;
                    case UserTileInteractionEventArgs.TileAction.SendMessage:



                        break;
                    case UserTileInteractionEventArgs.TileAction.RemoveFriend:

                        mainWindowViewModel.CurrentUser.RemoveFriend(e.InteractionID);
                        FriendTileViewModels.Remove(friendTileViewModel);

                        break;
                }
                
            }
        }
    }

}
