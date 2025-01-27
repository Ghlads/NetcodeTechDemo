using System;
using UnityEngine;

[Serializable]
public struct NetID
{
    public static NetID Invalid => new NetID( -1 );
    [SerializeField] private int m_id;

    public NetID( int id = 0 )
    {
        m_id = id;
    }

    public override bool Equals( object obj )
    {
        if (obj is NetID id)
        {
            return this == id;
        }

        return false;
    }

    public override int GetHashCode()
    {
        return m_id;
    }

    public static bool operator ==( NetID a, NetID b )
    {
        return a.m_id == b.m_id;
    }

    public static bool operator !=( NetID a, NetID b )
    {
        return !( a == b );
    }

    public static explicit operator byte( NetID id )
    {
        return ( byte )id.m_id;
    }

    public static explicit operator NetID( byte id )
    {
        return new NetID( ( int )id );
    }
}
