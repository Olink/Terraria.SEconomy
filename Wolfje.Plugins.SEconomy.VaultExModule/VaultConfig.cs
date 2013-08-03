using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;

using Newtonsoft.Json;

namespace Wolfje.Plugins.SEconomy.VaultExModule {

    public class VaultConfig {

        public float BattlePotionModifier = 0.75f;
        public Dictionary<int, float> OptionalMobModifier = new Dictionary<int, float>();
        public bool AnnounceKillGain = false;
        public bool AnnounceBossGain = true;
        public bool StaticDeathPenalty = false;
        public int DeathPenaltyMax = 10000;
        public int DeathPenaltyMin = 10000;
        public int DeathPenaltyPercent = 10;
        public bool PvPWinnerTakesLoosersPenalty = true;
        public float BossRewardModifier = 1.0f;

        public void CreateConfig(string Path) {
            try {
                string config = JsonConvert.SerializeObject(new VaultConfig(), Formatting.Indented);

                System.IO.File.WriteAllText(Path, config);

            } catch (Exception ex) {

                if (ex is DirectoryNotFoundException) {
                    TShockAPI.Log.ConsoleError("vault config: save directory not found: " + Path);

                } else if (ex is UnauthorizedAccessException || ex is System.Security.SecurityException) {
                    TShockAPI.Log.ConsoleError("vault config: Access is denied to Vault config: " + Path);
                } else {
                    TShockAPI.Log.ConsoleError("vault config: Error reading file: " + Path);
                    throw;
                }
            }
        }

        public static VaultConfig ReadConfig(string Path) {
            VaultConfig config = null;

            try {
                string fileInput = System.IO.File.ReadAllText(Path);
                config = JsonConvert.DeserializeObject<VaultConfig>(fileInput);

            } catch (Exception ex) {

                if (ex is DirectoryNotFoundException || ex is FileNotFoundException) {
                    TShockAPI.Log.ConsoleError("Vault config not found. Creating new one");
                    config = new VaultConfig();
                    config.CreateConfig(Path);
                } else if (ex is UnauthorizedAccessException || ex is System.Security.SecurityException) {
                    TShockAPI.Log.ConsoleError("vault config: Access is denied to Vault config: " + Path);
                } else {
                    TShockAPI.Log.ConsoleError("vault config: Error reading file: " + Path);
                    throw;
                }
            }

            return config;
        }
    }
}
