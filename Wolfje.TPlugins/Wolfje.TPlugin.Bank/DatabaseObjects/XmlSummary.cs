using System;
using System.Xml;
using System.Xml.Serialization;
using System.Xml.XPath;
using System.Collections.Generic;

namespace Wolfje.Plugins.SEconomy.DatabaseObjects {
    /// <summary>
    /// Xml summary.
    /// </summary>
    [Serializable]
    public class XmlSummary {

        List<BankAccountTransaction> Transactions { get; set; }
        List<BankAccount> Accounts { get; set; }

        public XmlSummary() {
            this.Transactions = new List<BankAccountTransaction>();
            this.Accounts = new List<BankAccount>();
        }

        public void SaveXml(string Path) {

        }

        public static XmlSummary LoadFromFile(string Path) {
  
            try {
                string xmlString = System.IO.File.ReadAllText(Path);

                XmlSerializer serializer = new XmlSerializer(typeof(XmlSummary));


            } catch (Exception ex) {
                if (ex is System.IO.FileNotFoundException || ex is System.IO.DirectoryNotFoundException) {
                    TShockAPI.Log.ConsoleError("seconomy xml: Cannot find file or directory. Creating new one.");

                    XmlSummary newSummary = new XmlSummary();
                    newSummary.SaveXml(Path);

                } else if (ex is System.Security.SecurityException) {
                    TShockAPI.Log.ConsoleError("seconomy xml: Access denied reading file " + Path);
                } else {
                    TShockAPI.Log.ConsoleError("seconomy xml: error " + ex.ToString());
                }
            }

        }
    }
}

