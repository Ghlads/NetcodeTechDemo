using System;

public interface INetConnection : IPacketHandler, IEquatable<INetConnection> 
{
    public string ConnectionToString();
}
