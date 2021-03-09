using System;
using System.IO;
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
using ChatBubble.FileManager;

using ChatBubbleClientWPF.ViewModels.Windows;
using ChatBubbleClientWPF.ViewModels.Basic;

namespace ChatBubbleClientWPF.ViewModels.Dialogue
{
    class DialoguesTabViewModel : BaseViewModel, IContextMenuTileContainer
    {
        bool isDisposed = false;

        readonly MainWindowViewModel mainWindowViewModel;

        public Dictionary<int, Models.Dialogue> dialogueDictionary;
        public ObservableCollection<DialoguePreviewViewModel> dialoguePreviewViewModels;

        bool areDialoguesSorted;
        public ObservableCollection<DialoguePreviewViewModel> DialoguePreviewViewModels
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

            mainWindowViewModel.MessageFlagReceived += OnFlagReceived;
        }

        void GetStoredDialogues()
        {
            FileManager fileManager = new FileManager();

            string[] dialogueFilenameArray = 
                fileManager.GetDirectoryFiles(ClientDirectories.GetUserDataFolder(ClientDirectories.UserDataDirectoryType.Dialogues, mainWindowViewModel.CurrentUser.ID));

            for (int i = 0; i < dialogueFilenameArray.Length; i++)
            {
                if (Path.GetExtension(dialogueFilenameArray[i]) == FileExtensions.GetExtensionForFileType(FileExtensions.FileType.Dialogue))
                {
                    string dialogueName = Path.GetFileNameWithoutExtension(dialogueFilenameArray[i]);
                    int senderID = Convert.ToInt32(dialogueName.Replace("dialogue", ""));

                    if (!dialogueDictionary.ContainsKey(senderID))
                    {
                        User recipient =
                            ((ServerGetUserReply)ClientRequestManager.SendClientRequest(new GetUserRequest(mainWindowViewModel.CurrentUser.Cookie, senderID))).User;

                        dialogueDictionary.Add(senderID,
                            new Models.Dialogue(recipient, mainWindowViewModel.CurrentUser));

                        AddDialogueViewModel(dialogueDictionary[senderID]);
                    }
                }
            }

            areDialoguesSorted = false;

            if(DialoguePreviewViewModels.Count > 0)
                SortDialogueViewModels();
        }

        void AddDialogueViewModel(Models.Dialogue dialogue)
        {
            DialoguePreviewViewModel dialoguePreview = new DialoguePreviewViewModel(dialogue);
            dialoguePreview.TileActionTriggered += OnTileAction;
            DateTime lastDialogueTime;

            DialoguePreviewViewModels.Add(dialoguePreview);
        }

        void SortDialogueViewModels()
        {
            if(!areDialoguesSorted)
                DialoguePreviewViewModels = new ObservableCollection<DialoguePreviewViewModel>(DialoguePreviewViewModels.OrderByDescending(viewmodel => viewmodel.LastMessageDateTime));

            areDialoguesSorted = true;
        }

        public void OnTileAction(object sender, TileInteractionEventArgs e)
        {
            if (sender is DialoguePreviewViewModel tileViewModel)
            {
                switch (e.Action)
                {
                    case TileInteractionEventArgs.TileAction.OpenPicture:
                        break;
                    case TileInteractionEventArgs.TileAction.OpenProfile:

                        if (mainWindowViewModel.OpenMainTabCommand.CanExecute(e.InteractionID))
                            mainWindowViewModel.OpenMainTabCommand.Execute(e.InteractionID);

                        break;
                    case TileInteractionEventArgs.TileAction.OpenDialogue:

                        Models.Dialogue dialogue;
                        dialogueDictionary.TryGetValue(Convert.ToInt32(e.InteractionID), out dialogue);

                        if (mainWindowViewModel.OpenActiveDialogueCommand.CanExecute(dialogue))
                            mainWindowViewModel.OpenActiveDialogueCommand.Execute(dialogue);

                        break;
                    case TileInteractionEventArgs.TileAction.RemoveDialogue:

                        int dialogueID = Convert.ToInt32(e.InteractionID);

                        dialogueDictionary[dialogueID].DeleteDialogueRecord();
                        dialogueDictionary.Remove(dialogueID);

                        dialoguePreviewViewModels.Remove((DialoguePreviewViewModel)sender);

                        break;
                }

            }
        }

        void OnFlagReceived(object sender, Models.ServerFlagEventArgs e)
        {
            if (!dialogueDictionary.ContainsKey(e.EventTargetID))
            {
                Application.Current.Dispatcher.Invoke(new Action(() =>
                {
                    GetStoredDialogues();
                }));
            }          

            if (e.FlagType == Models.ServerFlagEventArgs.FlagTypes.MessagesPending)
            {
                dialogueDictionary[e.EventTargetID].GetFreshMessages();
                areDialoguesSorted = false;
            }
            if (e.FlagType == Models.ServerFlagEventArgs.FlagTypes.MessageStatusRead)
            {
                dialogueDictionary[e.EventTargetID].MakeSentMessagesRead();
            }

            Application.Current.Dispatcher.Invoke(new Action(() =>
            {
                SortDialogueViewModels();
            }));
        }

        protected override void Dispose(bool disposing)
        {
            if (isDisposed)
            {
                return;
            }

            if (disposing)
            {
                DialoguePreviewViewModels.Clear();
                DialoguePreviewViewModels = null;
                dialogueDictionary.Clear();
                dialogueDictionary = null;
                mainWindowViewModel.MessageFlagReceived -= OnFlagReceived;
            }

            isDisposed = true;

            base.Dispose(disposing);
        }
    }
}
