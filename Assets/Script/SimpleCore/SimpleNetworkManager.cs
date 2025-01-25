using UnityEngine;

public class SimpleNetworkManager : MonoBehaviour, INetConnection
{
    [SerializeField] private int m_ip = 0;
    [SerializeField] private NetworkInterface m_networkInterface = null;

    private void Awake()
    {
        m_networkInterface.Connect( this );
    }

    public string ConnectionToString()
    {
        return m_ip.ToString();
    }

    public bool Equals( INetConnection other )
    {
        return other is SimpleNetworkManager manager && m_ip == manager.m_ip;
    }

    public void Handle( Packet packet )
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

        Debug.Log( GenericPacketUtils.GetString( packet ) );
    }
}
