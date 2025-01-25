using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu( fileName = "SimpleNetworkInterface", menuName = "NetcodeTechDemo/NetworkInterface/SimpleNetworkInterface" )]
public class SimpleNetworkInterface : NetworkInterface
{
    private List<INetConnection> m_connections = new List<INetConnection>();

    public override void Connect( INetConnection connection )
    {
        if ( m_connections.Contains( connection ) )
        {
            connection.Handle( GenericPacketUtils.ConnectionDenialPacket( connection, "Already connected" ) );
            return;
        }

        m_connections.Add( connection );
        connection.Handle( GenericPacketUtils.ConnectionApprovalPacket( connection ) );
    }

    public override void Disconnect( INetConnection connection, DisconnectionReason reason )
    {
        if ( m_connections.Contains( connection ) )
        {
            m_connections.Remove( connection );
            connection.Handle( GenericPacketUtils.DisconnectionNoticePacket( connection, reason ) );
        }
        else
        {
            connection.Handle( GenericPacketUtils.ErrorPacket( connection, ErrorCode.GenericError, "Not connected" ) );
        }
    }

    public override void Send( INetConnection connection, Packet packet, NetDeliveryMethod netDeliveryMethod = NetDeliveryMethod.Unreliable )
    {
        if ( m_connections.Contains( connection ) )
        {
            connection.Handle( packet );
        }
        else
        {
            packet.Sender.Handle( GenericPacketUtils.ErrorPacket( connection, ErrorCode.GenericError, "Target not connected" ) );
        }
    }
}