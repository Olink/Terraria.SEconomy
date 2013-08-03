using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using SQLite;

namespace Wolfje.Plugins.SEconomy.DatabaseObjects {

    [Flags]
    public enum BankAccountTransactionFlags {
        FundsAvailable = 1,
        Squashed = 1 << 1
    }
    
    public class BankAccountTransaction {

        [PrimaryKey, AutoIncrement]
        public int BankAccountTransactionK { get; set; }

        public int BankAccountFK { get; set; }

        public long Amount { get; set; }
        public string Message { get; set; }
        public BankAccountTransactionFlags Flags { get; set; }
        public int Flags2 { get; set; }
        
        [Indexed(Name="ix_transaction_date_desc", Order=-1)]
        public DateTime TransactionDateUtc { get; set; }

        public int BankAccountTransactionFK { get; set; }

    }
}
