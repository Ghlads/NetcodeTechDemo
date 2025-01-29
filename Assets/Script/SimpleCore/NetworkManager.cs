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
    RepComponentData = 3,
    CreateCube = 4,
}

public struct PacketTargetUnion 
{
    public INetConnection Connection;
    public Packet Packet;
    public bool IsGlobalBroadcast;
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
    [SerializeField][Tooltip("percentage of packet dropped")] private int m_packetLoss = 0;
    [SerializeField] private Material m_material = null;
    [SerializeField] private LayerMask m_layerMask = 0;
    [SerializeField] private NetPrefabSetupController m_netPrefabSetupController = null;
    [SerializeField] private Transform m_prefabSpawnPoint = null;

    private float m_nextNetProcessTime = 0.0f;
    private List<RepComponent> m_repComponents = new List<RepComponent>();

    private AsyncNetQueue<Packet> m_receivedPacketQueue = new AsyncNetQueue<Packet>();
    private AsyncNetQueue<PacketTargetUnion> m_toSendPacketQueue = new AsyncNetQueue<PacketTargetUnion>();

    public Material Material => m_material;
    public LayerMask LayerMask => m_layerMask;

    //server variables
    [Header( "Server" )]
    [SerializeField] private bool m_isServer = false;
    private List<INetConnection> m_clients = new List<INetConnection>();
    public event Action<INetConnection> OnClientConnected;

    public bool IsServer => m_isServer;

    //client variables
    [Header( "Client" )]
    [SerializeField] private bool m_isConnected = false;
    private INetConnection m_server = null;
    public event Action OnConnected;

    public bool IsConnected => m_isConnected;

    [Header( "Debug" )]
    [SerializeField] private bool m_debug_packet_received = false;


    private void Awake()
    {
        m_nextNetProcessTime = Time.time;
        m_networkInterface.Connect( this );
    }

    private void Update()
    {
        if ( Time.time >= m_nextNetProcessTime )
        {
            m_nextNetProcessTime = Time.time + m_netFrequency;
            // process received packets first these packet might enqueue new packet to send
            ProcessReceivedPackets();
            ProcessComponentToReplicate();
            ProcessToSendPackets();
        }
    }

    private void ProcessComponentToReplicate()
    {
        foreach ( RepComponent repComponent in m_repComponents )
        {
            if ( repComponent.NetRole == NetRole.Authoritative && repComponent.ReplicateOnNetUpdate )
            {
                if ( m_isServer )
                {
                    BroadcastToQueue( repComponent.OnReplicateValues() );
                }
                else
                {
                    SendToQueue( m_server, repComponent.OnReplicateValues() );
                }

            }
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
            if ( packetTarget.Packet.DeliveryMethod == NetDeliveryMethod.Unreliable && m_packetLoss != 0 && UnityEngine.Random.Range( 0, 100 ) < m_packetLoss ) // simulate packet loss
            {
                continue;
            }

            if ( packetTarget.Connection != null )
            {
                m_networkInterface.Send( packetTarget.Connection, packetTarget.Packet );
            }
            else if ( packetTarget.IsGlobalBroadcast )
            {
                m_networkInterface.Broadcast( packetTarget.Packet );
            }
            else
            {
                foreach ( INetConnection client in m_clients )
                {
                    m_networkInterface.Send( client, packetTarget.Packet );
                }
            }
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
            Packet packet = m_receivedPacketQueue.ActiveQueue.Dequeue();

            if ( packet.DeliveryMethod == NetDeliveryMethod.Unreliable )
            {
                if ( m_packetLoss != 0 && UnityEngine.Random.Range( 0, 100 ) < m_packetLoss ) // simulate packet loss
                {
                    continue;
                }
            }

            HandleFromQueue( packet );
        }
        while ( m_receivedPacketQueue.ActiveQueue.Count > 0 );
    }

    private void SendToQueue( INetConnection target, Packet packet )
    {
        m_toSendPacketQueue.PendingQueue.Enqueue( new PacketTargetUnion { Connection = target, Packet = packet } );
    }

    private void BroadcastToQueue( Packet packet )
    {
        m_toSendPacketQueue.PendingQueue.Enqueue( new PacketTargetUnion { Connection = null, Packet = packet } );
    }

    private void BroadcastToEveryone( Packet packet )
    {
        m_toSendPacketQueue.PendingQueue.Enqueue( new PacketTargetUnion { Connection = null, Packet = packet, IsGlobalBroadcast = true } );
    }

    public void AddRepComponent( RepComponent repComponent )
    {
        if ( repComponent != null && !m_repComponents.Contains( repComponent ) )
        {
            m_repComponents.Add( repComponent );
        }
    }


    public void RemoveRepComponent( RepComponent repComponent ) 
    {
        m_repComponents.Remove( repComponent );
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
        if ( m_debug_packet_received )
        {
            Debug.Log( $"ip : {ConnectionToString()} received packet type : {packet.Type} from ip : {packet.Sender.ConnectionToString()} as {packet.DeliveryMethod}" );
        }

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

        if ( packet.Type == PacketType.ConnectionResponse )
        {
            if ( packet.Bytes[0] == 1 )
            {
                byte[] bytes = new byte[]
                {
                    ( byte )GameNetEventCode.Discovery
                    , (byte )(m_isServer ? NetMode.Server : NetMode.Client)
                };

                BroadcastToEveryone( new( PacketType.Data, this, bytes, NetDeliveryMethod.Reliable ) );
                return;
            }
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
                case GameNetEventCode.RepComponentData:
                    HandleRepComponent( packet );
                    break;
                case GameNetEventCode.CreateCube:
                    HandleCreateCubePacket( packet );
                    break;
                default:
                    break;
            }
        }
    }

    private void HandleRepComponent( Packet packet )
    {
        NetID netID = RepComponent.GetNetID( packet );
        if ( netID == NetID.Invalid )
        {
            return;
        }

        foreach ( RepComponent repComponent in m_repComponents )
        {
            if ( repComponent.NetID == netID && repComponent.NetRole == NetRole.Proxy )
            {
                repComponent.OnValuesReplicated( packet );
                if ( IsServer ) // Since client can't send packet directly to other client server has to broadcast info to every one else
                {
                    Packet newPacket = new Packet( packet.Type, this, packet.Bytes );
                    foreach ( INetConnection client in m_clients )
                    {
                        if ( !client.Equals( packet.Sender ) && !client.Equals( this ) )
                        {
                            SendToQueue( client, newPacket );
                        }
                    }
                }

                return;
            }
        }

        Debug.LogWarning( "RepComponent not found might be a desync somewhere" );
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
                SendToQueue( packet.Sender, new Packet( PacketType.Data, this, bytes, NetDeliveryMethod.Reliable ) );
            }
        }
        else
        {
            if ( !senderIsClient && !m_isConnected )
            {
                SendToQueue( packet.Sender, new Packet( PacketType.Data, this, new byte[] { ( byte )GameNetEventCode.JoinRequest }, NetDeliveryMethod.Reliable ) );
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
        if ( response == CommonResponse.Accepted )
        {
            m_clients.Add( packet.Sender );
            OnClientConnected?.Invoke( packet.Sender );
            Debug.Log( "Client connected" );
        }

        byte[] bytes = new byte[]
        {
            ( byte )GameNetEventCode.JoinResponse,
            ( byte )response
        };
        SendToQueue( packet.Sender, new Packet( PacketType.Data, this, bytes, NetDeliveryMethod.Reliable ) );

    }

    private void HandleJoinResponse( Packet packet )
    {
        if ( m_isServer )
        {
            Debug.LogWarning( "Server should not receive JoinResponse" );
            return;
        }

        if ( m_isConnected )
        {
            Debug.LogWarning( $"Already connected, ip : {ConnectionToString()}" );
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

    private void HandleCreateCubePacket( Packet packet )
    {
        NetID netID = ( NetID )packet.Bytes[1];
        NetPrefabSetup netPrefabSetup = new NetPrefabSetup( netID, this );
        Instantiate( m_netPrefabSetupController, parent: m_prefabSpawnPoint ).NetInit( netPrefabSetup );
        if ( m_isServer )
        {
            Packet newPacket = new Packet( packet.Type, this, packet.Bytes ); // specific unreliable for presentation purposes. Should be reliable since it's an important and non redondant info
            BroadcastToQueue( packet );
        }
    }

    public void SendPacketToServer( Packet packet )
    {
        if ( m_isServer )
        {
            return;
        }

        SendToQueue( m_server, packet );
    }
}
