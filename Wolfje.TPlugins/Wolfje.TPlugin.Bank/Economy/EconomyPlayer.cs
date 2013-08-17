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
                if (Index < 0) {
                    return TShockAPI.TSServerPlayer.Server;
                } else {
                    return TShockAPI.TShock.Players[Index];
                }
            }
        }

        public EconomyPlayer(int index) {
            this.Index = index;
        }

        public Journal.XBankAccount BankAccount { get; internal set; }
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
        public void EnsureBankAccountExists() {
            Guid p = SEconomyPlugin.Profiler.Enter("BankAccount Load: " + this.TSPlayer.Name);
            Journal.XBankAccount account = Journal.TransactionJournal.GetBankAccountByName(this.TSPlayer.UserAccountName);
            if (account == null) {
                account = CreateAccount(); 
            }

            this.BankAccount = account;

            BankAccount.SyncBalanceAsync().ContinueWith((task) => {
                OnAccountLoaded();
                SEconomyPlugin.Profiler.ExitLog(p);
            });

        }

        Journal.XBankAccount CreateAccount() {
            Journal.XBankAccount newAccount = new Journal.XBankAccount(this.TSPlayer.UserAccountName, Terraria.Main.worldID, Journal.BankAccountFlags.Enabled, "");
            TShockAPI.Log.ConsoleInfo(string.Format("seconomy: bank account for {0} created.", TSPlayer.UserAccountName));

            return Journal.TransactionJournal.AddBankAccount(newAccount);
        }

        void LoadBankAccount(string BankAccountK) {
            Guid profile = SEconomyPlugin.Profiler.Enter(this.TSPlayer.UserAccountName + " LoadBankAccount");
            Journal.XBankAccount account = Journal.TransactionJournal.GetBankAccount(BankAccountK);

            if (account != null) {
                this.BankAccount = account;

                BankAccount.SyncBalanceAsync().ContinueWith((task) => {
                    OnAccountLoaded();
                    TShockAPI.Log.ConsoleInfo(string.Format("seconomy: bank account for {0} loaded.", TSPlayer.UserAccountName));
                    SEconomyPlugin.Profiler.ExitLog(profile);
                });
            } else {
                TShockAPI.Log.ConsoleError(string.Format("seconomy: bank account for {0} failed.", TSPlayer.UserAccountName));
                this.TSPlayer.SendErrorMessage("It appears you don't have a bank account.");
            }

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
