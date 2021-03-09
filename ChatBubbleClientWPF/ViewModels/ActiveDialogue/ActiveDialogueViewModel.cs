using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Collections.ObjectModel;
using System.Windows;
using System.Windows.Input;
using System.Timers;
using System.Windows.Threading;

using ChatBubble;
using ChatBubble.SharedAPI;
using ChatBubble.ClientAPI;

using ChatBubbleClientWPF.ViewModels.Windows;
using ChatBubbleClientWPF.ViewModels.Basic;

namespace ChatBubbleClientWPF.ViewModels.ActiveDialogue
{
    class ActiveDialogueViewModel : BaseViewModel, IContextMenuTileContainer
    {
        bool isDisposed = false;

        readonly MainWindowViewModel mainWindowViewModel;

        string messageInputContent;

        Models.Dialogue currentDialogueModel;
        public ObservableCollection<MessageLineViewModel> messageLineViewModels;
        public Dictionary<string, DateDisplayMessageLineViewModel> dateSeparators;

        bool isRecipientWriting;

        Timer replyTimer;

        string recipientName;

        public string RecipientName
        {
            get { return recipientName; }
            set
            {
                recipientName = value;
                OnPropertyChanged();
            }
        }

        public bool IsRecipientWriting
        {
            get { return isRecipientWriting; }
            set
            {
                isRecipientWriting = value;
                OnPropertyChanged();
            }
        }

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
                if (!replyTimer.Enabled)
                    StartReply();

                messageInputContent = value;
                OnPropertyChanged();
            }
        }

        public ActiveDialogueViewModel(MainWindowViewModel mainWindowViewModel, Models.Dialogue currentDialogue)
        {
            this.mainWindowViewModel = mainWindowViewModel;
            currentDialogueModel = currentDialogue;

            RecipientName = currentDialogueModel.Recipient.FullName;

            GenericServerReply serverReply =
                ClientRequestManager.SendClientRequest(new GetDialogueStatusRequest(currentDialogue.CurrentUser.Cookie, currentDialogue.DialogueID));

            MessageLineViewModels = new ObservableCollection<MessageLineViewModel>();
            dateSeparators = new Dictionary<string, DateDisplayMessageLineViewModel>();

            currentDialogueModel.DialogueMessagesChanged += OnDialogueEvent;

            PopulateMessageLineViewModels();

            mainWindowViewModel.MessageFlagReceived += OnFlagReceived;
        }

        void PopulateMessageLineViewModels()
        {
            List<MessageLineViewModel> failedMessages = new List<MessageLineViewModel>();

            foreach(Message message in currentDialogueModel.Messages.Values)
            {
                MessageLineViewModel messageLine = new MessageLineViewModel(message);

                TryCreateDateSeparator(message);

                if (message.Status != Message.MessageStatus.SendFailed)
                    MessageLineViewModels.Add(messageLine);
                else
                    failedMessages.Add(messageLine);

                messageLine.TileActionTriggered += OnTileAction;
            }

            foreach(MessageLineViewModel failedMessageViewModel in failedMessages)
            {
                MessageLineViewModels.Add(failedMessageViewModel);
            }

            if (currentDialogueModel.UnreadReceivedMessages.Count > 0)
            {
                CreateReadTimer();
            }

            CreateReplyTimer();
        }

        void CreateReadTimer()
        {
            Timer readTimer = new Timer(1000);
            readTimer.Elapsed += OnReadTimerElapsed;
            readTimer.Start();
        }

        void CreateReplyTimer()
        {
            replyTimer = new Timer(500);
            replyTimer.AutoReset = false;
            replyTimer.Elapsed += OnReplyTimerElapsed;            
        }

        void StartReply()
        {
            ClientRequestManager.SendClientRequest(new ChangeDialogueStatusRequest(currentDialogueModel.CurrentUser.Cookie,
                currentDialogueModel.DialogueID, ConnectionCodes.RecipientFormingReplyStatus));

            replyTimer.Start();
        }

        void TryCreateDateSeparator(Message message)
        {
            string messageDate = message.DateTime.ToShortDateString();

            if (!dateSeparators.ContainsKey(messageDate))
            {
                DateDisplayMessageLineViewModel dateSeparator = new DateDisplayMessageLineViewModel(message.DateTime);
                dateSeparators.Add(messageDate, dateSeparator);
                MessageLineViewModels.Add(dateSeparator);
            }
        }

        void TryRemoveLastSeparator()
        {
            if (MessageLineViewModels.Last() is DateDisplayMessageLineViewModel dateSeparator)
            {
                MessageLineViewModels.Remove(dateSeparator);
                dateSeparators.Remove(dateSeparator.MessageDateTime.ToShortDateString());
            }
        }

        public void OnTileAction(object sender, TileInteractionEventArgs e)
        {
            if (sender is MessageLineViewModel messageLine)
            {
                switch (e.Action)
                {
                    case TileInteractionEventArgs.TileAction.CopyMessage:
                        Clipboard.SetText(currentDialogueModel.Messages[e.InteractionID].Content);
                        break;
                    case TileInteractionEventArgs.TileAction.SendMessage:
                        MessageLineViewModels.Remove(messageLine);
                        currentDialogueModel.PendMessage(currentDialogueModel.Messages[e.InteractionID]);
                        break;
                    case TileInteractionEventArgs.TileAction.RemoveMessage:
                        MessageLineViewModels.Remove(messageLine);
                        TryRemoveLastSeparator();
                        currentDialogueModel.DeleteMessage(currentDialogueModel.Messages[e.InteractionID]);
                        break;
                }

            }
        }

        public void OnSendMessageCommand()
        {            
            currentDialogueModel.PendMessage(MessageInputContent);
            MessageInputContent = String.Empty;
        }

        void OnFlagReceived(object sender, Models.ServerFlagEventArgs e)
        {
            Application.Current.Dispatcher.Invoke(new Action(() =>
            {
                if (e.EventTargetID == currentDialogueModel.DialogueID)
                {
                    if (e.FlagType == Models.ServerFlagEventArgs.FlagTypes.MessagesPending)
                    {
                        currentDialogueModel.GetFreshMessages();
                    }
                    if (e.FlagType == Models.ServerFlagEventArgs.FlagTypes.MessageStatusRead)
                    {
                        currentDialogueModel.MakeSentMessagesRead();
                    }

                    if (e.FlagType == Models.ServerFlagEventArgs.FlagTypes.RecipientWritingReply)
                        isRecipientWriting = true;
                    if (e.FlagType == Models.ServerFlagEventArgs.FlagTypes.RecipientStoppedWritingReply)
                        isRecipientWriting = false;
                }
            }));            
        }

        void OnDialogueEvent(object sender, Models.DialogueMessagesChangedEventArgs eventArgs)
        {
            Application.Current.Dispatcher.BeginInvoke(System.Windows.Threading.DispatcherPriority.Normal, new Action(()=>
            {
                Message message = currentDialogueModel.Messages[eventArgs.MessageID];

                if (eventArgs.NewMessageState != Message.MessageStatus.Sending 
                && eventArgs.NewMessageState != Message.MessageStatus.ReceivedRead
                && eventArgs.NewMessageState != Message.MessageStatus.SentRead)
                {
                    MessageLineViewModel messageLine = new MessageLineViewModel(message);

                    TryCreateDateSeparator(message);
                    messageLineViewModels.Add(messageLine);

                    messageLine.TileActionTriggered += OnTileAction;
                }

                if (message.Status == Message.MessageStatus.ReceivedNotRead)
                    CreateReadTimer();
            }));       
        }

        void OnReadTimerElapsed(object sender, ElapsedEventArgs e)
        {
            Timer timer = (Timer)sender;
            timer.Stop();
            timer.Elapsed -= OnReadTimerElapsed;

            currentDialogueModel.MakeReceivedMessagesRead();           
        }

        void OnReplyTimerElapsed(object sender, ElapsedEventArgs e)
        {
            ClientRequestManager.SendClientRequest(new ChangeDialogueStatusRequest(currentDialogueModel.CurrentUser.Cookie, 
                currentDialogueModel.DialogueID, ConnectionCodes.RecipientStoppedFormingReplyStatus));

            Timer timer = (Timer)sender;
            timer.Stop();
        }

        protected override void Dispose(bool disposing)
        {
            if (isDisposed)
            {
                return;
            }

            if (disposing)
            {
                messageLineViewModels.Clear();
                messageLineViewModels = null;
                dateSeparators.Clear();
                dateSeparators = null;
                mainWindowViewModel.MessageFlagReceived -= OnFlagReceived;
            }

            isDisposed = true;

            base.Dispose(disposing);
        }
    }
}
