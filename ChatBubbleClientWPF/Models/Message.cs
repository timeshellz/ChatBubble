﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ChatBubbleClientWPF.Models
{
    class Message
    {
        public enum MessageStatus { ReceivedRead, ReceivedNotRead, Sending, SendFailed, SentServerPending, SentReceived, SentRead }

        public string MessageContent { get; set; }
        public DateTime MessageDateTime { get; set; }
        public User MessageSender { get; set; }
        public MessageStatus Status { get; set; }
        public int MessageID { get; set; }

        public Message(int messageID, User sender, string time, string status, string content)
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
            MessageSender = sender;
            MessageID = messageID;
        }

        public Message(int messageID, User sender, string content)
        {
            MessageSender = sender;
            MessageContent = content;
            MessageID = messageID;

            MessageDateTime = DateTime.Now;
            Status = MessageStatus.Sending;
        }
    }
}
