using System;
using System.Collections.Generic;
using System.Text;
using System.Net;
using System.Net.Sockets;

using ChatBubble.SharedAPI;

namespace ChatBubble.ClientAPI
{
    public class ClientNetworkConfigurator : INetworkConfigurator
    {
        public void InitializeSockets()
        {
            try
            {
                SharedNetworkConfiguration.MainSocket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
                SharedNetworkConfiguration.AuxilarryUDPSocket = new Socket(AddressFamily.InterNetwork, SocketType.Dgram, ProtocolType.Udp);
            }
            catch
            {
                throw new NetworkConfigurationException("Unable to initialize sockets.");
            }
        }

        public void DisconnectSockets(bool reuse)
        {
            try
            {
                SharedNetworkConfiguration.MainSocket.Shutdown(SocketShutdown.Both);
                SharedNetworkConfiguration.MainSocket.Close();
            }
            catch
            {
                throw new NetworkConfigurationException("Unable to completely close main socket.");
            }

            try
            {
                SharedNetworkConfiguration.AuxilarryUDPSocket.Shutdown(SocketShutdown.Both);
                SharedNetworkConfiguration.AuxilarryUDPSocket.Close();
            }
            catch
            {
                throw new NetworkConfigurationException("Unable to completely close auxillary socket.");
            }

            if (reuse)
                InitializeSockets();
        }

        public void SetServerEndPoint(string ipAddress, int portNumber)
        {
            try
            {
                ClientNetworkConfiguration.ServerPortNumber = portNumber;
            }
            catch
            {
                throw new NetworkConfigurationException("Unable to set port number.");
            }

            try
            {
                ClientNetworkConfiguration.ServerAddress = IPAddress.Parse(ipAddress);
                ClientNetworkConfiguration.ServerIPEndPoint = new IPEndPoint(ClientNetworkConfiguration.ServerAddress, ClientNetworkConfiguration.ServerPortNumber);
            }
            catch
            {
                throw new NetworkConfigurationException("Local Endpoints not set: unable to parse provided ip address.");
            }
        }
    }
}
