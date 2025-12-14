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
DHCP Field Server

Educational Guide & Technical Reference

1. Introduction

The DHCP Field Server is a Windows Presentation Foundation (WPF) application designed for network administrators and field technicians who need a portable, easy-to-use DHCP server for temporary network deployments, testing environments, or isolated networks.
This application provides a complete DHCP server implementation that can automatically configure network clients with IP addresses, subnet masks, gateway addresses, and DNS server information.

1.1 Key Features
•	Simple graphical interface - No complex configuration files
•	Automatic IP configuration - Sets static IP on selected network adapter
•	Real-time logging - Monitor DHCP requests and responses
•	Lease management - Tracks active IP assignments
•	Portable deployment - Perfect for field work and testing

1.2 Typical Use Cases
•	Setting up temporary networks at events or job sites
•	Testing network equipment in isolated environments
•	Educational demonstrations of DHCP protocol
•	Emergency network recovery scenarios
•	Development and testing of networked applications


2. Understanding DHCP
Dynamic Host Configuration Protocol (DHCP) is a network management protocol that automatically assigns IP addresses and other network configuration parameters to devices on a network.

2.1 The DHCP Process (DORA)
DHCP uses a four-step process known as DORA:
1.	Discover - Client broadcasts a request to find available DHCP servers
2.	Offer - Server responds with an available IP address and configuration
3.	Request - Client requests the offered IP address
4.	Acknowledge - Server confirms the IP assignment (lease)

2.2 DHCP Message Types
The application handles the following DHCP message types:
Message Type	Description
DISCOVER (1)	Client broadcasts to locate available servers
OFFER (2)	Server offers an IP address to the client
REQUEST (3)	Client requests the offered IP address
ACK (5)	Server acknowledges and confirms the lease

2.3 DHCP Options
DHCP options provide additional configuration parameters. This server provides:
Option	Name	Default Value
1	Subnet Mask	255.255.255.0
3	Router/Gateway	192.168.1.1
6	DNS Server	8.8.8.8 (Google DNS)
51	Lease Time	8 hours (28,800 seconds)
53	Message Type	DISCOVER, OFFER, REQUEST, ACK
54	Server Identifier	192.168.1.1


3. Application Architecture
The DHCP Field Server is built using a modular architecture with clear separation of concerns. The application consists of four main components:

3.1 Component Overview
Component	Responsibility
MainWindow	User interface and event handling
DhcpServer	Core DHCP protocol implementation
NetworkHelper	Network adapter configuration using netsh commands
DhcpLease	Data structure for tracking IP address leases

3.2 DhcpServer Class
The DhcpServer class is the heart of the application. It implements the complete DHCP server functionality:
Key Properties
•	_serverIp: The IP address of the DHCP server itself (192.168.1.1)
•	_poolStart, _poolEnd: Define the range of IP addresses available for lease
•	_subnetMask: Network subnet mask (255.255.255.0)
•	_routerIp: Default gateway address provided to clients
•	_dnsIp: DNS server address (8.8.8.8 - Google Public DNS)
•	_leases: Dictionary storing active leases by MAC address
Core Methods
•	Start(): Creates UDP socket on port 67 and starts listening thread
•	Stop(): Gracefully shuts down the server and releases resources
•	ListenLoop(): Main message processing loop, handles incoming DHCP packets
•	GetOrCreateLease(): Assigns IP addresses and manages the lease pool
•	BuildReplyPacket(): Constructs DHCP response packets with options
•	SendBroadcast(): Transmits responses to UDP port 68

3.3 Network Configuration
The application configures the following network parameters:
Parameter	Value
Server IP Address	192.168.1.1
Subnet Mask	255.255.255.0
DHCP Pool Start	192.168.1.50
DHCP Pool End	192.168.1.150
Available Addresses	101 IP addresses
Default Gateway	192.168.1.1 (server itself)
DNS Server	8.8.8.8 (Google Public DNS)
Lease Duration	8 hours


4. How to Use the Application

4.1 Prerequisites
•	Administrator privileges - Required to configure network adapters and bind to port 67
•	Windows operating system - Built as a WPF application for Windows
•	.NET Framework - Runtime environment for the application
•	Available network adapter - Physical or virtual network interface

4.2 Step-by-Step Guide
5.	Launch the application - Right-click and select "Run as Administrator"
6.	Select network adapter - Choose the adapter to use from the dropdown menu
7.	Click Start - Application will configure the adapter and start the DHCP server
8.	Monitor activity - Watch the log window for DHCP requests and responses
9.	Connect clients - Configure client devices to obtain IP addresses automatically
10.	Click Stop - When finished, stop the server to release the port

4.3 Understanding the Interface
•	Server NIC dropdown - Lists available network adapters
•	Start/Stop buttons - Control server operation
•	Status indicator - Shows current server state (Idle, Running, or Stopped)
•	Log window - Displays real-time DHCP activity with timestamps


5. Technical Implementation Details

5.1 DHCP Packet Structure
DHCP uses the BOOTP message format. The packet consists of:
•	Fixed Header (236 bytes) - Contains operation type, transaction ID, client/server addresses
•	Magic Cookie (4 bytes) - Value: 99.130.83.99 (identifies DHCP packet)
•	Options (variable) - Configuration parameters and message type

5.2 Lease Management Algorithm
The GetOrCreateLease method implements a simple but effective lease management strategy:
11.	Check for existing lease - If MAC address has active lease, return that IP
12.	Search for available IP - Iterate through pool from start to end
13.	Skip reserved IPs - Check if IP is currently leased to another device
14.	Assign and record - Create lease entry with 8-hour expiration
15.	Handle exhaustion - Throw exception if no IPs available

5.3 Network Adapter Configuration
The application uses the Windows netsh command to configure network adapters:
netsh interface ip set address "AdapterName" static 192.168.1.1 255.255.255.0
This command:
•	Sets the adapter to static IP configuration
•	Assigns IP address 192.168.1.1
•	Sets subnet mask to 255.255.255.0


6. Troubleshooting

6.1 Common Issues
"Failed to set static IP"
•	Cause: Application not running with administrator privileges
•	Solution: Right-click the application and select "Run as Administrator"
"No DHCP responses"
•	Cause: Windows Firewall blocking UDP port 67 or 68
•	Solution: Add firewall exception for the application or temporarily disable firewall for testing
"IP pool exhausted"
•	Cause: More than 101 clients requesting addresses
•	Solution: Wait for leases to expire or restart the server to clear old leases
"Clients not receiving configuration"
•	Cause: Client and server on different physical networks or VLANs
•	Solution: Ensure clients are connected to the same network segment as the server adapter

6.2 Diagnostic Tips
•	Monitor the log window for DISCOVER messages - this confirms clients are broadcasting
•	Use Wireshark to capture DHCP traffic on both server and client
•	Verify the selected network adapter is connected and has link
•	Check Windows Event Viewer for network-related errors
•	Test with a single client first before connecting multiple devices


7. Security Considerations

7.1 Security Warnings
•	No authentication - Any client can request an IP address
•	No encryption - DHCP communication is unencrypted
•	Broadcast responses - Server configuration visible to all network devices
•	Rogue server risk - Could conflict with legitimate DHCP servers

7.2 Best Practices
•	Use on isolated networks only - Avoid running on production networks
•	Physical network separation - Use dedicated hardware for test environments
•	Limit lease duration - Current 8-hour lease time is reasonable for temporary use
•	Monitor for conflicts - Watch for duplicate IP warnings on clients
•	Document usage - Keep records of when and where the server was deployed


8. Code Examples and Extensions

8.1 Customizing Network Parameters
To modify the IP address range, edit the DhcpServer instantiation in MainWindow.xaml.cs:
_server = new DhcpServer(
    serverIpAddress: "10.0.0.1",
    poolStart: "10.0.0.100",
    poolEnd: "10.0.0.200",
    subnetMask: "255.255.255.0",
    routerIp: "10.0.0.1",
    dnsIp: "1.1.1.1");  // Cloudflare DNS

8.2 Changing Lease Duration
To modify lease duration, edit the GetOrCreateLease method in DhcpServer.cs:
var newLease = new DhcpLease
{
    Mac = mac,
    Ip = candidate,
    Expiry = DateTime.Now.AddHours(24)  // 24-hour lease
};
Remember to also update the lease time option in BuildReplyPacket method (Option 51).

8.3 Adding Lease Persistence
Currently, leases are stored in memory and lost when the server stops. To add persistence, consider:
•	Serializing the _leases dictionary to JSON or XML on shutdown
•	Loading saved leases on startup
•	Implementing periodic saves during operation
•	Adding lease reservation functionality for specific MAC addresses

9. Conclusion
The DHCP Field Server provides a practical, educational implementation of the DHCP protocol. It serves as both a functional tool for network administrators and a learning resource for understanding how DHCP works at a technical level.
Key takeaways from this guide:
•	DHCP automates IP address assignment using a four-step process (DORA)
•	The application uses UDP broadcast communication on ports 67 and 68
•	Lease management ensures efficient use of the IP address pool
•	Security considerations limit deployment to isolated test networks
•	The modular architecture makes it easy to extend and customize

9.1 Further Learning
To deepen your understanding of DHCP and network protocols:
•	Study RFC 2131 (DHCP) and RFC 2132 (DHCP Options)
•	Experiment with packet capture tools like Wireshark
•	Compare with production DHCP servers (ISC DHCP, Windows DHCP)
•	Implement additional DHCP options (NTP servers, domain name, etc.)
•	Explore DHCPv6 for IPv6 networks
