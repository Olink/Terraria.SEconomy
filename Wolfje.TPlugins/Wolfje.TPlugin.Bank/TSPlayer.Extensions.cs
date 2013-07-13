using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using TShockAPI;

namespace Wolfje.Plugins.SEconomy {
    public static class TSPlayerExtensions {

        /// <summary>
        /// Simple wrapper around Player.SendErrorMessage that allows a string format of unlimited length
        /// </summary>
        public static void SendErrorMessageFormat(this TSPlayer Player, string MessageFormat, params object[] args) {
            Player.SendErrorMessage(string.Format(MessageFormat, args));
        }

        /// <summary>
        /// Simple wrapper aroun TSPlayer.SendInfoMessage that allows a string format of unlimited length
        /// </summary>
        public static void SendInfoMessageFormat(this TSPlayer Player, string MessageFormat, params object[] args) {
            Player.SendInfoMessage(string.Format(MessageFormat, args));
        }

    }
}
