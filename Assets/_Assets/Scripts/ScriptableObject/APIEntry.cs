using System;
using System.Linq;
using Scripts.API;
using UnityEngine;

namespace Scripts.ScriptableObject
{
    [CreateAssetMenu(fileName = "APIEntry", menuName = "ScriptableObjects/APIEntry")]
    public class APIEntry : UnityEngine.ScriptableObject
    {
        [Serializable]
        public class APIRequestBody
        {
            public APIEnum APIType;
            public string BodyContent;
        }
     
        [Serializable]
        public class APIConfiguration
        {
            public string BaseURL;
            public APIRequestBody RequestBody;
        }

        [SerializeField] private APIConfiguration[] apiConfiguration;
        
        public string GetApiUrl(APIEnum apiEnum)
        {
            return (from config in apiConfiguration where config.RequestBody.APIType == apiEnum select config.BaseURL + config.RequestBody.BodyContent).FirstOrDefault();
        }
    }
}