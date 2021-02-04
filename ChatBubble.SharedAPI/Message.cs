using System;
using ProtoBuf;

namespace ChatBubble.SharedAPI
{
    [Serializable]
    [ProtoContract]
    [ProtoInclude(5, typeof(MessageStatus))]
    public sealed class Message
    {
        [ProtoContract]
        public enum MessageStatus { ReceivedRead, ReceivedNotRead, Sending, SendFailed, SentServerPending, SentReceived, SentRead }

        [ProtoMember(1)]
        public int ID { get; private set; }
        [ProtoMember(2)]
        public string MessageContent { get; private set; }
        [ProtoMember(3)]
        public DateTime MessageDateTime { get; private set; }
        [ProtoMember(4)]
        public MessageStatus Status { get; set; }

        private Message() { }

        public Message(int messageID, string time, string status, string content)
        {
            MessageDateTime = DateTime.Parse(time).ToLocalTime();

            if (status == "read")
                Status = MessageStatus.ReceivedRead;
            else
            if (status == "sent")
                Status = MessageStatus.SentRead;
            else
            if (status == "unread")
                Status = MessageStatus.ReceivedNotRead;
            else Status = MessageStatus.SentRead;

            MessageContent = content;
            ID = messageID;
        }

        public Message(int messageID, string content)
        {
            MessageContent = content;
            ID = messageID;

            MessageDateTime = DateTime.Now;
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
    }
}
