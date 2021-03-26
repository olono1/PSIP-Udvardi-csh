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
        DateTime timerStart;
        private double timeoutValueSeconds;



        public Timer EntryTimeout { get => entryTimeout; set => entryTimeout = value; }
        public int Port { get => port; set => port = value; }

        public camEntry(string macAddress, int port_recv, double timeSeconds)
        {
            

            this.macAddress = macAddress;
            port = port_recv;

            

            CamTable.CamTableDict.TryAdd(macAddress, this);
            

            entryTimeout = new Timer();
            entryTimeout.Interval = TimeSpan.FromSeconds(timeSeconds).TotalMilliseconds;
            entryTimeout.Elapsed += removeMe;
            
            timerStart = DateTime.Now;
            entryTimeout.Enabled = true;
            timeoutValueSeconds = timeSeconds;

        }


        private void initializeNewTimer(double timeSeconds)
        {
            entryTimeout.Stop();
            entryTimeout.Dispose();

            entryTimeout = new Timer();
            entryTimeout.Interval = TimeSpan.FromSeconds(timeSeconds).TotalMilliseconds;
            entryTimeout.Elapsed += removeMe;
            timerStart = DateTime.Now;
            entryTimeout.Enabled = true;
            timeoutValueSeconds = timeSeconds;
        }

        public double getTimeRemaining()
        {
            TimeSpan timerRunningFor = DateTime.Now - timerStart;

            return timeoutValueSeconds - timerRunningFor.TotalSeconds;

        }



        private void removeMe(object sender, EventArgs e)
        {
            if(CamTable.CamTableDict.TryRemove(macAddress, out camEntry deletedEntry))
            {
                entryTimeout.Stop();
                entryTimeout.Dispose();
                Console.WriteLine(" Successfully deleted:" + deletedEntry.macAddress);
            }

            
        }

        public void resetTimer(double newTimeoutSeconds)
        {
            initializeNewTimer(newTimeoutSeconds);

        }

        public override string ToString()
        {
            return macAddress + " Port:" + port + " Timeout: " + getTimeRemaining() + "s";
        }

    }
}
