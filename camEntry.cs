using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using System.Timers;

namespace PSIP_Udvardi_csh
{
    class camEntry
    {
        private string macAddress;
        private int port;
        private Timer entryTimeout;
        
        

        public camEntry(string macAddress, int port_recv, int timeMilisec)
        {
            

            this.macAddress = macAddress;
            port = port_recv;

            

            CamTable.CamTableDict.TryAdd(macAddress, this);

            entryTimeout = new Timer();
            entryTimeout.Interval = timeMilisec;
            entryTimeout.Elapsed += removeMe;

            entryTimeout.Enabled = true;


        }

        private void removeMe(object sender, EventArgs e)
        {
            if(CamTable.CamTableDict.TryRemove(macAddress, out camEntry deletedEntry))
            {
                Console.WriteLine(" Successfully deleted:" + deletedEntry.macAddress);
            }

            
        }

        public void resetTimer(int newTimeout)
        {
            entryTimeout.Interval = newTimeout;
        }
    }
}
