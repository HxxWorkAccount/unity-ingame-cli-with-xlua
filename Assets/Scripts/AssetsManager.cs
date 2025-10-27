namespace UnityLearning {

using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.AddressableAssets;
using UnityEngine.ResourceManagement.AsyncOperations;
using UnityLearning.Utils;

public class AssetsManager : Singleton<AssetsManager>
{
    private static readonly List<string> initKeyList = new() {
        "Assets/Lua/__AddressableGroup.lua",

#if UNITY_EDITOR
        "Assets/Dev/Lua/__AddressableGroup.lua",
#endif
    };

    private static readonly List<string> asyncInitKeyList = new() {

    };

    private readonly List<AsyncOperationHandle<IList<Object>>> m_initHandles = new();

    protected override void Awake() {
        base.Awake();
        InitAssetsAsync();
        InitAssets();
    }

    private void InitAssets() {
        // 出于便捷考虑，直接阻塞线程同步加载资源了，之后有时间再改回异步
        var handle = Addressables.LoadAssetsAsync<Object>(initKeyList, null, Addressables.MergeMode.Union, false);
        m_initHandles.Append(handle);
        handle.WaitForCompletion();
    }

    private void InitAssetsAsync() {
        if (asyncInitKeyList.Count == 0)
            return;
        var handle = Addressables.LoadAssetsAsync<Object>(asyncInitKeyList, null, Addressables.MergeMode.Union, false);
        m_initHandles.Append(handle);
    }
}

}
