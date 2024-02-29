using UnityEngine;

namespace Scripts.Initialize
{
    public class ScopedSingletonMonoBehaviour<T> : MonoBehaviour where T : ScopedSingletonMonoBehaviour<T>
    {
        public static T Instance { get; private set; } = null;

        protected virtual void Awake()
        {
            if (Instance == null)
            {
                Instance = this as T;
            }
        }

        protected virtual void OnDestroy()
        {
            Instance = null;
        }
    }
}
