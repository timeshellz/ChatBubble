using System;
using System.Collections.Generic;
using System.Text;
using System.Net.Sockets;

namespace ChatBubble.SharedAPI
{
    public class RequestException : Exception
    {
        public string ExceptionCode { get; private set; }
        public Socket RequestSender { get; private set; }

        public RequestException(string exceptionCode)
        {
            ExceptionCode = exceptionCode;
        }

        public RequestException(Socket senderSocket, string exceptionCode) : this(exceptionCode)
        {
            RequestSender = senderSocket;
        }
    }
}
