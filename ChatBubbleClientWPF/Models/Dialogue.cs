using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Globalization;

using ChatBubble;
using ChatBubble.FileManager;
using ChatBubble.SharedAPI;
using ChatBubble.ClientAPI;

namespace ChatBubbleClientWPF.Models
{
    class Dialogue
    {
        FileManager fileManager = new FileManager();
        readonly object queueLock = new object();

        public enum DialogueStates { Default, MessageSending, MessageLoading}

        public int DialogueID { get; set; }
        public User Recipient { get; set; }
        public CurrentUser CurrentUser { get; set; }
        public Dictionary<int, Message> Messages { get; set; }
        public Queue<Message> PendingMessages { get; set; }

        public EventHandler<DialogueMessagesChangedEventArgs> DialogueMessagesChanged;

        string dialogueFilePath;

        public Dialogue(User recipient, CurrentUser currentUser)
        {
            DialogueID = recipient.ID;
            Recipient = recipient;
            CurrentUser = currentUser;

            PendingMessages = new Queue<Message>();

            dialogueFilePath = ClientDirectories.GetUserDataFolder(ClientDirectories.UserDataDirectoryType.Dialogues, CurrentUser.ID)
                + @"\dialogue" + DialogueID.ToString() + FileExtensions.GetExtensionForFileType(FileExtensions.FileType.Dialogue);

            GetStoredMessages();
        }

        void GetStoredMessages()
        {
            Dictionary<int, object> dialogueContents = new Dictionary<int, object>();

            try
            {
                dialogueContents = fileManager.ReadFromFile(dialogueFilePath);
            }
            catch
            {
                dialogueContents = new Dictionary<int, object>();
                fileManager.TryCreateFile(dialogueFilePath);
            }

            try
            {
                Messages = dialogueContents.ToDictionary(p => p.Key, p => (Message)p.Value);
            }
            catch
            {
                return;
            }     
        }

        public void DeleteDialogueRecord()
        {
            fileManager.DeleteFile(dialogueFilePath);
        }

        public void PendMessage(string content)
        {
            if (String.IsNullOrEmpty(content))
                return;

            Message newMessage;

            if (Messages.Count > 0)
            {
                newMessage = new Message(Messages.Last().Key + 1, content);
            }
            else
            {
                newMessage = new Message(0, content);
            }

            Messages.Add(newMessage.ID, newMessage);
            PendingMessages.Enqueue(newMessage);

            DialogueMessagesChanged?.Invoke(this, new DialogueMessagesChangedEventArgs()
            { MessageID = newMessage.ID, NewMessageState = newMessage.Status });

            Task.Run(() => SendQueuedMessage());
        }

        void SendQueuedMessage()
        {
            lock(queueLock)
            {
                Message pendingMessage = PendingMessages.Dequeue();

                SendMessageRequest messageRequest = new SendMessageRequest(pendingMessage, CurrentUser.ID, Recipient.ID);
                GenericServerReply serverReply = null;
                try
                {
                    serverReply = ClientRequestManager.SendClientRequest(messageRequest);
                }
                catch 
                {
                    pendingMessage.Status = Message.MessageStatus.SendFailed;
                }

                if (serverReply != null && serverReply.NetFlag == ConnectionCodes.MsgSendSuccess)
                {
                    pendingMessage.Status = Message.MessageStatus.SentRead;
                }

                RecordMessageLocally(pendingMessage);

                DialogueMessagesChanged?.Invoke(this, new DialogueMessagesChangedEventArgs()
                { MessageID = pendingMessage.ID, NewMessageState = pendingMessage.Status});                
            }
        }

        void RecordMessageLocally(Message message)
        {
            fileManager.AppendToFile(dialogueFilePath, message, message.ID);
        }
    }

    public class DialogueMessagesChangedEventArgs : EventArgs
    {
        internal int MessageID { get; set; }

        internal Message.MessageStatus NewMessageState { get; set; }
    }
}
