using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Input;

namespace ChatBubbleClientWPF.ViewModels
{
    class DialoguePreviewViewModel : UserTileViewModel
    {
        string messagePreview;
        int dialogueID;

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

        public DialoguePreviewViewModel(Models.Dialogue dialogueModel) : this(dialogueModel, new ContextMenuActions[0] { })
        {
        }

        public DialoguePreviewViewModel(Models.Dialogue dialogueModel, params ContextMenuActions[] contextMenuActions) : base(dialogueModel.Recipient, contextMenuActions)
        {
            MessagePreview = dialogueModel.LastMessage.MessageContent;
            DialogueID = dialogueModel.DialogueID;

            if (dialogueModel.LastMessage.Status == Models.Message.MessageStatus.SentRead)
                MessagePreview = MessagePreview.Insert(0, "You: ");

            ContextMenuItems.Add(new MenuItemViewModel("Remove dialogue", RemoveDialogueCommand));
        }

        void OnRemoveDialogue()
        {
            TileActionTriggered?.Invoke(this, new UserTileInteractionEventArgs(UserTileInteractionEventArgs.TileAction.RemoveDialogue, DialogueID));
        }

        void OnOpenDialogue()
        {
            TileActionTriggered?.Invoke(this, new UserTileInteractionEventArgs(UserTileInteractionEventArgs.TileAction.OpenDialogue, DialogueID));
        }
    }
}
