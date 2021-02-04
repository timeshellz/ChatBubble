using System;
using System.Collections.Generic;
using System.Text;
using System.Net;
using System.Net.Sockets;

using ChatBubble.SharedAPI;

namespace ChatBubble.ServerAPI
{
    public static class ServerNetworkConfiguration
    {
        public static IPAddress LocalAddress { get; set; }
        public static IPEndPoint LocalTCPIPEndPoint { get; set; }
        public static IPEndPoint LocalUDPIPEndPoint { get; set; }

        public static int LocalTCPPortNumber { get; set; }
        public static int LocalUDPPortNumber { get; set; }
    }
}
