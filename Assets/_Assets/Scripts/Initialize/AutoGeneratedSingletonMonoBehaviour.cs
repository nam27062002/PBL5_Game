﻿using UnityEngine;

namespace Scripts.Initialize
{
    public class AutoGeneratedSingletonMonoBehaviour<T> : MonoBehaviour where T : AutoGeneratedSingletonMonoBehaviour<T>
    {
        private static T m_instance = null;
        private static bool m_isCreated = false;

        public static bool HasInstance => m_instance != null;

        public static T Instance
        {
            get
            {
                if (m_instance != null || m_isCreated) return m_instance;
                var tInScene = FindAnyObjectByType<T>();
                if (tInScene == null)
                {
                    var tPrefab = Root.Instance.GetPrefabFromType(typeof(T).ToString());
                    if (tPrefab != null)
                    {
                        var tObj = Instantiate(tPrefab);
                        DontDestroyOnLoad(tObj);
                        tInScene = tObj.GetComponent<T>();
                        m_isCreated = true;
                    }
                }
                m_instance = tInScene;
                Root.Instance.SetInstanceForType(typeof(T).ToString(), m_instance.gameObject);
                return m_instance;
            }
        }
    }
}