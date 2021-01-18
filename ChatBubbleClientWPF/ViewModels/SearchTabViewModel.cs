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

namespace ChatBubbleClientWPF.ViewModels
{
    class SearchTabViewModel : BaseViewModel, IUserTileContainer
    {
        readonly MainWindowViewModel mainWindowViewModel;

        public Dictionary<int, Models.User> resultDictionary;
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

            resultDictionary = new Dictionary<int, Models.User>();

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

                string searchResultString = "";

                await Task.Delay(600); //Slow down requests to ease server and client resource usage

                await Task.Run(() =>
                {
                    searchResultString = NetComponents.ClientRequestArbitrary(NetComponents.ConnectionCodes.SearchRequest, SearchParameter, true, false, true);
                });
              
                if (searchResultString != NetComponents.ConnectionCodes.NotFoundError)
                    await Task.Run(() =>
                    {
                        PopulateSearchList(searchResultString.Split(new string[1] { "user=" }, StringSplitOptions.RemoveEmptyEntries));
                    });
            }
        }

        void PopulateSearchList(string[] userResults)
        {
            resultDictionary.Clear();

            foreach (string user in userResults)
            {
                string[] userData = user.Split(new string[3] { "id=", "login=", "name=" }, StringSplitOptions.RemoveEmptyEntries);

                try
                {
                    resultDictionary.Add(Convert.ToInt32(userData[0]), new Models.User(Convert.ToInt32(userData[0]), userData[2], userData[1]));
                }
                catch { };
            }


            Application.Current.Dispatcher.BeginInvoke(DispatcherPriority.Normal, new Action(() =>
            {
                SearchResultViewModels.Clear();

                foreach (Models.User user in resultDictionary.Values)
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
