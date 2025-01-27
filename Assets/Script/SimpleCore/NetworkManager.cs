using System;
using System.Collections.Generic;
using UnityEngine;

public static class ClientServerConstants
{
    public const int NET_EVENT_CODE_INDEX = 0;
    public const int NET_MODE_CODE_INDEX = 1;
    public const int JOIN_RESPONSE_INDEX = 1;
}

public enum GameNetEventCode : byte
{
    Discovery = 0,
    JoinRequest = 1,
    JoinResponse = 2,
}

public struct PacketTargetUnion 
{
    public INetConnection Connection;
    public Packet Packet;
}

public class AsyncNetQueue<T> 
{
    private Queue<T> m_activeQueue = new Queue<T>();
    private Queue<T> m_pendingQueue = new Queue<T>();

    public Queue<T> ActiveQueue => m_activeQueue;
    public Queue<T> PendingQueue => m_pendingQueue;

    public void SwapQueue()
    {
        Queue<T> temp = m_activeQueue;
        m_activeQueue = m_pendingQueue;
        m_pendingQueue = temp;
    }
}


public class NetworkManager : MonoBehaviour, INetConnection
{
    [SerializeField] private int m_ip = 0;
    [SerializeField] private NetworkInterface m_networkInterface = null;
    [SerializeField][Tooltip("frequence where queues are processed")] private float m_netFrequency = 0.1f;
    private float m_nextNetProcessTime = 0.0f;

    private AsyncNetQueue<Packet> m_receivedPacketQueue = new AsyncNetQueue<Packet>();
    private AsyncNetQueue<PacketTargetUnion> m_toSendPacketQueue = new AsyncNetQueue<PacketTargetUnion>();

    //server variables
    [Header( "Server" )]
    [SerializeField] private bool m_isServer = false;
    private List<INetConnection> m_clients = new List<INetConnection>();
    public event Action<INetConnection> OnClientConnected;

    //client variables
    [Header( "Client" )]
    [SerializeField] private bool m_isConnected = false;
    private INetConnection m_server = null;
    public event Action OnConnected;

    private void Awake()
    {
        m_nextNetProcessTime = Time.time;
        byte[] bytes = new byte[]
        {
            ( byte )GameNetEventCode.Discovery
            , (byte )(m_isServer ? NetMode.Server : NetMode.Client)
        };
        Packet discoverPacket = new( PacketType.Data, this, bytes );
        m_networkInterface.Connect( this, discoverPacket );
    }

    private void Update()
    {
        if ( Time.time >= m_nextNetProcessTime )
        {
            m_nextNetProcessTime = Time.time + m_netFrequency;
            // process received packets first these packet might enqueue new packet to send
            ProcessReceivedPackets(); 
            ProcessToSendPackets();
        }
    }

    private void ProcessToSendPackets()
    {
        m_toSendPacketQueue.SwapQueue();
        if ( m_toSendPacketQueue.ActiveQueue.Count <= 0 )
        {
            return;
        }

        do
        {
            PacketTargetUnion packetTarget = m_toSendPacketQueue.ActiveQueue.Dequeue();
            m_networkInterface.Send( packetTarget.Connection, packetTarget.Packet );
        }
        while ( m_toSendPacketQueue.ActiveQueue.Count > 0 );
    }

    private void ProcessReceivedPackets()
    {
        m_receivedPacketQueue.SwapQueue();
        if ( m_receivedPacketQueue.ActiveQueue.Count <= 0 )
        {
            return;
        }

        do
        {
            HandleFromQueue( m_receivedPacketQueue.ActiveQueue.Dequeue() );
        }
        while ( m_receivedPacketQueue.ActiveQueue.Count > 0 );
    }

    private void SendToQueue( INetConnection target, Packet packet )
    {
        m_toSendPacketQueue.PendingQueue.Enqueue( new PacketTargetUnion { Connection = target, Packet = packet } );
    }

    public string ConnectionToString()
    {
        return m_ip.ToString();
    }

    public bool Equals( INetConnection other )
    {
        return other is NetworkManager manager && m_ip == manager.m_ip;
    }

    public void Handle( Packet packet )
    {
        Debug.Log( "Packet received" );
        m_receivedPacketQueue.PendingQueue.Enqueue( packet );
    }

    public void HandleFromQueue( Packet packet )
    {
        if ( packet == null )
        {
            return;
        }

        if ( GenericPacketUtils.IsPacketAnError( packet ) )
        {
            Debug.LogError( GenericPacketUtils.GetString( packet ) );
            return;
        }

        if ( packet.Type == PacketType.Data )
        {
            byte[] bytes = packet.Bytes;
            switch ( ( GameNetEventCode )bytes[ClientServerConstants.NET_EVENT_CODE_INDEX] )
            {
                case GameNetEventCode.Discovery:
                    HandleDiscoveryPacket( packet );
                    break;
                case GameNetEventCode.JoinRequest:
                    HandleJoinRequest( packet );
                    break;
                case GameNetEventCode.JoinResponse:
                    HandleJoinResponse( packet );
                    break;
                default:
                    break;
            }
        }
    }

    private void HandleDiscoveryPacket( Packet packet )
    {
        bool senderIsClient = ( NetMode )packet.Bytes[ClientServerConstants.NET_MODE_CODE_INDEX] == NetMode.Client;
        if ( m_isServer )
        {
            if ( senderIsClient && !m_clients.Contains( packet.Sender ) )
            {
                byte[] bytes = new byte[]
                {
                    ( byte )GameNetEventCode.Discovery,
                    ( byte )NetMode.Server
                };
                SendToQueue( packet.Sender, new Packet( PacketType.Data, this, bytes ) );
            }
        }
        else
        {
            if ( !senderIsClient && !m_isConnected )
            {
                SendToQueue( packet.Sender, new Packet( PacketType.Data, this, new byte[] { ( byte )GameNetEventCode.JoinRequest } ) );
            }
        }
    }

    private void HandleJoinRequest( Packet packet )
    {
        if ( !m_isServer )
        {
            return;
        }

        CommonResponse response = m_clients.Contains( packet.Sender ) ? CommonResponse.Denied : CommonResponse.Accepted;
        m_clients.Add( packet.Sender );
        OnClientConnected?.Invoke( packet.Sender );
        Debug.Log( "Client connected" );
        byte[] bytes = new byte[]
        {
            ( byte )GameNetEventCode.JoinResponse,
            ( byte )response
        };
        SendToQueue( packet.Sender, new Packet( PacketType.Data, this, bytes ) );

    }

    private void HandleJoinResponse( Packet packet )
    {
        if ( m_isServer )
        {
            return;
        }

        CommonResponse response = ( CommonResponse )packet.Bytes[ClientServerConstants.JOIN_RESPONSE_INDEX];
        if ( response == CommonResponse.Accepted )
        {
            m_isConnected = true;
            m_server = packet.Sender;
            OnConnected?.Invoke();
            Debug.Log( "Connected to server" );
        }
        else
        {
            Debug.LogError( "Connection denied" );
        }
    }
}
