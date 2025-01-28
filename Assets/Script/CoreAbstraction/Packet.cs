using System.Text;
using UnityEngine;

public enum PacketType
{
    Generic = 0,
    ConnectionRequest = 1,
    ConnectionResponse = 2,
    DisconnectionRequest = 3,
    DisconnectionResponse = 4,
    Data = 5,
    Ack = 6,
    Error = 7,
}

public class Packet
{
    private NetDeliveryMethod m_deliveryMethod;
    private PacketType m_type;
    private INetConnection m_senderID;
    private long m_timeStamp;
    private byte[] m_bytes;

    public NetDeliveryMethod DeliveryMethod => m_deliveryMethod;
    public PacketType Type => m_type;
    public INetConnection Sender => m_senderID;
    public long TimeStamp => m_timeStamp;
    public byte[] Bytes => m_bytes;

    public Packet( PacketType type, INetConnection sender, byte[] bytes, NetDeliveryMethod deliveryMethod = NetDeliveryMethod.Unreliable )
    {
        if ( sender == default )
        {
            Debug.LogError( "Sender can't be null" );
        }

        if ( type == PacketType.Data && ( bytes == null || bytes.Length <= 0 ) )
        {
            Debug.LogError( "Bytes can't be null or empty for data packet, there is nothing send" );
        }

        m_deliveryMethod = deliveryMethod;
        m_type = type;
        m_senderID = sender;
        m_bytes = bytes;
        m_timeStamp = System.DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
    }
}

public enum ErrorCode
{
    GenericError = 0,
}

public enum DisconnectionReason
{
    Request = 0,
    Timeout = 1,
    Kicked = 2,
}

public static class GenericPacketUtils
{

    public static void CopyAsBytes( this string str, byte[] bytes, int offset = 0 )
    {
        for ( int i = 0; i < str.Length; i++ )
        {
            bytes[i + offset] = ( byte )str[i];
        }
    }

    public static string GetString( Packet packet )
    {
        StringBuilder stringBuilder = new StringBuilder();
        stringBuilder.Append( packet.TimeStamp )
                    .Append( "|Sender: " )
                    .Append( packet.Sender.ConnectionToString() )
                    .Append( "|Packet Type: " )
                    .Append( packet.Type )
                    .Append( "|" );

        switch ( packet.Type )
        {
            case PacketType.ConnectionResponse:
                if ( packet.Bytes[0] == 1 )
                {
                    stringBuilder.Append( "Connection Approved" );
                }
                else
                {
                    stringBuilder.Append( "Connection Denied: " )
                                .Append( GetString( packet.Bytes, 1 ) );
                }
                break;
            case PacketType.DisconnectionResponse:
                stringBuilder.Append( ( ( DisconnectionReason )packet.Bytes[0] ).ToString() );
                break;
            case PacketType.Data:
                stringBuilder.Append( "Data" ).Append( GetString( packet.Bytes ) );
                break;
            case PacketType.Error:
                stringBuilder.Append( ( ( ErrorCode )packet.Bytes[0] ).ToString() )
                    .Append( GetString( packet.Bytes, 1 ) );
                break;
            default:
                break;

        }

        return stringBuilder.ToString();
    }

    public static bool IsPacketAnError( Packet packet )
    {
        if ( packet == null )
        {
            return false;
        }

        if ( packet.Type == PacketType.ConnectionResponse )
        {
            return packet.Bytes[0] == 0;
        }

        if ( packet.Type == PacketType.DisconnectionResponse )
        {
            return ( DisconnectionReason )packet.Bytes[0] != DisconnectionReason.Request;
        }

        return packet.Type == PacketType.Error;
    }

    public static string GetString( byte[] bytes, int offset = 0 )
    {
        string str = "";
        for ( int i = offset; i < bytes.Length; i++ )
        {
            str += ( char )bytes[i];
        }
        return str;
    }

    public static Packet ErrorPacket( INetConnection sender, ErrorCode errorCode, string message, NetDeliveryMethod deliveryMethod = NetDeliveryMethod.Unreliable )
    {
        byte[] bytes = new byte[1 + message.Length];
        bytes[0] = ( byte )errorCode;
        CopyAsBytes( message, bytes, 1 );
        return new Packet( PacketType.Error, sender, bytes, deliveryMethod );
    }

    public static Packet ConnectionApprovalPacket( INetConnection sender, NetDeliveryMethod deliveryMethod = NetDeliveryMethod.Unreliable )
    {
        byte[] bytes = new byte[1];
        bytes[0] = 1;
        return new Packet( PacketType.ConnectionResponse, sender, bytes, deliveryMethod );
    }

    public static Packet ConnectionDenialPacket( INetConnection sender, string reason, NetDeliveryMethod deliveryMethod = NetDeliveryMethod.Unreliable )
    {
        byte[] bytes = new byte[1 + reason.Length];
        bytes[0] = 0;
        CopyAsBytes( reason, bytes, 1 );
        return new Packet( PacketType.ConnectionResponse, sender, bytes, deliveryMethod );
    }

    public static Packet DisconnectionNoticePacket( INetConnection sender, DisconnectionReason code, NetDeliveryMethod deliveryMethod = NetDeliveryMethod.Unreliable )
    {
        byte[] bytes = new byte[] { ( byte )code };
        return new Packet( PacketType.DisconnectionResponse, sender, bytes, deliveryMethod );
    }
}

