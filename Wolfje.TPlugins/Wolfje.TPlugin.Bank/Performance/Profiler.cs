using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Diagnostics;

namespace Wolfje.Plugins.SEconomy.Performance {
    internal class Profiler {

        Hashtable _profilerTable;

        public void Enter(string Name) {
            if (_profilerTable == null) {
                _profilerTable = new Hashtable(100);
            }

            if (!_profilerTable.ContainsKey(Name)) {
                _profilerTable.Add(Name, Stopwatch.StartNew());
            }
        }

        public KeyValuePair<string, long>? Exit(string Name) {
            if (_profilerTable.ContainsKey(Name)) {
                Stopwatch value = _profilerTable[Name] as Stopwatch;
                value.Stop();

                _profilerTable.Remove(Name);
                return new KeyValuePair<string, long>(Name, value.ElapsedMilliseconds);
            }

            return null;
        }

        public string ExitString(string Name) {
            var profile = Exit(Name);

            if (profile.HasValue) {
                return string.Format("profiler: {0} took {1}ms.", profile.Value.Key, profile.Value.Value);
            } else {
                return null;
            }
        }

        public void ExitLog(string Name) {
            var profile = Exit(Name);

            if (profile.HasValue) {
                TShockAPI.Log.ConsoleInfo(string.Format("profiler: {0} took {1}ms.", profile.Value.Key, profile.Value.Value));
            }
        }

    }
}
