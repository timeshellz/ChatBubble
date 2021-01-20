using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Collections.ObjectModel;
using System.Windows;
using System.Windows.Input;

namespace ChatBubbleClientWPF.ViewModels
{
    class ActiveDialogueViewModel : BaseViewModel, IUserTileContainer
    {
        readonly MainWindowViewModel mainWindowViewModel;

        string messageInputContent;

        Models.Dialogue currentDialogueModel;
        public ObservableCollection<MessageLineViewModel> messageLineViewModels;

        public ObservableCollection<MessageLineViewModel> MessageLineViewModels
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

        ICommand sendMessageCommand;

        public ICommand SendMessageCommand
        {
            get
            {
                if(sendMessageCommand == null)
                {
                    sendMessageCommand = new Command(p => OnSendMessageCommand());
                }

                return sendMessageCommand;
            }
        }

        public string MessageInputContent
        {
            get { return messageInputContent; }
            set
            {
                messageInputContent = value;
                OnPropertyChanged();
            }
        }

        public ActiveDialogueViewModel(MainWindowViewModel mainWindowViewModel, Models.Dialogue currentDialogue)
        {
            this.mainWindowViewModel = mainWindowViewModel;
            currentDialogueModel = currentDialogue;

            MessageLineViewModels = new ObservableCollection<MessageLineViewModel>();

            currentDialogueModel.DialogueMessagesChanged += OnDialogueEvent;

            PopulateMessageLineViewModels();
        }

        void PopulateMessageLineViewModels()
        {
            foreach(Models.Message message in currentDialogueModel.Messages.Values)
            {
                MessageLineViewModels.Insert(message.MessageID, new MessageLineViewModel(message));
            }
        }

        public void OnTileAction(object sender, UserTileInteractionEventArgs e)
        {
            
        }

        public void OnSendMessageCommand()
        {            
            currentDialogueModel.PendMessage(MessageInputContent);
            MessageInputContent = String.Empty;
        }

        void OnDialogueEvent(object sender, Models.DialogueMessagesChangedEventArgs eventArgs)
        {
            Application.Current.Dispatcher.BeginInvoke(System.Windows.Threading.DispatcherPriority.Normal, new Action(()=>
            {
                switch (eventArgs.NewMessageState)
                {
                    case Models.Message.MessageStatus.SentRead:
                    case Models.Message.MessageStatus.SentReceived:
                        MessageLineViewModels.Insert(eventArgs.MessageID, new MessageLineViewModel(currentDialogueModel.Messages[eventArgs.MessageID]));
                        break;
                }
            }));       
        }
    }
}
