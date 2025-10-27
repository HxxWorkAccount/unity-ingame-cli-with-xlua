namespace UnityLearning.Utils {
using System;
using UnityEngine;
using XLua;

public static class LuaUtils
{
    public static LuaTable CreateBindingScope(Component component, string bindingModuleName, bool cache) {
        var luaEnv = LuaManager.Instance.LuaEnv;

        // 独立脚本域
        LuaTable scriptScope = luaEnv.NewTable();
        using (LuaTable meta = luaEnv.NewTable()) {
            meta.Set("__index", luaEnv.Global);
            scriptScope.SetMetaTable(meta);
        }

        // 将所需值注入到 Lua 脚本域中
        scriptScope.Set("this", scriptScope);
        scriptScope.Set("component", component);
        scriptScope.Set("global", luaEnv.Global);

        byte[] script = LuaManager.LuaModuleLoader(ref bindingModuleName);
        luaEnv.DoString(script, bindingModuleName, scriptScope);

        // 将脚本域存入 package.loaded
        if (cache) {
            using LuaTable packageLoaded = luaEnv.Global.GetInPath<LuaTable>("package.loaded");
            packageLoaded.Set(bindingModuleName, scriptScope);
        }

        return scriptScope;
    }

    internal static LuaTable CreateScopeFromBinding(string bindingModuleName, byte[] script = null) {
        var luaEnv = LuaManager.Instance.LuaEnv;

        object[] results = luaEnv.DoString($"return require('res.LuaHotfix').createScopeFromBinding('{bindingModuleName}')");
        if (results == null || results.Length == 0 || results[0] == null) {
            Debug.LogWarning($"CreateScopeFromBinding failed, module not loaded: {bindingModuleName}");
            return null;
        }
        LuaTable copyScriptScope = results[0] as LuaTable;
        copyScriptScope.Set("this", copyScriptScope);

        script ??= LuaManager.LuaModuleLoader(ref bindingModuleName);
        luaEnv.DoString(script, bindingModuleName, copyScriptScope);
        return copyScriptScope;
    }
}

}
