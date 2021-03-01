using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using PcapDotNet.Core;
using PcapDotNet.Packets;
using System.Threading;

namespace PSIP_Udvardi_csh
{
    public partial class Form1 : Form
    {
        
        public Form1()
        {

            InitializeComponent();
            
        }
        private Thread trd;
        public delegate void UpdateLabelUi();

        public void UiUpdateLabel()
        {
            this.progressBar1.Value = 5;
        }
        private void ThreadTask()
        {
            int stp;
            int newval;
            Random rnd = new Random();

            while (true)
            {
                stp = this.progressBar1.Step * rnd.Next(1, 2);
                newval = this.progressBar1.Value + stp;
                if (newval > this.progressBar1.Maximum)
                    newval = this.progressBar1.Maximum;
                else if (newval < this.progressBar1.Minimum)
                    newval = this.progressBar1.Minimum;
                this.progressBar1.Invoke(new MethodInvoker(delegate { this.progressBar1.Value = newval; }));
                Thread.Sleep(100);
            }

        }

       
        private void Form1_Load_1(object sender, EventArgs e)
        {
            Thread trd = new Thread(new ThreadStart(this.ThreadTask));
            trd.IsBackground = true;
            trd.Start();
        }


        private void btnClickThis_Click(object sender, EventArgs e)
        {
            //lblHelloWorld.Text = "Hello C#";
            Console.WriteLine("Console write Test");
            IList<LivePacketDevice> allDevices = LivePacketDevice.AllLocalMachine;
            if (allDevices.Count == 0)
            {
                Console.WriteLine("Hoppa!");
            }
            Console.WriteLine("Hoppa!");
            string resultare = "";
            string deviceInfo = "";
            for (int i = 0; i != allDevices.Count; ++i)
            {
                LivePacketDevice device = allDevices[i];
                Console.Write((i + 1) + ". " + device.Name);
                if (device.Description != null)
                    Console.WriteLine(" (" + device.Description + ")");
                else
                    Console.WriteLine(" (No description available)");
            }

            PacketDevice loopback1 = allDevices[0];

            using (PacketCommunicator communicator =
                loopback1.Open(1000, PacketDeviceOpenAttributes.NoCaptureLocal | PacketDeviceOpenAttributes.Promiscuous, 1000))
            {
                Console.WriteLine("Listen on " + loopback1.Description);
                communicator.ReceivePackets(0, PacketHandler);
            }

        }
        private static void PacketHandler(Packet packet)
        {
            Console.WriteLine(packet.Timestamp.ToString("yyyy-MM-dd hh:mm:ss.fff") + " lenght:" + packet.Length + " " + packet.Ethernet);
        }

        private void lblHelloWorld_Click(object sender, EventArgs e)
        {

        }


    }


}


