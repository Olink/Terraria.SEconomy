using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Wolfje.Plugins.SEconomy {
    internal class ChatCommands {

        /// <summary>
        /// Hooks to chat commands.
        /// </summary>
        public static void Initialize() {
            TShockAPI.Commands.ChatCommands.Add(new TShockAPI.Command(Chat_BankCommand, "bank"));
        }

        static void Chat_BankCommand(TShockAPI.CommandArgs args) 
        {
            //The initator of the command with bank account...
            Economy.EconomyPlayer selectedPlayer = SEconomyPlugin.GetEconomyPlayerSafe(args.Player.Index);
            Economy.EconomyPlayer caller = SEconomyPlugin.GetEconomyPlayerSafe(args.Player.Index);

            string namePrefix = "Your";

            //Bank balance
            if (args.Parameters[0].Equals("bal", StringComparison.CurrentCultureIgnoreCase)
                || args.Parameters[0].Equals("balance", StringComparison.CurrentCultureIgnoreCase)) {


                //The command supports viewing other people's balance if the caller has permission
                if (args.Player.Group.HasPermission("bank.viewothers")) {
                    if (args.Parameters.Count >= 2) {
                        selectedPlayer = SEconomyPlugin.GetEconomyPlayerSafe(args.Parameters[1]);
                    }

                    if (selectedPlayer != null) {
                        namePrefix = selectedPlayer.TSPlayer.Name + "'s";
                    }
                }

                if (selectedPlayer != null && selectedPlayer.BankAccount != null) {

                    if (!selectedPlayer.BankAccount.IsAccountEnabled && !args.Player.Group.HasPermission("bank.viewothers")) {
                        args.Player.SendErrorMessage("bank balance: your account is disabled");
                    } else {
                        args.Player.SendInfoMessageFormat("{1} balance: {0} {2}", selectedPlayer.BankAccount.Money.ToLongString(true), namePrefix, selectedPlayer.BankAccount.IsAccountEnabled ? "" : "(disabled)");
                    }

                } else {
                    args.Player.SendInfoMessage("bank balance: Cannot find player or no bank account.");
                }
            }

            //Account enable

            if (args.Parameters[0].Equals("ena", StringComparison.CurrentCultureIgnoreCase)
                || args.Parameters[0].Equals("enable", StringComparison.CurrentCultureIgnoreCase)
                || args.Parameters[0].Equals("dis", StringComparison.CurrentCultureIgnoreCase)
                || args.Parameters[0].Equals("disable", StringComparison.CurrentCultureIgnoreCase)) {

                    //Flag to set the account to
                    bool enableAccount = args.Parameters[0].Equals("ena", StringComparison.CurrentCultureIgnoreCase) || args.Parameters[0].Equals("enable", StringComparison.CurrentCultureIgnoreCase);

                    if (args.Player.Group.HasPermission("bank.modifyothers")) {
                        if (args.Parameters.Count >= 2) {
                            selectedPlayer = SEconomyPlugin.GetEconomyPlayerSafe(args.Parameters[1]);
                        }

                        if (selectedPlayer != null) {
                            namePrefix = selectedPlayer.TSPlayer.Name + "'s";
                        }
                    }

                    if (selectedPlayer != null && selectedPlayer.BankAccount != null) {
                        selectedPlayer.BankAccount.SetAccountEnabled(args.Player.Index, enableAccount);
                    }
            }

            //Player-to-player transfer
            if ( args.Parameters[0].Equals("pay", StringComparison.CurrentCultureIgnoreCase)
                || args.Parameters[0].Equals("transfer", StringComparison.CurrentCultureIgnoreCase)
                || args.Parameters[0].Equals("tfr", StringComparison.CurrentCultureIgnoreCase)) {

                    if (selectedPlayer.TSPlayer.Group.HasPermission("bank.transfer")) {
                        // /bank pay wolfje 1p
                        if (args.Parameters.Count >= 3) {
                            selectedPlayer = SEconomyPlugin.GetEconomyPlayerSafe(args.Parameters[1]);
                            Money amount = 0;

                            if (selectedPlayer == null) {
                                args.Player.SendErrorMessageFormat("Cannot find player by the name of {0}.", args.Parameters[1]);
                            } else {
                                if (Money.TryParse(args.Parameters[2], out amount)) {

                                    //Instruct the world bank to give the player money.
                                    caller.BankAccount.TransferAndReturn(selectedPlayer.BankAccount, amount, Economy.BankAccountTransferOptions.AnnounceToReceiver | Economy.BankAccountTransferOptions.AnnounceToSender | Economy.BankAccountTransferOptions.IsPlayerToPlayerTransfer);
                                } else {
                                    args.Player.SendErrorMessageFormat("bank give: \"{0}\" isn't a valid amount of money.", args.Parameters[2]);
                                }
                            }
                        } else {
                            args.Player.SendErrorMessage("Usage: /bank pay <Player> <Amount");
                        }
                    } else {
                        args.Player.SendErrorMessageFormat("bank pay: You don't have permission to do that.");
                    }

            }

            //World-to-player transfer
            if (args.Parameters[0].Equals("give", StringComparison.CurrentCultureIgnoreCase)
               || args.Parameters[0].Equals("take", StringComparison.CurrentCultureIgnoreCase)) {

                   if (selectedPlayer.TSPlayer.Group.HasPermission("bank.worldtransfer")) {
                       // /bank give wolfje 1p
                       if (args.Parameters.Count >= 3) {
                           selectedPlayer = SEconomyPlugin.GetEconomyPlayerSafe(args.Parameters[1]);
                           Money amount = 0;

                           if (selectedPlayer == null) {
                               args.Player.SendErrorMessageFormat("Cannot find player by the name of {0}.", args.Parameters[1]);
                           } else {
                               if (Money.TryParse(args.Parameters[2], out amount)) {

                                   //eliminate a double-negative.  saying "take Player -1p1c" will give them 1 plat 1 copper!
                                   if (args.Parameters[0].Equals("take", StringComparison.CurrentCultureIgnoreCase) && amount > 0) {
                                       amount = -amount;
                                   }

                                   //Instruct the world bank to give the player money.
                                   SEconomyPlugin.WorldAccount.TransferAndReturn(selectedPlayer.BankAccount, amount, Economy.BankAccountTransferOptions.AnnounceToReceiver);
                               } else {
                                   args.Player.SendErrorMessageFormat("bank give: \"{0}\" isn't a valid amount of money.", args.Parameters[2]);
                               }
                           }
                       } else {
                           args.Player.SendErrorMessage("Usage: /bank give|take <Player> <Amount");
                       }
                   } else {
                       args.Player.SendErrorMessageFormat("bank give: You don't have permission to do that.");
                   }
            }


        }
        
    }
}
