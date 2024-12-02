using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Windows.Forms;

namespace Chat2
{
    public partial class Form1 : Form
    {
        private UdpClient udpClient;
        private const string MulticastAddress = "224.5.5.5";
        private const int Port = 5000;
        private Thread receiveThread;
        private string userName;
        private HashSet<string> activeUsers = new HashSet<string>();
        private System.Windows.Forms.Timer statusTimer;

        public Form1()
        {
            InitializeComponent();
            userName = "Боб";
            StartListening();
            InitializeStatusTimer();
        }

        private void Form1_Load(object sender, EventArgs e)
        {
            SendUserStatus("в сети");
        }

        private void StartListening()
        {
            udpClient = new UdpClient();
            udpClient.Client.SetSocketOption(SocketOptionLevel.Socket, SocketOptionName.ReuseAddress, true);
            udpClient.Client.Bind(new IPEndPoint(IPAddress.Any, Port));
            udpClient.JoinMulticastGroup(IPAddress.Parse(MulticastAddress));
            receiveThread = new Thread(ReceiveMessages);
            receiveThread.IsBackground = true;
            receiveThread.Start();
        }

        private void InitializeStatusTimer()
        {
            statusTimer = new System.Windows.Forms.Timer();
            statusTimer.Interval = 1000;
            statusTimer.Tick += (s, e) => SendUserStatus("в сети");
            statusTimer.Start();
        }

        private void ReceiveMessages()
        {
            IPEndPoint remoteEndPoint = new IPEndPoint(IPAddress.Any, Port);
            while (true)
            {
                byte[] receivedBytes = udpClient.Receive(ref remoteEndPoint);
                string message = Encoding.UTF8.GetString(receivedBytes);
                ProcessMessage(message);
            }
        }

        private void ProcessMessage(string message)
        {
            if (message.StartsWith("в сети: "))
            {
                string user = message.Substring(8).Trim();
                if (activeUsers.Add(user))
                {
                    Invoke(new Action(() => UpdateActiveUsersList()));
                    string displayName = user == userName ? "Вы" : user;
                    Invoke(new Action(() => lstMessages.Items.Add($"{displayName} подключился.")));
                }
            }
            else if (message.StartsWith("вышел: "))
            {
                string user = message.Substring(7).Trim();
                if (activeUsers.Remove(user))
                {
                    Invoke(new Action(() => UpdateActiveUsersList()));
                    string displayName = user == userName ? "Вы" : user;
                    Invoke(new Action(() => lstMessages.Items.Add($"{displayName} отключился.")));
                }
            }
            else
            {
                string sender = message.Split(':')[0];
                string displayName = sender == userName ? "Вы" : sender;
                Invoke(new Action(() => lstMessages.Items.Add($"{displayName}: {message.Substring(sender.Length + 1).Trim()}")));
            }
        }

        private void UpdateActiveUsersList()
        {
            lstActiveUsers.Items.Clear();
            foreach (var user in activeUsers)
            {
                lstActiveUsers.Items.Add(user);
            }
        }

        private void btnSend_Click(object sender, EventArgs e)
        {
            string message = txtMessage.Text;
            if (!string.IsNullOrWhiteSpace(message))
            {
                string formattedMessage = $"{userName}: {message}";
                byte[] messageBytes = Encoding.UTF8.GetBytes(formattedMessage);
                udpClient.Send(messageBytes, messageBytes.Length, MulticastAddress, Port);
                txtMessage.Clear();
            }
        }

        protected override void OnFormClosing(FormClosingEventArgs e)
        {
            SendUserStatus("вышел");
            udpClient.DropMulticastGroup(IPAddress.Parse(MulticastAddress));
            udpClient.Close();
            receiveThread.Abort();
            statusTimer.Stop();
            base.OnFormClosing(e);
        }

        private void SendUserStatus(string status)
        {
            string message = $"{status}: {userName}";
            byte[] messageBytes = Encoding.UTF8.GetBytes(message);
            udpClient.Send(messageBytes, messageBytes.Length, MulticastAddress, Port);
        }
    }
}