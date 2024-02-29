using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Scripts.Initialize
{
    [System.Serializable]
    public class LookupEntry
    {
        public string type;
        public GameObject prefab;
        [System.NonSerialized] public GameObject Instance;

        public bool CheckValid()
        {
            return !string.IsNullOrEmpty(type) && prefab != null;
        }
    }

    public class Root : MonoBehaviour
    {
        [SerializeField] private List<LookupEntry> lookupEntries;
        [SerializeField] private bool controlledInstantiate;

        private static Root m_instance = null;
        public static Root Instance => m_instance;
        
        private void Awake()
        {
            Debug.Log("Root.Awake");

            if (m_instance != null)
            {
                Destroy(gameObject);
                return;
            }

            m_instance = this;

            if (controlledInstantiate)
            {
                InstantiatePrefabs();
            }
            // load scene
            SceneManager.LoadScene("UIInGame");
        }
        
        public GameObject GetPrefabFromType(string typeName)
        {
            foreach (var entry in lookupEntries)
            {
                if (entry.type.Equals(typeName))
                {
                    return entry.prefab;
                }
            }
            return null;
        }

        public void SetInstanceForType(string typeName, GameObject instance)
        {
            foreach (var entry in lookupEntries)
            {
                if (entry.type.Equals(typeName))
                {
                    entry.Instance = instance;
                    break;
                }
            }
        }

        private void InstantiatePrefabs()
        {
            foreach (var entry in lookupEntries)
            {
                if (entry.CheckValid() && entry.Instance == null)
                {
                    GameObject newObj = Instantiate(entry.prefab);
                    entry.Instance = newObj;
                    DontDestroyOnLoad(newObj);
                }
            }
        }
    }
}