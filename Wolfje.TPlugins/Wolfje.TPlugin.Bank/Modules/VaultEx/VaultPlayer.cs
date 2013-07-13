using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Wolfje.Plugins.SEconomy.Modules.VaultEx {
    public class VaultPlayer {

        public byte LastState { get; set; }
        public byte IdleCount { get; set; }
        public int LastPVPID { get; set; }
        public int TotalOnline { get; set; }
        public int TempMin { get; set; }

        public Dictionary<int, int> KillData { get; set; }

        
        public int Index { get; set; }

         public TShockAPI.TSPlayer TSPlayer {
             get {
                 return TShockAPI.TShock.Players[Index];
             }
         }

        public VaultPlayer(int index) {
            this.KillData = new Dictionary<int, int>();
            this.Index = index;
        }


        public void AddKill(int mobID) {
            if (KillData.ContainsKey(mobID))
                KillData[mobID]++;
            else
                KillData.Add(mobID, 1);
        }

    }
}
