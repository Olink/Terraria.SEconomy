using System;
using System.Xml.Linq;

namespace Wolfje.Plugins.SEconomy.Journal {

    [Flags]
    public enum BankAccountTransactionFlags {
        FundsAvailable = 1,
        Squashed = 1 << 1
    }

    /// <summary>
    /// SEconomy Transaction in XML
    /// </summary>
    public class XTransaction {
        XElement _element;

        #region "Constructors & toll-free bridging"

        /// <summary>
        /// Creates a new transaction element for the specified amount.
        /// </summary>
        public XTransaction(long Amount) {
            this._element = new XElement("Transaction");

            this.Amount = Amount;
        }

        XTransaction(XElement element) {
            this._element = element;
        }

        /// <summary>
        /// Returns the element equivalent of this XBankAccount instance
        /// </summary>
        public static implicit operator XElement(XTransaction instance) {
            return instance._element;
        }

        /// <summary>
        /// Returns the XBankAccount equivalent of this XElement instance
        /// </summary>
        public static implicit operator XTransaction(XElement element) {
            if (element.Name == "Transaction") {
                return new XTransaction(element);
            } else {
                throw new TypeLoadException("Provided XElement is not a Transaction");
            }
        }

        #endregion

        /// <summary>
        /// The unique-identifier of this transaction.
        /// </summary>
        public string BankAccountTransactionK { 
            get {
                return _element.Attribute("BankAccountTransactionK").Value;
            }
            set {
                _element.SetAttributeValue("BankAccountTransactionK", value);
            }
        }

        /// <summary>
        /// The unique-identifier of the bank account this this transaction belongs to.
        /// </summary>
        public string BankAccountFK {
            get {
                return _element.Attribute("BankAccountFK").Value;
            }
            set {
                _element.SetAttributeValue("BankAccountFK", value);
            }
        }

        /// <summary>
        /// The amount this transaction was for.
        /// </summary>
        public long Amount {
            get {
                return long.Parse(_element.Attribute("Amount").Value);
            }
            set {
                _element.SetAttributeValue("Amount", value.ToString());
            }
        }

        /// <summary>
        /// Transaction description
        /// </summary>
        public string Message {
            get {
                if (_element.Attribute("Message") != null) {
                    return _element.Attribute("Message").Value;
                } else {
                    return "";
                }
            }
            set {
                _element.SetAttributeValue("Message", value);
            }
        }
        
        /// <summary>
        /// Transaction Flags
        /// </summary>
        public BankAccountTransactionFlags Flags {
            get {
                BankAccountTransactionFlags flags;
                Enum.TryParse<BankAccountTransactionFlags>(_element.Attribute("Flags").Value, out flags);
                return flags;
            }
            set {
                _element.SetAttributeValue("Flags", value.ToString());
            }
        }

        /// <summary>
        /// Transaction flags
        /// </summary>
        public BankAccountTransactionFlags Flags2 {
            get {
                BankAccountTransactionFlags flags = 0;
                if (_element.Attribute("Flags2") != null) {
                    Enum.TryParse<BankAccountTransactionFlags>(_element.Attribute("Flags2").Value, out flags);
                }

                return flags;
            }
            set {
                _element.SetAttributeValue("Flags2", value.ToString());
            }
        }

        /// <summary>
        /// The UTC Date and time in which this transaction occured
        /// </summary>
        public DateTime TransactionDateUtc {
            get {
                DateTime tranDate;
                DateTime.TryParse(_element.Attribute("TransactionDateUtc").Value, out tranDate);
                return tranDate;
            }
            set {
                _element.SetAttributeValue("TransactionDateUtc", value.ToString());
            }
        }

        /// <summary>
        /// The self-reference link between double-entry transactions
        /// </summary>
        public string BankAccountTransactionFK {
            get {
                return _element.Attribute("BankAccountTransactionFK").Value;
            }
            set {
                _element.SetAttributeValue("BankAccountTransactionFK", value);
            }
        }
    }
}

