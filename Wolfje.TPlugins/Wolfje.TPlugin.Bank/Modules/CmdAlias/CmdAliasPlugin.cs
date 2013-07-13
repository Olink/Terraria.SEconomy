using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Reflection;
using System.Text.RegularExpressions;

using Wolfje.Plugins.SEconomy;
using Wolfje.Plugins.SEconomy.ModuleFramework;
using System.Threading.Tasks;



namespace Wolfje.Plugins.SEconomy.Modules.CmdAlias {

    /// <summary>
    /// Provides command aliases that can cost money to execute in SEconomy.
    /// </summary>
    /// 
    [SEconomyModule]
    public class CmdAliasPlugin : ModuleBase {

        static Configuration Configuration { get; set; }

        #region "API stub"

        public override string Author {
            get {
                return "Wolfje";
            }
        }

        public override string Description {
            get {
                return "Provides a list of customized command aliases that cost money in SEconomy.";
            }
        }

        public override string Name {
            get {
                return "CmdAlias";
            }
        }

        public override Version Version {
            get {
                return Assembly.GetExecutingAssembly().GetName().Version;
            }
        }

        #endregion

        static readonly Regex parameterRegex = new Regex(@"\$(\d)(-(\d)?)?");
        static readonly Regex randomRegex = new Regex(@"\$random\((\d*),(\d*)\)", RegexOptions.IgnoreCase);
        static readonly Regex runasFunctionRegex = new Regex(@"(\$runas\((.*?),(.*?)\)$)", RegexOptions.IgnoreCase);

        /// <summary>
        /// Format for this dictionary:
        /// Key: KVP with User's character name, and the command they ran>
        /// Value: UTC datetime when they last ran that command
        /// </summary>
        static readonly Dictionary<KeyValuePair<string, AliasCommand>, DateTime> CooldownList = new Dictionary<KeyValuePair<string, AliasCommand>, DateTime>();

        public override void Initialize() {

            base.ConfigFileChanged += CmdAliasPlugin_ConfigFileChanged;

            Configuration = Configuration.LoadConfigurationFromFile(this.ConfigFilePath);

            ParseCommands();

            base.Initialize();
        }

        async Task ReloadConfigAfterDelay(int DelaySeconds) {
            await TaskEx.Delay(DelaySeconds * 1000);
            TShockAPI.Log.ConsoleInfo("AliasCmd: reloading config.");

            try {
           
                Configuration reloadedConfig = Configuration.LoadConfigurationFromFile(this.ConfigFilePath);

                Configuration = reloadedConfig;

                ParseCommands();
            } catch (Exception ex) {
                TShockAPI.Log.ConsoleError("aliascmd: Your new config could not be loaded, fix any problems and save the file.  Your old configuration is in effect until this is fixed. \r\n\r\n" + ex.ToString());
            }

            TShockAPI.Log.ConsoleInfo("AliasCmd: config reload done.");
        }

        /// <summary>
        /// Occurs when this module's configuration file has been modified, causes a reload.
        /// </summary>
        void CmdAliasPlugin_ConfigFileChanged(object sender, EventArgs e) {

            TShockAPI.Log.ConsoleInfo("AliasCmd: config file changed.  Reloading in 5 seconds");

            Task shutTheCompilerWarningUp = ReloadConfigAfterDelay(5);
          
        }


        public void ParseCommands() {

            //potential shit idea and thread deadlocks.  If this proves a problem and the mutex ends up indefinite
            //will need to revisit this
            lock (TShockAPI.Commands.ChatCommands) {
                TShockAPI.Commands.ChatCommands.RemoveAll(i => i.Names.Where(x => x.StartsWith("cmdalias.")).Count() > 0);
                

                foreach (AliasCommand aliasCmd in Configuration.CommandAliases) {
                    //The command delegate points to the same function for all aliases, which will generically handle all of them.
                    TShockAPI.Command newCommand = new TShockAPI.Command(aliasCmd.Permissions, ChatCommand_AliasExecuted, new string[] { aliasCmd.CommandAlias, "cmdalias." + aliasCmd.CommandAlias });
                    TShockAPI.Commands.ChatCommands.Add(newCommand);
                }
            }
        }

        /// <summary>
        /// Mangles the command to execute with the supplied parameters according to the parameter rules.
        /// </summary>
        /// <param name="parameters">
        ///      * Parameter format:
        ///      * $1 $2 $3 $4: Takes the individual parameter number from the typed alias and puts it into the commands to execute
        ///      * $1-: Takes everything from the indiviual parameter to the end of the line
        ///      * $1-3: Take all parameters ranging from the lowest to the highest.
        /// </param>
        void ReplaceParameterMarkers(List<string> parameters, ref string CommandToExecute) {
            if (parameterRegex.IsMatch(CommandToExecute)) {

                /* Parameter format:
                 * 
                 * $1 $2 $3 $4: Takes the individual parameter number from the typed alias and puts it into the commands to execute
                 * $1-: Takes everything from the indiviual parameter to the end of the line
                 * $1-3: Take all parameters ranging from the lowest to the highest.
                 */
                foreach (Match match in parameterRegex.Matches(CommandToExecute)) {
                    int parameterFrom = !string.IsNullOrEmpty(match.Groups[1].Value) ? int.Parse(match.Groups[1].Value) : 0;
                    int parameterTo = !string.IsNullOrEmpty(match.Groups[3].Value) ? int.Parse(match.Groups[3].Value) : 0;
                    bool takeMoreThanOne = !string.IsNullOrEmpty(match.Groups[2].Value);
                    StringBuilder sb = new StringBuilder();


                    //take n
                    if (!takeMoreThanOne && parameterFrom > 0) {
                        if (parameterFrom <= parameters.Count) {
                            sb.Append(parameters[parameterFrom - 1]);
                        } else {
                            //If the match is put there but no parameter was input, then replace it with nothing.
                            sb.Append("");
                        }

                        //take from n to x
                    } else if (takeMoreThanOne && parameterTo > parameterFrom) {
                        for (int i = parameterFrom; i <= parameterTo; ++i) {
                            if (parameters.Count >= i) {
                                sb.Append(" " + parameters[i - 1]);
                            }
                        }

                        //take from n to infinite.
                    } else if (takeMoreThanOne && parameterTo == 0) {
                        for (int i = parameterFrom; i <= parameters.Count; ++i) {
                            sb.Append(" " + parameters[i - 1]);
                        }
                        //do fuck all lelz
                    } else {
                        sb.Append("");
                    }

                    //replace the match expression with the replacement.Oh
                    CommandToExecute = CommandToExecute.Replace(match.ToString(), sb.ToString());
                }
            }
        }

        void DoCommands(AliasCommand alias, TShockAPI.TSPlayer player, List<string> parameters) {
            
            //loop through each alias and do the commands.
            foreach (string commandToExecute in alias.CommandsToExecute) {
                //todo: parse paramaters and dynamics
                string mangledString = commandToExecute;

                //replace parameter markers with actual parameter values
                ReplaceParameterMarkers(parameters, ref mangledString);

                mangledString = mangledString.Replace("$calleraccount", "\"" + player.UserAccountName + "\"");
                mangledString = mangledString.Replace("$callername", player.Name);

                //$random(x,y) support.  Returns a random number between x and y

                if (randomRegex.IsMatch(mangledString)) {
                    foreach (Match match in randomRegex.Matches(mangledString)) {
                        int randomFrom = 0;
                        int randomTo = 0;

                        if (!string.IsNullOrEmpty(match.Groups[2].Value) && int.TryParse(match.Groups[2].Value, out randomTo)
                            && !string.IsNullOrEmpty(match.Groups[1].Value) && int.TryParse(match.Groups[1].Value, out randomFrom)) {

                            Random random = new Random();

                            mangledString = mangledString.Replace(match.ToString(), random.Next(randomFrom, randomTo).ToString());
                        } else {
                            TShockAPI.Log.ConsoleError(match.ToString() + " has some stupid shit in it, have a look at your AliasCmd config file.");
                            mangledString = mangledString.Replace(match.ToString(), "");
                        }
                    }
                }

                // $runas(u,cmd) support.  Run command as user
                if (runasFunctionRegex.IsMatch(mangledString)) {

                    foreach (Match match in runasFunctionRegex.Matches(mangledString)) {
                        string impersonatedName = match.Groups[2].Value;
                        string commandToRun = match.Groups[3].Value;
                        Economy.EconomyPlayer impersonatedPlayer = SEconomyPlugin.GetEconomyPlayerSafe(impersonatedName);

                        if (impersonatedPlayer != null) {
                            player = impersonatedPlayer.TSPlayer;
                            mangledString = commandToRun.Trim();
                        }
                    }
                }

                //and send the command to tshock to do.
                try {
                    //prevent an infinite loop for a subcommand calling the alias again causing a commandloop
                    string command = mangledString.Split(' ')[0].Substring(1);
                    if (!command.Equals(alias.CommandAlias, StringComparison.CurrentCultureIgnoreCase)) {
                        HandleCommandWithoutPermissions(player, mangledString);
                    } else {
                        TShockAPI.Log.ConsoleError(string.Format("cmdalias {0}: calling yourself in an alias will cause an infinite loop. Ignoring.", alias.CommandAlias));
                    }
                } catch {
                    //execute the command disregarding permissions
                    player.SendErrorMessage(alias.UsageHelpText);
                }
            }
        }

        /// <summary>
        /// Occurs when someone executes an alias command
        /// </summary>
        void ChatCommand_AliasExecuted(TShockAPI.CommandArgs e) {
            string commandIdentifier = e.Message;

            if (!string.IsNullOrEmpty(e.Message)) {
                commandIdentifier = e.Message.Split(' ').FirstOrDefault();
            }
            
            //Get the corresponding alias in the config that matches what the user typed.
            foreach (AliasCommand alias in Configuration.CommandAliases.Where(i => i.CommandAlias == commandIdentifier)) {
                if (alias != null) {
                    TimeSpan timeSinceLastUsedCommand = TimeSpan.MaxValue;
                    //cooldown key is a pair of the user's character name, and the command they have called.
                    //cooldown value is a DateTime they last used the command.
                    KeyValuePair<string, AliasCommand> cooldownReference = new KeyValuePair<string, AliasCommand>(e.Player.Name, alias);

                    if (CooldownList.ContainsKey(cooldownReference)) {
                        //UTC time so we don't get any daylight saving shit cuntery
                        timeSinceLastUsedCommand = DateTime.UtcNow.Subtract(CooldownList[cooldownReference]);
                    }

                    //has the time elapsed greater than the cooldown period?
                    if (timeSinceLastUsedCommand.TotalSeconds >= alias.CooldownSeconds || e.Player.Group.HasPermission("aliascmd.bypasscooldown")) {
                        Money commandCost = 0;
                        Economy.EconomyPlayer ePlayer = SEconomyPlugin.GetEconomyPlayerSafe(e.Player.Index);

                        if (!string.IsNullOrEmpty(alias.Cost) && Money.TryParse(alias.Cost, out commandCost) && !e.Player.Group.HasPermission("aliascmd.bypasscost")) {
                            if (ePlayer.BankAccount != null) {

                                if (!ePlayer.BankAccount.IsAccountEnabled) {
                                    e.Player.SendErrorMessageFormat("You cannot use this command because your account is disabled.");
                                } else if (ePlayer.BankAccount.Money >= commandCost) {
                                    SEconomyPlugin.WorldAccount.TransferAsync(ePlayer.BankAccount, -commandCost, Economy.BankAccountTransferOptions.AnnounceToReceiver | Economy.BankAccountTransferOptions.IsPayment).ContinueWith((task) => {
                                        if (task.Result.TransferSucceeded == true) {
                                            DoCommands(alias, ePlayer.TSPlayer, e.Parameters);
                                        } else {
                                            e.Player.SendErrorMessageFormat("Your payment failed.");
                                        }
                                    });
                                } else {
                                    e.Player.SendErrorMessageFormat("This command costs {0}. You need {1} more to be able to use this.", commandCost.ToLongString(), ((Money)(ePlayer.BankAccount.Money - commandCost)).ToLongString());
                                }
                            } else {
                                e.Player.SendErrorMessageFormat("This command costs money and you don't have a bank account.  Please log in first.");
                            }
                        } else {
                            //Command is free
                            DoCommands(alias, ePlayer.TSPlayer, e.Parameters);
                        }

                        //populate the cooldown list.  This dictionary does not go away when people leave so they can't
                        //reset cooldowns by simply logging out or disconnecting.  They can reset it however by logging into 
                        //a different account.
                        if (CooldownList.ContainsKey(cooldownReference)) {
                            CooldownList[cooldownReference] = DateTime.UtcNow;
                        } else {
                            CooldownList.Add(cooldownReference, DateTime.UtcNow);
                        }

                    } else {
                        e.Player.SendErrorMessageFormat("{0}: You need to wait {1:0} more seconds to be able to use that.", alias.CommandAlias, (alias.CooldownSeconds - timeSinceLastUsedCommand.TotalSeconds));
                    }
                }
            }
        }



        /// <summary>
        /// This is a copy of TShocks handlecommand method, sans the permission checks
        /// </summary>
        public static bool HandleCommandWithoutPermissions(TShockAPI.TSPlayer player, string text) {

            string cmdText = text.Remove(0, 1);

            var args = CallPrivateMethod<List<string>>(typeof(TShockAPI.Commands), true, "ParseParameters", cmdText);

            if (args.Count < 1)
                return false;

            string cmdName = args[0].ToLower();
            args.RemoveAt(0);

            IEnumerable<TShockAPI.Command> cmds = TShockAPI.Commands.ChatCommands.Where(c => c.HasAlias(cmdName));

            if (cmds.Count() == 0) {
                if (player.AwaitingResponse.ContainsKey(cmdName)) {
                    Action<TShockAPI.CommandArgs> call = player.AwaitingResponse[cmdName];
                    player.AwaitingResponse.Remove(cmdName);
                    call(new TShockAPI.CommandArgs(cmdText, player, args));
                    return true;
                }
                player.SendErrorMessage("Invalid command entered. Type /help for a list of valid commands.");
                return true;
            }
            foreach (TShockAPI.Command cmd in cmds) {
                if (!cmd.AllowServer && !player.RealPlayer) {
                    player.SendErrorMessage("You must use this command in-game.");
                } else {
                    if (cmd.DoLog)
                        TShockAPI.TShock.Utils.SendLogs(string.Format("{0} executed: /{1}.", player.Name, cmdText), Color.Red);
                    cmd.RunWithoutPermissions(cmdText, player, args);
                }
            }
            return true;
        }

       public static T CallPrivateMethod<T>(Type type, bool staticMember, string name, params object[] param) {
            BindingFlags flags = BindingFlags.NonPublic;
            if (staticMember) {
                flags |= BindingFlags.Static;
            } else {
                flags |= BindingFlags.Instance;
            }
            MethodInfo method = type.GetMethod(name, flags);
            return (T)method.Invoke(staticMember ? null : type, param);
        }

        public static T GetPrivateField<T>(Type type, object instance, string name, params object[] param) {
            BindingFlags flags = BindingFlags.NonPublic | BindingFlags.Instance;
            
            FieldInfo field = type.GetField(name, flags) as FieldInfo;

            return (T)field.GetValue(instance);
        }


    }
}
