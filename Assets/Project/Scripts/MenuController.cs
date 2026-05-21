using UnityEngine;
using UnityEngine.SceneManagement;
#if UNITY_EDITOR
using UnityEditor;
#endif

public class MenuController : MonoBehaviour
{
    public void GoSceneGame()
    {
        PlayerPrefs.SetInt("Points", 0);
        PlayerPrefs.SetInt("AlreadyRevive", 0);
        PlayerPrefs.Save();

        SceneManager.LoadScene("Game");
    }

    public void QuitApp()
    {
    #if UNITY_EDITOR
        EditorApplication.isPlaying = false;
    #else
        Application.Quit();
    #endif
    }

    void Update()
    {
        if (Application.platform == RuntimePlatform.Android &&
            Input.GetKeyDown(KeyCode.Escape))
        {
            QuitApp();
        }
    }
}