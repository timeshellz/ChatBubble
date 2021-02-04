using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Input;

using ChatBubble.SharedAPI;

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
            if (dialogueModel.Messages.Count > 0)
            {
                MessagePreview = dialogueModel.Messages.Last().Value.MessageContent;

                if (dialogueModel.Messages.Last().Value.Status == Message.MessageStatus.SentRead)
                    MessagePreview = MessagePreview.Insert(0, "You: ");
            }
            else
                MessagePreview = "";

            DialogueID = dialogueModel.DialogueID;
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
