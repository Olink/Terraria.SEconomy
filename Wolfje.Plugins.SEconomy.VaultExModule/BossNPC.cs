using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using Terraria;

namespace Wolfje.Plugins.SEconomy.VaultExModule {
    internal class BossNPC {
        public NPC Npc;
        public Dictionary<int, long> damageData = new Dictionary<int, long>();

        public static readonly object __lock = new object();

        public int modifiedHealth;

        public BossNPC(NPC npc) {
            this.Npc = npc;
            this.modifiedHealth = npc.life;
        }

        public void AddDamage(int playerID, int damage) {
            lock (__lock) {
               if (damageData.ContainsKey(playerID))
                    damageData[playerID] += damage;
                else
                    damageData.Add(playerID, damage);
            }
        }

        public Dictionary<int, long> GetRecalculatedReward() {
            long totalDmg = 0;

            lock (__lock) {
                foreach (var v in damageData.Values) {
                    totalDmg += v;
                }

                float valueMod = ((float)totalDmg / (float)modifiedHealth) <= 1 ? ((float)totalDmg / (float)modifiedHealth) : 1;
                float newValue = ((float)modifiedHealth * valueMod) * VaultEx.Config.BossRewardModifier;
                float valuePerDmg = (float)newValue / (float)totalDmg;

                float Mod = 1;
                //    if (Vault.config.OptionalMobModifier.ContainsKey(Npc.netID))
                //      Mod *= Vault.config.OptionalMobModifier[Npc.netID]; // apply custom modifiers      
                Dictionary<int, long> returnDict = new Dictionary<int, long>();

                foreach (KeyValuePair<int, long> kv in damageData) {
                    returnDict[kv.Key] = (long)(kv.Value * valuePerDmg * Mod);

                }
                return returnDict;
            }
        }

    }
}
