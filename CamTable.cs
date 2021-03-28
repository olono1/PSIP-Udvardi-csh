using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Timers;

namespace PSIP_Udvardi_csh
{
    public class CamTable
    {
        private int timeoutExpire = 10;
        private int portsExpireTimeout;
        private static ConcurrentDictionary<string, camEntry> camTableDict;
        private ConcurrentDictionary<int, Timer> noTrafficDictionrary;
        private Timer port1TimeOut;
        private Timer port2TimeOut;
        private string Pc4MacAddr = "C2:04:44:B8:00:00";



        internal static ConcurrentDictionary<string, camEntry> CamTableDict { get => camTableDict; set => camTableDict = value; }
        public int TimeoutExpire { get => timeoutExpire; set => timeoutExpire = value; }

        public CamTable()
        {
            camTableDict = new ConcurrentDictionary<string, camEntry>();
            noTrafficDictionrary = new ConcurrentDictionary<int, Timer>();
            timeoutExpire = 30;
            portsExpireTimeout = 10;

            noTrafficDictionrary.TryAdd(1, new Timer());
            noTrafficDictionrary.TryAdd(2, new Timer());

            noTrafficDictionrary[1].Interval= TimeSpan.FromSeconds(portsExpireTimeout).TotalMilliseconds;
            noTrafficDictionrary[1].Elapsed += (sender, eventArgs) => { removeEntriesByPort(1); };
            noTrafficDictionrary[1].Enabled = true;

            noTrafficDictionrary[2].Interval = TimeSpan.FromSeconds(portsExpireTimeout).TotalMilliseconds;
            noTrafficDictionrary[2].Elapsed += (sender, eventArgs) => { removeEntriesByPort(2); };
            noTrafficDictionrary[2].Enabled = true;


        }

        public void expireEntry(String key)
        {
            Console.WriteLine(key + " Key deleted because of timeout");
        }

        public void changeExpireTimeout(int timeoutValue)
        {
            this.timeoutExpire = timeoutValue;
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

            //Update port NoTraffic timers
            resetTrafficTimer(packet.IngressPort);

            if (isPc4Ping(packet))
            {
                packet.EgressPort = -1;
                return packet;
            }

            //Update Cam Table
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
                    valueForAdd.stopAndDisposeTimer();
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

        private bool isPc4Ping(packetHandlig packet)
        {
            if(packet.MacAddrSource == Pc4MacAddr)
            {
                return true;
            }else if(packet.MacAddrDestination == Pc4MacAddr)
            {
                return true;
            }
            return false;
        }

        private void resetTrafficTimer(int port)
        {
            //Remove old
            noTrafficDictionrary[port].Stop();
            noTrafficDictionrary[port].Dispose();

            //Create New
            noTrafficDictionrary[port] = new Timer();
            noTrafficDictionrary[port].Interval = TimeSpan.FromSeconds(portsExpireTimeout).TotalMilliseconds;
            noTrafficDictionrary[port].Elapsed += (sender, eventArgs) => { removeEntriesByPort(port); };
            noTrafficDictionrary[port].Enabled = true;

        }

        private camEntry createEntry(packetHandlig packet)
        {
            return new camEntry(packet.MacAddrSource, packet.IngressPort, timeoutExpire);
        }
        
        public void removeEntriesByPort(int port)
        {
            Console.WriteLine("------Cable disconnected from port:" + port);
            foreach(KeyValuePair<string, camEntry> entry in camTableDict)
            {
                if(entry.Value.Port == port)
                {
                    entry.Value.stopAndDisposeTimer();
                    camTableDict.TryRemove(entry.Key, out camEntry deletedEntry);
                }
            }

            resetTrafficTimer(port);

        }


        public void clearTable()
        {
            foreach (KeyValuePair<string, camEntry> entry in camTableDict)
            {
                entry.Value.stopAndDisposeTimer();
            }
            camTableDict.Clear();
        }
    }
}
