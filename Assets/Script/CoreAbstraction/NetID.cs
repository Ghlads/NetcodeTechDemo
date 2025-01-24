using System;

public class NetID
{
    private int m_id;

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
}
