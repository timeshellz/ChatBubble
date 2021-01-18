using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Input;

using ChatBubble;

namespace ChatBubbleClientWPF.ViewModels
{
    class MessageLineViewModel : UserTileViewModel
    {
        string messageContent;
        string messageShortTime;
        Models.Message.MessageStatus messageStatus;
        DateTime messageDateTime;

        ICommand copyContentCommand;

        public ICommand CopyContentCommand
        {
            get
            {
                if(copyContentCommand != null)
                {
                    copyContentCommand = new Command(p => OnCopyMessageContent());
                }
                return copyContentCommand;
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

        public Models.Message.MessageStatus MessageStatus
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

        public MessageLineViewModel(Models.Message message) : base(message.MessageSender, false)
        {
            MessageStatus = message.Status;
            MessageContent = message.MessageContent;
            MessageDateTime = message.MessageDateTime.ToLocalTime();
            MessageShortTime = MessageDateTime.ToShortTimeString();

            ContextMenuItems.Add(new MenuItemViewModel("Copy", CopyContentCommand));
        }

        void OnCopyMessageContent()
        {

        }
    }
}
