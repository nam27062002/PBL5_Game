using UnityEngine;
namespace Scripts.Utils
{
    public abstract class MonoSingleton<T> : MonoBehaviour where T : MonoSingleton<T>
    {
        [SerializeField] bool thereCanBeOnlyOne = true;
        [SerializeField] bool dontDestroyOnLoad = true;
        private static T _instance;
        public static T Instance
        {
            get {
                if (_instance) return _instance;
                var type = typeof (T).Name;
                Debug.Log("Instance path: " + "Prefabs/Singletons/" + type);

                var prefab = Resources.Load<T>("Prefabs/Singletons/" + type);

                if (prefab == null)
                {
                    Debug.LogError("Unable to load prefab for MonoSingleton<" + type + ">");
                }
                    
                Instantiate(prefab, Vector3.zero, Quaternion.identity);

                return _instance;
            }
            private set => _instance = value;
        }

        public static bool HasInstance => _instance != null;

        protected virtual void Awake()
        {
            if(  thereCanBeOnlyOne && _instance )
            {
                Debug.LogWarning("THERE CAN BE ONLY ONE " + typeof(T) + "!!!!!");
                Release();
            }
            else
            {
                Instance = (T)this;
                if(dontDestroyOnLoad)
                {
                    DontDestroyOnLoad(gameObject);
                }
            }
        }

        private void Release()
        {
            Destroy(gameObject);
        }

    }
}
