using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Globalization;
using ChatBubble;


namespace ChatBubbleClientWPF.Models
{
    class Dialogue
    {
        ISpecialSymbolConverter converter = new GenericSpecialSymbolConverter();
        ClientFileManager fileManager = new ClientFileManager();
        readonly object queueLock = new object();

        public enum DialogueStates { Default, MessageSending, MessageLoading}

        public int DialogueID { get; set; }
        public User Recipient { get; set; }
        public CurrentUser CurrentUser { get; set; }
        public Dictionary<int, Message> Messages { get; set; }
        public Queue<Message> PendingMessages { get; set; }
        public Message LastMessage;

        EventHandler<EventArgs> messageQueued;
        public EventHandler<DialogueMessagesChangedEventArgs> DialogueMessagesChanged;

        public Dialogue(User recipient, CurrentUser currentUser)
        {
            DialogueID = recipient.ID;
            Recipient = recipient;
            CurrentUser = currentUser;

            PendingMessages = new Queue<Message>();

            GetStoredMessages();
        }

        void GetStoredMessages()
        {
            IWritableFileContent fileContent = fileManager.ReadFromFile(ClientFileManager.defaultLocalUserDialoguesDirectory + "chatid=" + 
                DialogueID + FileExtensions.FileExtensionDictionary[FileExtensions.FileType.ClientMessages]);

            if (String.IsNullOrEmpty(fileContent.WritableContents))
                return;

            Messages = new Dictionary<int, Message>();

            if (fileContent is EntrySeparatedFileContents contents)
            {
                foreach (MessageFileEntry entry in contents.EntryDictionary.Values)
                {
                    if(entry.Content is Dictionary<string, FileEntry> innerEntries)
                    {
                        Messages.Add((int)innerEntries["id"].Content,
                            new Message((int)innerEntries["id"].Content, (string)innerEntries["time"].Content,
                            (string)innerEntries["status"].Content, (string)innerEntries["content"].Content));
                    }
                }

                LastMessage = Messages.Values.Last();
            }           
        }

        public void DeleteDialogueRecord()
        {
            fileManager.RemoveFile(ClientFileManager.defaultLocalUserDialoguesDirectory + "chatid=" +
                DialogueID + FileExtensions.FileExtensionDictionary[FileExtensions.FileType.ClientMessages]);
        }

        public void PendMessage(string content)
        {
            if (String.IsNullOrEmpty(content))
                return;

            Message newMessage = new Message(LastMessage.MessageID, content);

            Messages.Add(newMessage.MessageID, newMessage);
            PendingMessages.Enqueue(newMessage);

            DialogueMessagesChanged?.Invoke(this, new DialogueMessagesChangedEventArgs()
            { MessageID = newMessage.MessageID, NewMessageState = newMessage.Status });

            Task.Run(() => SendQueuedMessage());
        }

        void SendQueuedMessage()
        {
            lock(queueLock)
            {
                Message pendingMessage = PendingMessages.Dequeue();

                string reply = NetComponents.ClientSendMessage(Recipient.ID.ToString(), pendingMessage.MessageContent);

                if (reply == NetComponents.ConnectionCodes.MsgSendSuccess)
                {
                    pendingMessage.Status = Message.MessageStatus.SentRead;
                }
                else pendingMessage.Status = Message.MessageStatus.SendFailed;

                LastMessage = pendingMessage;

                RecordMessageLocally(pendingMessage);

                DialogueMessagesChanged?.Invoke(this, new DialogueMessagesChangedEventArgs()
                { MessageID = pendingMessage.MessageID, NewMessageState = pendingMessage.Status});                
            }
        }

        void RecordMessageLocally(Message message)
        {
            MessageFileEntry messageEntry =
                new MessageFileEntry(MessagesFileContent.MessageEntryKeys[MessageFileEntry.MessageEntryType.Message] + message.MessageID.ToString(),
                message.MessageID, message.GetStatusAsString(),
                message.MessageDateTime.ToString("dddd, dd MMMM yyyy HH: mm:ss", CultureInfo.InvariantCulture), message.MessageContent, converter);

            MessagesFileContent fileContent = new MessagesFileContent(new Dictionary<string, FileEntry>() { [messageEntry.EntryName] = messageEntry }, converter);

            fileManager.WriteToFile(ClientFileManager.defaultLocalUserDialoguesDirectory + "chatid=" + DialogueID
                + FileExtensions.FileExtensionDictionary[FileExtensions.FileType.ClientMessages], fileContent, true);
        }
    }

    public class DialogueMessagesChangedEventArgs : EventArgs
    {
        internal int MessageID { get; set; }

        internal Message.MessageStatus NewMessageState { get; set; }
    }
}
