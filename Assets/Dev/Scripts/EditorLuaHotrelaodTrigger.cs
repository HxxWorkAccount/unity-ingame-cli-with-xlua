namespace UnityLearning.Dev {

using System.Collections.Generic;
using System.IO;
using UnityEngine;
using UnityEditor;
using XLua;
using System.Linq;

public class EditorLuaHotreloadTrigger : MonoBehaviour
{
    private static readonly List<string> watchFolders = new List<string> {
        // "Resources/Lua",
        "Dev/Lua",
        "Lua",
    };

    private readonly List<FileSystemWatcher> m_watchers = new();
    private readonly HashSet<string> m_changedFiles = new();

    void Awake() {
        foreach (var folder in watchFolders)
            CreateWatcher(folder);
        Debug.Log("Watching lua file changes...");
    }
    void CreateWatcher(string folderPath) {
        string path = Path.Combine(Application.dataPath, folderPath);
        if (!Directory.Exists(path)) {
            // Debug.LogWarning($"Create watcher failed, directory doesn't exist: {path}");
            return;
        }
        var watcher = new FileSystemWatcher(path);

        // 监视的更改类型
        watcher.NotifyFilter = NotifyFilters.LastWrite | NotifyFilters.FileName | NotifyFilters.DirectoryName;

        // 添加事件处理器
        watcher.Changed += OnChanged;
        watcher.Created += OnCreated;
        watcher.Deleted += OnDeleted;

        // 监视子目录
        watcher.IncludeSubdirectories = true;

        // 开始监视
        watcher.EnableRaisingEvents = true;
        m_watchers.Add(watcher);
        Debug.Log($"Create watcher: {path}");
    }

    void Update() {
        foreach (string luaFilepath in m_changedFiles.ToList()) {
            string path = Path.Combine("Assets", Path.GetRelativePath(Application.dataPath, luaFilepath));
            string moduleName = LuaManager.PathToModuleName(path);
            // 编辑器下要用 File.ReadAllBytes 直接重读文件内容，资源系统是不会自动更新的
            LuaManager.Instance.HotReload(moduleName, File.ReadAllBytes(luaFilepath));
        }
        m_changedFiles.Clear();
    }

    void OnDestroy() {
        foreach (var watcher in m_watchers) {
            watcher.EnableRaisingEvents = false;
            watcher.Changed -= OnChanged;
            watcher.Created -= OnCreated;
            watcher.Deleted -= OnDeleted;
            watcher.Dispose();
        }
        Debug.Log("Stopped watching lua files.");
    }

    /*====-------------- Events --------------====*/
    // 注意，下列事件可能不会在 Unity 主线程上触发，它是文件系统触发的

    private void OnChanged(object source, FileSystemEventArgs e) {
        if (!LuaManager.IsLuaFile(e.FullPath))
            return;
        Debug.Log($"Module changed: {e.FullPath}, change type: {e.ChangeType}");
        m_changedFiles.Add(e.FullPath);
    }

    private void OnCreated(object source, FileSystemEventArgs e) {
        if (!LuaManager.IsLuaFile(e.FullPath))
            return;
        // Debug.Log($"Module created: {LuaManager.PathToModuleName(e.FullPath)}, change type: {e.ChangeType}");
    }

    private void OnDeleted(object source, FileSystemEventArgs e) {
        if (!LuaManager.IsLuaFile(e.FullPath))
            return;
        // Debug.Log($"Module deleted: {LuaManager.PathToModuleName(e.FullPath)}, change type: {e.ChangeType}");
    }
}

}
