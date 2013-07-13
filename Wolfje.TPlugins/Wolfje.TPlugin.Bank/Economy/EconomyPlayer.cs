using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Wolfje.Plugins.SEconomy.Economy {

    public class EconomyPlayer {

    #region "Events"

        /// <summary>
        /// Fires when a bank account is successully loaded.
        /// </summary>
        public static event EventHandler PlayerBankAccountLoaded;
    #endregion
        
        public int Index { get; set; }
        public TShockAPI.TSPlayer TSPlayer {
            get {
                return TShockAPI.TShock.Players[Index];
            }
        }

        public EconomyPlayer(int index) {
            this.Index = index;
        }

        public Economy.BankAccount BankAccount { get; internal set; }
        public PlayerControlFlags LastKnownState { get; internal set; }
        
        /// <summary>
        /// Returns the date and time of a player's last action
        /// </summary>
        public DateTime IdleSince { get; internal set; }

        /// <summary>
        /// Returns a TimeSpan representing the amount of time the user has been idle for
        /// </summary>
        public TimeSpan TimeSinceIdle {
            get {
                return DateTime.Now.Subtract(this.IdleSince);
            }
        }


        /// <summary>
        /// Ensures a bank account exists for the logged-in user and makes sure it's loaded properly.
        /// </summary>
        public Task EnsureBankAccountExists() {
            return LoadBankAccountAsync(CreateIfNone: true);
        }

        /// <summary>
        /// Asynchronously loads a bank account
        /// </summary>
        Task LoadBankAccountAsync(int BankAccountK) {
            if (!SEconomyPlugin.Database.Ready) {
                return new Task(() => { return; });
            }

            //Bank account status flags, could change as more features are implemented
            return SEconomyPlugin.Database.AsyncConnection.Table<DatabaseObjects.BankAccount>().Where(i => i.BankAccountK == BankAccountK).FirstOrDefaultAsync().ContinueWith((bankAccountResult) => {
                if (!bankAccountResult.IsFaulted && bankAccountResult.Result != null) {
                    //Parse the databse object into a player bank account
                    this.BankAccount = new BankAccount(bankAccountResult.Result);

                    this.BankAccount.SyncBalanceAsync().ContinueWith((task) => {
                        //Raise the OnAccountLoaded event to inform that this bank account is loaded.
                        OnAccountLoaded();

                        SEconomyPlugin.Profiler.ExitLog(this.TSPlayer.UserAccountName + " LoadBankAccountAsync");

                        TShockAPI.Log.ConsoleInfo(string.Format("seconomy: bank account for {0} loaded.", TSPlayer.UserAccountName));
                    });

                } else {
                    TShockAPI.Log.ConsoleError(string.Format("seconomy: bank account for {0} failed.", TSPlayer.UserAccountName));
                    this.TSPlayer.SendErrorMessage("It appears you don't have a bank account.");
                }
            });

        }

        Task LoadBankAccountAsync(bool CreateIfNone = false) {
            if (!SEconomyPlugin.Database.Ready) {
                return null;
            }

            SEconomyPlugin.Profiler.Enter(this.TSPlayer.UserAccountName + " LoadBankAccountAsync");

            return SEconomyPlugin.Database.AsyncConnection.Table<DatabaseObjects.BankAccount>().Where(i => i.UserAccountName == this.TSPlayer.UserAccountName).FirstOrDefaultAsync().ContinueWith((bankAccountResult) => {
                if (bankAccountResult.Result != null) {
                    //Parse the databse object into a player bank account
                    this.BankAccount = new BankAccount(bankAccountResult.Result);

                    this.BankAccount.SyncBalanceAsync().ContinueWith((task) => {
                        //Raise the OnAccountLoaded event to inform that this bank account is loaded.
                        OnAccountLoaded();

                        SEconomyPlugin.Profiler.ExitLog(this.TSPlayer.UserAccountName + " LoadBankAccountAsync");

                        TShockAPI.Log.ConsoleInfo(string.Format("seconomy: bank account for {0} loaded.", TSPlayer.UserAccountName));
                    });

                } else if ( CreateIfNone ) {
                    //create the bank account if there isn't already one for this player.
                    CreateBankAccountAsync();
                } else {
                    TShockAPI.Log.ConsoleError(string.Format("seconomy: bank account for {0} failed.", TSPlayer.UserAccountName));
                    this.TSPlayer.SendErrorMessage("It appears you don't have a bank account.");
                }
            });
        }

        /// <summary>
        /// Loads a bank account by username.  NOT TO BE USED FOR USERS THAT ARE NOT SUPERADMIN
        /// </summary>
        public Task LoadBankAccountByPlayerNameAsync() {
            if (!SEconomyPlugin.Database.Ready) {
                return new Task(() => { return; });
            }

            SEconomyPlugin.Profiler.Enter(this.TSPlayer.Name + " LoadBankAccountAsync");

            return SEconomyPlugin.Database.AsyncConnection.Table<DatabaseObjects.BankAccount>().Where(i => i.UserAccountName == this.TSPlayer.Name).FirstOrDefaultAsync().ContinueWith((bankAccountResult) => {
                if (bankAccountResult.Result != null) {
                    //Parse the databse object into a player bank account
                    this.BankAccount = new BankAccount(bankAccountResult.Result);

                    this.BankAccount.SyncBalanceAsync().ContinueWith((task) => {
                        //Raise the OnAccountLoaded event to inform that this bank account is loaded.

                        OnAccountLoaded();

                        SEconomyPlugin.Profiler.ExitLog(this.TSPlayer.Name + " LoadBankAccountAsync");

                        TShockAPI.Log.ConsoleInfo(string.Format("seconomy: bank account for {0} loaded.", TSPlayer.Name));
                    });

                } else {
                    TShockAPI.Log.ConsoleError(string.Format("seconomy: bank account for {0} failed.", TSPlayer.Name));
                    this.TSPlayer.SendErrorMessage("It appears you don't have a bank account.");
                }
            });
        }

        /// <summary>
        /// Asynchronously creates a bank account for this player.
        /// </summary>
        /// <param name="AccountName">Name of the account, for multiple account.  NOT IMPLEMENTED</param>
        public Task CreateBankAccountAsync(string AccountName = "") {

            SEconomyPlugin.Profiler.Enter(this.TSPlayer.UserAccountName + "account create");

            var qBankAccountsForPlayer = SEconomyPlugin.Database.AsyncConnection.Table<DatabaseObjects.BankAccount>().Where(i => i.UserAccountName == TSPlayer.UserAccountName);
            return qBankAccountsForPlayer.FirstOrDefaultAsync().ContinueWith((firstOrDefaultResult) => {

                //Only continue if a bank account for this user does not exist.
                if (!firstOrDefaultResult.IsFaulted && firstOrDefaultResult.Result == null) {

                    //new up an account
                    DatabaseObjects.BankAccount newAccount = new DatabaseObjects.BankAccount();
                    newAccount.UserAccountName = TSPlayer.UserAccountName;
                    newAccount.WorldID = Terraria.Main.worldID;
                    newAccount.Flags = DatabaseObjects.BankAccountFlags.Enabled;

                    try {
                        SEconomyPlugin.Database.AsyncConnection.InsertAsync(newAccount).ContinueWith((newPrimaryKey) => {
                            SEconomyPlugin.Profiler.ExitLog(this.TSPlayer.UserAccountName + "account create");

                            SEconomyPlugin.Profiler.Enter(this.TSPlayer.UserAccountName + "account load");

                            //Bank account insert successful, load into this object.
                            LoadBankAccountAsync(newPrimaryKey.Result);
                        });
                    } catch (Exception ex) {
                        TShockAPI.Log.ConsoleError(string.Format("Could not create bank account for account {0}: {1}", TSPlayer.UserAccountName, ex.ToString()));
                    }

                }

            });
        }

        /// <summary>
        /// Raises the OnAccountLoaded event.
        /// </summary>
        protected virtual void OnAccountLoaded() {
            EventHandler onLoadedHandler = PlayerBankAccountLoaded;
            if (onLoadedHandler != null) {
                onLoadedHandler(this, new EventArgs());
            }
        }
    }


}
