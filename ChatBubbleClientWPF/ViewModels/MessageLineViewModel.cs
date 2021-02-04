using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Input;

using ChatBubble;
using ChatBubble.SharedAPI;

namespace ChatBubbleClientWPF.ViewModels
{
    class MessageLineViewModel : BaseViewModel
    {
        string messageContent;
        string messageShortTime;
        Message.MessageStatus messageStatus;
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
            MessageStatus = message.Status;
            MessageContent = message.MessageContent;
            MessageDateTime = message.MessageDateTime.ToLocalTime();
            MessageShortTime = MessageDateTime.ToShortTimeString();
        }

        void OnCopyMessageContent()
        {

        }
    }
}
