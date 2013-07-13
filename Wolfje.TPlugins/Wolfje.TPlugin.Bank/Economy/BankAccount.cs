using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Wolfje.Plugins.SEconomy.Economy {

    /// <summary>
    /// A list of options to consider when making a bank transfer.
    /// </summary>
    [Flags]
    public enum BankAccountTransferOptions {
        /// <summary>
        /// None, indicates a silent, normal payment.
        /// </summary>
        None = 0,
        /// <summary>
        /// Announces the payment to the reciever that they recieved, or gained money
        /// </summary>
        AnnounceToReceiver = 1,
        /// <summary>
        /// Announces the payment to the sender that they sent, or paid money
        /// </summary>
        AnnounceToSender = 1 << 1,
        /// <summary>
        /// Overrides the normal deficit logic, and will allow a normal player account to go into 
        /// </summary>
        AllowDeficitOnNormalAccount = 1 << 2,
        /// <summary>
        /// Indicates that the transfer happened because of PvP.
        /// </summary>
        MoneyFromPvP = 1 << 3,

        /// <summary>
        /// Indicates that the money was taken from the player because they died.
        /// </summary>
        MoneyTakenOnDeath = 1 << 4,

        /// <summary>
        /// Indicates that this transfer is a player-to-player transfer.
        /// 
        /// Note that PVP penalties ARE a player to player transfer but are forcefully taken; this is NOT set for these type of transfers, set MoneyFromPvP instead.
        /// </summary>
        IsPlayerToPlayerTransfer = 1 << 5,

        /// <summary>
        /// Indicates that this transaction was a payment for something tangible.
        /// </summary>
        IsPayment = 1 << 6,

        /// <summary>
        /// Suppresses the default announce messages.  Used for modules that have their own announcements for their own transfers.
        /// 
        /// Handle BankAccount.BankTransferSucceeded to hook your own.
        /// </summary>
        SuppressDefaultAnnounceMessages = 1 << 7
    }

    /// <summary>
    /// Represents a bank account in SEconomy
    /// </summary>
    public partial class BankAccount {

        Money _money;
        DatabaseObjects.BankAccountFlags _flags;
        DatabaseObjects.BankAccount DatabaseBankAccount { get; set; }
        int BankAccountK { get; set; }
        public string BankAccountName { get; private set; }
        public long WorldID { get; set; }
        DatabaseObjects.BankAccountFlags Flags {
            get {
                return _flags;
            }
        }

        /// <summary>
        /// Returns how poverty stricken you are. ;)
        /// </summary>
        public Money Money {
            get {
                return _money;
            }
        }

        /// <summary>
        /// This is shit as fuck and is likely to change.  Payer is a reftype and really needs to be passed into here
        /// </summary>
        public EconomyPlayer Owner {
            get {
                return SEconomyPlugin.GetEconomyPlayerByBankAccountNameSafe(this.BankAccountName);
            }
        }

        #region "Account Flags"


        /// <summary>
        /// Returns if this account is enabled
        /// </summary>
        public bool IsAccountEnabled {
            get {
                return (this.Flags & DatabaseObjects.BankAccountFlags.Enabled) == DatabaseObjects.BankAccountFlags.Enabled;
            }
        }

        /// <summary>
        /// Returns if this account is a system (world) account
        /// </summary>
        public bool IsSystemAccount {
            get {
                return (this.Flags & DatabaseObjects.BankAccountFlags.SystemAccount) == DatabaseObjects.BankAccountFlags.SystemAccount;
            }
        }

        /// <summary>
        /// Returns if this account is locked to the world it was created in
        /// </summary>
        public bool IsLockedToWorld {
            get {
                return (this.Flags & DatabaseObjects.BankAccountFlags.LockedToWorld) == DatabaseObjects.BankAccountFlags.LockedToWorld;
            }
        }

        /// <summary>
        /// Returns if this account is a plugin account
        /// </summary>
        public bool IsPluginAccount {
            get {
                return (this.Flags & DatabaseObjects.BankAccountFlags.PluginAccount) == DatabaseObjects.BankAccountFlags.PluginAccount;
            }
        }

        /// <summary>
        /// Enables or disables the account.
        /// </summary>
        public void SetAccountEnabled(int CallerID, bool Enabled) {
            DatabaseObjects.BankAccountFlags _newFlags = this.Flags;

            if (Enabled == false) {
                _newFlags &= (~DatabaseObjects.BankAccountFlags.Enabled);
            } else {
                _newFlags |= DatabaseObjects.BankAccountFlags.Enabled;
            }

            //asynchronously update the database flags.
            this.DatabaseBankAccount.UpdateFlagsAsync(_newFlags).ContinueWith((newFlagsResult) => {
                //Nullable result here, the update could actually fail.
                if (newFlagsResult.Result.HasValue) {
                    _flags = newFlagsResult.Result.Value;

                    BankAccountChangedEventArgs args = new BankAccountChangedEventArgs();
                    args.NewFlags = _flags;
                    //Inform the event handler that the flags on this bank account have changed.
                    OnBankAccountChanged(args);
                }
            });
        }

        #endregion

        #region "Constructors"

        /// <summary>
        /// Constructs a new BankAccount from the supplied database object.
        /// </summary>
        public BankAccount(DatabaseObjects.BankAccount Account) {
            this.DatabaseBankAccount = Account;
            _flags = Account.Flags;
            this.BankAccountK = Account.BankAccountK;
            this.BankAccountName = Account.UserAccountName;
            this.WorldID = Account.WorldID;

            //refresh the balance from the database, money will be set when the async I/O finishes
            this.DatabaseBankAccount.GetBalanceFromDatabaseAsync().ContinueWith((balanceResult) => {
                _money = balanceResult.Result;
            });
        }

        #endregion

        /// <summary>
        /// Asynchronously updates this bank account's balance.
        /// </summary>
        /// <returns></returns>
        public async Task SyncBalanceAsync() {
            Money oldMoney = _money;
            Money newMoney = await DatabaseBankAccount.GetBalanceFromDatabaseAsync();

            //todo: raise money changed event, now it's not useful so I just don't care at all for the moment

            this._money = newMoney;
        }

        /// <summary>
        /// Inserts the opposite double-entry transaction in the source account database.
        /// </summary>
        async Task<int> BeginSourceTransaction(Money Amount) {
            DatabaseObjects.BankAccountTransaction sourceTransaction = new DatabaseObjects.BankAccountTransaction();

            sourceTransaction.BankAccountFK = this.BankAccountK;
            sourceTransaction.Flags = DatabaseObjects.BankAccountTransactionFlags.FundsAvailable;
            sourceTransaction.TransactionDateUtc = DateTime.UtcNow;
            sourceTransaction.Amount = (Amount * (-1));

            return await SEconomyPlugin.Database.AsyncConnection.InsertAsync(sourceTransaction);
        }

        async Task<int> FinishEndTransaction(int SourceBankTransactionKey, BankAccount ToAccount, Money Amount) {
            DatabaseObjects.BankAccountTransaction destTransaction = new DatabaseObjects.BankAccountTransaction();

            if (SourceBankTransactionKey == 0) {
                //TODO: Update to Task.FromResult() when/if TShock gets updated to netfx 4.5
                return 0;
            }

            destTransaction.BankAccountFK = ToAccount.BankAccountK;
            destTransaction.Flags = DatabaseObjects.BankAccountTransactionFlags.FundsAvailable;
            destTransaction.TransactionDateUtc = DateTime.UtcNow;
            destTransaction.Amount = Amount;
            destTransaction.BankAccountTransactionFK = SourceBankTransactionKey;

            return await SEconomyPlugin.Database.AsyncConnection.InsertAsync(destTransaction);
        }

        async Task<DatabaseObjects.BankAccountTransaction> GetTransaction(int BankAccountTransactionK) {
            return await SEconomyPlugin.Database.AsyncConnection.Table<DatabaseObjects.BankAccountTransaction>().Where(i => i.BankAccountTransactionK == BankAccountTransactionK).FirstOrDefaultAsync();
        }

        /// <summary>
        /// Binds a double-entry transaction together.
        /// </summary>
        async Task<int> BindTransactionToTransactionAsync(int TransactionK, int TransactionFK) {
            return await SEconomyPlugin.Database.AsyncConnection.ExecuteAsync("update bankaccounttransaction set bankaccounttransactionfk = @0 where bankaccounttransactionk = @1", TransactionK, TransactionFK);
        }

        /// <summary>
        /// Transfers from this account into a destination player's account, by their player slot.
        /// </summary>
        /// <param name="CallerID">The index of the caller.</param>
        public async Task<BankTransferEventArgs> TransferToPlayerAsync(int PlayerIndex, Money Amount, BankAccountTransferOptions Options) {
            Economy.EconomyPlayer ePlayer = SEconomyPlugin.GetEconomyPlayerSafe(PlayerIndex);

            return await TransferAsync(ePlayer.BankAccount, Amount, Options);
        }

        public static bool TransferMaySucceed(BankAccount FromAccount, BankAccount ToAccount, Money MoneyNeeded, BankAccountTransferOptions Options) {
            //return (((!FromAccount.IsSystemAccount || !FromAccount.IsPluginAccount || (Options & BankAccountTransferOptions.AllowDeficitOnNormalAccount) == BankAccountTransferOptions.AllowDeficitOnNormalAccount) || FromAccount.Money >= MoneyNeeded));
            return ((FromAccount.IsSystemAccount || FromAccount.IsPluginAccount || ((Options & BankAccountTransferOptions.AllowDeficitOnNormalAccount) == BankAccountTransferOptions.AllowDeficitOnNormalAccount)) || FromAccount.Money >= MoneyNeeded);
        }

        /// <summary>
        /// Performs an asynchronous transfer but does not await a result.  This method returns instantly so that other code may execute
        /// 
        /// Hook on BankTransferCompleted event to be informed when the transfer completes.
        /// </summary>
        public void TransferAndReturn(BankAccount ToAccount, Money Amount, BankAccountTransferOptions Options) {
            Task<BankTransferEventArgs> shuttingTheAwaitWarningUp = TransferAsync(ToAccount, Amount, Options);
        }

        /// <summary>
        /// Asynchronously Transfers money to a destination account. Money can be negative to take money from someone else's account.
        /// 
        /// Await this to return with the bank account transfer details.
        /// </summary>
        public async Task<BankTransferEventArgs> TransferAsync(BankAccount ToAccount, Money Amount, BankAccountTransferOptions Options) {
            BankTransferEventArgs args = new BankTransferEventArgs();
            Economy.EconomyPlayer ePlayer = this.Owner;
            Economy.EconomyPlayer toPlayer;

            SEconomyPlugin.Profiler.Enter(string.Format("transfer: {0} to {1}", this.BankAccountName, ToAccount.BankAccountName));

            if (ToAccount != null && TransferMaySucceed(this, ToAccount, Amount, Options)) {
                toPlayer = ToAccount.Owner;
                args.Amount = Amount;
                args.SenderAccount = this;
                args.ReceiverAccount = ToAccount;
                args.TransferOptions = Options;
                args.TransferSucceeded = false;

                //asynchronously await the source insert
                int sourceTransactionID = await this.BeginSourceTransaction(Amount);
                int endTransactionID = 0;
                if (sourceTransactionID > 0) {
                    //asynchronously await end
                    endTransactionID = await this.FinishEndTransaction(sourceTransactionID, ToAccount, Amount);
                }

                if (sourceTransactionID > 0 && endTransactionID > 0) {
                    //perform the double-entry binding.
                    await BindTransactionToTransactionAsync(sourceTransactionID, endTransactionID);
                    //andf sync both the accounts
                    await this.SyncBalanceAsync();
                    await ToAccount.SyncBalanceAsync();

                    args.TransferSucceeded = true;
                    args.TransactionID = sourceTransactionID;
                }

            } else {
                args.TransferSucceeded = false;
                this.Owner.TSPlayer.SendErrorMessageFormat("You need {0} more money to make this payment.", ((Money)(this.Money - Amount)).ToLongString());
            }

            //raise the transfer event
            OnBankTransferComplete(args);

            SEconomyPlugin.Profiler.ExitLog(string.Format("transfer: {0} to {1}", this.BankAccountName, ToAccount.BankAccountName));

            return args;
        }

        /*  This is why I moved to async ctp...
         * 
         * Async patterns are just too fucking hard to maintain without async.await sugar
         * 
        /// <summary>
        /// Initiates a transfer to the specified account.  World and Plugin accounts can pay anyone without restriction.
        /// </summary>
        public async Task TransferToAccountAsync(BankAccount ToAccount, Money Amount, BankAccountTransferOptions Options) {

                Economy.EconomyPlayer ePlayer = this.Owner;
                Economy.EconomyPlayer toPlayer;
                int sourceTransactionK = 0;

                if (ToAccount == null) {
                    if (ePlayer == null) {
                        ePlayer.TSPlayer.SendErrorMessage("Player has no bank account.");
                    }

                    return new Task(() => { return; });
                }

                toPlayer = SEconomyPlugin.GetEconomyPlayerByBankAccountNameSafe(ToAccount.BankAccountName);

                

                //System or plugin accounts can go into deficit.
                //If the source account is not a plugin or system account, and the person has the balance
                //Or, if AllowDeficitOnNormalAccount is set to true
                if (((!this.IsSystemAccount || !this.IsPluginAccount || (Options & BankAccountTransferOptions.AllowDeficitOnNormalAccount) == BankAccountTransferOptions.AllowDeficitOnNormalAccount) || this.Money >= Amount)) {

                    //Insert the first side of the double entry account, minusing the money out of the source account
                    return BeginSourceTransaction(Amount).ContinueWith((transactionResult) => {
                        sourceTransactionK = transactionResult.Result;

                        if (sourceTransactionK > 0) {
                            FinishEndTransaction(sourceTransactionK, ToAccount, Amount).ContinueWith((endTransactionResult) => {
                                if (endTransactionResult.Result == 0) {
                                    ePlayer.TSPlayer.SendErrorMessageFormat("bank transfer: transfer failed.");
                                    return;
                                }

                                //perform the double-entry binding.
                                BindTransactionToTransactionAsync(endTransactionResult.Result, sourceTransactionK).ContinueWith((bindResult) => {

                                    //Process has completed, sync both the from and to accounts.

                                    this.SyncBalanceAsync().ContinueWith(_ => {
                                        toPlayer.BankAccount.SyncBalanceAsync().ContinueWith((task) => {
                                            //I FUCKING LOVE CLOSURES WHOOPITY WHOOP WHOOP WHOOP
                                            BankTransferEventArgs txArgs = new BankTransferEventArgs() { Amount = Amount, ReceiverAccount = ToAccount, SenderAccount = this, TransactionID = endTransactionResult.Result, TransferOptions = Options };

                                            //Raise the transfer completed event.
                                            OnBankTransferComplete(txArgs);

                                            
                                        });
                                    });
                                });
                            });
                        }
                    });

                } else {
                    return new Task(() => {
                        ePlayer.TSPlayer.SendErrorMessageFormat("bank transfer: you need {0} more to be able to pay {1}.", ((Money)(Amount - this.Money)).ToLongString(), toPlayer.TSPlayer.Name);
                    });
                }
        }*/

    }
}
