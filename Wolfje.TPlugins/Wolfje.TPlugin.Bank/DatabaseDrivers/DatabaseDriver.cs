using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using SQLite;
using TShockAPI;
using System.Threading.Tasks;

namespace Wolfje.Plugins.SEconomy {
    public class DatabaseDriver {
        public readonly SQLite.SQLiteAsyncConnection AsyncConnection;
        bool _ready = false;
        string _path;

        /// <summary>
        /// Returns whether the database driver is ready for commands or not.
        /// </summary>
        public bool Ready {
            get {
                return _ready;
            }
        }

        public DatabaseDriver(string Path) {
            this.AsyncConnection = new SQLiteAsyncConnection(Path, true);
            int err = this.AsyncConnection.ExecuteScalarAsync<int>("PRAGMA journal_mode = MEMORY").Result;

            _path = Path;
            Task shuttingTheCompilerWarningUp = InitializeDatabaseAsync();
        }

        //#region "Initialize/close"

        /// <summary>
        /// Initializes the database connection
        /// </summary>
        public async Task InitializeDatabaseAsync() {
          
            //Create table schema if doesn't exist.
            //Note that is non-destructive and should not drop tables if they are already there

            await AsyncConnection.CreateTableAsync<DatabaseObjects.BankAccount>();
            await AsyncConnection.CreateTableAsync<DatabaseObjects.BankAccountTransaction>();
            
            int bankAccountCount = await AsyncConnection.Table<DatabaseObjects.BankAccount>().CountAsync();
            Log.ConsoleInfo(string.Format("seconomy: {0} clean - {1} accounts", _path, bankAccountCount));

            _ready = true;

            /*
            return AsyncConnection.CreateTableAsync<DatabaseObjects.BankAccount>().ContinueWith((i) => {
                AsyncConnection.CreateTableAsync<DatabaseObjects.BankAccountTransaction>().ContinueWith((x) => {
                    AsyncConnection.Table<DatabaseObjects.BankAccount>().CountAsync().ContinueWith((count) => {
                        int bankAccountCount = count.Result;
                        Log.ConsoleInfo(string.Format("seconomy: {0} clean - {1} accounts", _path, bankAccountCount));

                        _ready = true;
                    });
                });
            });
             */
        }

        /// <summary>
        /// Creates the world account if needed and sets it.
        /// </summary>
        public async Task EnsureWorldAccountExistsAsync() {

            //World account matches the current world, ignore.
            if (SEconomyPlugin.WorldAccount != null && SEconomyPlugin.WorldAccount.WorldID == Terraria.Main.worldID) {
                return;
            }

            if (Terraria.Main.worldID > 0) {
                int bankAccountK = 0;
                DatabaseObjects.BankAccount worldAccount = await (from i in AsyncConnection.Table<DatabaseObjects.BankAccount>()
                                                                  where (i.Flags & DatabaseObjects.BankAccountFlags.SystemAccount) == DatabaseObjects.BankAccountFlags.SystemAccount
                                                                     && (i.Flags & DatabaseObjects.BankAccountFlags.PluginAccount) == 0
                                                                     && i.WorldID == Terraria.Main.worldID
                                                                  select i).FirstOrDefaultAsync();

                if (worldAccount == null) {
                    //world account does not exist for this world ID, create one
                    worldAccount = new DatabaseObjects.BankAccount();
                    worldAccount.UserAccountName = "SYSTEM";
                    worldAccount.WorldID = Terraria.Main.worldID;
                    worldAccount.Description = "World account for world " + Terraria.Main.worldName;
                    //This account is always enabled, locked to the world it's in and a system account (ie. can run into deficit) but not a plugin account
                    worldAccount.Flags = DatabaseObjects.BankAccountFlags.Enabled | DatabaseObjects.BankAccountFlags.LockedToWorld | DatabaseObjects.BankAccountFlags.SystemAccount;

                    bankAccountK = await AsyncConnection.InsertAsync(worldAccount);
                    worldAccount.BankAccountK = bankAccountK;
                }

                if (worldAccount != null && worldAccount.BankAccountK > 0) {
                    //Is this account listed as enabled?
                    bool accountEnabled = (worldAccount.Flags & DatabaseObjects.BankAccountFlags.Enabled) == DatabaseObjects.BankAccountFlags.Enabled;

                    if (!accountEnabled) {
                        TShockAPI.Log.ConsoleError("The world account for world " + Terraria.Main.worldName + " is disabled.  Currency will not work for this game.");
                    } else {
                        SEconomyPlugin.WorldAccount = new Economy.BankAccount(worldAccount);
                    }
                } else {
                    Log.ConsoleError("There was an error loading the bank account for this world.  Currency will not work for this game.");
                }



                /*
                 * See how much easier this shit is?
                 * 
                 * 
                return AsyncConnection.Table<DatabaseObjects.BankAccount>().Where(i => (i.Flags & DatabaseObjects.BankAccountFlags.SystemAccount) == DatabaseObjects.BankAccountFlags.SystemAccount
                        && (i.Flags & DatabaseObjects.BankAccountFlags.PluginAccount) == 0
                        && i.WorldID == Terraria.Main.worldID).FirstOrDefaultAsync().ContinueWith((worldAccountResult) => {

                    if (worldAccountResult.Result == null) {
                        //world account does not exist for this world ID, create one
                        DatabaseObjects.BankAccount worldAccount = new DatabaseObjects.BankAccount();
                        worldAccount.UserAccountName = "SYSTEM";
                        worldAccount.WorldID = Terraria.Main.worldID;
                        worldAccount.Description = "World account for world " + Terraria.Main.worldName;

                        //This account is always enabled, locked to the world it's in and a system account (ie. can run into deficit) but not a plugin account
                        worldAccount.Flags = DatabaseObjects.BankAccountFlags.Enabled | DatabaseObjects.BankAccountFlags.LockedToWorld | DatabaseObjects.BankAccountFlags.SystemAccount;

                        AsyncConnection.InsertAsync(worldAccount).ContinueWith((newPrimaryKey) => {
                            int bankAccountK = newPrimaryKey.Result;

                            if (bankAccountK > 0) {

                                //Retrieve the new world account from the database
                                AsyncConnection.Table<DatabaseObjects.BankAccount>().Where(i => i.BankAccountK == bankAccountK).FirstOrDefaultAsync().ContinueWith((newWorldAccountResult) => {
                                    if (newWorldAccountResult.Result != null) {

                                        //override world account inserter with the new one.
                                        worldAccount = newWorldAccountResult.Result;

                                        //Is this account listed as enabled?
                                        bool accountEnabled = (worldAccount.Flags & DatabaseObjects.BankAccountFlags.Enabled) == DatabaseObjects.BankAccountFlags.Enabled;

                                        if (!accountEnabled) {
                                            TShockAPI.Log.ConsoleError("The world account for world " + Terraria.Main.worldName + " is disabled.  Currency will not work for this game.");
                                        }

                                        //Push it back to the main instance.
                                        SEconomyPlugin.WorldAccount = new Economy.BankAccount(worldAccount);
                                    }
                                });

                            } else {
                                Log.ConsoleError(string.Format("SEconomy: error: create world account for {0} failed.", Terraria.Main.worldName));
                            }
                        });

                    } else {
                        DatabaseObjects.BankAccount worldAccount = worldAccountResult.Result;

                        //Is this account listed as enabled?
                        bool accountEnabled = (worldAccount.Flags & DatabaseObjects.BankAccountFlags.Enabled) == DatabaseObjects.BankAccountFlags.Enabled;

                        if (!accountEnabled) {
                            TShockAPI.Log.ConsoleError("The world account for world " + Terraria.Main.worldName + " is disabled.  Currency will not work for this game.");
                        }

                        //Assign the world account to the running world.
                        SEconomyPlugin.WorldAccount = new Economy.BankAccount(worldAccount);
                    }
                });

            } else {
                return Task.Factory.StartNew(() => { 
                    TShockAPI.Log.ConsoleError("SEconomy: EnsureBankAccountExists called but no world has been loaded yet.");
                });
            */
            }
        }


  //      #endregion

    }
}
