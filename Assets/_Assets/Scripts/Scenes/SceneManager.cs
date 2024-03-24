using UnityEngine.SceneManagement;
namespace Scripts.Scenes
{
    public class SceneManager
    {
        public enum SceneName
        {
            SplashLoaderScene,
            MainMenu
        }

        public static void LoadScene(SceneName sceneName, LoadSceneMode loadSceneMode=LoadSceneMode.Single)
        {
            UnityEngine.SceneManagement.SceneManager.LoadScene(sceneName.ToString(),loadSceneMode);
        }
    }
}