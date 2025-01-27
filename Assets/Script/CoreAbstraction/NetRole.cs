public enum NetRole 
{
    Authoritative,
    AutonomousProxy,
    SimulatedProxy,
}

public enum NetMode 
{
    Server = 0,
    Client = 1,
}

public enum NetDeliveryMethod
{
    Unreliable,
    Reliable,
}

public enum CommonResponse 
{
    Success = 0,
    Failure = 1,
    Accepted = 2,
    Denied = 3,
}
