﻿using Fleck;

namespace Server.Infrastructure.Network.Packets
{
    public class Connection
    {
        private readonly IPacketSerializer<string> _packetSerializer;
        private readonly IWebSocketConnection _socket;
        public GameClient Client;

        public Connection(IWebSocketConnection socket, IPacketSerializer<string> packetSerializer)
        {
            _socket = socket;
            _packetSerializer = packetSerializer;
        }

        public bool IsAvailable
        {
            get { return _socket.IsAvailable; }
        }

        public string Address
        {
            get { return _socket.ConnectionInfo.ClientIpAddress; }
        }


        public void Send<T>(T packet) where T : IPacket
        {
            _socket.Send(_packetSerializer.Serialize(packet));
        }
    }
}