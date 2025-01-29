using UnityEngine;
using UnityEngine.Assertions;

public enum RepComponentPacketIndices
{
    NET_ID = ClientServerConstants.NET_EVENT_CODE_INDEX + 1,
}

public abstract class RepComponent : MonoBehaviour
{
    protected const int MINIMAL_PACKET_SIZE = 2;
    [SerializeField] private NetworkManager m_networkManager = null;
    [SerializeField] private NetID m_netID = default;
    [SerializeField] private bool m_replicateOnNetUpdate = false;
    [SerializeField] private NetRole m_netRole = NetRole.Authoritative;

    public NetworkManager NetworkManager
    {
        get
        {
            return m_networkManager;
        }
        set
        {
            m_networkManager = value;
        }
    }

    public NetID NetID
    {
        get
        {
            return m_netID;
        }
        set
        {
            m_netID = value;
        }
    }

    public bool ReplicateOnNetUpdate
    {
        get
        {
            return m_replicateOnNetUpdate;
        }
        set 
        {
            m_replicateOnNetUpdate = value;
        }
    }

    public NetRole NetRole
    {
        get
        {
            return m_netRole;
        }
        set
        {
            m_netRole = value;
        }
    }

    public static NetID GetNetID( Packet packet )
    {
        if ( packet.Type != PacketType.Data
            || ( GameNetEventCode )packet.Bytes[ClientServerConstants.NET_EVENT_CODE_INDEX] != GameNetEventCode.RepComponentData )
        {
            return NetID.Invalid;
        }

        return ( NetID )packet.Bytes[( int )RepComponentPacketIndices.NET_ID];
    }

    private void Start()
    {
        m_networkManager.AddRepComponent( this );
    }

    private void OnDestroy()
    {
        if ( m_networkManager )
        {
            m_networkManager.RemoveRepComponent( this );
        }
    }

    public abstract Packet OnReplicateValues();

    public abstract void OnValuesReplicated( Packet values );

    protected byte[] GetPacketDataTemplate( int size = MINIMAL_PACKET_SIZE )
    {
        Assert.IsTrue( size >= MINIMAL_PACKET_SIZE );
        byte[] bytes = new byte[size];
        bytes[ClientServerConstants.NET_EVENT_CODE_INDEX] = ( byte )GameNetEventCode.RepComponentData;
        bytes[( int )RepComponentPacketIndices.NET_ID] = ( byte )m_netID;
        return bytes;
    }
}

