namespace UnityLearning {

using XLua;
using System;
using System.IO;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AddressableAssets;
using UnityEngine.ResourceManagement.AsyncOperations;
using UnityLearning.Utils;
using System.Linq;

public class LuaManager : Singleton<LuaManager>
{
    public readonly static List<string> luaExts = new() {
        ".lua",
        ".lua.txt",
    };

    public readonly static List<string> luaFolders = new() {
        "Resources/Lua",
        "Dev/Lua",
        "Lua",
    };

    private readonly LuaEnv m_luaEnv = new();
    public LuaEnv           LuaEnv   => m_luaEnv;

    private byte[] m_hotreloadTempScript;

    /*====-------------- Life --------------====*/

    protected override void Awake() {
        base.Awake();
        // 注册自定义 loader
        m_luaEnv.AddLoader(HotReloadLoader);
        m_luaEnv.AddLoader(LuaModuleLoader);
        m_luaEnv.DoString("require 'res.main'");
        Debug.Log("Lua environment initialized.");
    }

    void Start() { }

    void Update() {
        m_luaEnv.Tick();
    }

    void OnDestroy() {
        m_luaEnv?.Dispose();
    }

    /*====-------------- Lua Loader --------------====*/

    public static byte[] LuaModuleLoader(ref string modulePath) {
        string    path      = ModuleNameToPath(modulePath, false, out string guessExt);
        TextAsset luaScript = null;

        // 加载合规 Resources 资源
        if (path.StartsWith("Resources/")) {
            string respath = path.Substring("Resources/".Length);
            luaScript      = Resources.Load<TextAsset>(respath);
            if (luaScript != null)
                return luaScript.bytes;
        }

        // 加载合规 Addressables 资源
        string address = $"Assets/{path}{guessExt}";
        try {
            if (AddressablesUtils.IsAddressableAssetExist(address)) {
                luaScript = Addressables.LoadAssetAsync<TextAsset>(address).WaitForCompletion();
                if (luaScript != null)
                    return luaScript.bytes;
            }
        } catch (Exception) {
            // Debug.LogWarning($"Addressables load lua script failed: {e.Message}");
        }

        // 若还失败，则认为是 Resources/Lua 下的路径，并尝试用 .lua.txt 后缀读取
        string resPath = $"{path}.lua";
        luaScript      = Resources.Load<TextAsset>(resPath);
        if (luaScript != null)
            return luaScript.bytes;

        return null;
    }

    private byte[] HotReloadLoader(ref string modulePath) {
        if (modulePath != "__hotreload_temp_script__")
            return null;
        Debug.Assert(m_hotreloadTempScript != null, "Hot reload script is null");
        return m_hotreloadTempScript;
    }

    public static bool IsLuaFile(string path) {
        foreach (var ext in luaExts) {
            if (path.EndsWith(ext))
                return true;
        }
        return false;
    }

    /*
     * Lua 路径与模块映射规则如下：
     * - 只用 .lua 做脚本后缀，出于兼容考虑 .lua.txt 也能加载，但一些工具无法使用（如：ModuleNameToPath）
     * - 记录 Assets 的 Lua, Resources/Lua Dev/Lua 路径下的脚本，这三个目录下的**脚本相对路径**必须唯一，会作为 require 标识
     * - Resources/Lua 目录下的脚本应放到 res 子目录下，Dev/Lua 目录下的脚本应放到 dev 子目录下
     * - 绑定组件的脚本须放到 Lua/binding 目录下的，且不应该在正常脚本中被 require
     * - 特殊后缀：.lua, .lua.txt 这些后缀都会被剔除，因此不应该重名
     */

    /// <param name="path">Assets 目录路径（不带 Assets 开头）</param>
    public static string PathToModuleName(string path) {
        // 裁掉后缀
        foreach (var ext in luaExts) {
            if (path.EndsWith(ext)) {
                path = path[0..(path.Length - ext.Length)];
                break;
            }
        }
        string relpath = Path.GetRelativePath("Assets", path).Replace('\\', '/');

        string rawpath = "";
        foreach (var folder in luaFolders) {
            if (relpath.StartsWith(folder)) {
                rawpath = relpath[(folder.Length + 1)..];
                break;
            }
        }
        return rawpath.Replace('/', '.');
    }

    /// <summary>该函数基于脚本规范，通过模块开头来判断。部分脚本不遵循该规范，无法使用该函数获得路径（如：XLua 自带脚本、Resources 下的第三方库）</summary>
    /// <param name="autoExt">是否自动添加文件后缀</param>
    /// <returns>Assets 目录的路径</returns>
    public static string ModuleNameToPath(string module, bool appendGuessExt, out string guessExt) {
        guessExt   = ".lua";
        string ext = appendGuessExt ? guessExt : "";
        if (module.StartsWith("res.")) {
            return $"Resources/Lua/{module.Replace('.', '/')}{ext}";
        } else if (module.StartsWith("dev.")) {
            return $"Dev/Lua/{module.Replace('.', '/')}{ext}";
        } else {
            return $"Lua/{module.Replace('.', '/')}{ext}";
        }
    }

    /*====-------------- Hot Reload --------------====*/

    internal void HotReload(string module) {
        string[] parts = module.Split('.')[0.. ^ 1];  // 去掉最后一部分，得到路径
        if (parts.Contains("binding")) {              // 如果包含 binding 目录，则认为是绑定脚本
            using LuaTable copyScope = LuaUtils.CreateScopeFromBinding(module);
            m_luaEnv.Global.Set("__hotreload_temp_binding__", copyScope);
            m_luaEnv.DoString($"require('res.LuaHotfix').hotfixBinding('{module}')");
            Debug.Log($"require('res.LuaHotfix').hotfixBinding('{module}')");
        } else {
            m_luaEnv.DoString($"require('res.LuaHotfix').hotfixByRequire('{module}')");
            Debug.Log($"require('res.LuaHotfix').hotfixByRequire('{module}')");
        }
    }

    internal void HotReload(string module, byte[] script) {
        string[] parts = module.Split('.')[0.. ^ 1];  // 去掉最后一部分，得到路径
        if (parts.Contains("binding")) {              // 如果包含 binding 目录，则认为是绑定脚本
            using LuaTable copyScope = LuaUtils.CreateScopeFromBinding(module, script);
            if (copyScope != null) {
                m_luaEnv.Global.Set("__hotreload_temp_binding__", copyScope);
                m_luaEnv.DoString($"require('res.LuaHotfix').hotfixBinding('{module}')");
                Debug.Log($"require('res.LuaHotfix').hotfixBinding('{module}')");
            }
        } else {
            m_hotreloadTempScript = script;
            m_luaEnv.DoString($"require('res.LuaHotfix').hotfixByByte('{module}')");
            Debug.Log($"require('res.LuaHotfix').hotfixByByte('{module}')");
        }
    }
}

}
