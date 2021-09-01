using System.Net;
using System.Net.NetworkInformation;

namespace Granite
{
    public class GraniteUtil
    {
        public const string LocalHost = "127.0.0.1";

        public static IPAddress GetBindableAddress(bool ignoreLocalHost)
        {
            NetworkInterface[] adapters = NetworkInterface.GetAllNetworkInterfaces();

            foreach (var ni in adapters)
            {
                UnicastIPAddressInformationCollection ips = ni.GetIPProperties().UnicastAddresses;
                IPv4InterfaceProperties ipv4 = ni.GetIPProperties().GetIPv4Properties();

                foreach (UnicastIPAddressInformation ip in ips)
                {
                    if (ip.Address.AddressFamily == System.Net.Sockets.AddressFamily.InterNetwork)
                    {
                        if (ignoreLocalHost && ip.Address.MapToIPv4().ToString() == LocalHost)
                        {
                            continue;
                        }
                        return ip.Address;
                    }
                }
            }
            return null;
        }

        public static IPAddress StringToIpAddress(string hostname)
        {
            IPAddress serverAddress;
            if (!IPAddress.TryParse(hostname, out serverAddress))
            {
                serverAddress = Dns.GetHostEntry(hostname).AddressList[0];
            }
            return serverAddress;
        }
    }
}
