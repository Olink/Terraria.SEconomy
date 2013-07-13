using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Wolfje.Plugins.SEconomy.ModuleFramework {

    /// <summary>
    /// This attribute indicates a SEconomy linked module entry point.
    /// </summary>
    [AttributeUsage(AttributeTargets.All, Inherited = false, AllowMultiple = true)]
    public sealed class SEconomyModuleAttribute : Attribute {
        // See the attribute guidelines at 
        //  http://go.microsoft.com/fwlink/?LinkId=85236
    
        // This is a positional argument
        public SEconomyModuleAttribute() {
        }
    }
}
