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
        //Make stats with a dictionary.IDictionary<string, int> dict = new Dictionary<string, int>();

        PacketDevice loopback1; //Global variables
        PacketDevice loopback2;
        PacketCommunicator lpbckcomm1;
        PacketCommunicator lpbckcomm2;
        public Form1()
        {
            InitializeComponent();
            
        }

        private void Form1_Load_1(object sender, EventArgs e)
        {
            IList<LivePacketDevice> allDevices = LivePacketDevice.AllLocalMachine;
            loopback1 = allDevices[0]; //allDevices[0] = GNSloopback
            loopback2 = allDevices[5]; //allDevices[5] = GNSloopback2


            lpbckcomm1 = loopback1.Open(65536, PacketDeviceOpenAttributes.NoCaptureLocal, 1000);
            lpbckcomm2 = loopback2.Open(65536, PacketDeviceOpenAttributes.NoCaptureLocal, 1000);

            Console.WriteLine("Int1: " + loopback1.Description);
            Console.WriteLine("Int2: " + loopback2.Description);
;

            Thread trdLoop1 = new Thread(() => start_sniffing_lpbck1());//equal> new Thread(delegate() { this.start_sniffing_lpbck1(); });
            trdLoop1.Start();
            Thread trdLoop2 = new Thread(() => start_sniffing_lpbck2());
            trdLoop2.Start();
        }

      
        public delegate void UpdateLabelUi();

        public void start_sniffing_lpbck1()
        {

                Console.WriteLine("Listen on " + loopback1.Description + "name: " + loopback1.Name);
                
                Packet packet;
                Packet forSending;

           
                do
                {                    
                    PacketCommunicatorReceiveResult result = lpbckcomm1.ReceivePacket(out packet);
                    switch (result)
                    {
                        case PacketCommunicatorReceiveResult.Timeout:
                            // Timeout elapsed
                            continue;
                        case PacketCommunicatorReceiveResult.Ok:
                            forSending = packet;
                            Console.WriteLine("length:" +
                                              packet.Length + "Eth:" + packet.Ethernet);

                            //send a recieved packet on loopback 1 from loopback2
                            Thread trdLoop1_snd = new Thread(() => this.send_packet_lpbck1(forSending)); //equal> new Thread(delegate() { this.ThreadTask(loopback1); });
                            trdLoop1_snd.Start();
                            break;
                        default:
                            throw new InvalidOperationException("The result " + result + " shoudl never be reached here");
                    }
                } while (true);


                
            
        }

        public void start_sniffing_lpbck2()
        {
 
                Console.WriteLine("Listen on " + loopback2.Description + "name: " + loopback2.Name);
                //Second port Interface

                Packet packet;
                do
                {
                    PacketCommunicatorReceiveResult result = lpbckcomm2.ReceivePacket(out packet);
                    switch (result)
                    {
                        case PacketCommunicatorReceiveResult.Timeout:
                            // Timeout elapsed
                            continue;
                        case PacketCommunicatorReceiveResult.Ok:
                            Packet forSending = packet;
                            Console.WriteLine("length:" +
                                              packet.Length + " Eth:" + packet.Ethernet);
                            
                        //send a packet recived on loopback2 from loopback1
                            Thread trdLoop2_snd = new Thread(() => send_packet_lpbck2(forSending)); //equal> new Thread(delegate() { this.ThreadTask(loopback1); });
                            trdLoop2_snd.Start();
                            break;
                        default:
                            throw new InvalidOperationException("The result " + result + " shoudl never be reached here");
                    }
                } while (true);
           
        }
        public int send_packet_lpbck1(Packet toSend)
        {

                
                lpbckcomm2.SendPacket(toSend);
                Console.WriteLine("SEND" + loopback2.Name +" Packet with IP: " + toSend.Ethernet + "and Timestmp: " + toSend.Timestamp + " SENT!");
                
                return 0;
        }

        private int send_packet_lpbck2(Packet toSend)
        {

                lpbckcomm1.SendPacket(toSend);
                Console.WriteLine("SEND" + loopback1.Name + " Packet with IP: " + toSend.Ethernet + "and Timestmp: " + toSend.Timestamp + " SENT!");

            
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


