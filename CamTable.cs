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
        private int timeoutExpire = 10;
        private static ConcurrentDictionary<string, camEntry> camTableDict;

        internal static ConcurrentDictionary<string, camEntry> CamTableDict { get => camTableDict; set => camTableDict = value; }
    

        public CamTable()
        {
            camTableDict = new ConcurrentDictionary<string, camEntry>();
            timeoutExpire = 10;
        }

        public void expireEntry(String key)
        {
            Console.WriteLine(key + " Key deleted because of timeout");
        }

        public double getCurrentTime(string macAddr)
        {
            double time = 0;
            if (camTableDict.TryGetValue(macAddr, out camEntry value))
            {
                time = value.getTimeRemaining();
            }
            else
            {
                time = -1;
            }
            
            return time;

        }

        public packetHandlig processPacket(packetHandlig packet)
        {
            //camEntry newPacketProcessing = new camEntry(packet.MacAddrSource, packet.IngressPort, timeoutExpire);

            if(camTableDict.TryGetValue(packet.MacAddrSource, out camEntry valueForAdd))
            {
                if(valueForAdd.Port == packet.IngressPort)
                {
                    valueForAdd.resetTimer(timeoutExpire);
                }
                else
                {
                    camEntry newEntryCam = createEntry(packet);

                    camTableDict.TryUpdate(packet.MacAddrSource, newEntryCam, valueForAdd);
                }
            }
            else
            {
                if (camTableDict.TryAdd(packet.MacAddrSource, createEntry(packet))) {

                }
                else
                {
                    Console.WriteLine("*Error*: Could not add Entry to CAM Table, but have checked if this entry Exists" +
                        " Fatal Error, Debug required");
                }
            }

            //Search phase
            if(camTableDict.TryGetValue(packet.MacAddrDestination, out camEntry valueForSearch))
            {
                packet.EgressPort = valueForSearch.Port;
            }
            else
            {
                packet.EgressPort = -1;
            }
           
            


            return packet;

        }

        private camEntry createEntry(packetHandlig packet)
        {
            return new camEntry(packet.MacAddrSource, packet.IngressPort, timeoutExpire);
        }
    
    }
}
