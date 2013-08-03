using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;

namespace Wolfje.Plugins.SEconomy {
    /// <summary>
    /// A representation of Money in Seconomy.  Money objects are toll-free bridged with 64-bit integers (long).
    /// </summary>
    public struct Money {
        //ye olde godly value base type
        private long _moneyValue;

        private const int ONE_PLATINUM = 1000000;
        private const int ONE_GOLD = 10000;
        private const int ONE_SILVER = 100;
        private const int ONE_COPPER = 1;

        private static readonly Regex moneyRegex = new Regex(@"(-)?((\d*)p)?((\d*)g)?((\d*)s)?((\d*)c)?", RegexOptions.IgnoreCase);

        #region "Constructors"

        /// <summary>
        /// Initializes a new instance of Money and copies the amount specified in it.
        /// </summary>
        public Money(Money money) {
            _moneyValue = money._moneyValue;
        }

        /// <summary>
        /// Initalizes a new instance of money with the specified amount in integer form.
        /// </summary>
        public Money(long money) {
            _moneyValue = money;
        }

        /// <summary>
        /// Makes a new money object based on the supplied platinum, gold, silver, and copper.
        /// </summary>
        public Money(uint Platinum, uint Gold, int Silver, int Copper) {
            _moneyValue = 0;

            if (Gold > 99 || Silver > 99 || Copper > 99) {
                throw new ArgumentException("Supplied values for Gold, silver and copper cannot be over 99.");
            } else {
                _moneyValue += (long)Math.Pow(Platinum, 6);
                _moneyValue += (long)Math.Pow(Gold, 4);
                _moneyValue += (long)Math.Pow(Silver, 2);
                _moneyValue += (long)Copper;
            }
        }

        #endregion

        #region "Long toll-free bridging"

        /// <summary>
        /// Cast a Long to Money implicitly
        /// </summary>
        public static implicit operator Money(long money) {
            return new Money(money);
        }

        /// <summary>
        /// Cast Money to a Long implicitly
        /// </summary>
        public static implicit operator long(Money money) {
            return money._moneyValue;
        }

        #endregion
        
        /// <summary>
        /// Returns the Platinum portion of this money instance
        /// </summary>
        public long Platinum {
            get {
                return (long)Math.Floor((decimal)(_moneyValue / ONE_PLATINUM));
            }
        }

        /// <summary>
        /// Returns the Gold portion of this money instance
        /// </summary>
        public long Gold {
            get {
                return (long)((_moneyValue % ONE_PLATINUM) - (_moneyValue % ONE_GOLD)) / 10000;
            }
        }

        /// <summary>
        /// Returns the Silver portion of this money instance
        /// </summary>
        public long Silver {
            get {
                return (long)((_moneyValue % ONE_GOLD) - (_moneyValue % ONE_SILVER)) / 100;
            }
        }

        /// <summary>
        /// Returns the Copper portion of this money instance
        /// </summary>
        public long Copper {
            get {
                return (long)_moneyValue % 100;
            }
        }

        /// <summary>
        /// Returns the string representation of this money (in "pgsc" format)
        /// </summary>
        public override string ToString() {
            StringBuilder sb = new StringBuilder();
            Money moneyCopy = this;

            //Negative balances still need to display like they are positives
            if (moneyCopy < 0) {
                sb.Append("-");
                moneyCopy = moneyCopy * (-1);
            }

            if (moneyCopy.Platinum > 0) {
                sb.AppendFormat("{0}p", moneyCopy.Platinum);
            }
            if (moneyCopy.Gold > 0) {
                sb.AppendFormat("{0}g", moneyCopy.Gold);
            }
            if (moneyCopy.Silver > 0) {
                sb.AppendFormat("{0}s", moneyCopy.Silver);
            }

            sb.AppendFormat("{0}c", moneyCopy.Copper);
            
            return sb.ToString();
        }

        /// <summary>
        /// Returns a long representation of this Money object.
        /// </summary>
        public string ToLongString(bool ShowNegativeSign = false) {
            StringBuilder sb = new StringBuilder();
            Money moneyCopy = this;

            //Negative balances still need to display like they are positives
            if (moneyCopy < 0) {
                if (ShowNegativeSign) {
                    sb.Append("-");
                }

                moneyCopy = moneyCopy * (-1);
            }

            if (moneyCopy.Platinum > 0) {
                sb.AppendFormat("{0} plat", moneyCopy.Platinum);
            }
            if (moneyCopy.Gold > 0) {
                sb.AppendFormat("{1}{0} gold", moneyCopy.Gold, sb.Length > 0 ? " " : "");
            }
            if (moneyCopy.Silver > 0) {
                sb.AppendFormat("{1}{0} silver", moneyCopy.Silver, sb.Length > 0 ? " " : "");
            }

            if (moneyCopy.Copper > 0 || moneyCopy._moneyValue == 0) {
                sb.AppendFormat("{1}{0} copper", moneyCopy.Copper, sb.Length > 0 ? " " : "");
            }

            return sb.ToString();
        }

        /// <summary>
        /// Tries to parse Money out of a money representation.
        /// </summary>
        /// <param name="MoneyRepresentation">The money representation string, eg "1p1g", or "30s20c"</param>
        /// <param name="money">Reference to the money variable to parse to.</param>
        /// <returns>true if the parsing succeeded.</returns>
        public static bool TryParse(string MoneyRepresentation, out Money Money) {
            bool succeeded = false;

            try {
                Money = Parse(MoneyRepresentation);

                succeeded = true;
            } catch {
                //any exception marks a failed conversion, the reference must be set to 0
                succeeded = false;

                Money = 0;
            } 
            
            return succeeded;
        }

        /// <summary>
        /// Parses a money representation into a Money object.  Will throw exception if parsing fails.
        /// </summary>
        /// <param name="MoneyRepresentation">The money representation string, eg "1p1g", or "30s20c"</param>
        /// <returns>The money object parsed.  If not it'll return a big fat exception back in your face. :)</returns>
        public static Money Parse(string MoneyRepresentation) {
            long totalMoney = 0;

            if (!string.IsNullOrWhiteSpace(MoneyRepresentation) && new Regex("p|g|s|c").IsMatch(MoneyRepresentation)) {
                Match moneyMatch = moneyRegex.Match(MoneyRepresentation);
                long plat = 0, gold = 0, silver = 0, copper = 0;
                string signedness = "";

                if (!string.IsNullOrWhiteSpace(moneyMatch.Groups[1].Value))
                    signedness = moneyMatch.Groups[1].Value;

                if (!string.IsNullOrWhiteSpace(moneyMatch.Groups[2].Value))
                    plat = long.Parse(moneyMatch.Groups[3].Value);

                if (!string.IsNullOrWhiteSpace(moneyMatch.Groups[4].Value))
                    gold = long.Parse(moneyMatch.Groups[5].Value);

                if (!string.IsNullOrWhiteSpace(moneyMatch.Groups[6].Value))
                    silver = long.Parse(moneyMatch.Groups[7].Value);

                if (!string.IsNullOrWhiteSpace(moneyMatch.Groups[8].Value))
                    copper = long.Parse(moneyMatch.Groups[9].Value);

                totalMoney += plat * ONE_PLATINUM;
                totalMoney += gold * ONE_GOLD;
                totalMoney += silver * ONE_SILVER;
                totalMoney += copper;

                //you can specify a minus at the start to indicate a negative amount.
                if (!string.IsNullOrWhiteSpace(signedness)) {
                    totalMoney = -totalMoney;
                }
            } else {
                //Attempt a plain conversion from a whole integer
                long.TryParse(MoneyRepresentation, out totalMoney);
            }

            return totalMoney;
        }
    }
}
