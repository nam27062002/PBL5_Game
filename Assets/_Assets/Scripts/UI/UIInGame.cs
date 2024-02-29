using System;
using Scripts.ScriptableObject;
using Scripts.UI.Background;
using UnityEngine;

namespace Scripts.UI
{
    public class UIInGame : MonoBehaviour
    {
        [SerializeField] private BackgroundHandler backgroundHandler;

        private void Update()
        {

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
