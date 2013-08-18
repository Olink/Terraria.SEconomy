using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using Newtonsoft.Json;

namespace Wolfje.Plugins.SEconomy {
    public class Configuration {

        public bool BankAccountsEnabled = true;
        public string StartingMoney = "0";

        public int PayIntervalMinutes = 30;
        public int IdleThresholdMinutes = 10;
        public string IntervalPayAmount = "0";
        public static string BaseDirectory = @"tshock" + System.IO.Path.DirectorySeparatorChar + "SEconomy";
        public static string JournalPath = Configuration.BaseDirectory + System.IO.Path.DirectorySeparatorChar + "SEconomy.journal.xml.gz";

        public int JournalBackupMinutes = 1;

        public MoneyProperties MoneyConfiguration = new MoneyProperties();

        public bool EnableProfiler = false;

        /// <summary>
        /// Loads a configuration file and deserializes it from JSON
        /// </summary>
        public static Configuration LoadConfigurationFromFile(string Path) {
            Configuration config = null;

            try {
                string fileText = System.IO.File.ReadAllText(Path);

                config = JsonConvert.DeserializeObject<Configuration>(fileText);

            } catch (Exception ex) {
                if (ex is System.IO.FileNotFoundException || ex is System.IO.DirectoryNotFoundException) {
                    TShockAPI.Log.ConsoleError("seconomy configuration: Cannot find file or directory. Creating new one.");

                    config = Configuration.NewSampleConfiguration();

                    config.SaveConfiguration(Path);

                } else if (ex is System.Security.SecurityException) {
                    TShockAPI.Log.ConsoleError("seconomy configuration: Access denied reading file " + Path);
                } else {
                    TShockAPI.Log.ConsoleError("seconomy configuration: error " + ex.ToString());
                }
            }

            return config;
        }

        public static Configuration NewSampleConfiguration() {
            Configuration newConfig = new Configuration();

            return newConfig;
        }

        public void SaveConfiguration(string Path) {
            try {
                string config = JsonConvert.SerializeObject(this, Formatting.Indented);
                System.IO.File.WriteAllText(Path, config);
            } catch (Exception ex) {

                if (ex is System.IO.DirectoryNotFoundException) {
                    TShockAPI.Log.ConsoleError("vault config: save directory not found: " + Path);

                } else if (ex is UnauthorizedAccessException || ex is System.Security.SecurityException) {
                    TShockAPI.Log.ConsoleError("vault config: Access is denied to Vault config: " + Path);
                } else {
                    TShockAPI.Log.ConsoleError("vault config: Error reading file: " + Path);
                    throw;
                }
            }
        }
    }
}
