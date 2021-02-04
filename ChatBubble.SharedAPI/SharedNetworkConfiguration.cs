using System;
using System.Collections.Generic;
using System.Text;
using System.Net;
using System.Net.Sockets;

namespace ChatBubble.SharedAPI
{
    public static class SharedNetworkConfiguration
    {
        public static Encoding Encoding { get; private set; } = Encoding.Unicode;

        public static Socket MainSocket { get; set; }
        public static Socket AuxilarryUDPSocket { get; set; }
    }

    public class NetworkConfigurationException : Exception
    {
        public NetworkConfigurationException(string message) : base(message) { }
    }
}
