using System;
using Scripts.ScriptableObject;
using Scripts.UI.Background;
using UnityEngine;

namespace Scripts.UI
{
    public class UIInGame : MonoBehaviour
    {
        [SerializeField] private BackgroundHandler backgroundHandler;
        
        private void Awake()
        {
            string imagePath = "Assets/_Assets/Sprites/asl_alphabet_test/M_test.jpg";
            StartCoroutine(APIManager.Instance.CallAPI(imagePath, HandleResponse));
        }

        private void Update()
        {

        }
        private void HandleResponse(string jsonResponse)
        {
            Debug.Log("Response: " + jsonResponse);
        }
        
        private void ChangeBackground(bool isIncrease)
        {
            if (isIncrease)
            {
                backgroundHandler.IncreaseBackgroundIndex();
            }
            else
            {
                backgroundHandler.DecreaseBackgroundIndex();
            }
        }
        
    }
}
