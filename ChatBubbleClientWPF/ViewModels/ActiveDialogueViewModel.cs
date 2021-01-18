using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Collections.ObjectModel;

namespace ChatBubbleClientWPF.ViewModels
{
    class ActiveDialogueViewModel : BaseViewModel, IUserTileContainer
    {
        readonly MainWindowViewModel mainWindowViewModel;

        public Dictionary<int, Models.Message> messageDictionary;
        public ObservableCollection<ViewModels.MessageLineViewModel> messageLineViewModels;

        public ObservableCollection<ViewModels.MessageLineViewModel> MessageLineViewModels
        {
            get { return messageLineViewModels; }
            set
            {
                if (messageLineViewModels != value)
                {
                    messageLineViewModels = value;
                    OnPropertyChanged();
                }

            }
        }

        public ActiveDialogueViewModel(MainWindowViewModel mainWindowViewModel, Dictionary<int, Models.Message> messageDictionary)
        {
            this.mainWindowViewModel = mainWindowViewModel;

            this.messageDictionary = messageDictionary;

            MessageLineViewModels = new ObservableCollection<MessageLineViewModel>();

            PopulateMessageLineViewModels();
        }

        void PopulateMessageLineViewModels()
        {
            foreach(Models.Message message in messageDictionary.Values)
            {
                MessageLineViewModels.Add(new MessageLineViewModel(message));
            }
        }

        public void OnTileAction(object sender, UserTileInteractionEventArgs e)
        {
            
        }
    }
}
