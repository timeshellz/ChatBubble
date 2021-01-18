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
        public int DialogueID { get; set; }
        public User Sender { get; set; }
        public User Receipient { get; set; }
        public Dictionary<int, Message> Messages { get; set; }
        public Message LastMessage;

        public Dialogue(User sender, User receipient)
        {
            DialogueID = sender.ID;
            Sender = sender;
            Receipient = receipient;

            GetStoredMessages();
        }

        void GetStoredMessages()
        {
            string dialogueContent = FileIOStreamer.ReadFromFile(FileIOStreamer.defaultLocalUserDialoguesDirectory + "chatid=" + DialogueID + ".txt");

            //if (!String.IsNullOrEmpty(dialogueContent))
            {
                Messages = new Dictionary<int, Message>();

                int messageID = 1;
                //foreach(string messageData in dialogueContent.Split(new string[] { "message==", "==message" }, StringSplitOptions.RemoveEmptyEntries))
                //{
                //    string[] messageSubstrings = messageData.Split(new string[] { "time=", "status=", "content=" }, StringSplitOptions.RemoveEmptyEntries);
                //Messages.Add(messageID, new Message(messageSubstrings[0], messageSubstrings[1], messageSubstrings[2]));

                //    messageID++;
                // }

                Messages.Add(messageID, new Message(Sender, DateTime.Now.ToLongDateString(), "read", "message"));
                Messages.Add(2, new Message(Receipient, DateTime.Now.ToLongDateString(), "sent", "mymessage"));

                LastMessage = Messages.Values.Last();
            }
        }

        public void DeleteDialogueRecord()
        {
            FileIOStreamer.RemoveFile(FileIOStreamer.defaultLocalUserDialoguesDirectory + "chatid=" + DialogueID + ".txt");
        }
    }
}
