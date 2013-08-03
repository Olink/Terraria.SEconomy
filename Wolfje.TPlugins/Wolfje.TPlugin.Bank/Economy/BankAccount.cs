using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Wolfje.Plugins.SEconomy.Economy {

    

    /// <summary>
    /// Represents a bank account in SEconomy
    /// </summary>
    public partial class BankAccount {

        Money _money;
        Journal.BankAccountFlags _flags;
        
        Journal.XBankAccount XmlBankAccount { get; set; }

        int BankAccountK { get; set; }
        public string BankAccountName { get; private set; }
        public long WorldID { get; set; }

        Journal.BankAccountFlags Flags {
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


        

        #region "Constructors"

        /// <summary>
        /// Constructs a new BankAccount from the supplied database object.
        /// </summary>
        public BankAccount(Journal.XBankAccount Account) {
           // this.DatabaseBankAccount = Account;
            _flags = Account.Flags;
            this.BankAccountK = Account.BankAccountK;
            this.BankAccountName = Account.UserAccountName;
            this.WorldID = Account.WorldID;

            //refresh the balance from the database, money will be set when the async I/O finishes
            //this.DatabaseBankAccount.GetBalanceFromDatabaseAsync().ContinueWith((balanceResult) => {
            //    _money = balanceResult.Result;
            //});
        }

        #endregion
        


        /// <summary>
        /// Asynchronously updates this bank account's balance.
        /// </summary>
        /// <returns></returns>
        //public async Task SyncBalanceAsync() {
        //    Money oldMoney = _money;
        //    Money newMoney = 0; // await DatabaseBankAccount.GetBalanceFromDatabaseAsync();

        //    //todo: raise money changed event, now it's not useful so I just don't care at all for the moment

        //    this._money = newMoney;
        //}

        /// <summary>
        /// Inserts the opposite double-entry transaction in the source account database.
        /// </summary>
        //async Task<int> BeginSourceTransaction(Money Amount) {
        //    DatabaseObjects.BankAccountTransaction sourceTransaction = new DatabaseObjects.BankAccountTransaction();

        //    sourceTransaction.BankAccountFK = this.BankAccountK;
        //    sourceTransaction.Flags = DatabaseObjects.BankAccountTransactionFlags.FundsAvailable;
        //    sourceTransaction.TransactionDateUtc = DateTime.UtcNow;
        //    sourceTransaction.Amount = (Amount * (-1));

        //    return await SEconomyPlugin.Database.AsyncConnection.InsertAsync(sourceTransaction);
        //}

        //async Task<int> FinishEndTransaction(int SourceBankTransactionKey, BankAccount ToAccount, Money Amount) {
        //    DatabaseObjects.BankAccountTransaction destTransaction = new DatabaseObjects.BankAccountTransaction();

        //    if (SourceBankTransactionKey == 0) {
        //        TODO: Update to Task.FromResult() when/if TShock gets updated to netfx 4.5
        //        return 0;
        //    }

        //    destTransaction.BankAccountFK = ToAccount.BankAccountK;
        //    destTransaction.Flags = DatabaseObjects.BankAccountTransactionFlags.FundsAvailable;
        //    destTransaction.TransactionDateUtc = DateTime.UtcNow;
        //    destTransaction.Amount = Amount;
        //    destTransaction.BankAccountTransactionFK = SourceBankTransactionKey;

        //    return await SEconomyPlugin.Database.AsyncConnection.InsertAsync(destTransaction);
        //}

        //async Task<DatabaseObjects.BankAccountTransaction> GetTransaction(int BankAccountTransactionK) {
        //    return await SEconomyPlugin.Database.AsyncConnection.Table<DatabaseObjects.BankAccountTransaction>().Where(i => i.BankAccountTransactionK == BankAccountTransactionK).FirstOrDefaultAsync();
        //}

        /// <summary>
        /// Binds a double-entry transaction together.
        /// </summary>
        //async Task<int> BindTransactionToTransactionAsync(int TransactionK, int TransactionFK) {
        //    return await SEconomyPlugin.Database.AsyncConnection.ExecuteAsync("update bankaccounttransaction set bankaccounttransactionfk = @0 where bankaccounttransactionk = @1", TransactionK, TransactionFK);
        //}

        /// <summary>
        /// Transfers from this account into a destination player's account, by their player slot.
        /// </summary>
        /// <param name="CallerID">The index of the caller.</param>
        //public async Task<BankTransferEventArgs> TransferToPlayerAsync(int PlayerIndex, Money Amount, Journal.BankAccountTransferOptions Options) {
        //    Economy.EconomyPlayer ePlayer = SEconomyPlugin.GetEconomyPlayerSafe(PlayerIndex);

        //    return await TransferAsync(ePlayer.BankAccount, Amount, Options);
        //}

      

        /// <summary>
        /// Performs an asynchronous transfer but does not await a result.  This method returns instantly so that other code may execute
        /// 
        /// Hook on BankTransferCompleted event to be informed when the transfer completes.
        /// </summary>
        //public void TransferAndReturn(BankAccount ToAccount, Money Amount, Journal.BankAccountTransferOptions Options) {
        //    Task<BankTransferEventArgs> shuttingTheAwaitWarningUp = TransferAsync(ToAccount, Amount, Options);
        //}

        /// <summary>
        /// Asynchronously Transfers money to a destination account. Money can be negative to take money from someone else's account.
        /// 
        /// Await this to return with the bank account transfer details.
        /// </summary>
        //public async Task<BankTransferEventArgs> TransferAsync(BankAccount ToAccount, Money Amount, Journal.BankAccountTransferOptions Options) {
        //    BankTransferEventArgs args = new BankTransferEventArgs();
        //    Economy.EconomyPlayer ePlayer = this.Owner;
        //    Economy.EconomyPlayer toPlayer;

        //    SEconomyPlugin.Profiler.Enter(string.Format("transfer: {0} to {1}", this.BankAccountName, ToAccount.BankAccountName));

        //    if (ToAccount != null && TransferMaySucceed(this, ToAccount, Amount, Options)) {
        //        toPlayer = ToAccount.Owner;
        //        args.Amount = Amount;
        //        args.SenderAccount = this;
        //        args.ReceiverAccount = ToAccount;
        //        args.TransferOptions = Options;
        //        args.TransferSucceeded = false;

        //        //asynchronously await the source insert
        //        int sourceTransactionID = await this.BeginSourceTransaction(Amount);
        //        int endTransactionID = 0;
        //        if (sourceTransactionID > 0) {
        //            //asynchronously await end
        //            endTransactionID = await this.FinishEndTransaction(sourceTransactionID, ToAccount, Amount);

        //            args.TransactionID = sourceTransactionID;
        //        }

        //        if (sourceTransactionID > 0 && endTransactionID > 0) {
        //            //perform the double-entry binding.
        //            await BindTransactionToTransactionAsync(sourceTransactionID, endTransactionID);
        //            //andf sync both the accounts
        //            await this.SyncBalanceAsync();
        //            await ToAccount.SyncBalanceAsync();

        //            args.TransferSucceeded = true;
        //        }

        //    } else {
        //        args.TransferSucceeded = false;
        //        if (Amount < 0) {
        //            this.Owner.TSPlayer.SendErrorMessageFormat("Invalid amount.");
        //        } else {
        //            this.Owner.TSPlayer.SendErrorMessageFormat("You need {0} more money to make this payment.", ((Money)(this.Money - Amount)).ToLongString());
        //        }
        //    }

        //    //raise the transfer event
        //    OnBankTransferComplete(args);

        //    SEconomyPlugin.Profiler.ExitLog(string.Format("transfer: {0} to {1}", this.BankAccountName, ToAccount.BankAccountName));

        //    return args;
        //}

    }
}
