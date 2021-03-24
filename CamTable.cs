using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PSIP_Udvardi_csh
{
    public class CamTable
    {
        private int timeoutExpire;
        private static ConcurrentDictionary<String, camEntry> camTableDict;

        internal static ConcurrentDictionary<string, camEntry> CamTableDict { get => camTableDict; set => camTableDict = value; }
    

        public CamTable()
        {
            camTableDict = new ConcurrentDictionary<string, camEntry>();
        }

        public void expireEntry(String key)
        {
            Console.WriteLine(key + " Key deleted because of timeout");
        }

        public void processPacket(packetHandlig packet)
        {
            camEntry newPacketProcessing = new camEntry(packet.MacAddrSource, packet.IngressPort, timeoutExpire);




        }


    
    }
}
