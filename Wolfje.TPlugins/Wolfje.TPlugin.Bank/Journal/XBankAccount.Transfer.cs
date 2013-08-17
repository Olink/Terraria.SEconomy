using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Wolfje.Plugins.SEconomy.Journal {
    public partial class XBankAccount {

        /// <summary>
        /// Returns whether a transfer is allowed to succeed or not.
        /// </summary>
        public static bool TransferMaySucceed(XBankAccount FromAccount, XBankAccount ToAccount, Money MoneyNeeded, Journal.BankAccountTransferOptions Options) {
            return ((FromAccount.IsSystemAccount || FromAccount.IsPluginAccount || ((Options & Journal.BankAccountTransferOptions.AllowDeficitOnNormalAccount) == Journal.BankAccountTransferOptions.AllowDeficitOnNormalAccount)) || (FromAccount.Balance >= MoneyNeeded && MoneyNeeded > 0));
        }

        XTransaction BeginSourceTransaction(Money Amount, string Message) {
            XTransaction sourceTran = new XTransaction(Amount);

            sourceTran.BankAccountFK = this.BankAccountK;
            sourceTran.Flags = Journal.BankAccountTransactionFlags.FundsAvailable;
            sourceTran.TransactionDateUtc = DateTime.UtcNow;
            sourceTran.Amount = (Amount * (-1));

            if (!string.IsNullOrEmpty(Message)) {
                sourceTran.Message = Message;
            }

            lock (TransactionJournal.XmlJournal) {
                return Journal.TransactionJournal.AddTransaction(sourceTran);
            }
        }

        XTransaction FinishEndTransaction(string SourceBankTransactionKey, XBankAccount ToAccount, Money Amount, string Message) {
            XTransaction destTran = new XTransaction(Amount);

            destTran.BankAccountFK = ToAccount.BankAccountK;
            destTran.Flags = Journal.BankAccountTransactionFlags.FundsAvailable;
            destTran.TransactionDateUtc = DateTime.UtcNow;
            destTran.Amount = Amount;
            destTran.BankAccountTransactionFK = SourceBankTransactionKey;

            if (!string.IsNullOrEmpty(Message)) {
                destTran.Message = Message;
            }

            lock (TransactionJournal.XmlJournal) {
                return Journal.TransactionJournal.AddTransaction(destTran);
            }
        }

        void BindTransactions(ref XTransaction SourceTransaction, ref XTransaction DestTransaction) {

            lock (TransactionJournal.XmlJournal) {
                SourceTransaction.BankAccountTransactionFK = DestTransaction.BankAccountTransactionK;
                DestTransaction.BankAccountTransactionFK = SourceTransaction.BankAccountTransactionK;
            }

        }

        /// <summary>
        /// Asynchronously transfers to another account.
        /// </summary>
        public Task<BankTransferEventArgs> TransferToAsync(XBankAccount ToAccount, Money Amount, BankAccountTransferOptions Options, string Message = "") {
            Guid profile = SEconomyPlugin.Profiler.Enter(string.Format("transferAsync: {0} to {1}", this.UserAccountName, ToAccount.UserAccountName));
            return Task.Factory.StartNew<BankTransferEventArgs>(() => {
                BankTransferEventArgs args = TransferTo(ToAccount, Amount, Options, UseProfiler: false, Message: Message);
                return args;
            }).ContinueWith((task) => {
                SEconomyPlugin.Profiler.ExitLog(profile);
                return task.Result;
            });
        }

        /// <summary>
        /// Asynchronously transfers to another account.
        /// </summary>
        public Task<BankTransferEventArgs> TransferToAsync(int Index, Money Amount, BankAccountTransferOptions Options, string Message = "") {
            Economy.EconomyPlayer ePlayer = SEconomyPlugin.GetEconomyPlayerSafe(Index);

            Guid profile = SEconomyPlugin.Profiler.Enter(string.Format("transferAsync: {0} to {1}", this.UserAccountName, ePlayer.BankAccount != null ? ePlayer.BankAccount.UserAccountName : "Unknown"));
            return Task.Factory.StartNew<BankTransferEventArgs>(() => {
                BankTransferEventArgs args = TransferTo(ePlayer.BankAccount, Amount, Options, UseProfiler: false, Message: Message);
                return args;
            }).ContinueWith((task) => {
                SEconomyPlugin.Profiler.ExitLog(profile);
                return task.Result;
            });
        }

        /// <summary>
        /// Transfers money from this player to the destination account.  If negative, takes money from the destionation account into this account.
        /// </summary>
        public BankTransferEventArgs TransferTo(int Index, Money Amount, Journal.BankAccountTransferOptions Options, string Message = "") {
            Economy.EconomyPlayer ePlayer = SEconomyPlugin.GetEconomyPlayerSafe(Index);

            return TransferTo(ePlayer.BankAccount, Amount, Options, Message: Message);
        }

        public static readonly object __tranlock = new object();

        /// <summary>
        /// Transfers money from this account to the destination account, if negative, takes money from the destination account into this account.
        /// </summary>
        public BankTransferEventArgs TransferTo(XBankAccount ToAccount, Money Amount, BankAccountTransferOptions Options, bool UseProfiler = true, string Message = "") {
            
            
            lock (__tranlock) {
                BankTransferEventArgs args = new BankTransferEventArgs();
                Guid profile = Guid.Empty;

                if (UseProfiler) {
                    profile = SEconomyPlugin.Profiler.Enter(string.Format("transfer: {0} to {1}", this.UserAccountName, ToAccount.UserAccountName));
                }

                if (ToAccount != null && TransferMaySucceed(this, ToAccount, Amount, Options)) {
                    args.Amount = Amount;
                    args.SenderAccount = this;
                    args.ReceiverAccount = ToAccount;
                    args.TransferOptions = Options;
                    args.TransferSucceeded = false;

                    //insert the source negative transaction
                    XTransaction sourceTran = BeginSourceTransaction(Amount, Message);
                    if (sourceTran != null && !string.IsNullOrEmpty(sourceTran.BankAccountTransactionK)) {
                        //insert the destination inverse transaction
                        XTransaction destTran = FinishEndTransaction(sourceTran.BankAccountTransactionK, ToAccount, Amount, Message);

                        if (destTran != null && !string.IsNullOrEmpty(destTran.BankAccountTransactionK)) {
                            //perform the double-entry binding
                            BindTransactions(ref sourceTran, ref destTran);

                            args.TransactionID = sourceTran.BankAccountTransactionK;

                            //update balances
                            this.Balance += (Amount * (-1));
                            ToAccount.Balance += Amount;

                            //transaction complete
                            args.TransferSucceeded = true;
                        }
                    }
                } else {
                    args.TransferSucceeded = false;

                    if (!ToAccount.IsSystemAccount && !ToAccount.IsPluginAccount) {
                        if (Amount < 0) {
                            this.Owner.TSPlayer.SendErrorMessageFormat("Invalid amount.");
                        } else {
                            this.Owner.TSPlayer.SendErrorMessageFormat("You need {0} more money to make this payment.", ((Money)(this.Balance - Amount)).ToLongString());
                        }
                    }
                }

                //raise the transfer event
                OnBankTransferComplete(args);

                if (UseProfiler) {
                    SEconomyPlugin.Profiler.ExitLog(profile);
                }

                return args;
            }
        }
    }
}
