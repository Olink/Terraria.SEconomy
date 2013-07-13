using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Wolfje.Plugins.SEconomy.ModuleFramework {
    public class ModuleBase : IDisposable {

        bool _disposed;

        public ModuleBase() { }

        /// <summary>
        /// This event fires when the configuration file for this module has changed, it's up to you to reload/refresh it if needed.
        /// </summary>
        public event EventHandler ConfigFileChanged;

        /// <summary>
        /// causes the module to load
        /// </summary>
        public virtual void Initialize() {

        }

        public virtual string Author {
            get {
                return "";
            }
        }

        public virtual string Description {
            get {
                return "";
            }
        }

        public virtual string Name {
            get {
                return "";
            }
        }

        public virtual Version Version {
            get {
                return null;
            }
        }

        public virtual string ConfigFilePath { get; set; }

        /// <summary>
        /// Raises the configFileChanged event
        /// </summary>
        public void OnConfigFileChanged() {
            if (ConfigFileChanged != null) {
                ConfigFileChanged(this, EventArgs.Empty);
            }
        }

        protected virtual void Dispose(bool disposing) {

            if (_disposed) {
                return;
            }

            if (disposing) {
                //dipose managed resources here

                _disposed = true;
            }
            
        }

        public void Dispose() {
            this.Dispose(true);
            GC.SuppressFinalize((object)this);
        }
    }
}
