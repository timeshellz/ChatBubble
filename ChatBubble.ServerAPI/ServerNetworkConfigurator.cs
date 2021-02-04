using System;
using System.Collections.Generic;
using System.Text;
using System.Net;
using System.Net.Sockets;

using ChatBubble.SharedAPI;

namespace ChatBubble.ServerAPI
{
    public class ServerNetworkConfigurator : INetworkConfigurator
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

        public void BindSockets()
        {
            try
            {
                SharedNetworkConfiguration.MainSocket.Bind(ServerNetworkConfiguration.LocalTCPIPEndPoint);
                SharedNetworkConfiguration.AuxilarryUDPSocket.Bind(ServerNetworkConfiguration.LocalUDPIPEndPoint);
            }
            catch (Exception e)
            {
                DisconnectSockets(false);
                throw new NetworkConfigurationException("Sockets not bound: " + e.Message);
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

        public void SetPorts(int portNumber)
        {
            try
            {
                ServerNetworkConfiguration.LocalTCPPortNumber = portNumber;
                ServerNetworkConfiguration.LocalUDPPortNumber = portNumber;
            }
            catch
            {
                throw new NetworkConfigurationException("Unable to set port numbers.");
            }
        }

        public void SetLocalEndpoints(string ipAddress)
        {
            try
            {
                ServerNetworkConfiguration.LocalAddress = IPAddress.Parse(ipAddress);
                ServerNetworkConfiguration.LocalTCPIPEndPoint = new IPEndPoint(ServerNetworkConfiguration.LocalAddress, ServerNetworkConfiguration.LocalTCPPortNumber);
                ServerNetworkConfiguration.LocalUDPIPEndPoint = new IPEndPoint(ServerNetworkConfiguration.LocalAddress, ServerNetworkConfiguration.LocalUDPPortNumber);
            }
            catch
            {
                throw new NetworkConfigurationException("Local Endpoints not set: unable to parse provided ip address.");
            }
        }

        public void SetLocalEndpoints()
        {
            IPHostEntry localMachineIP = Dns.GetHostByName(Dns.GetHostName());
            IPAddress[] ipAddressMass = localMachineIP.AddressList;

            string ipAddress = ipAddressMass[ipAddressMass.Length - 1].ToString();

            try
            {
                SetLocalEndpoints(ipAddress);
            }
            catch
            {
                throw new NetworkConfigurationException("Local Endpoints not set: Unable to auto-detect correct ip address.");
            }
        }
    }
}
