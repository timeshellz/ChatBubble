using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Input;

using ChatBubble;
using ChatBubble.SharedAPI;

using ChatBubbleClientWPF.ViewModels.Windows;
using ChatBubbleClientWPF.ViewModels.Basic;

namespace ChatBubbleClientWPF.ViewModels.ActiveDialogue
{
    class MessageLineViewModel : BaseViewModel, IContextMenuTile
    {
        public EventHandler<TileInteractionEventArgs> TileActionTriggered { get; set; }
        public List<MenuItemViewModel> ContextMenuItems { get; set; }

        int messageID;
        string messageContent;
        string messageShortTime;
        Message.MessageStatus messageStatus;
        DateTime messageDateTime;

        ICommand copyContentCommand;
        ICommand resendMessageCommand;
        ICommand removeMessageCommand;

        public ICommand CopyContentCommand
        {
            get
            {
                if(copyContentCommand == null)
                {
                    copyContentCommand = new Command(p => OnCopyMessageContent());
                }
                return copyContentCommand;
            }
        }

        public ICommand ResendMessageCommand
        {
            get
            {
                if (resendMessageCommand == null)
                {
                    resendMessageCommand = new Command(p => OnResendMessage());
                }
                return resendMessageCommand;
            }
        }

        public ICommand RemoveMessageCommand
        {
            get
            {
                if (removeMessageCommand == null)
                {
                    removeMessageCommand = new Command(p => OnRemoveMessage());
                }
                return removeMessageCommand;
            }
        }

        public int MessageID
        {
            get { return messageID; }
            set
            {
                messageID = value;
                OnPropertyChanged();
            }
        }

        public string MessageContent
        {
            get { return messageContent; }
            set
            {
                messageContent = value;
                OnPropertyChanged();
            }
        }

        public Message.MessageStatus MessageStatus
        {
            get { return messageStatus; }
            set
            {
                messageStatus = value;
                OnPropertyChanged();
            }
        }

        public DateTime MessageDateTime
        {
            get { return messageDateTime; }
            set
            {
                messageDateTime = value;
                OnPropertyChanged();
            }
        }

        public string MessageShortTime
        {
            get { return messageShortTime; }
            set
            {
                messageShortTime = value;
                OnPropertyChanged();
            }
        }

        public MessageLineViewModel(Message message)
        {
            MessageID = message.ID;
            MessageStatus = message.Status;
            MessageContent = message.Content;
            MessageDateTime = message.DateTime.ToLocalTime();
            MessageShortTime = MessageDateTime.ToShortTimeString();

            message.StatusChanged += OnMessageStatusChanged;

            ContextMenuItems = new List<MenuItemViewModel>();

            ContextMenuItems.Add(new MenuItemViewModel("Copy", CopyContentCommand));
            ContextMenuItems.Add(new MenuItemViewModel("Delete", RemoveMessageCommand));

            if(MessageStatus == Message.MessageStatus.SendFailed)
            {
                ContextMenuItems.Add(new MenuItemViewModel("Resend", ResendMessageCommand));
            }
        }

        protected MessageLineViewModel() { }

        void OnCopyMessageContent()
        {
            TileActionTriggered?.Invoke(this, new TileInteractionEventArgs(TileInteractionEventArgs.TileAction.CopyMessage, MessageID));
        }

        void OnResendMessage()
        {
            TileActionTriggered?.Invoke(this, new TileInteractionEventArgs(TileInteractionEventArgs.TileAction.SendMessage, MessageID));
        }

        void OnRemoveMessage()
        {
            TileActionTriggered?.Invoke(this, new TileInteractionEventArgs(TileInteractionEventArgs.TileAction.RemoveMessage, MessageID));
        }

        void OnMessageStatusChanged(object sender, EventArgs e)
        {
            MessageStatus = ((Message)sender).Status;
        }
    }
}
