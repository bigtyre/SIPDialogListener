using System;

namespace BigTyre.Phones
{
    public class PBXSettings
    {
        public PBXSettings(string authenticationRealm, string iPAddress, TimeSpan expiryTime, int port)
        {
            AuthenticationRealm = authenticationRealm;
            IPAddress = iPAddress;
            ExpiryTime = expiryTime;
            Port = port;
        }

        public string AuthenticationRealm { get; }
        public string IPAddress { get;   }
        public TimeSpan ExpiryTime { get; } = TimeSpan.FromSeconds(110);
        public int Port { get; }
    }

}
