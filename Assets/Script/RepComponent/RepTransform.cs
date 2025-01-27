using UnityEngine;

public class RepTransform : RepComponent
{
    private const int MATRIX_4X4_BYTE_SIZE = sizeof( float ) * 15;
    public override Packet OnReplicateValues()
    {
        byte[] bytes = GetPacketDataTemplate( RepComponent.MINIMAL_PACKET_SIZE + MATRIX_4X4_BYTE_SIZE );
        Matrix4x4 localMatrix = Matrix4x4.TRS( transform.localPosition, transform.localRotation, transform.localScale );
        for ( int i = 0; i < 15; i++ )
        {
            System.Buffer.BlockCopy( System.BitConverter.GetBytes( localMatrix[i] ), 0, bytes, RepComponent.MINIMAL_PACKET_SIZE + ( sizeof( float ) * i ), sizeof( float ) );
        }
        return new Packet( PacketType.Data, NetworkManager, bytes );
    }

    public override void OnValuesReplicated( Packet values )
    {
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
}
