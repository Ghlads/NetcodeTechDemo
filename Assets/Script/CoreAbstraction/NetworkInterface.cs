using UnityEngine;

public abstract class NetworkInterface : ScriptableObject  
{
    public abstract void Connect( INetConnection connection );
           
    public abstract void Disconnect( INetConnection connection, DisconnectionReason reason );
           
    public abstract void Send( INetConnection connection, Packet packet, NetDeliveryMethod netDeliveryMethod = NetDeliveryMethod.Unreliable );
}