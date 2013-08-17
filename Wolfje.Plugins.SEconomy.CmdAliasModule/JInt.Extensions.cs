using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using Jint;

namespace Wolfje.Plugins.SEconomy.CmdAliasModule {
    public static class JInt {


        public static void SetFunction(this JintEngine engine, string name, Action action) {
            engine.SetFunction(name, new Action(action));
        }

        public static void SetFunction<TActionType>(this JintEngine engine, string name, Action<TActionType> action) {
            engine.SetFunction(name, new Action<TActionType>(action));
        }

        public static void SetFunction<TResult>(this JintEngine engine, string name, Func<TResult> func) {
            engine.SetFunction(name, new Func<TResult>(func));
        }

        public static void SetFunction<TParamType, TResult>(this JintEngine engine, string name, Func<TParamType, TResult> func) {
            engine.SetFunction(name, new Func<TParamType, TResult>(func));
        }

    }
}
