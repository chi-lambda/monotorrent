using Open.Nat;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Linq;
using System.Net;
using System.Net.NetworkInformation;
using System.Net.Sockets;
using System.Runtime.InteropServices;
using System.Threading;
using System.Threading.Tasks;

namespace SampleClient.WPF.Utils
{
    class NetworkUtil
    {
        private NatDiscoverer nat = new NatDiscoverer();


        [DllImport("iphlpapi.dll", CharSet = CharSet.Auto)]
        private static extern int GetBestInterface(UInt32 destAddr, out UInt32 bestIfIndex);

        private IEnumerable<IPAddress> GetGatewayForDestination(IPAddress destinationAddress)
        {
            UInt32 destaddr = BitConverter.ToUInt32(destinationAddress.GetAddressBytes(), 0);

            uint interfaceIndex;
            int result = GetBestInterface(destaddr, out interfaceIndex);
            if (result != 0)
            {
                throw new Win32Exception(result);
            }

            foreach (var ni in NetworkInterface.GetAllNetworkInterfaces())
            {
                var niprops = ni.GetIPProperties();
                if (niprops == null)
                {
                    continue;
                }

                var gateway = niprops.GatewayAddresses?.Select(addr => addr.Address);
                if (gateway == null)
                {
                    continue;
                }

                if (ni.Supports(NetworkInterfaceComponent.IPv4))
                {
                    var v4props = niprops.GetIPv4Properties();
                    if (v4props == null)
                    {
                        continue;
                    }

                    if (v4props.Index == interfaceIndex)
                    {
                        return gateway;
                    }
                }

                if (ni.Supports(NetworkInterfaceComponent.IPv6))
                {
                    var v6props = niprops.GetIPv6Properties();
                    if (v6props == null)
                    {
                        continue;
                    }

                    if (v6props.Index == interfaceIndex)
                    {
                        return gateway;
                    }
                }
            }

            return null;
        }

        public async Task<IEnumerable<NatDevice>> OpenPort(int port)
        {
            var gatewayInterfaces = GetGatewayForDestination(new IPAddress(new byte[] { 8, 8, 8, 8 }));
            var cts = new CancellationTokenSource(2000);
            var devices = await nat.DiscoverDevicesAsync(PortMapper.Upnp, cts);
            if (devices.Any(dev => LocalAddress(dev).AddressFamily == AddressFamily.InterNetworkV6))
            {

            }
            Debug.WriteLine($"Found UPnP devices: {string.Join(", ", devices.Select(dev => LocalAddress(dev)))}");
            foreach (var device in devices.Where(dev => gatewayInterfaces.Any(addr => LocalAddress(dev).Equals(addr))))
            {
                Debug.WriteLine($"Setting UPnP redirects for host {LocalAddress(device)}");
                try
                {
                    await device.CreatePortMapAsync(new Mapping(Protocol.Udp, port, port, 86400, "MonoTorrent"));
                }
                catch (Exception ex)
                {
                }
                try
                {
                    await device.CreatePortMapAsync(new Mapping(Protocol.Tcp, port, port, 86400, "MonoTorrent"));
                }
                catch (Exception ex)
                {
                }
            }
            return devices;
        }

        public async Task ClosePort(NatDevice device, int port)
        {
            try
            {
                await device.DeletePortMapAsync(new Mapping(Protocol.Udp, port, port, 86400, "MonoTorrent"));
            }
            catch (Exception ex)
            {
            }
            try
            {
                await device.DeletePortMapAsync(new Mapping(Protocol.Tcp, port, port, 86400, "MonoTorrent"));
            }
            catch (Exception ex)
            {
            }
        }

        public IPAddress LocalAddress(NatDevice device)
        {
            dynamic dynDevice = device;
            switch (device)
            {
                case PmpNatDevice pmpDevice:
                    return pmpDevice.LocalAddress;
                case UpnpNatDevice upnpDevice:
                    return upnpDevice.DeviceInfo.HostEndPoint.Address;
                default:
                    throw new ArgumentException($"Invalid type {device.GetType().Name}.");
            }
        }
    }
}
