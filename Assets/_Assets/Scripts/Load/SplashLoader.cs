using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using SceneManager = Scripts.Scenes.SceneManager;

namespace Scripts.Load
{
    public class SplashLoader : MonoBehaviour
    {
        private float _alphaFade = 1.0f;
        private bool _fadeOut;
        [SerializeField] private Camera splashCam;
        [SerializeField] private RawImage imgBackground;
		
        private void Start()
        {
            Debug.Log("Starting SplashLoader...");
            StartCoroutine(LoadNextScene());
        }

        private void Update()
        {
            if (!_fadeOut || !(_alphaFade > 0.0f)) return;
            _alphaFade -= Mathf.Min(Time.deltaTime, 0.05f);
            imgBackground.color = new Color(imgBackground.color.r, imgBackground.color.g, imgBackground.color.b, _alphaFade);
        }

        private IEnumerator LoadNextScene()
        {
            Debug.Log("Waiting before loading next scene...");
            yield return new WaitForSeconds(1);
            Debug.Log("Loading next scene...");
            SceneManager.LoadScene(SceneManager.SceneName.MainMenu, LoadSceneMode.Additive);
            _fadeOut = true;
            while (_alphaFade > 0.0f)
            {
                yield return new WaitForSeconds(0.1f);
            }
            useGUILayout = false;
            Destroy(this);
            DestroyEverything();
            Resources.UnloadUnusedAssets();
            Debug.Log("Finished loading next scene and cleaning up.");
        }
	
        private void DestroyEverything()
        {
            Debug.Log("Destroying objects...");
            Destroy(imgBackground.gameObject);
            Destroy(splashCam.gameObject);
            Debug.Log("Objects destroyed.");
        }
    }
}