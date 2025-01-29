using UnityEngine;

// this is an horrible implementation of what ever I was trying to do
// It was 11 PM and had to make this work X)
public class NetPrefabSetupController : MonoBehaviour 
{
    [SerializeField] private RepComponent m_repComponent;
    [SerializeField] private MeshRenderer m_meshRenderer;
    private NetworkManager m_networkManager;
    [SerializeField] private Rigidbody m_rigidbody;
    [SerializeField] private Collider m_collider;

    public void NetInit( NetPrefabSetup netPrefabSetup )
    {
        m_networkManager = netPrefabSetup.NetworkManager;
        m_repComponent.NetID = netPrefabSetup.NetID;
        m_repComponent.NetworkManager = m_networkManager;

        m_meshRenderer.material = m_networkManager.Material;
        m_repComponent.gameObject.layer = Mathf.RoundToInt( Mathf.Log( m_networkManager.LayerMask.value, 2 ) ); //feels wrong but no better idea right now

        if (m_networkManager.IsServer )
        {
            m_repComponent.NetRole = NetRole.Authoritative;
            m_repComponent.ReplicateOnNetUpdate = true;
            m_collider.enabled = true;
        }
        else
        {
            m_repComponent.NetRole = NetRole.Proxy;
            m_repComponent.ReplicateOnNetUpdate = false;
            Destroy( m_rigidbody );
            m_collider.enabled = false;
        }
    }

}

public class NetPrefabSetup
{
    private NetID m_netID;
    public NetID NetID
    {
        get
        {
            return m_netID;
        }
    }

    private NetworkManager m_networkManager;
    public NetworkManager NetworkManager
    {
        get
        {
            return m_networkManager;
        }
    }

    public NetPrefabSetup( NetID netID, NetworkManager networkManager )
    {
        m_netID = netID;
        m_networkManager = networkManager;
    }
}
