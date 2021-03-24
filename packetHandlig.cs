using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using PcapDotNet.Packets;

namespace PSIP_Udvardi_csh
{
    public class packetHandlig
    {

        private Packet packetToProcess;
        private int ingressPort;
        private int egressPort;
        private String macAddrSource;
        private String macAddrDestination;


        public packetHandlig(Packet packetToProcess, int ingressPort)
        {
            this.PacketToProcess = packetToProcess;
            this.IngressPort = ingressPort;
            savePacketInfo();
            
        }

        private void savePacketInfo()
        {
            MacAddrSource = PacketToProcess.Ethernet.Source.ToString();
            macAddrDestination = PacketToProcess.Ethernet.Destination.ToString();

            Console.WriteLine("*SrcAddrMAC: " + MacAddrSource + "  *DstAddrMAC: " + macAddrDestination);
        }

        public int EgressPort { get => egressPort; set => egressPort = value; }
        public string MacAddrSource { get => macAddrSource; set => macAddrSource = value; }
        public Packet PacketToProcess { get => packetToProcess; set => packetToProcess = value; }
        public int IngressPort { get => ingressPort; set => ingressPort = value; }
    }
}
