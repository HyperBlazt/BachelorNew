using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.Management;
using Microsoft.Win32;

namespace Virus
{
    public partial class Form1 : Form
    {

        private TcpListener _tcpListener;
        private Thread _listenThread;

        public Form1()
        {
            InitializeComponent();
            Server();

        }

        public void SendData()
        {
            var clientConnection = new TcpClient();
            clientConnection.Connect("192.168.1.217",3000);
            NetworkStream nwStream = clientConnection.GetStream();
            byte[] bytesToSend = ASCIIEncoding.ASCII.GetBytes(Environment.MachineName + Environment.NewLine + Environment.OSVersion);
            nwStream.Write(bytesToSend, 0, bytesToSend.Length);
            clientConnection.Close();
        }


        public void Server()
        {
            _tcpListener = new TcpListener(IPAddress.Any, 3000);
            _listenThread = new Thread(new ThreadStart(ListenForClients));
            richTextBox1.Text = "Server started - listining on port 3000";
            _listenThread.Start();
        }


        private void ListenForClients()
        {
            this._tcpListener.Start();

            while (true)
            {
                //blocks until a client has connected to the server
                TcpClient client = this._tcpListener.AcceptTcpClient();
                HandleClientComm(client);
                //create a thread to handle communication 
                //with connected client
                //Thread clientThread = new Thread(new ParameterizedThreadStart(HandleClientComm));
                //clientThread.Start(client);
            }
        }

        private void HandleClientComm(object client)
        {
            TcpClient tcpClient = (TcpClient)client;
            NetworkStream clientStream = tcpClient.GetStream();

            byte[] message = new byte[4096];
            int bytesRead;
            richTextBox1.Text = "listining for clients...";
            while (true)
            {
                bytesRead = 0;

                try
                {
                    //blocks until a client sends a message
                    bytesRead = clientStream.Read(message, 0, 4096);
                    string result = System.Text.Encoding.UTF8.GetString(message);
                    richTextBox1.Text = result;

                }
                catch
                {
                    //a socket error has occured
                    break;
                }

                if (bytesRead == 0)
                {
                    //the client has disconnected from the server
                    break;
                }

                //message has successfully been received
                ASCIIEncoding encoder = new ASCIIEncoding();
                System.Diagnostics.Debug.WriteLine(encoder.GetString(message, 0, bytesRead));
            }

            tcpClient.Close();
        }
    }
}
