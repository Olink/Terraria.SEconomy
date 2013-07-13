using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using System.Reflection;

using Wolfje.Plugins.SEconomy.ModuleFramework;

namespace Wolfje.Plugins.SEconomy.Modules {
    
    /// <summary>
    /// Handles loading of modules 
    /// </summary>
    public class ModuleLoader {

        /// <summary>
        /// Iterates through all the modules in the configuration file and loads them if they're enabled.
        /// </summary>
        public static List<ModuleBase> LoadModules(List<ModuleDescription> ModuleDescriptions) {
            List<ModuleBase> modules = new List<ModuleBase>();

            foreach (var moduleDef in ModuleDescriptions) {

                //load assembly
                try {
                    Assembly asm;
                    if (string.IsNullOrEmpty(moduleDef.DllFile) || moduleDef.DllFile.Equals("internal", StringComparison.CurrentCultureIgnoreCase)) {
                        asm = Assembly.GetExecutingAssembly();
                    } else {
                        byte[] modData = System.IO.File.ReadAllBytes(moduleDef.DllFile);

                        asm = AppDomain.CurrentDomain.Load(modData);
                    }

                    foreach (Type type in asm.GetTypes()) {

                        if ( type.GetCustomAttributes(true).Where(i=>i.GetType().FullName == typeof(SEconomyModuleAttribute).FullName).Count() > 0
                            && type.Name.Equals(moduleDef.Name, StringComparison.CurrentCultureIgnoreCase)
                            && moduleDef.Enabled
                            && modules.Where(i=> i.GetType().FullName == type.FullName).Count() == 0) {
                            try {
                                ModuleBase modInstance = (ModuleBase)Activator.CreateInstance(type);
                                modInstance.ConfigFilePath = moduleDef.ConfigFilePath;

                                modules.Add(modInstance);
                            } catch {
                                TShockAPI.Log.ConsoleError(string.Format("Module {0} Dll {1}: Error loading module.", moduleDef.Name, moduleDef.DllFile));
                            }
                        }
                    }

                } catch(Exception ex) {
                    if (ex is System.IO.FileNotFoundException) {
                        TShockAPI.Log.ConsoleError("seconomy modules: Cannot find file " + moduleDef.DllFile);
                    } else {
                        TShockAPI.Log.ConsoleError("seconomy modules: Error loading module " + moduleDef.DllFile);
                    }
                }
            }

            return modules;
        }

    }
}
