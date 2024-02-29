using Scripts.Initialize;
using Scripts.ScriptableObject;
using UnityEngine;
using UnityEngine.UI;

namespace Scripts.UI.Background
{
    public class BackgroundHandler : ScopedSingletonMonoBehaviour<BackgroundHandler>
    {
        [SerializeField] private BackgroundImageEntry backgroundImageEntry;
        [SerializeField] private Image backgroundImage;
        [SerializeField]private int backgroundIndex = 0;

        protected override void Awake()
        {
            base.Awake();
            SetBackground(backgroundIndex);
        }

        private void SetBackground(int index)
        {
            if (backgroundImageEntry == null || backgroundImageEntry.Sprites == null || backgroundImageEntry.Sprites.Length == 0)
            {
                Debug.LogError("Background image entry or sprites are missing.");
                return;
            }

            index = Mathf.Clamp(index, 0, backgroundImageEntry.Sprites.Length - 1);
            backgroundImage.sprite = backgroundImageEntry.Sprites[index];
            backgroundIndex = index;
        }

        public void IncreaseBackgroundIndex()
        {
            backgroundIndex++;
            if (backgroundIndex >= backgroundImageEntry.Sprites.Length)
                backgroundIndex = 0;
            SetBackground(backgroundIndex);
        }

        public void DecreaseBackgroundIndex()
        {
            backgroundIndex--;
            if (backgroundIndex < 0)
                backgroundIndex = backgroundImageEntry.Sprites.Length - 1;
            SetBackground(backgroundIndex);
        }
    }
}