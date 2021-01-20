using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using ChatBubble;


namespace ChatBubbleClientWPF.Models
{
    class Dialogue
    {
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
            string dialogueContent = FileIOStreamer.ReadFromFile(FileIOStreamer.defaultLocalUserDialoguesDirectory + "chatid=" + DialogueID + ".txt");

            if (!String.IsNullOrEmpty(dialogueContent))
            {
                Messages = new Dictionary<int, Message>();

                string[] messages = dialogueContent.Split(new string[] { "message==", "==message" }, StringSplitOptions.RemoveEmptyEntries);

                int messageID = 0;
                foreach(string messageData in messages)
                {
                    string[] messageSubstrings = messageData.Split(new string[] { "time=", "status=", "content=" }, StringSplitOptions.RemoveEmptyEntries);
                Messages.Add(messageID, new Message(messageID, Recipient, messageSubstrings[0], messageSubstrings[1], messageSubstrings[2]));

                    messageID++;
                 }

                LastMessage = Messages.Values.Last();
            }
        }

        public void DeleteDialogueRecord()
        {
            FileIOStreamer.RemoveFile(FileIOStreamer.defaultLocalUserDialoguesDirectory + "chatid=" + DialogueID + ".txt");
        }

        public void PendMessage(string content)
        {
            if (String.IsNullOrEmpty(content))
                return;

            Message newMessage = new Message(Messages.Keys.Count, CurrentUser, content);

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

                DialogueMessagesChanged?.Invoke(this, new DialogueMessagesChangedEventArgs()
                { MessageID = pendingMessage.MessageID, NewMessageState = pendingMessage.Status});                
            }
        }
    }

    public class DialogueMessagesChangedEventArgs : EventArgs
    {
        internal int MessageID { get; set; }

        internal Message.MessageStatus NewMessageState { get; set; }
    }
}
