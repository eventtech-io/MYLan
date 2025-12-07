This document will be editied 

# MYLan

YOU MUST RUN THIS PROGRAM AS ADMINISTRATOR - RIGHT CLICK THEN SELECT 'RUN AS ADMINISTRATOR'

OR THE PROGRAM SIMPLY WILL NOT WORK...…..

-----------------------------------------------------------------------------------------------------
Educational Guide: Building a Portable DHCP Server for Field Engineers
By Rob Handyside - Event Tech Research Dec2025 

Networking on the fly can feel complex—especially when systems must communicate smoothly without enterprise infrastructure or IT permissions. This guide walks engineers through designing and deploying a fully portable DHCP server that runs directly from a laptop, enabling quick network configuration for audio, lighting, and embedded systems in live or temporary setups.

Designed for real-world use, this project combines practical field workflows with protocol-level learning. Engineers will explore how to:

Automate address assignment for Dante, lighting, and embedded control networks.
Configure and control Windows network adapters using netsh.
Build a lightweight DHCP service capable of defining static ranges, lease management, and consistent IP mapping.
Implement a simple WPF interface for real-time logs, adapter selection, and quick start/stop control.
Understand the internals of DHCP—discovering, offering, requesting, and acknowledging IP leases.
Manually construct and parse DHCP/BOOTP packets for deeper insight into network behavior.

By the end, engineers will learn not only how to stand up a local DHCP environment within minutes but also why it works—developing both operational speed and protocol fluency. The project blends educational theory with field application, making it a perfect training resource for anyone supporting live event networks, embedded systems, or rapid deployment scenarios.

---------------------------------------------------------------------------------------------------

-----------------------------------------------------------------------------------------------------
Educational Guide: Building a Portable DHCP Server for Field Engineers -FULL-

Networking can be confusing at the best of times—especially when you’re just trying to get devices talking to each other in a controlled setup, like at a live show or a small lab environment. This guide explains, step by step, how and why we built a custom DHCP (Dynamic Host Configuration Protocol) server that runs directly from a laptop, without needing enterprise tools or IT permissions.

1. Why Build Our Own DHCP Server?
Most Windows DHCP solutions are designed for enterprise environments. They often require domain integration, administrator configuration, or centralized infrastructure. This isn’t practical for field engineers who simply need quick, reliable configuration in places like:

Audio-over-IP and Dante networks
Lighting control desks and show controllers
Raspberry Pi and embedded systems setups
Temporary event networks or broadcast racks

The goal is a portable DHCP app that:
Runs on any laptop, even offline
Sets a static IP of 192.168.1.1 automatically
Shows real-time logs so engineers can confirm it’s working
Assigns predictable IP ranges (192.168.1.50–150)
Operates in what we call “Field Engineer Mode”: select a network, click Start, and everything configures itself.

This design means faster setup, fewer configuration errors, and no more waiting for IT-managed DHCP services.

2. Setting the NIC Static with netsh
Before the server can hand out addresses, the laptop must take control of its network adapter. The app sets your chosen interface (like a USB Ethernet dongle) to:

IP: 192.168.1.1
Subnet mask: 255.255.255.0

It does this through a Windows command-line tool called netsh. Behind the scenes, this runs something like:

netsh interface ip set address "Ethernet 2" static 192.168.1.1 255.255.255.0

Key points:
netsh configures Windows network adapters directly.

The app must run with Administrator rights.

Logs from netsh are captured for transparency and troubleshooting.

This ensures the laptop always starts from a known and predictable network state—a crucial factor in event networks where random addressing (like 169.254.x.x) can break control systems.

3. Discovering Network Interfaces (NICs)
The application scans the system for usable network adapters using NetworkInterface.GetAllNetworkInterfaces(). From that list, it filters out:

Loopback interfaces (used internally by the OS)
Virtual or tunnel adapters (VPNs, for instance)
Adapters that are disabled or disconnected

This leaves you with only the real, active ports—like onboard Ethernet or connected USB LAN adapters—ready for selection.

This step ensures users don’t need to troubleshoot Windows Device Manager just to find the correct port.

4. The Simple WPF Interface
The app’s interface is minimal by design, built in WPF (Windows Presentation Foundation). It includes:

A dropdown to select your network adapter
Start and Stop buttons
A live log window
A simple status display

Example layout:

[ Select Network Adapter ▼ ]
[ Start DHCP Server ] [ Stop DHCP Server ]
Status: DHCP Running
Log Output:
DISCOVER from MAC...
ACK sent...
This simplicity matters. Field engineers don’t need overly technical dashboards; they need visual confirmation that the network tool is running effectively.

5. The DHCP Server Workflow
Once you press “Start”:

The selected NIC is confirmed.
The app applies the static IP via netsh.
If this succeeds, the DHCP engine is launched.

The app then starts a lightweight DHCP process with defined parameters:

Server IP: 192.168.1.1
IP Pool: 192.168.1.50 – 192.168.1.150
Subnet: 255.255.255.0
#Default Gateway: 192.168.1.1
#DNS: 8.8.8.8

From this point, the laptop acts as the network’s DHCP authority, ready to issue addresses.

6. The DHCP Protocol in Action
DHCP operates on two key ports:

UDP port 67 (server)

UDP port 68 (client)

The server listens for broadcast “DISCOVER” messages from devices that need an IP address. When it receives one, it responds with an “OFFER.” The client then sends a “REQUEST,” and the server finalizes it with an “ACK.”

These simple exchanges—DISCOVER, OFFER, REQUEST, ACK—power almost every device that connects to a network.

7. Managing IP Leases
The server tracks each device using its MAC address. A small in-memory database (dictionary) stores:

Device MAC
Assigned IP
Lease time

When a new device joins, the app checks this list:
If it’s seen before, it renews the same IP.
If not, it gives the next one in the range (192.168.1.50 onward).

This ensures devices keep predictable IPs, maintaining stable routing for control and audio systems.

8. Building DHCP Packets by Hand
This section offers deep educational value. Each DHCP response must be built byte by byte, following the BOOTP (Bootstrap Protocol) structure.

Each packet includes:

Operation code (BOOTREPLY = 2)
Client’s MAC address
Assigned IP address
“Magic cookie” that identifies the packet as DHCP
Option fields describing the configuration (DNS, subnet mask, gateway, etc.)

By manually constructing these packets, learners see how network configuration data is encoded, sent, and interpreted. It reveals why network misbehavior happens and how tools like Wireshark decode it.

9. Real-Time Logging
Every event—like receiving DISCOVER or sending ACK—is logged in real time to the UI. This gives engineers instant visibility into the process:

DISCOVER from 00-1A-2B-3C-4D-5E
OFFER 192.168.1.51
REQUEST from 00-1A-2B-3C-4D-5E
ACK sent
This mirrors how professional debugging tools work but within an easy-to-use interface.

10. Supporting Application Files
Under the hood, the app is built on modern .NET with WPF support. Key files include:

App.xaml and App.xaml.cs – Entry point and startup logic
MainWindow.xaml – Defines the UI
ThemeInfo.cs – WPF resource behaviors
.csproj – Project configuration (net8.0-windows, UseWPF=true)
Targeting .NET 8 ensures smooth performance and compatibility with current Windows platforms.

11. Putting It All Together
The full process looks like this:

User selects a network adapter.
App applies static IP (192.168.1.1).
Server starts listening on UDP port 67.
Client devices broadcast DISCOVER.
Server issues OFFER and ACK messages.
Devices receive IP configurations.

The result:

Audio and control networks stabilize.
Dante nodes become visible.
Lighting controllers connect.
Embedded devices boot with valid IPs.
This solves the most common source of field network chaos: unpredictable addressing.

12. Educational Takeaways
By walking through this project, students and new engineers learn how to:

Configure network interfaces using system commands
Understand how DHCP functions at protocol level
Build GUI tools for practical field use
Debug live network problems effectively

Appreciate how low-level networking concepts apply in real-world productions

Understanding DHCP this way transforms engineers from “users of networks” into “managers of networks,” giving them the confidence to troubleshoot and design stable setups everywhere they work.

