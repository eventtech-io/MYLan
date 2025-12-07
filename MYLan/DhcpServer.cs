using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Sockets;
using System.Threading;

namespace DhcpFieldServer
{
    public class DhcpLease
    {
        public string Mac { get; set; } = "";
        public IPAddress Ip { get; set; } = IPAddress.None;
        public DateTime Expiry { get; set; }
    }

    public class DhcpServer
    {
        private readonly IPAddress _serverIp;
        private readonly IPAddress _poolStart;
        private readonly IPAddress _poolEnd;
        private readonly IPAddress _subnetMask;
        private readonly IPAddress _routerIp;
        private readonly IPAddress _dnsIp;

        private readonly Dictionary<string, DhcpLease> _leases = new();
        private UdpClient? _udp;
        private Thread? _listenerThread;
        private volatile bool _running;

        public event Action<string>? Log;

        public DhcpServer(string serverIpAddress, string poolStart, string poolEnd,
                          string subnetMask, string routerIp, string dnsIp)
        {
            _serverIp = IPAddress.Parse(serverIpAddress);
            _poolStart = IPAddress.Parse(poolStart);
            _poolEnd = IPAddress.Parse(poolEnd);
            _subnetMask = IPAddress.Parse(subnetMask);
            _routerIp = IPAddress.Parse(routerIp);
            _dnsIp = IPAddress.Parse(dnsIp);
        }

        public void Start()
        {
            if (_running) return;

            _udp = new UdpClient(new IPEndPoint(IPAddress.Any, 67));
            _udp.EnableBroadcast = true;

            _running = true;
            _listenerThread = new Thread(ListenLoop)
            {
                IsBackground = true
            };
            _listenerThread.Start();

            Log?.Invoke("DHCP server started on UDP port 67.");
        }

        public void Stop()
        {
            _running = false;

            try
            {
                _udp?.Close();
            }
            catch { }

            try
            {
                _listenerThread?.Join(500);
            }
            catch { }

            Log?.Invoke("DHCP server stopped.");
        }

        private void ListenLoop()
        {
            if (_udp == null) return;
            var remote = new IPEndPoint(IPAddress.Any, 0);

            while (_running)
            {
                try
                {
                    byte[] data = _udp.Receive(ref remote);

                    if (data.Length < 244) // header + magic + at least one option
                        continue;

                    // BOOTP op code
                    byte op = data[0]; // 1 = BOOTREQUEST, 2 = BOOTREPLY
                    if (op != 1)
                        continue;

                    // Transaction ID
                    uint xid = (uint)((data[4] << 24) | (data[5] << 16) | (data[6] << 8) | data[7]);

                    // Client MAC (first 6 bytes of chaddr)
                    byte[] macBytes = new byte[6];
                    Array.Copy(data, 28, macBytes, 0, 6);
                    string mac = BitConverter.ToString(macBytes);

                    // Options start at byte 240 (236 fixed header + 4 magic cookie)
                    int optionsIndex = 240;
                    if (!(data[236] == 99 && data[237] == 130 && data[238] == 83 && data[239] == 99))
                    {
                        Log?.Invoke("Invalid DHCP magic cookie, ignoring.");
                        continue;
                    }

                    byte msgType = 0;
                    // Very simple option parser just to find option 53 (message type)
                    while (optionsIndex < data.Length)
                    {
                        byte option = data[optionsIndex++];
                        if (option == 255) // end
                            break;
                        if (option == 0)
                            continue;

                        if (optionsIndex >= data.Length) break;
                        byte len = data[optionsIndex++];
                        if (optionsIndex + len > data.Length) break;

                        if (option == 53 && len == 1)
                        {
                            msgType = data[optionsIndex];
                            break;
                        }
                        optionsIndex += len;
                    }

                    if (msgType == 1) // DISCOVER
                    {
                        Log?.Invoke($"DISCOVER from {mac}");
                        IPAddress offerIp = GetOrCreateLease(mac);
                        SendOffer(xid, macBytes, offerIp);
                    }
                    else if (msgType == 3) // REQUEST
                    {
                        Log?.Invoke($"REQUEST from {mac}");
                        IPAddress leaseIp = GetOrCreateLease(mac);
                        SendAck(xid, macBytes, leaseIp);
                    }
                    else
                    {
                        // Other message types: inform, decline, etc – ignore for this simple build
                    }
                }
                catch (SocketException)
                {
                    if (!_running) break;
                }
                catch (Exception ex)
                {
                    Log?.Invoke("Error in ListenLoop: " + ex.Message);
                }
            }
        }

        private IPAddress GetOrCreateLease(string mac)
        {
            if (_leases.TryGetValue(mac, out var lease) &&
                lease.Expiry > DateTime.Now)
            {
                return lease.Ip;
            }

            IPAddress candidate = _poolStart;
            while (true)
            {
                bool inUse = false;
                foreach (var kv in _leases)
                {
                    if (kv.Value.Ip.Equals(candidate) && kv.Value.Expiry > DateTime.Now)
                    {
                        inUse = true;
                        break;
                    }
                }

                if (!inUse)
                {
                    var newLease = new DhcpLease
                    {
                        Mac = mac,
                        Ip = candidate,
                        Expiry = DateTime.Now.AddHours(8)
                    };
                    _leases[mac] = newLease;
                    Log?.Invoke($"Assigned lease {candidate} to {mac}");
                    return candidate;
                }

                byte[] bytes = candidate.GetAddressBytes();
                bytes[3]++;
                candidate = new IPAddress(bytes);

                if (candidate.AddressFamily != AddressFamily.InterNetwork ||
                    CompareIp(candidate, _poolEnd) > 0)
                {
                    throw new Exception("IP pool exhausted.");
                }
            }
        }

        private static int CompareIp(IPAddress a, IPAddress b)
        {
            byte[] ab = a.GetAddressBytes();
            byte[] bb = b.GetAddressBytes();
            for (int i = 0; i < ab.Length; i++)
            {
                int d = ab[i].CompareTo(bb[i]);
                if (d != 0) return d;
            }
            return 0;
        }

        private void SendOffer(uint xid, byte[] clientMac, IPAddress yiaddr)
        {
            byte[] packet = BuildReplyPacket(xid, clientMac, yiaddr, 2); // 2 = OFFER
            SendBroadcast(packet);
            Log?.Invoke($"Sent OFFER → {yiaddr}");
        }

        private void SendAck(uint xid, byte[] clientMac, IPAddress yiaddr)
        {
            byte[] packet = BuildReplyPacket(xid, clientMac, yiaddr, 5); // 5 = ACK
            SendBroadcast(packet);
            Log?.Invoke($"Sent ACK → {yiaddr}");
        }

        private byte[] BuildReplyPacket(uint xid, byte[] clientMac, IPAddress yiaddr, byte dhcpMessageType)
        {
            byte[] packet = new byte[300]; // enough for basic options

            // BOOTP fixed header (236 bytes)
            packet[0] = 2; // op = BOOTREPLY
            packet[1] = 1; // htype = Ethernet
            packet[2] = 6; // hlen = 6
            packet[3] = 0; // hops

            // xid
            packet[4] = (byte)((xid >> 24) & 0xFF);
            packet[5] = (byte)((xid >> 16) & 0xFF);
            packet[6] = (byte)((xid >> 8) & 0xFF);
            packet[7] = (byte)(xid & 0xFF);

            // secs (0), flags (0x8000 = broadcast)
            packet[8] = 0;
            packet[9] = 0;
            packet[10] = 0x80;
            packet[11] = 0x00;

            // ciaddr = 0 (client IP address)
            // yiaddr = your (client) IP address
            Array.Copy(yiaddr.GetAddressBytes(), 0, packet, 16, 4);

            // siaddr = server IP
            Array.Copy(_serverIp.GetAddressBytes(), 0, packet, 20, 4);

            // giaddr = 0
            // chaddr = client hardware address (16 bytes)
            Array.Copy(clientMac, 0, packet, 28, 6);

            // sname, file left as zeros

            // Magic cookie
            packet[236] = 99;
            packet[237] = 130;
            packet[238] = 83;
            packet[239] = 99;

            int idx = 240;

            // Option 53: DHCP Message Type
            packet[idx++] = 53;
            packet[idx++] = 1;
            packet[idx++] = dhcpMessageType;

            // Option 54: Server Identifier
            packet[idx++] = 54;
            packet[idx++] = 4;
            Array.Copy(_serverIp.GetAddressBytes(), 0, packet, idx, 4);
            idx += 4;

            // Option 1: Subnet Mask
            packet[idx++] = 1;
            packet[idx++] = 4;
            Array.Copy(_subnetMask.GetAddressBytes(), 0, packet, idx, 4);
            idx += 4;

            // Option 3: Router
            packet[idx++] = 3;
            packet[idx++] = 4;
            Array.Copy(_routerIp.GetAddressBytes(), 0, packet, idx, 4);
            idx += 4;

            // Option 6: DNS Server
            packet[idx++] = 6;
            packet[idx++] = 4;
            Array.Copy(_dnsIp.GetAddressBytes(), 0, packet, idx, 4);
            idx += 4;

            // Option 51: IP Address Lease Time (8 hours)
            packet[idx++] = 51;
            packet[idx++] = 4;
            int leaseSeconds = 8 * 60 * 60;
            packet[idx++] = (byte)((leaseSeconds >> 24) & 0xFF);
            packet[idx++] = (byte)((leaseSeconds >> 16) & 0xFF);
            packet[idx++] = (byte)((leaseSeconds >> 8) & 0xFF);
            packet[idx++] = (byte)(leaseSeconds & 0xFF);

            // End option
            packet[idx++] = 255;

            // Return exact-used length slice
            byte[] finalPacket = new byte[idx];
            Array.Copy(packet, finalPacket, idx);
            return finalPacket;
        }

        private void SendBroadcast(byte[] packet)
        {
            if (_udp == null) return;
            var dest = new IPEndPoint(IPAddress.Broadcast, 68);
            _udp.Send(packet, packet.Length, dest);
        }
    }
}
