using System;
using System.Linq;
using System.Net.NetworkInformation;
using System.Windows;

namespace DhcpFieldServer
{
    public partial class MainWindow : Window
    {
        private DhcpServer? _server;

        public MainWindow()
        {
            InitializeComponent();
            LoadAdapters();
            StatusText.Text = "Idle";
        }

        private void LoadAdapters()
        {
            ServerNicCombo.Items.Clear();

            var nics = NetworkInterface.GetAllNetworkInterfaces()
                .Where(n =>
                    n.NetworkInterfaceType != NetworkInterfaceType.Loopback &&
                    n.NetworkInterfaceType != NetworkInterfaceType.Tunnel &&
                    n.OperationalStatus == OperationalStatus.Up);

            foreach (var nic in nics)
            {
                ServerNicCombo.Items.Add(nic.Name);
            }

            if (ServerNicCombo.Items.Count > 0)
                ServerNicCombo.SelectedIndex = 0;
        }

        private void StartBtn_Click(object sender, RoutedEventArgs e)
        {
            if (ServerNicCombo.SelectedItem == null)
            {
                MessageBox.Show("Select a server network adapter first.", "DHCP Field Server",
                    MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            string adapterName = ServerNicCombo.SelectedItem.ToString()!;

            AppendLog($"[INFO] Using adapter: {adapterName}");

            // Set static IP on the chosen adapter: 192.168.1.1 / 255.255.255.0
            AppendLog("[INFO] Setting static IP 192.168.1.1/255.255.255.0 on adapter (via netsh)...");
            bool ipOk = NetworkHelper.SetStaticIp(adapterName, "192.168.1.1", "255.255.255.0");

            if (!ipOk)
            {
                AppendLog("[ERROR] Failed to set static IP. Are you running as Administrator?");
                MessageBox.Show("Failed to set static IP. Run this app as Administrator.",
                    "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }

            // Start DHCP server
            try
            {
                _server = new DhcpServer(
                    serverIpAddress: "192.168.1.1",
                    poolStart: "192.168.1.50",
                    poolEnd: "192.168.1.150",
                    subnetMask: "255.255.255.0",
                    routerIp: "192.168.1.1",
                    dnsIp: "8.8.8.8");

                _server.Log += msg =>
                {
                    Dispatcher.Invoke(() => AppendLog(msg));
                };

                _server.Start();
                StatusText.Text = "DHCP Running";
                StartBtn.IsEnabled = false;
                StopBtn.IsEnabled = true;
            }
            catch (Exception ex)
            {
                AppendLog("[ERROR] " + ex.Message);
                MessageBox.Show("Failed to start DHCP server:\n" + ex.Message,
                    "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void StopBtn_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                _server?.Stop();
            }
            catch (Exception ex)
            {
                AppendLog("[ERROR] " + ex.Message);
            }

            StatusText.Text = "Stopped";
            StartBtn.IsEnabled = true;
            StopBtn.IsEnabled = false;
        }

        private void AppendLog(string message)
        {
            LogBox.AppendText($"[{DateTime.Now:HH:mm:ss}] {message}{Environment.NewLine}");
            LogBox.ScrollToEnd();
        }
    }
}
