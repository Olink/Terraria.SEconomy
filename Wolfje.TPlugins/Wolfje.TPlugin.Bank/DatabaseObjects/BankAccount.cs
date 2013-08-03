using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using SQLite;

namespace Wolfje.Plugins.SEconomy.DatabaseObjects {

    /// <summary>
    /// Bank Account flags, sets things like enabled, system bank account, etc
    /// </summary>
    [Flags]
    public enum BankAccountFlags {
        Enabled = 1,
        SystemAccount = 1 << 1,
        LockedToWorld = 1 << 2,
        PluginAccount = 1 << 3,
    }

   public class BankAccount {

        [PrimaryKey, AutoIncrement]
        public int BankAccountK { get; set; }
        public string UserAccountName { get; set; }
        public long WorldID { get; set; }
        public Journal.BankAccountFlags Flags { get; set; }
        public string Description { get; set; }

       /// <summary>
       /// Updates the bank account flags from a database.  Called when enabled changes, etc.
       /// </summary>
        internal async Task<Journal.BankAccountFlags?> UpdateFlagsAsync(Journal.BankAccountFlags NewFlags) {
            int numberOfRecordsUpdated = await SEconomyPlugin.Database.AsyncConnection.ExecuteAsync("update bankaccount set flags = @0 where bankaccountk = @1", NewFlags, this.BankAccountK);
            // number of rows affected
            if (numberOfRecordsUpdated == 1) {
                this.Flags = NewFlags;

                return NewFlags;
            }

            //In a lambda there is no way for it to infer what type (null) is to anything other than object. 
            //We just help it along a bit by telling it we're talking about a nullable
            return (BankAccountFlags?)null;
        }

       /// <summary>
       /// Updates the bank balance in money from the database, performing an asynchronous sync
       /// </summary>
        internal async Task<long> GetBalanceFromDatabaseAsync() {
            
            //Directly query a bank balance.
            return await SEconomyPlugin.Database.AsyncConnection.ExecuteScalarAsync<long>("select coalesce(sum(Amount), 0) AS Balance from BankAccountTransaction where BankAccountFK = @0 and (Flags & 1) = 1", this.BankAccountK);
        }
    }
}
