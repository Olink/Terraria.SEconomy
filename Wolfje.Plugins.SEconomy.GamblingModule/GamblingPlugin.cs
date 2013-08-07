using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using TShockAPI;
using Terraria;
using System.Reflection;

namespace Wolfje.Plugins.SEconomy.GamblingModule {
    
    [APIVersion(1,12)]
    public sealed class GamblingPlugin : TerrariaPlugin {

        #region "API Plugin Stub"

        public GamblingPlugin(Main game)
            : base(game) {
                
        }

        public override string Author {
            get {
                return "Wolfje";
            }
        }

        public override string Description {
            get {
                return "Provides gambling and fun games for players to play with SEconomy money";
            }
        }

        public override string Name {
            get {
                return "GamblingModule for SEconomy";
            }
        }

        public override Version Version {
            get {
                return Assembly.GetExecutingAssembly().GetName().Version;
            }
        }

        #endregion

        /// <summary>
        /// Main initialize point for Gambling games
        /// </summary>
        public override void Initialize() {
            Hooks.GameHooks.PostInitialize += GameHooks_PostInitialize;
            Hooks.NetHooks.GetData += NetHooks_GetData;
        }

        void NetHooks_GetData(Hooks.GetDataEventArgs e) {

            if (e.MsgID == PacketTypes.Tile) {

            }

        }


        protected override void Dispose(bool disposing) {

            if (disposing) {
                Hooks.GameHooks.PostInitialize -= GameHooks_PostInitialize;
            }
            
            base.Dispose(disposing);
        }

        #region "event handlers"


        void GameHooks_PostInitialize() {

        }

        #endregion

    }
}
