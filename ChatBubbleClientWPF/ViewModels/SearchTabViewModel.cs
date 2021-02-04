using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Collections.ObjectModel;
using System.Windows.Threading;
using System.Diagnostics;
using System.Windows;
using System.Windows.Input;
using System.ComponentModel;

using ChatBubble;
using ChatBubble.SharedAPI;
using ChatBubble.ClientAPI;

namespace ChatBubbleClientWPF.ViewModels
{
    class SearchTabViewModel : BaseViewModel, IUserTileContainer
    {
        readonly MainWindowViewModel mainWindowViewModel;

        public Dictionary<int, User> resultDictionary;
        public ObservableCollection<ViewModels.UserTileViewModel> searchResultViewModelsList;

        string searchParameter;

        public string SearchParameter
        {
            get { return searchParameter; }
            set
            {
                searchParameter = value;
                OnPropertyChanged();
            }
        }

        public ObservableCollection<ViewModels.UserTileViewModel> SearchResultViewModels
        {
            get { return searchResultViewModelsList; }
            set
            {
                if (searchResultViewModelsList != value)
                {
                    searchResultViewModelsList = value;
                    OnPropertyChanged();
                }

            }
        }

        public SearchTabViewModel(MainWindowViewModel mainViewModel)
        {
            mainWindowViewModel = mainViewModel;

            resultDictionary = new Dictionary<int, User>();

            searchResultViewModelsList = new ObservableCollection<UserTileViewModel>();

            base.PropertyChanged += FetchSearchResults;
        }

        async void FetchSearchResults(object sender, PropertyChangedEventArgs e)
        {
            if (e.PropertyName == nameof(SearchParameter))
            {
                if (SearchParameter == String.Empty || SearchParameter == null)
                {
                    SearchResultViewModels.Clear();
                    return;
                }

                GenericServerReply serverReply = null;

                await Task.Delay(600); //Slow down requests to ease server and client resource usage

                await Task.Run(() =>
                {
                    serverReply = ClientRequestManager.SendClientRequest(new SearchRequest(mainWindowViewModel.CurrentUser.Cookie, SearchParameter));
                });
              
                if (serverReply != null && serverReply.NetFlag != ConnectionCodes.NotFoundError && serverReply is ServerSearchReply searchReply)
                    await Task.Run(() =>
                    {
                        PopulateSearchList(searchReply.SearchResults);
                    });
            }
        }

        void PopulateSearchList(List<User> results)
        {
            resultDictionary.Clear();

            foreach (User user in results)
            {
                try
                {
                    resultDictionary.Add(user.ID, user);
                }
                catch { };
            }


            Application.Current.Dispatcher.BeginInvoke(DispatcherPriority.Normal, new Action(() =>
            {
                SearchResultViewModels.Clear();

                foreach (User user in resultDictionary.Values)
                {
                    UserTileViewModel userSearchTile = new UserTileViewModel(user);
                    userSearchTile.TileActionTriggered += OnTileAction;

                    SearchResultViewModels.Add(userSearchTile);
                }
            }));
        }

        public void OnTileAction(object sender, UserTileInteractionEventArgs e)
        {
            if (sender is ViewModels.UserTileViewModel userSearchTileViewModel)
            {
                switch (e.Action)
                {
                    case UserTileInteractionEventArgs.TileAction.OpenPicture:
                        break;
                    case UserTileInteractionEventArgs.TileAction.OpenProfile:

                        if (mainWindowViewModel.OpenMainTabCommand.CanExecute(e.InteractionID))
                            mainWindowViewModel.OpenMainTabCommand.Execute(e.InteractionID);

                        break;
                    case UserTileInteractionEventArgs.TileAction.SendMessage:
                        break;
                }

            }
        }
    }
}
