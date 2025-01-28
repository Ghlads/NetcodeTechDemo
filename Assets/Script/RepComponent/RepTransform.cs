using UnityEngine;

public class RepTransform : RepComponent
{
    private const int MATRIX_4X4_BYTE_SIZE = sizeof( float ) * 15;
    private const int DELTA_VECTOR_BYTE_SIZE = sizeof( float ) * 3;

    [SerializeField] private bool m_useBadSyncMethod = false;
    private Vector3 m_lastPosition = Vector3.zero;
    private void Awake()
    {
        m_lastPosition = transform.localPosition;
    }

    public override Packet OnReplicateValues()
    {
        if ( m_useBadSyncMethod )
        {
            return ReplicateAsDelta();
        }
        else
        {
            return ReplicateAsMatrix();
        }
    }

    public override void OnValuesReplicated( Packet values )
    {
        if ( m_useBadSyncMethod )
        {
            ValuesReplicatedFromDelta( values );
        }
        else
        {
            ValuesReplicatedFromMatrix( values );
        }
    }

    private Packet ReplicateAsMatrix()
    {
        byte[] bytes = GetPacketDataTemplate( RepComponent.MINIMAL_PACKET_SIZE + MATRIX_4X4_BYTE_SIZE );
        Matrix4x4 localMatrix = Matrix4x4.TRS( transform.localPosition, transform.localRotation, transform.localScale );
        for ( int i = 0; i < 15; i++ )
        {
            System.Buffer.BlockCopy( System.BitConverter.GetBytes( localMatrix[i] ), 0, bytes, RepComponent.MINIMAL_PACKET_SIZE + ( sizeof( float ) * i ), sizeof( float ) );
        }
        return new Packet( PacketType.Data, NetworkManager, bytes );
    }

    private void ValuesReplicatedFromMatrix( Packet values )
    {
        if ( values.Bytes.Length != RepComponent.MINIMAL_PACKET_SIZE + MATRIX_4X4_BYTE_SIZE )
        {
            // safety for runtime change method
            return;
        }

        Matrix4x4 newMatrix = new Matrix4x4();
        for ( int i = 0; i < 15; i++ )
        {
            newMatrix[i] = System.BitConverter.ToSingle( values.Bytes, RepComponent.MINIMAL_PACKET_SIZE + ( sizeof( float ) * i ) );
        }


        Vector3 localPosition = new Vector3( newMatrix.m03, newMatrix.m13, newMatrix.m23 );

        Vector3 localScale = new Vector3(
            new Vector3( newMatrix.m00, newMatrix.m10, newMatrix.m20 ).magnitude,
            new Vector3( newMatrix.m01, newMatrix.m11, newMatrix.m21 ).magnitude,
            new Vector3( newMatrix.m02, newMatrix.m12, newMatrix.m22 ).magnitude
        );

        Vector3 colY = new Vector3( newMatrix.m01, newMatrix.m11, newMatrix.m21 ).normalized;
        Vector3 colZ = new Vector3( newMatrix.m02, newMatrix.m12, newMatrix.m22 ).normalized;
        Quaternion localRotation = Quaternion.LookRotation( colZ, colY );

        transform.SetLocalPositionAndRotation( localPosition, localRotation );
        transform.localScale = localScale;
    }

    private Packet ReplicateAsDelta()
    {
        byte[] bytes = GetPacketDataTemplate( RepComponent.MINIMAL_PACKET_SIZE + DELTA_VECTOR_BYTE_SIZE );
        Vector3 delta = transform.localPosition - m_lastPosition;
        m_lastPosition = transform.localPosition;
        System.Buffer.BlockCopy( System.BitConverter.GetBytes( delta.x ), 0, bytes, RepComponent.MINIMAL_PACKET_SIZE + ( sizeof( float ) * 0 ), sizeof( float ) );
        System.Buffer.BlockCopy( System.BitConverter.GetBytes( delta.y ), 0, bytes, RepComponent.MINIMAL_PACKET_SIZE + ( sizeof( float ) * 1 ), sizeof( float ) );
        System.Buffer.BlockCopy( System.BitConverter.GetBytes( delta.z ), 0, bytes, RepComponent.MINIMAL_PACKET_SIZE + ( sizeof( float ) * 2 ), sizeof( float ) );
        return new Packet( PacketType.Data, NetworkManager, bytes );
    }

    private void ValuesReplicatedFromDelta( Packet values )
    {
        if ( values.Bytes.Length != RepComponent.MINIMAL_PACKET_SIZE + DELTA_VECTOR_BYTE_SIZE )
        {
            // safety for runtime change method
            return;
        }

        Vector3 delta = new Vector3(
            System.BitConverter.ToSingle( values.Bytes, RepComponent.MINIMAL_PACKET_SIZE + ( sizeof( float ) * 0 ) ),
            System.BitConverter.ToSingle( values.Bytes, RepComponent.MINIMAL_PACKET_SIZE + ( sizeof( float ) * 1 ) ),
            System.BitConverter.ToSingle( values.Bytes, RepComponent.MINIMAL_PACKET_SIZE + ( sizeof( float ) * 2 ) )
        );

        transform.localPosition += delta;
    }
}
