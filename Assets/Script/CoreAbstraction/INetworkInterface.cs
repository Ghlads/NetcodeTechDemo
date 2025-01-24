using System;
using System.Collections.Generic;

public interface INetworkInterface
{
    public INetConnection GetNetConnection();

    public NetRole GetRole();

    public event Action<Packet> OnPacketReceived;
    public void SendPacket( Packet packet, INetConnection receiverConnection, NetDeliveryMethod deliveryMethod = NetDeliveryMethod.Unreliable );

    public INetConnection GetServerNetConnection();

    public IReadOnlyList<INetConnection> GetConnectedClientsConnection();

    public NetID GetNewNetID();
}