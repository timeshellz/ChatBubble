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
using ChatBubble.SharedAPI;
using ChatBubble.ClientAPI;

namespace ChatBubbleClientWPF.ViewModels
{
    class DialoguesTabViewModel : BaseViewModel, IUserTileContainer
    {
        readonly MainWindowViewModel mainWindowViewModel;

        public Dictionary<int, Models.Dialogue> dialogueDictionary;
        public ObservableCollection<ViewModels.DialoguePreviewViewModel> dialoguePreviewViewModels;

        public ObservableCollection<ViewModels.DialoguePreviewViewModel> DialoguePreviewViewModels
        {
            get { return dialoguePreviewViewModels; }
            set
            {
                if (dialoguePreviewViewModels != value)
                {
                    dialoguePreviewViewModels = value;
                    OnPropertyChanged();
                }

            }
        }

        public DialoguesTabViewModel(MainWindowViewModel mainWindowViewModel)
        {
            this.mainWindowViewModel = mainWindowViewModel;

            dialogueDictionary = new Dictionary<int, Models.Dialogue>();

            DialoguePreviewViewModels = new ObservableCollection<DialoguePreviewViewModel>();

            GetStoredDialogues();
        }

        void GetStoredDialogues()
        {
            /*string[] dialogueFilenameArray = FileIOStreamer.GetDirectoryFiles(FileIOStreamer.defaultLocalUserDialoguesDirectory, false, false);

            for (int i = 0; i < dialogueFilenameArray.Length; i++)
            {
                int senderID = Convert.ToInt32(dialogueFilenameArray[i].Substring(dialogueFilenameArray[i].IndexOf('=') + 1));
                
                dialogueDictionary.Add(senderID,
                    new Models.Dialogue(new Models.User(senderID), Convert.ToInt32(mainWindowViewModel.CurrentUserID)));               
            }*/

            User user = ((ServerGetUserReply)ClientRequestManager.SendClientRequest(new GetUserRequest(mainWindowViewModel.CurrentUser.Cookie, 39))).User;

            dialogueDictionary.Add(39, new Models.Dialogue(user, mainWindowViewModel.CurrentUser));

            PopulateDialogueViewModelList();
        }

        void PopulateDialogueViewModelList()
        {
            foreach(Models.Dialogue dialogue in dialogueDictionary.Values)
            {
                DialoguePreviewViewModel dialoguePreview = new DialoguePreviewViewModel(dialogue);
                dialoguePreview.TileActionTriggered += OnTileAction;

                DialoguePreviewViewModels.Add(dialoguePreview);
            }
        }

        public void OnTileAction(object sender, UserTileInteractionEventArgs e)
        {
            if (sender is ViewModels.DialoguePreviewViewModel tileViewModel)
            {
                switch (e.Action)
                {
                    case UserTileInteractionEventArgs.TileAction.OpenPicture:
                        break;
                    case UserTileInteractionEventArgs.TileAction.OpenProfile:

                        if (mainWindowViewModel.OpenMainTabCommand.CanExecute(e.InteractionID))
                            mainWindowViewModel.OpenMainTabCommand.Execute(e.InteractionID);

                        break;
                    case UserTileInteractionEventArgs.TileAction.OpenDialogue:

                        Models.Dialogue dialogue;
                        dialogueDictionary.TryGetValue(Convert.ToInt32(e.InteractionID), out dialogue);

                        if (mainWindowViewModel.OpenActiveDialogueCommand.CanExecute(dialogue))
                            mainWindowViewModel.OpenActiveDialogueCommand.Execute(dialogue);

                        break;
                    case UserTileInteractionEventArgs.TileAction.RemoveDialogue:

                        int dialogueID = Convert.ToInt32(e.InteractionID);

                        dialogueDictionary[dialogueID].DeleteDialogueRecord();
                        dialogueDictionary.Remove(dialogueID);

                        dialoguePreviewViewModels.Remove((DialoguePreviewViewModel)sender);

                        break;
                }

            }
        }
    }
}
