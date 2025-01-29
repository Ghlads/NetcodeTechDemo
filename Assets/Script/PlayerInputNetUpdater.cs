using UnityEngine;
using UnityEngine.InputSystem;

public class PlayerInputNetUpdater : MonoBehaviour
{
    [SerializeField] private NetworkManager m_networkManager;
    [SerializeField] private byte m_nextNetID = 2;
    public void OnSpawnCube( InputAction.CallbackContext context )
    {
        if ( context.performed )
        {
            byte[] bytes = new byte[2];
            bytes[ClientServerConstants.NET_EVENT_CODE_INDEX] = ( byte )GameNetEventCode.CreateCube;
            bytes[1] = m_nextNetID++;
            Packet packet = new Packet( PacketType.Data, m_networkManager, bytes, NetDeliveryMethod.Reliable );
            m_networkManager.SendPacketToServer( packet );
        }
    }


}
