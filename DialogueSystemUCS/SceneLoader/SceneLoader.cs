using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;

public class SceneLoader : MonoBehaviour
{
    public static SceneLoader Ins;
    [HideInInspector] public bool IsLoading;
    [HideInInspector] public float AsyncProgress;

    [SerializeField] private RawImage _colorBoard;

    void Awake()
    {
        if (Ins == null)
        {
            Ins = this;
            DontDestroyOnLoad(Ins);
        }
        else
            Destroy(this);
    }

    public IEnumerator SceneLoad(string sceneName, Color fadeColor, float duration, bool BGMFadeout = false)
    {
        if (IsLoading)
        {
            Debug.LogError("You cannot load multiple scenes at same time.");
            yield break;
        }

        // Prevent mouse from interacting with UI.
        _colorBoard.raycastTarget = true;   // MouseFilterEnum.Stop
        IsLoading = true;

        if (BGMFadeout)
            StartCoroutine(SoundManager.Ins.AudioFadeOut(SoundManager.SoundChannel.BGM, duration));

        // Fade in
        float timeElapsed = 0f, halfDuration = duration / 2;
        while (timeElapsed < halfDuration)
        {
            _colorBoard.color = new Color(fadeColor.r, fadeColor.g, fadeColor.b, Mathf.Lerp(0, 1, timeElapsed / halfDuration));
            timeElapsed += Time.deltaTime;
            yield return null;
        }

        SceneManager.LoadScene(sceneName);

        // Fade out
        timeElapsed = 0f;
        while (timeElapsed < halfDuration)
        {
            _colorBoard.color = new Color(fadeColor.r, fadeColor.g, fadeColor.b, Mathf.Lerp(1, 0, timeElapsed / halfDuration));
            timeElapsed += Time.deltaTime;
            yield return null;
        }

        _colorBoard.raycastTarget = false;   // MouseFilterEnum.Ignore
        IsLoading = false;
    }
    public IEnumerator SceneLoadAsync(string sceneName, Color fadeColor, float duration, bool BGMFadeout = false)
    {
        if (IsLoading)
        {
            Debug.LogError("You cannot load multiple scenes at same time.");
            yield break;
        }

        // Prevent mouse from interacting with UI.
        _colorBoard.raycastTarget = true;   // MouseFilterEnum.Stop
        IsLoading = true;

        if (BGMFadeout)
            StartCoroutine(SoundManager.Ins.AudioFadeOut(SoundManager.SoundChannel.BGM, duration));

        // Fade in
        float timeElapsed = 0f, halfDuration = duration / 2;
        while (timeElapsed < halfDuration)
        {
            _colorBoard.color = new Color(fadeColor.r, fadeColor.g, fadeColor.b, Mathf.Lerp(0, 1, timeElapsed / halfDuration));
            timeElapsed += Time.deltaTime;
            yield return null;
        }

        AsyncOperation sceneAsync = SceneManager.LoadSceneAsync(sceneName);
        while (!sceneAsync.isDone)
        {
            AsyncProgress = sceneAsync.progress;
            yield return null;
        }

        // Fade out
        timeElapsed = 0f;
        while (timeElapsed < halfDuration)
        {
            _colorBoard.color = new Color(fadeColor.r, fadeColor.g, fadeColor.b, Mathf.Lerp(1, 0, timeElapsed / halfDuration));
            timeElapsed += Time.deltaTime;
            yield return null;
        }

        _colorBoard.raycastTarget = false;   // MouseFilterEnum.Ignore
        IsLoading = false;
    }

    public IEnumerator ColorBoardFadeIn(Color fadeColor, float duration)
    {
        float timeElapsed = 0f;
        while (timeElapsed < duration)
        {
            _colorBoard.color = new Color(fadeColor.r, fadeColor.g, fadeColor.b, Mathf.Lerp(0, 1, timeElapsed / duration));
            timeElapsed += Time.deltaTime;
            yield return null;
        }
    }
    public IEnumerator ColorBoardFadeIn(float duration)
    {
        float timeElapsed = 0f;
        while (timeElapsed < duration)
        {
            _colorBoard.color = new Color(_colorBoard.color.r, _colorBoard.color.g, _colorBoard.color.b, Mathf.Lerp(1, 0, timeElapsed / duration));
            timeElapsed += Time.deltaTime;
            yield return null;
        }
    }
}
