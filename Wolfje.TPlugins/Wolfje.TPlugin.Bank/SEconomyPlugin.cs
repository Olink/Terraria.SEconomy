using System;
using System.Collections.Generic;
using System.Collections.Concurrent;
using System.Linq;
using System.Text;
using System.Reflection;

using Terraria;
using TShockAPI;
using System.Threading.Tasks;

namespace Wolfje.Plugins.SEconomy {

    /// <summary>
    /// Seconomy for Terraria and TShock.  Copyright (C) Tyler Watson, 2013.
    /// 
    /// API Version 1.12
    /// </summary>
    [APIVersion(1, 12)]
    public class SEconomyPlugin : TerrariaPlugin {

        public static DatabaseDriver Database;
        internal static readonly Performance.Profiler Profiler = new Performance.Profiler();
        static List<Economy.EconomyPlayer> economyPlayers;
        static Economy.BankAccount _worldBankAccount;
        static readonly List<ModuleFramework.ModuleBase> modules = new List<ModuleFramework.ModuleBase>();
        static Configuration Configuration { get; set; }
        static System.Timers.Timer PayRunTimer { get; set; }
        static System.IO.FileSystemWatcher ConfigFileWatcher { get; set; }
       

        #region "API Plugin Stub"
        public override string Author {
            get {
                return "Wolfje";
            }
        }

        public override string Description {
            get {
                return "Provides server-sided currency and accounting for servers running TShock";
            }
        }

        public override string Name {
            get {
                return "SEconomy (Milestone 1 BETA)";
            }
        }

        public override Version Version {
            get {
                return Assembly.GetExecutingAssembly().GetName().Version;
            }
        }

        #endregion

        #region "Constructors"

        

        public SEconomyPlugin(Main game)
            : base(game) {
            Order = 20000;

            if (!System.IO.Directory.Exists(Configuration.BaseDirectory)) {
                System.IO.Directory.CreateDirectory(Configuration.BaseDirectory);
            }

            economyPlayers = new List<Economy.EconomyPlayer>();

            TShockAPI.Hooks.PlayerHooks.PlayerLogin += PlayerHooks_PlayerLogin;

            Hooks.GameHooks.Initialize += GameHooks_Initialize;
            Hooks.ServerHooks.Join += ServerHooks_Join;
            Hooks.ServerHooks.Leave += ServerHooks_Leave;
            Hooks.NetHooks.GetData += NetHooks_GetData;

            Economy.EconomyPlayer.PlayerBankAccountLoaded += EconomyPlayer_PlayerBankAccountLoaded;
            Economy.BankAccount.BankAccountFlagsChanged += BankAccount_BankAccountFlagsChanged;
            Economy.BankAccount.BankTransferCompleted += BankAccount_BankTransferCompleted;
        }

        /// <summary>
        /// Occurs when the server receives data from the client.
        /// </summary>
        void NetHooks_GetData(Hooks.GetDataEventArgs e) {

            if (e.MsgID == PacketTypes.PlayerUpdate) {
                byte playerIndex = e.Msg.readBuffer[e.Index];
                PlayerControlFlags playerState = (PlayerControlFlags)e.Msg.readBuffer[e.Index + 1];
                Economy.EconomyPlayer currentPlayer = GetEconomyPlayerSafe(playerIndex);

                //The idea behind this logic is that IdleSince resets to now any time the server an action from the client.
                //If the client never updates, or updates to 0 (Idle) then "IdleSince" never changes.
                //When you want to get the amount of time the player has been idle, just subtract it from DateTime.Now
                //And voila, you get a TimeSpan with how long the user has been idle for.
                if (playerState != PlayerControlFlags.Idle) {
                    currentPlayer.IdleSince = DateTime.Now;
                }

                currentPlayer.LastKnownState = playerState;
            }
        }

        protected override void Dispose(bool disposing) {

            if (disposing) {
                TShockAPI.Hooks.PlayerHooks.PlayerLogin -= PlayerHooks_PlayerLogin;

                Hooks.GameHooks.Initialize -= GameHooks_Initialize;
                Hooks.ServerHooks.Join -= ServerHooks_Join;
                Hooks.NetHooks.GetData -= NetHooks_GetData;
                Hooks.ServerHooks.Leave -= ServerHooks_Leave;

                Economy.EconomyPlayer.PlayerBankAccountLoaded -= EconomyPlayer_PlayerBankAccountLoaded;
                Economy.BankAccount.BankAccountFlagsChanged -= BankAccount_BankAccountFlagsChanged;
                Economy.BankAccount.BankTransferCompleted -= BankAccount_BankTransferCompleted;

                economyPlayers = null;
                Database = null;

                TShockAPI.Log.ConsoleInfo("Turning off modules");

                foreach (ModuleFramework.ModuleBase m in modules) {
                    m.Dispose();
                }

                modules.Clear();
            }

            base.Dispose(disposing);
        }

        #endregion

        /// <summary>
        /// Initialization point for the Terrraria API
        /// </summary>
        public override void Initialize() {
            Configuration = Configuration.LoadConfigurationFromFile(Configuration.BaseDirectory + System.IO.Path.DirectorySeparatorChar + "SEconomy.config.json");
        }

        #region "Event Handlers"

        /// <summary>
        /// Fires when a player's bank account is loaded from the database.
        /// </summary>
        void EconomyPlayer_PlayerBankAccountLoaded(object sender, EventArgs e) {
            Economy.EconomyPlayer ePlayer = sender as Economy.EconomyPlayer;

            if (ePlayer.BankAccount != null) {
                if (ePlayer.BankAccount.IsAccountEnabled) {
                    ePlayer.TSPlayer.SendInfoMessage(string.Format("You have {0}", ePlayer.BankAccount.Money.ToLongString(true)));
                } else {
                    ePlayer.TSPlayer.SendInfoMessage("Your bank account is disabled.");
                }
            }
        }


        /// <summary>
        /// Occurs when a bank transfer completes.
        /// </summary>
        void BankAccount_BankTransferCompleted(object sender, Economy.BankTransferEventArgs e) {
            //this is pretty balls too, but will do for now.

            if ((e.TransferOptions & Economy.BankAccountTransferOptions.SuppressDefaultAnnounceMessages) == Economy.BankAccountTransferOptions.SuppressDefaultAnnounceMessages) {
                return;
            } else if (e.ReceiverAccount != null) {

                //Player died from PvP
                if ((e.TransferOptions & Economy.BankAccountTransferOptions.MoneyFromPvP) == Economy.BankAccountTransferOptions.MoneyFromPvP) {
                    if ((e.TransferOptions & Economy.BankAccountTransferOptions.AnnounceToReceiver) == Economy.BankAccountTransferOptions.AnnounceToReceiver) {
                        e.ReceiverAccount.Owner.TSPlayer.SendMessage(string.Format("You killed {0} and gained {1}.", e.SenderAccount.Owner.TSPlayer.Name, e.Amount.ToLongString()), Color.Orange);
                    }
                    if ((e.TransferOptions & Economy.BankAccountTransferOptions.AnnounceToSender) == Economy.BankAccountTransferOptions.AnnounceToSender) {
                        e.SenderAccount.Owner.TSPlayer.SendMessage(string.Format("{0} killed you and you lost {1}.", e.ReceiverAccount.Owner.TSPlayer.Name, e.Amount.ToLongString()), Color.Orange);
                    }

                    //P2P transfers, both the sender and the reciever get notified.
                } else if ((e.TransferOptions & Economy.BankAccountTransferOptions.IsPlayerToPlayerTransfer) == Economy.BankAccountTransferOptions.IsPlayerToPlayerTransfer) {
                    if ((e.TransferOptions & Economy.BankAccountTransferOptions.AnnounceToReceiver) == Economy.BankAccountTransferOptions.AnnounceToReceiver) {
                        e.ReceiverAccount.Owner.TSPlayer.SendMessage(string.Format("You received {0} from {1} Transaction # {2}", e.Amount.ToLongString(), e.SenderAccount.Owner.TSPlayer.Name, e.TransactionID), Color.Orange);
                    }
                    if ((e.TransferOptions & Economy.BankAccountTransferOptions.AnnounceToSender) == Economy.BankAccountTransferOptions.AnnounceToSender) {
                        e.SenderAccount.Owner.TSPlayer.SendMessage(string.Format("You sent {0} to {1}. Transaction # {2}", e.Amount.ToLongString(), e.ReceiverAccount.Owner.TSPlayer.Name, e.TransactionID), Color.Orange);
                    }

                    //Everything else, including world to player, and player to world.
                } else {
                    string moneyVerb = "gained";
                    if (e.Amount < 0) {
                        if ((e.TransferOptions & Economy.BankAccountTransferOptions.IsPayment) == Economy.BankAccountTransferOptions.IsPayment) {
                            moneyVerb = "paid";
                        } else {
                            moneyVerb = "lost";
                        }
                    }

                    if ((e.TransferOptions & Economy.BankAccountTransferOptions.AnnounceToSender) == Economy.BankAccountTransferOptions.AnnounceToSender) {
                        e.SenderAccount.Owner.TSPlayer.SendMessage(string.Format("You {0} {1}", moneyVerb, e.Amount.ToLongString()), Color.Orange);
                    }
                    if ((e.TransferOptions & Economy.BankAccountTransferOptions.AnnounceToReceiver) == Economy.BankAccountTransferOptions.AnnounceToReceiver) {
                        e.ReceiverAccount.Owner.TSPlayer.SendMessage(string.Format("You {0} {1}", moneyVerb, e.Amount.ToLongString()), Color.Orange);
                    }
                }
            } else {
                TShockAPI.Log.ConsoleError("seconomy error: Bank account transfer completed without a receiver: ID " + e.TransactionID);
            }
        }

        /// <summary>
        /// Occurs when a player's bank account flags change
        /// </summary>
        void BankAccount_BankAccountFlagsChanged(object sender, Economy.BankAccountChangedEventArgs e) {
            Economy.BankAccount bankAccount = sender as Economy.BankAccount;
            Economy.EconomyPlayer player = GetEconomyPlayerByBankAccountNameSafe(bankAccount.BankAccountName);
            TSPlayer caller = TShock.Players[e.CallerID];

            //You can technically make payments to anyone even if they are offline.
            //This serves as a basic online check as we don't give a fuck about informing
            //an offline person that their account has been disabled or not.
            if (player != null) {
                bool enabled = (e.NewFlags & DatabaseObjects.BankAccountFlags.Enabled) == DatabaseObjects.BankAccountFlags.Enabled;

                if (player.TSPlayer.Name == caller.Name) {
                    player.TSPlayer.SendInfoMessageFormat("bank: Your bank account has been {0}d.", enabled ? "enable" : "disable");
                } else {
                    player.TSPlayer.SendInfoMessageFormat("bank: {1} {0}d your account.", enabled ? "enable" : "disable", caller.Name);
                }
            }

        }

        /// <summary>
        /// Fires when a player leaves.
        /// </summary>
        void ServerHooks_Leave(int PlayerIndex) {
            Economy.EconomyPlayer ePlayer = GetEconomyPlayerSafe(PlayerIndex);

            //Lock players, deleting needs to block to avoid iterator crashes and race conditions
            lock (__accountSafeLock) {
                economyPlayers.Remove(ePlayer);
            }
        }

        /// <summary>
        /// Fires when a player joins
        /// </summary>
        void ServerHooks_Join(int playerId, System.ComponentModel.HandledEventArgs e) {
            //Add economy player wrapper to the static list of players.
            lock (__accountSafeLock) {
                Economy.EconomyPlayer player = new Economy.EconomyPlayer(playerId);

                economyPlayers.Add(player);

                //if the user belongs to group superadmin we can assume they are trusted and attempt to load a bank account via name.
                //everyone else has to login
                if (player.TSPlayer.Group is TShockAPI.SuperAdminGroup) {
                    player.LoadBankAccountByPlayerNameAsync();
                }
            }
            
            Task shuttingTheCompilerWarningUp = Database.EnsureWorldAccountExistsAsync();
        }

        /// <summary>
        /// Fires when a user logs in.
        /// </summary>
        void PlayerHooks_PlayerLogin(TShockAPI.Hooks.PlayerLoginEventArgs e) {
            Economy.EconomyPlayer ePlayer = GetEconomyPlayerSafe(e.Player.Index);

            //Ensure a bank account for the economy player exists, and asynchronously load it.
            ePlayer.EnsureBankAccountExists();
        }

        /// <summary>
        /// Fires when the server initializes.
        /// </summary>
        void GameHooks_Initialize() {
            Database = new DatabaseDriver(Configuration.DatabaseFilePath);

            Log.ConsoleInfo("seconomy modules: Turning on modules");

            modules.AddRange(Modules.ModuleLoader.LoadModules(Configuration.Modules));
            foreach (ModuleFramework.ModuleBase loadedModule in modules) {
                Log.ConsoleInfo(string.Format("seconomy modules: {0} v{1} by {2} loaded.", loadedModule.Name, loadedModule.Version, loadedModule.Author));
                //init the module.
                loadedModule.Initialize();
            }

            //Initialize the command interface
            ChatCommands.Initialize();

            ConfigFileWatcher = new System.IO.FileSystemWatcher(System.IO.Path.Combine(Environment.CurrentDirectory, Configuration.BaseDirectory));
            ConfigFileWatcher.Changed += ConfigFileWatcher_Changed;
            ConfigFileWatcher.NotifyFilter = System.IO.NotifyFilters.LastAccess | System.IO.NotifyFilters.LastWrite | System.IO.NotifyFilters.DirectoryName;
            ConfigFileWatcher.EnableRaisingEvents = true;

            //this is the pay run timer.
            //The timer event fires when it's time to do a pay run to the online players
            //This event fires in PayRunTimer_Elapsed
            if (Configuration.PayIntervalMinutes > 0) {
                PayRunTimer = new System.Timers.Timer(Configuration.PayIntervalMinutes * 60000);
                PayRunTimer.Elapsed += PayRunTimer_Elapsed;
                PayRunTimer.Start();
            }
        }

        /// <summary>
        /// Occurs when a file changes in the tshock directory.  Used to determine when modules' config files change.
        /// </summary>
        void ConfigFileWatcher_Changed(object sender, System.IO.FileSystemEventArgs e) {

            try {
                ConfigFileWatcher.EnableRaisingEvents = false;

                foreach (ModuleFramework.ModuleBase loadedModule in modules) {
                    if (System.IO.Path.GetFileName(e.Name).Equals(System.IO.Path.GetFileName(loadedModule.ConfigFilePath), StringComparison.CurrentCultureIgnoreCase)) {
                        loadedModule.OnConfigFileChanged();
                    }
                }
            } finally {
                ConfigFileWatcher.EnableRaisingEvents = true;
            }
        }

        /// <summary>
        /// Occurs when a player online payment needs to occur.
        /// </summary>
        void PayRunTimer_Elapsed(object sender, System.Timers.ElapsedEventArgs e) {
            lock (__accountSafeLock) {

                if (Configuration.PayIntervalMinutes > 0 && !string.IsNullOrEmpty(Configuration.IntervalPayAmount)) {
                    Money payAmount;

                    if (Money.TryParse(Configuration.IntervalPayAmount, out payAmount)) {
                        foreach (Economy.EconomyPlayer ep in economyPlayers) {
                            //if the time since the player was idle is less than or equal to the configuration idle threshold
                            //then the player is considered not AFK.
                            if (ep.TimeSinceIdle.TotalMinutes <= Configuration.IdleThresholdMinutes && ep.BankAccount != null) {
                                //Pay them from the world account
                                WorldAccount.TransferAndReturn(ep.BankAccount, payAmount, Economy.BankAccountTransferOptions.AnnounceToReceiver);
                            }
                        }
                    }
                }
            }
        }

        #endregion

        #region "Static APIs"

        /// <summary>
        /// Gets an economy-enabled player by their player name. 
        /// </summary>
        static object __accountSafeLock = new object();
        public static Economy.EconomyPlayer GetEconomyPlayerByBankAccountNameSafe(string Name) {
            lock (__accountSafeLock) {
                return economyPlayers.FirstOrDefault(i => (i.BankAccount != null) && i.BankAccount.BankAccountName == Name);
            }
        }

        /// <summary>
        /// Gets an economy-enabled player by their player name. 
        /// </summary>
        public static Economy.EconomyPlayer GetEconomyPlayerSafe(string Name) {
            lock (__accountSafeLock) {
                return economyPlayers.FirstOrDefault(i => i.TSPlayer.Name == Name);
            }
        }

        /// <summary>
        /// Gets an economy-enabled player by their index.  This method is thread-safe.
        /// </summary>
        public static Economy.EconomyPlayer GetEconomyPlayerSafe(int id) {
            Economy.EconomyPlayer p = null;

            lock (__accountSafeLock) {
                foreach (Economy.EconomyPlayer ep in economyPlayers) {
                    if (ep.Index == id) {
                        p = ep;
                    }
                }
            }

            return p;
        }

        /// <summary>
        /// Gets the world bank account (system account) for paying players.
        /// </summary>
        public static Economy.BankAccount WorldAccount {
            get {
                return _worldBankAccount;
            }

            internal set {
                _worldBankAccount = value;

                if (_worldBankAccount != null) {

                    _worldBankAccount.SyncBalanceAsync().ContinueWith((task) => {
                        long worldAccountBalance = _worldBankAccount.Money;
                        if (worldAccountBalance < 0) {
                            worldAccountBalance *= -1;
                        }

                        Log.ConsoleInfo(string.Format("SEconomy: world account: paid {0} to players.", ((Money)worldAccountBalance).ToLongString()));
                    });

                }
            }
        }


        #endregion

    }
}
