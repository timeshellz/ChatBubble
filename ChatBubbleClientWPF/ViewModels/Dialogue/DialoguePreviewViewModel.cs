using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;

using ChatBubble.SharedAPI;

using ChatBubbleClientWPF.ViewModels.Windows;
using ChatBubbleClientWPF.ViewModels.Basic;

namespace ChatBubbleClientWPF.ViewModels.Dialogue
{
    class DialoguePreviewViewModel : UserTileViewModel
    {
        bool isDisposed = false;

        string messagePreview;
        int dialogueID;
        public Message.MessageStatus lastMessageStatus;
        public DateTime LastMessageDateTime { get; set; }

        Models.Dialogue dialogue;

        ICommand removeDialogueCommand;
        ICommand openDialogueCommand;

        public ICommand RemoveDialogueCommand
        {
            get
            {
                if (removeDialogueCommand == null)
                {
                    removeDialogueCommand = new Command(p => OnRemoveDialogue());
                }
                return removeDialogueCommand;
            }
        }

        public ICommand OpenDialogueCommand
        {
            get
            {
                if(openDialogueCommand == null)
                {
                    openDialogueCommand = new Command(p => OnOpenDialogue());
                }
                return openDialogueCommand;
            }
        }

        public string MessagePreview
        {
            get { return messagePreview; }
            set
            {
                messagePreview = value;
                OnPropertyChanged();
            }
        }

        public int DialogueID
        {
            get { return dialogueID; }
            set
            {
                dialogueID = value;
                OnPropertyChanged();
            }
        }

        public Message.MessageStatus LastMessageStatus
        {
            get { return lastMessageStatus; }
            set
            {
                lastMessageStatus = value;
                OnPropertyChanged();
            }
        }

        public DialoguePreviewViewModel(Models.Dialogue dialogueModel) : this(dialogueModel, new UserContextMenuActions[0] { })
        {
        }

        public DialoguePreviewViewModel(Models.Dialogue dialogueModel, params UserContextMenuActions[] contextMenuActions) : base(dialogueModel.Recipient, contextMenuActions)
        {           
            dialogue = dialogueModel;
            GetMessagePreview();

            dialogue.DialogueMessagesChanged += OnDialogueEvent;
          
            DialogueID = dialogueModel.DialogueID;
            ContextMenuItems.Add(new MenuItemViewModel("Remove dialogue", RemoveDialogueCommand));
        }

        void GetMessagePreview()
        {
            if (dialogue.Messages.Count > 0)
            {
                Message lastMessage = dialogue.Messages.Last().Value;
                MessagePreview = lastMessage.Content;
                LastMessageStatus = lastMessage.Status;
                LastMessageDateTime = lastMessage.DateTime;

                switch (lastMessage.Status)
                {
                    case Message.MessageStatus.SentReceived:
                    case Message.MessageStatus.SentRead:
                        MessagePreview = MessagePreview.Insert(0, "You: ");
                        break;
                }
            }
            else
                MessagePreview = "";
        }

        void OnRemoveDialogue()
        {
            TileActionTriggered?.Invoke(this, new TileInteractionEventArgs(TileInteractionEventArgs.TileAction.RemoveDialogue, DialogueID));
        }

        void OnOpenDialogue()
        {
            TileActionTriggered?.Invoke(this, new TileInteractionEventArgs(TileInteractionEventArgs.TileAction.OpenDialogue, DialogueID));
        }

        void OnDialogueEvent(object sender, Models.DialogueMessagesChangedEventArgs eventArgs)
        {
            Application.Current.Dispatcher.BeginInvoke(System.Windows.Threading.DispatcherPriority.Normal, new Action(() =>
            {
                if (eventArgs.NewMessageState == Message.MessageStatus.ReceivedNotRead)
                    GetMessagePreview();

            }));
        }
    }
}
