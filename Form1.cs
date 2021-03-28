﻿using System;
using System.Collections.Generic;
using System.Collections.Concurrent;
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


        PacketDevice loopback1; //Global variables
        PacketDevice loopback2;
        PacketCommunicator lpbckcomm1;
        PacketCommunicator lpbckcomm2;

        int port1_in;
        int port1_out;
        int port2_in;
        int port2_out;
        ConcurrentDictionary<string, int> port1_in_stat;
        ConcurrentDictionary<string, int> port1_out_stat;
        ConcurrentDictionary<string, int> port2_in_stat;
        ConcurrentDictionary<string, int> port2_out_stat;

        public static CamTable camTableClass;

        

        public Form1()
        {
            
            
            InitializeComponent();
            
        }

        public void Form1_Load_1(object sender, EventArgs e)
        {
            
            camTableClass = new CamTable();
            timeoutValue.Value = camTableClass.TimeoutExpire; //Set current timeout in UI.


            IList<LivePacketDevice> allDevices = LivePacketDevice.AllLocalMachine;
            loopback1 = allDevices[0]; //allDevices[0] = GNSloopback2
            loopback2 = allDevices[6]; //allDevices[7] = GNSloopback


            lpbckcomm1 = loopback1.Open(65536, PacketDeviceOpenAttributes.NoCaptureLocal | PacketDeviceOpenAttributes.Promiscuous, 1);
            lpbckcomm2 = loopback2.Open(65536, PacketDeviceOpenAttributes.NoCaptureLocal | PacketDeviceOpenAttributes.Promiscuous, 1);


            this.LoopbackLbl.Text = loopback1.Description;
            this.LoopbackLbl2.Text = loopback2.Description;

            Console.WriteLine("Int1: " + loopback1.Description);
            Console.WriteLine("Int2: " + loopback2.Description);

            //Inicialize Statistics dictionaries.
            port1_in_stat = new ConcurrentDictionary<string, int>();
            port1_out_stat = new ConcurrentDictionary<string, int>();
            port2_in_stat = new ConcurrentDictionary<string, int>();
            port2_out_stat = new ConcurrentDictionary<string, int>();
            initializeCounters();


            resetStatistics();
            Thread statisticsThrd = new Thread(() => update_statistics());


            Thread trdLoop1 = new Thread(() => start_sniffing_lpbck1());//equal> new Thread(delegate() { this.start_sniffing_lpbck1(); });
           
            Thread trdLoop2 = new Thread(() => start_sniffing_lpbck2());

            //Start sniffing, sending and showing stats.
            statisticsThrd.Start();
            trdLoop1.Start();
            trdLoop2.Start();
        }

        
        public delegate void UpdateLabelUi();




        public void start_sniffing_lpbck1()
        {

            Console.WriteLine("Listen on " + loopback1.Description + "name: " + loopback1.Name);
            lpbckcomm1.ReceivePackets(0, PacketHandler1);
            
            
        }
        public void PacketHandler1(Packet packet)
        {
            //Update statistics dictionary.
            IDictionary<string, int> toSendPacketStats = analysePacket(packet);
            foreach (KeyValuePair<string, int> protocol in toSendPacketStats)
            {
                if (protocol.Value == 1) { port1_in_stat[protocol.Key]++; }
            }
            
            //Wrapp packet for processing.
            packetHandlig newPacket = new packetHandlig(packet, 1);
            
            newPacket = camTableClass.processPacket(newPacket);

            //Ignore PC4 traffic and statistics.
            if (!(camTableClass.isPc4Ping(newPacket)))
            {
                port1_in++;
            }

            //Do not send packets that should go out the same port they came in.
            if (newPacket.EgressPort == newPacket.IngressPort)
            {
                Console.WriteLine("*Not sending Packet to port " + newPacket.EgressPort + "Destination address is" + newPacket.MacAddrDestination + "Source Address is: " + newPacket.MacAddrSource);
            }
            else
            {
                Thread trdLoop1_snd = new Thread(() => send_packet_lpbck1(packet)); //equal> new Thread(delegate() { this.ThreadTask(loopback1); });
                trdLoop1_snd.Start();
            }



            Console.WriteLine(packet.Timestamp.ToString("yyyy-MM-dd hh:mm:ss.fff") + " length:" + packet.Length);
            
        }

        public void send_packet_lpbck1(Packet toSend)
        {
            //Update packet out statistics dictionary.
            IDictionary<string, int> toSendPacketStats = analysePacket(toSend);
            foreach (KeyValuePair<string, int> protocol in toSendPacketStats)
            {
                if (protocol.Value == 1) { port2_out_stat[protocol.Key]++; }
            }
            
            
            //Ignore PC4.
            if (!((toSend.Ethernet.Source.ToString() == CamTable.Pc4MacAddr1) || (toSend.Ethernet.Destination.ToString() == CamTable.Pc4MacAddr1)))
            {
                port2_out++;
            }


            //Send packet out.
            lpbckcomm2.SendPacket(toSend);
            Console.WriteLine("SEND L2 " + loopback2.Name + " Packet with IP: " + toSend.Ethernet + "and Timestmp: " + toSend.Timestamp + " SENT!");

            return;
        }

        public void start_sniffing_lpbck2()
        {
 
            Console.WriteLine("Listen on " + loopback2.Description + "name: " + loopback2.Name);
           
            lpbckcomm2.ReceivePackets(0, PacketHandler2);
            
        }

        public void PacketHandler2(Packet packet)
        {
            
            //Stats.
            IDictionary<string, int> toSendPacketStats = analysePacket(packet);
            foreach (KeyValuePair<string, int> protocol in toSendPacketStats)
            {
                if (protocol.Value == 1) { port2_in_stat[protocol.Key]++; }
            }


            //Wrapper for packet.
            packetHandlig newPacketP2 = new packetHandlig(packet, 2);

            newPacketP2 = camTableClass.processPacket(newPacketP2);

            //Ignore PC4
            if ( ! (camTableClass.isPc4Ping(newPacketP2)) )
            {
                port2_in++;
            }

            if (newPacketP2.EgressPort == newPacketP2.IngressPort)
            {
                Console.WriteLine("*Not sending Packet to port " + newPacketP2.EgressPort + "Destination address is" + newPacketP2.MacAddrDestination + "Source Address is: " + newPacketP2.MacAddrSource);
            }
            else
            {
                Thread trdLoop2_snd = new Thread(() => send_packet_lpbck2(packet)); //equal> new Thread(delegate() { this.ThreadTask(loopback1); });
                trdLoop2_snd.Start();
            }

            Console.WriteLine(packet.Timestamp.ToString("yyyy-MM-dd hh:mm:ss.fff") + " length:" + packet.Length);
        }




        public void send_packet_lpbck2(Packet toSend)
        {
            //Stats.
            IDictionary<string, int> toSendPacketStats = analysePacket(toSend);
            foreach(KeyValuePair<string, int> protocol in toSendPacketStats)
            {
                if(protocol.Value == 1) { port1_out_stat[protocol.Key]++; }
            }
            

            lpbckcomm1.SendPacket(toSend);
            Console.WriteLine("SEND L1 " + loopback1.Name + " Packet with IP: " + toSend.Ethernet + "and Timestmp: " + toSend.Timestamp + " SENT!");

            //Ignore PC4.
            if ( ! ((toSend.Ethernet.Source.ToString() == CamTable.Pc4MacAddr1) || (toSend.Ethernet.Destination.ToString() == CamTable.Pc4MacAddr1)))
            {
                port1_out++;
            }

            

            return;
        }

        //Returns a dictionary of protocols with 1 if they are present and 0 if not.
        public IDictionary<string, int> analysePacket(Packet packet)
        {
            IDictionary<string, int> packetStats = new Dictionary<string, int>();

            //Ignore PC4.
            if (packet.Ethernet.Source.ToString() == CamTable.Pc4MacAddr1)
            {
                return packetStats;
            }else if(packet.Ethernet.Destination.ToString() == CamTable.Pc4MacAddr1)
            {
                return packetStats;
            }

            
            if(packet.DataLink.Kind == DataLinkKind.Ethernet)
            {
                packetStats["eth"] = 1;
                if(packet.Ethernet.EtherType == EthernetType.IpV4)
                {
                    packetStats["ipv4"] = 1;
                    if(packet.Ethernet.IpV4.Protocol == PcapDotNet.Packets.IpV4.IpV4Protocol.InternetControlMessageProtocol)
                    {
                        packetStats["icmp"] = 1;
                    }
                    else if(packet.Ethernet.IpV4.Protocol == PcapDotNet.Packets.IpV4.IpV4Protocol.Tcp)
                    {
                        packetStats["tcp"] = 1;
                        if(packet.Ethernet.IpV4.Tcp.DestinationPort == 80)
                        {
                            packetStats["http"] = 1;
                        }
                    }
                    else if(packet.Ethernet.IpV4.Protocol == PcapDotNet.Packets.IpV4.IpV4Protocol.Udp)
                    {
                        packetStats["udp"] = 1;
                    }

                }else if (packet.Ethernet.EtherType == EthernetType.Arp)
                {
                    packetStats["arp"] = 1;
                }
            }


            return packetStats;
        }

        //For testing. Not used.
        private static Packet BuildEthernetPacket(string message)
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
                    Data = new Datagram(Encoding.ASCII.GetBytes(message)),
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

        
       


        //Write to console all interfaces on this PC.
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

            PacketDevice loopback1 = allDevices[5];
            /*
            using (PacketCommunicator communicator =
                loopback1.Open(1000, PacketDeviceOpenAttributes.NoCaptureLocal | PacketDeviceOpenAttributes.Promiscuous, 1000))
            {
                Console.WriteLine("Listen on " + loopback1.Description);
                communicator.ReceivePackets(0, PacketHandler);
            }
            */
        }
        private static void PacketHandler(Packet packet)
        {
            Console.WriteLine(packet.Timestamp.ToString("yyyy-MM-dd hh:mm:ss.fff") + " lenght:" + packet.Length + " " + packet.Ethernet);
        }

        private void lblHelloWorld_Click(object sender, EventArgs e)
        {

        }

        private void descLbl_Click(object sender, EventArgs e)
        {

        }


        //Updates statistics labels and ListBox(CAM table) in UI.
        public void update_statistics()
        {
            Console.WriteLine("Thread for update started");
            while (true)
            {
                this.p1InLabel.Invoke(new MethodInvoker(delegate { this.p1InLabel.Text = port1_in.ToString(); }));
                this.p2OutLabel.Invoke(new MethodInvoker(delegate { this.p2OutLabel.Text = port2_out.ToString(); }));
                this.p1OutLabel.Invoke(new MethodInvoker(delegate { this.p1OutLabel.Text = port1_out.ToString(); }));
                this.p2InLabel.Invoke(new MethodInvoker(delegate { this.p2InLabel.Text = port2_in.ToString(); }));
               

                

                this.p1InEthLabel.Invoke(new MethodInvoker(delegate { this.p1InEthLabel.Text = port1_in_stat["eth"].ToString(); }));
                this.p1InArpLabel.Invoke(new MethodInvoker(delegate { this.p1InArpLabel.Text = port1_in_stat["arp"].ToString(); }));
                this.p1InIpLabel.Invoke(new MethodInvoker(delegate { this.p1InIpLabel.Text = port1_in_stat["ipv4"].ToString(); }));
                this.p1InIcmpLabel.Invoke(new MethodInvoker(delegate { this.p1InIcmpLabel.Text = port1_in_stat["icmp"].ToString(); }));
                this.p1InUdpLabel.Invoke(new MethodInvoker(delegate { this.p1InUdpLabel.Text = port1_in_stat["udp"].ToString(); }));
                this.p1InTcpLabel.Invoke(new MethodInvoker(delegate { this.p1InTcpLabel.Text = port1_in_stat["tcp"].ToString(); }));
                this.p1InHttpLabel.Invoke(new MethodInvoker(delegate { this.p1InHttpLabel.Text = port1_in_stat["http"].ToString(); }));



                
                this.p1OutEthLabel.Invoke(new MethodInvoker(delegate { this.p1OutEthLabel.Text = port1_out_stat["eth"].ToString(); }));
                this.p1OutArpLabel.Invoke(new MethodInvoker(delegate { this.p1OutArpLabel.Text = port1_out_stat["arp"].ToString(); }));
                this.p1OutIpLabel.Invoke(new MethodInvoker(delegate { this.p1OutIpLabel.Text = port1_out_stat["ipv4"].ToString(); }));
                this.p1OutIcmpLabel.Invoke(new MethodInvoker(delegate { this.p1OutIcmpLabel.Text = port1_out_stat["icmp"].ToString(); }));
                this.p1OutUdpLabel.Invoke(new MethodInvoker(delegate { this.p1OutUdpLabel.Text = port1_out_stat["udp"].ToString(); }));
                this.p1OutTcpLabel.Invoke(new MethodInvoker(delegate { this.p1OutTcpLabel.Text = port1_out_stat["tcp"].ToString(); }));
                this.p1OutHttpLabel.Invoke(new MethodInvoker(delegate { this.p1OutHttpLabel.Text = port1_out_stat["http"].ToString(); }));


               
                this.p2InEthLabel.Invoke(new MethodInvoker(delegate { this.p2InEthLabel.Text = port2_in_stat["eth"].ToString(); }));
                this.p2InArpLabel.Invoke(new MethodInvoker(delegate { this.p2InArpLabel.Text = port2_in_stat["arp"].ToString(); }));
                this.p2InIpLabel.Invoke(new MethodInvoker(delegate { this.p2InIpLabel.Text = port2_in_stat["ipv4"].ToString(); }));
                this.p2InIcmpLabel.Invoke(new MethodInvoker(delegate { this.p2InIcmpLabel.Text = port2_in_stat["icmp"].ToString(); }));
                this.p2InUdpLabel.Invoke(new MethodInvoker(delegate { this.p2InUdpLabel.Text = port2_in_stat["udp"].ToString(); }));
                this.p2InTcpLabel.Invoke(new MethodInvoker(delegate { this.p2InTcpLabel.Text = port2_in_stat["tcp"].ToString(); }));
                this.p2InHttpLabel.Invoke(new MethodInvoker(delegate { this.p2InHttpLabel.Text = port2_in_stat["http"].ToString(); }));


         

                this.p2OutEthLabel.Invoke(new MethodInvoker(delegate { this.p2OutEthLabel.Text = port2_out_stat["eth"].ToString(); }));
                this.p2OutArpLabel.Invoke(new MethodInvoker(delegate { this.p2OutArpLabel.Text = port2_out_stat["arp"].ToString(); }));
                this.p2OutIpLabel.Invoke(new MethodInvoker(delegate { this.p2OutIpLabel.Text = port2_out_stat["ipv4"].ToString(); }));
                this.p2OutIcmpLabel.Invoke(new MethodInvoker(delegate { this.p2OutIcmpLabel.Text = port2_out_stat["icmp"].ToString(); }));
                this.p2OutUdpLabel.Invoke(new MethodInvoker(delegate { this.p2OutUdpLabel.Text = port2_out_stat["udp"].ToString(); }));
                this.p2OutTcpLabel.Invoke(new MethodInvoker(delegate { this.p2OutTcpLabel.Text = port2_out_stat["tcp"].ToString(); }));
                this.p2OutHttpLabel.Invoke(new MethodInvoker(delegate { this.p2OutHttpLabel.Text = port2_out_stat["http"].ToString(); }));

                updateCamTableUI();

                Thread.Sleep((int)TimeSpan.FromSeconds(1).TotalMilliseconds);
            }

        }

        //Updates CAM table UI.
        public void updateCamTableUI()
        {
            CamTableBox.Invoke(new MethodInvoker(delegate { this.CamTableBox.Items.Clear(); }));

            foreach (KeyValuePair<string, camEntry> tableEntry in CamTable.CamTableDict)
            {
                CamTableBox.Invoke(new MethodInvoker(delegate{ CamTableBox.Items.Add(tableEntry.Value); }));
            }

            this.entriesLbl.Invoke(new MethodInvoker(delegate { this.entriesLbl.Text = CamTable.CamTableDict.Count.ToString(); }));

        }


        //Resets all counters.
        public void resetStatistics()
        {

            resetCounters();
            port1_in = 0;
            port1_out = 0;
            port2_in = 0;
            port2_out = 0;
            this.p1InLabel.Text = port1_in.ToString();
            this.p1OutLabel.Text = port1_out.ToString();
            this.p2InLabel.Text = port2_in.ToString();
            this.p2OutLabel.Text = port2_out.ToString();
            Console.WriteLine("Stats reset" + port1_in + port1_out + port2_in + port2_out);
        }

        public void resetCounters()
        {
            foreach (KeyValuePair<string, int> protocol in port1_in_stat)
            {
                port1_in_stat[protocol.Key] = 0;
            }
            foreach (KeyValuePair<string, int> protocol in port1_out_stat)
            {
                port1_out_stat[protocol.Key] = 0;
            }
            foreach (KeyValuePair<string, int> protocol in port2_in_stat)
            {
                port2_in_stat[protocol.Key] = 0;
            }
            foreach (KeyValuePair<string, int> protocol in port2_out_stat)
            {
                port2_out_stat[protocol.Key] = 0;
            }
        }

        private void rstBtn_Click(object sender, EventArgs e)
        {
            resetStatistics();

        }


        //Imports all protocols to dictionaries with count 0.
        private void initializeCounters()
        {
            port1_in_stat.TryAdd("eth", 0);
            port1_in_stat.TryAdd("ipv4", 0);
            port1_in_stat.TryAdd("icmp", 0);
            port1_in_stat.TryAdd("tcp", 0);
            port1_in_stat.TryAdd("udp", 0);
            port1_in_stat.TryAdd("http", 0);
            port1_in_stat.TryAdd("arp", 0);

            port1_out_stat.TryAdd("eth", 0);
            port1_out_stat.TryAdd("ipv4", 0);
            port1_out_stat.TryAdd("icmp", 0);
            port1_out_stat.TryAdd("tcp", 0);
            port1_out_stat.TryAdd("udp", 0);
            port1_out_stat.TryAdd("http", 0);
            port1_out_stat.TryAdd("arp", 0);

            port2_in_stat.TryAdd("eth", 0);
            port2_in_stat.TryAdd("ipv4", 0);
            port2_in_stat.TryAdd("icmp", 0);
            port2_in_stat.TryAdd("tcp", 0);
            port2_in_stat.TryAdd("udp", 0);
            port2_in_stat.TryAdd("http", 0);
            port2_in_stat.TryAdd("arp", 0);

            port2_out_stat.TryAdd("eth", 0);
            port2_out_stat.TryAdd("ipv4", 0);
            port2_out_stat.TryAdd("icmp", 0);
            port2_out_stat.TryAdd("tcp", 0);
            port2_out_stat.TryAdd("udp", 0);
            port2_out_stat.TryAdd("http", 0);
            port2_out_stat.TryAdd("arp", 0);
        }

        private void label11_Click(object sender, EventArgs e)
        {

        }

        private void timeoutValue_KeyUp(object sender, KeyEventArgs e)
        {
            camTableClass.changeExpireTimeout(Convert.ToInt32(timeoutValue.Value));
        }

        private void button1_Click(object sender, EventArgs e)
        {
            camTableClass.clearTable();
        }
    }


}


