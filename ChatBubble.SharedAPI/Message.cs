using System;
using ProtoBuf;

namespace ChatBubble.SharedAPI
{
    [Serializable]
    [ProtoContract]
    [ProtoInclude(5, typeof(MessageStatus))]
    public sealed class Message
    {
        [field: NonSerialized]
        public event EventHandler<EventArgs> StatusChanged;

        [ProtoContract]
        public enum MessageStatus { ReceivedRead, ReceivedNotRead, Sending, SendFailed, Pending, SentReceived, SentRead }

        [ProtoMember(1)]
        public int ID { get; private set; }
        [ProtoMember(2)]
        public string Content { get; private set; }
        [ProtoMember(3)]
        public DateTime DateTime { get; private set; }
        [ProtoMember(4)]
        MessageStatus status;
        [ProtoMember(5)]
        public MessageStatus Status 
        {
            get { return status; }
            set
            {
                status = value;
                StatusChanged?.Invoke(this, new EventArgs());
            }
        }

        private Message() { }

        public Message(int messageID, string time, MessageStatus status, string content)
        {
            DateTime = DateTime.SpecifyKind(DateTime.Parse(time), DateTimeKind.Utc);
            
            Status = status;

            Content = content;
            ID = messageID;
        }

        public Message(int messageID, string content)
        {
            Content = content;
            ID = messageID;

            DateTime = DateTime.UtcNow;
            Status = MessageStatus.Sending;
        }

        public string GetStatusAsString()
        {
            switch(Status)
            {
                case MessageStatus.ReceivedRead:
                    return "read";
                case MessageStatus.SentRead:
                    return "sent";
                case MessageStatus.ReceivedNotRead:
                    return "unread";
            }

            return "sent";
        }

        public void SetID(int newID)
        {
            ID = newID;
        }
    }
}
