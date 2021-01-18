using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Collections.ObjectModel;
using System.Collections;
using ChatBubble;

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

        public Dictionary<int, Models.User> friendDictionary;

        public FriendsTabViewModel(MainWindowViewModel mainViewModel)
        {
            mainWindowViewModel = mainViewModel;

            string friendListResultString = NetComponents.ClientRequestArbitrary(NetComponents.ConnectionCodes.GetFriendListRequest, "", true, true);

            if (friendListResultString == NetComponents.ConnectionCodes.DatabaseError)      //TO DO: Output an error message here
            {
                return;
            }

            friendDictionary = new Dictionary<int, Models.User>();
            string[] friendListSplitstrings = { "id=", "login=", "name=" };

            foreach (string friend in friendListResultString.Split(new string[] { "user=" }, StringSplitOptions.RemoveEmptyEntries))
            {
                string[] friendData = friend.Split(friendListSplitstrings, StringSplitOptions.RemoveEmptyEntries);
                friendDictionary.Add(Convert.ToInt32(friendData[0]), new Models.User(Convert.ToInt32(friendData[0]), friendData[2], friendData[1]));
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
