using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class LoadingScreen : MonoBehaviour
{
    public Slider progressBar;
    public string SceneName;
    public float minimumLoadingTime = 1f;

    void Start()
    {
        StartCoroutine(LoadSceneAsync(SceneName));
    }

    IEnumerator LoadSceneAsync(string sceneName)
    {
        float timer = 0f;

        AsyncOperation operation = SceneManager.LoadSceneAsync(sceneName);
        operation.allowSceneActivation = false;

        while (!operation.isDone)
        {
            timer += Time.deltaTime;

            float progress = Mathf.Clamp01(operation.progress / 0.9f);

            if(progressBar != null)
                progressBar.value = progress;

            if (operation.progress >= 0.9f &&
                timer >= minimumLoadingTime)
            {
                // ready to activate (you can wait for input here)
                operation.allowSceneActivation = true;
            }

            yield return null;
        }
        operation.allowSceneActivation = true;
    }
}
