using System.Net;

namespace OolamaCommunication.Erweiterungen;

record class IPAddressComparer : IEqualityComparer<IPAddress>
{
    public bool Equals(IPAddress? x, IPAddress? y) => x?.ToString() == y?.ToString();
    public int GetHashCode(IPAddress obj) => obj.ToString().GetHashCode();
}
