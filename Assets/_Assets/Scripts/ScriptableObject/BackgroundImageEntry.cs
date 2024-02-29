using UnityEngine;

namespace Scripts.ScriptableObject
{
    [CreateAssetMenu(fileName = "BackgroundImageEntry", menuName = "ScriptableObjects/BackgroundImageEntry")]
    public class BackgroundImageEntry : UnityEngine.ScriptableObject
    {
        [SerializeField] private Sprite[] sprites;

        public Sprite[] Sprites => sprites;
    }
}
