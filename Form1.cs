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

namespace PSIP_Udvardi_csh
{
    public partial class Form1 : Form
    {
        public Form1()
        {
            InitializeComponent();
            
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
    }


}


