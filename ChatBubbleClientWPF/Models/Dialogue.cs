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
        public List<Message> UnreadReceivedMessages { get; set; }
        public List<Message> UnreadSentMessages { get; set; }

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

        void GetStoredMessages(int startIndex = 0)
        {
            Dictionary<int, object> dialogueContents = new Dictionary<int, object>();

            if (!fileManager.FileExists(dialogueFilePath))
            {
                dialogueContents = new Dictionary<int, object>();
                startIndex = 0;
                fileManager.TryCreateFile(dialogueFilePath);
            }
            else
                dialogueContents = fileManager.ReadFromFile(dialogueFilePath, startIndex);           

            UnreadReceivedMessages = new List<Message>();
            UnreadSentMessages = new List<Message>();

            if (Messages == null)
                Messages = new Dictionary<int, Message>();

            try
            {
                foreach (KeyValuePair<int, object> pair in dialogueContents)
                {
                    Message message = (Message)pair.Value;
                    Messages.Add(pair.Key, message);

                    if (message.Status == Message.MessageStatus.ReceivedNotRead)
                        UnreadReceivedMessages.Add(message);
                    if (message.Status == Message.MessageStatus.SentReceived)
                        UnreadSentMessages.Add(message);

                    if(startIndex != 0)
                        DialogueMessagesChanged?.Invoke(this, new DialogueMessagesChangedEventArgs()
                    { MessageID = message.ID, NewMessageState = message.Status });
                }
            }
            catch
            {
                return;
            }

            string dialogueStatus = ClientRequestManager.SendClientRequest(new GetDialogueStatusRequest(CurrentUser.Cookie, DialogueID)).NetFlag;

            if (dialogueStatus == ConnectionCodes.MessagesReadStatus)
                MakeSentMessagesRead();
        }

        public void GetFreshMessages()
        {
            int lastMessageID = Messages.Keys.Max();

            GetStoredMessages(lastMessageID + 1);
        }

        public void DeleteDialogueRecord()
        {
            fileManager.DeleteFile(dialogueFilePath);
        }

        public void DeleteMessage(Message message)
        {
            fileManager.RemoveObjectFromFile(dialogueFilePath, message.ID);
        }

        public void PendMessage(string content)
        {
            if (String.IsNullOrEmpty(content))
                return;

            Message newMessage;

            if (Messages.Count > 0)
            {
                newMessage = new Message(Messages.Keys.Max() + 1, content);
            }
            else
            {
                newMessage = new Message(0, content);
            }

            PendMessage(newMessage);
        }

        public void PendMessage(Message message)
        {
            MakeReceivedMessagesRead();

            message.Status = Message.MessageStatus.Sending;

            if (Messages.ContainsKey(message.ID))
            {
                DeleteMessage(Messages[message.ID]);
                Messages.Remove(message.ID);

                message.SetID(Messages.Keys.Max() + 1);
            }

            Messages.Add(message.ID, message);

            PendingMessages.Enqueue(message);

            DialogueMessagesChanged?.Invoke(this, new DialogueMessagesChangedEventArgs()
            { MessageID = message.ID, NewMessageState = message.Status });

            Task.Run(() => SendQueuedMessage());
        }

        void SendQueuedMessage()
        {
            lock (queueLock)
            {
                Message pendingMessage = PendingMessages.Dequeue();

                SendMessageRequest messageRequest = new SendMessageRequest(pendingMessage, CurrentUser.ID, Recipient.ID);
                GenericServerReply serverReply = null;
                try
                {
                    serverReply = ClientRequestManager.SendClientRequest(messageRequest);
                }
                catch (RequestException e)
                {
                    pendingMessage.Status = Message.MessageStatus.SendFailed;
                }

                if (serverReply != null && serverReply.NetFlag == ConnectionCodes.MsgSendSuccess)
                {
                    pendingMessage.Status = Message.MessageStatus.SentReceived;
                    UnreadSentMessages.Add(pendingMessage);
                }

                RecordMessagesLocally(pendingMessage, DialogueID, CurrentUser.Cookie);

                DialogueMessagesChanged?.Invoke(this, new DialogueMessagesChangedEventArgs()
                { MessageID = pendingMessage.ID, NewMessageState = pendingMessage.Status });
            }
        }

        public void MakeReceivedMessagesRead()
        {
            foreach (Message message in UnreadReceivedMessages)
            {
                message.Status = Message.MessageStatus.ReceivedRead;

                DialogueMessagesChanged?.Invoke(this, new DialogueMessagesChangedEventArgs()
                { MessageID = message.ID, NewMessageState = message.Status });
            }
            
            RecordMessagesLocally(UnreadReceivedMessages, DialogueID, CurrentUser.Cookie);
            ClientRequestManager.SendClientRequest(new ChangeDialogueStatusRequest(CurrentUser.Cookie, DialogueID, ConnectionCodes.MessagesReadStatus));

            UnreadReceivedMessages.Clear();
        }

        public void MakeSentMessagesRead()
        {
            foreach (Message message in UnreadSentMessages)
            {
                message.Status = Message.MessageStatus.SentRead;

                DialogueMessagesChanged?.Invoke(this, new DialogueMessagesChangedEventArgs()
                { MessageID = message.ID, NewMessageState = message.Status });
            }

            RecordMessagesLocally(UnreadSentMessages, DialogueID, CurrentUser.Cookie);
            UnreadSentMessages.Clear();
        }

        public static void RecordMessagesLocally(Message message, int dialogueID, Cookie cookie)
        {
            FileManager fileManager = new FileManager();
            string dialoguePath = ClientDirectories.GetUserDataFolder(ClientDirectories.UserDataDirectoryType.Dialogues, cookie.ID)
                + @"\dialogue" + dialogueID + FileExtensions.GetExtensionForFileType(FileExtensions.FileType.Dialogue);

            int lastMetadataKey;

            try
            {
                lastMetadataKey = fileManager.GetLastMetadataKey(dialoguePath);
            }
            catch
            {
                fileManager.TryCreateFile(dialoguePath);
                lastMetadataKey = 0;
            }

            if (message.Status == Message.MessageStatus.Pending)
            {
                message.Status = Message.MessageStatus.ReceivedNotRead;
                message.SetID(lastMetadataKey + 1);
            }    

            if (!fileManager.FileContainsObjectKey(dialoguePath, message.ID))
                fileManager.AppendToFile(dialoguePath, message, message.ID);
            else
                fileManager.ReplaceObjectInFile(dialoguePath, message, message.ID);
        }

        public static void RecordMessagesLocally(List<Message> messages, int dialogueID, Cookie cookie)
        {
            FileManager fileManager = new FileManager();
            string dialoguePath = ClientDirectories.GetUserDataFolder(ClientDirectories.UserDataDirectoryType.Dialogues, cookie.ID)
                + @"\dialogue" + dialogueID + FileExtensions.GetExtensionForFileType(FileExtensions.FileType.Dialogue);

            Dictionary<int, object> messagesDictionary = new Dictionary<int, object>();
            int lastMetadataKey;

            try
            {
                lastMetadataKey = fileManager.GetLastMetadataKey(dialoguePath);
            }
            catch
            {
                fileManager.TryCreateFile(dialoguePath);
                lastMetadataKey = 0;
            }

            foreach (Message message in messages)
            {
                if (message.Status == Message.MessageStatus.Pending)
                {
                    message.SetID(++lastMetadataKey);
                    message.Status = Message.MessageStatus.ReceivedNotRead;
                }

                messagesDictionary.Add(message.ID, message);
            }

            fileManager.AppendToFile(dialoguePath, messagesDictionary);
        }
    }

    public class DialogueMessagesChangedEventArgs : EventArgs
    {
        internal int MessageID { get; set; }

        internal Message.MessageStatus NewMessageState { get; set; }
    }
}
