using System;
using System.Collections.Generic;
using System.Text;
using System.Net;
using System.Net.Sockets;

using ChatBubble.SharedAPI;

namespace ChatBubble.ClientAPI
{
    public static class ClientNetworkConfiguration
    {
        public static IPAddress ServerAddress { get; set; }
        public static IPEndPoint ServerIPEndPoint { get; set; }

        public static int ServerPortNumber { get; set; }
    }
}
