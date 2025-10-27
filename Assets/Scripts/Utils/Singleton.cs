namespace UnityLearning.Utils {
using UnityEngine;

public abstract class Singleton<T> : MonoBehaviour
    where T : MonoBehaviour
{
    private static T instance;
    public static T  Instance {
        get {
            if (instance == null) {  // 如果实例为空，尝试在场景中查找
                instance = FindObjectOfType<T>();
                if (instance == null) {  // 找不到，则创建一个
                    GameObject obj = new GameObject();
                    obj.name       = $"Singleton: {typeof(T).Name}";
                    instance       = obj.AddComponent<T>();
                }
            }
            return instance;
        }
    }
    
    public static T RawInstance => instance;


    protected virtual void Awake() {
        // 确保只有一个实例
        if (instance == null) {
            instance = this as T;
            // FIXIT hxx DontDestroyOnLoad only works for root GameObjects
            DontDestroyOnLoad(this.gameObject);  // 场景切换时不会销毁
        } else {
            // 如果已经有实例，销毁当前对象
            Destroy(this.gameObject);
        }
    }
}

}
