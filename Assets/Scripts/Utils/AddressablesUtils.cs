namespace UnityLearning.Utils {

using UnityEngine;
using UnityEngine.AddressableAssets;

public static class AddressablesUtils
{
    /// <summary>只能检查当前已加载的 catalog</summary>
    public static bool IsAddressableAssetExist(string address) {
        foreach (var locator in Addressables.ResourceLocators) {
            if (locator.Locate(address, typeof(object), out var locations))
                if (locations != null && locations.Count > 0)
                    return true;
        }
        return false;
    }
}

}
