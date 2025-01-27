using UnityEngine;

public abstract class NetworkInterface : ScriptableObject  
{
    public abstract void Connect( INetConnection connection, Packet discoverPacket );
           
    public abstract void Disconnect( INetConnection connection, DisconnectionReason reason );
           
    public abstract void Send( INetConnection connection, Packet packet, NetDeliveryMethod netDeliveryMethod = NetDeliveryMethod.Unreliable );

    public abstract void Broadcast( Packet packet, NetDeliveryMethod netDeliveryMethod = NetDeliveryMethod.Unreliable );
}