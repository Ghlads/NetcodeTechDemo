using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu( fileName = "SimpleNetworkInterface", menuName = "NetcodeTechDemo/NetworkInterface/SimpleNetworkInterface" )]
public class SimpleNetworkInterface : NetworkInterface
{
    private List<INetConnection> m_connections = new List<INetConnection>();

    public override void Broadcast( Packet packet )
    {
        foreach ( INetConnection connection in m_connections )
        {
            if ( connection != packet.Sender )
            {
                connection.Handle( packet );
            }
        }
    }

    public override void Connect( INetConnection connection )
    {
        if ( m_connections.Contains( connection ) )
        {
            connection.Handle( GenericPacketUtils.ConnectionDenialPacket( connection, "Already connected", NetDeliveryMethod.Reliable ) );
            return;
        }

        m_connections.Add( connection );
        connection.Handle( GenericPacketUtils.ConnectionApprovalPacket( connection, NetDeliveryMethod.Reliable ) );
    }

    public override void Disconnect( INetConnection connection, DisconnectionReason reason )
    {
        if ( m_connections.Contains( connection ) )
        {
            m_connections.Remove( connection );
            connection.Handle( GenericPacketUtils.DisconnectionNoticePacket( connection, reason, NetDeliveryMethod.Reliable ) );
        }
        else
        {
            connection.Handle( GenericPacketUtils.ErrorPacket( connection, ErrorCode.GenericError, "Not connected", NetDeliveryMethod.Reliable ) );
        }
    }

    public override void Send( INetConnection connection, Packet packet )
    {
        if ( m_connections.Contains( connection ) )
        {
            connection.Handle( packet );
        }
        else
        {
            packet.Sender.Handle( GenericPacketUtils.ErrorPacket( connection, ErrorCode.GenericError, "Target not connected", NetDeliveryMethod.Reliable ) );
        }
    }
}