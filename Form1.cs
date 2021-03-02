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
using PcapDotNet.Packets.Ethernet;
using System.Threading;

namespace PSIP_Udvardi_csh
{

    public partial class Form1 : Form
    {
        //IDEA:: Create a class that will carry the statistics details accross the threads
        public Form1()
        {
            InitializeComponent();
            
        }

        private void Form1_Load_1(object sender, EventArgs e)
        {
            IList<LivePacketDevice> allDevices = LivePacketDevice.AllLocalMachine;
            PacketDevice loopback1 = allDevices[4];
            PacketDevice loopback2 = allDevices[0];
            Console.WriteLine("Int1: " + loopback1.Description);
            Console.WriteLine("Int2: " + loopback2.Description);
            //Thread trdLoop1_snd = new Thread(() => this.ThreadTask(loopback1)); //equal> new Thread(delegate() { this.ThreadTask(loopback1); });
           // trdLoop1_snd.IsBackground = true;
           // trdLoop1_snd.Start();
            Thread trdLoop1_recv = new Thread(() => this.start_sniffing(loopback2,loopback1));
            trdLoop1_recv.IsBackground = true;
            trdLoop1_recv.Start();
            //Thread trdLoop2_snd = new Thread(() => this.ThreadTask(loopback2));
            //trdLoop2_snd.IsBackground = true;
            //trdLoop2_snd.Start();
            //Thread trdLoop2_recv = new Thread(() => this.start_sniffing(loopback1, loopback2));
            //trdLoop2_recv.IsBackground = true;
           // trdLoop2_recv.Start();
        }

        private Thread trd;
        public delegate void UpdateLabelUi();

        public void start_sniffing(PacketDevice sendingInterface, PacketDevice recvInterface)
        {
            using (PacketCommunicator communicator =
                    recvInterface.Open(1000, 
                    PacketDeviceOpenAttributes.NoCaptureLocal | 
                    PacketDeviceOpenAttributes.Promiscuous, 1000))
            {
                Console.WriteLine("Listen on " + recvInterface.Description);
                Packet packet;
                do
                {
                    PacketCommunicatorReceiveResult result = communicator.ReceivePacket(out packet);
                    switch (result)
                    {
                        case PacketCommunicatorReceiveResult.Timeout:
                            // Timeout elapsed
                            continue;
                        case PacketCommunicatorReceiveResult.Ok:
                            Console.WriteLine(packet.Timestamp.ToString("yyyy-MM-dd hh:mm:ss.fff") + " length:" +
                                              packet.Length + "Eth:" + packet.Ethernet);

                            Thread trdLoop1_snd = new Thread(() => this.send_packet(sendingInterface, packet)); //equal> new Thread(delegate() { this.ThreadTask(loopback1); });
                            trdLoop1_snd.IsBackground = true;
                            trdLoop1_snd.Start();
                            break;
                        default:
                            throw new InvalidOperationException("The result " + result + " shoudl never be reached here");
                    }
                } while (true);


                
            }
        }
        private int send_packet(PacketDevice sendingInterface, Packet toSend)
        {
            using (PacketCommunicator communicator =
                   sendingInterface.Open(1000,
                   PacketDeviceOpenAttributes.NoCaptureLocal |
                   PacketDeviceOpenAttributes.Promiscuous, 1000))
            {
                communicator.SendPacket(toSend);
                Console.WriteLine("Packet with IP: " + toSend.IpV4 + "and Timestmp: " + toSend.Timestamp + " SENT!");
                Console.WriteLine("Sending garbage Ethernet");
                communicator.SendPacket(BuildEthernetPacket());
            }
            


                return 0;
        }


        private static Packet BuildEthernetPacket()
        {
            EthernetLayer ethernetLayer =
                new EthernetLayer
                {
                    Source = new MacAddress("01:01:01:01:01:01"),
                    Destination = new MacAddress("02:02:02:02:02:02"),
                    EtherType = EthernetType.IpV4,
                };

            PayloadLayer payloadLayer =
                new PayloadLayer
                {
                    Data = new Datagram(Encoding.ASCII.GetBytes("hello world")),
                };

            PacketBuilder builder = new PacketBuilder(ethernetLayer, payloadLayer);

            return builder.Build(DateTime.Now);
        }






        public void UiUpdateLabel()
        {
            this.progressBar1.Value = 5;
        }
        private void ThreadTask(PacketDevice deviceInterfaceLoopBck)
        {
            

                //this.progressBar1.Invoke(new MethodInvoker(delegate { this.progressBar1.Value = newval; }));
               
            

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


