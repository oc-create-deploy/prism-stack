using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;
using TMPro;

public class GameController : MonoBehaviour
{
    public AdsManager adsManager;
    [Header("UI Panels (CanvasGroups)")]
    public CanvasGroup pauseCanvas;
    public CanvasGroup loseCanvas;
    public CanvasGroup reviveCanvas;

    [Header("UI Text")]
    public TMP_Text infoText;
    public TMP_Text recordText;

    [Header("Fade Settings")]
    public float panelFadeDuration = 0.5f;

    private int points;
    private int recordPoints;
    private bool hasRevived;

    private void Awake()
    {
        points = PlayerPrefs.GetInt("Points", 0);
        recordPoints = PlayerPrefs.GetInt("RecordPoints", 0);
        hasRevived = PlayerPrefs.GetInt("AlreadyRevive", 0) == 1;

        UpdateInfoText();
        UpdateRecordText();
    }

    private void Start()
    {
        if (adsManager != null)
        {
            adsManager.OnUserEarnedReward += HandleUserEarnedReward;
        }
    }

    private void Update()
    {
        if (Input.GetKeyDown(KeyCode.P))
        {
            IncrementValue(100);
            Debug.Log($"[GameController] 'P' premut: +100 punts. Total ara: {points}");
        }
    }

    private void OnDestroy()
    {
        if (adsManager != null)
        {
            adsManager.OnUserEarnedReward -= HandleUserEarnedReward;
        }
    }

    public void SelectPanel()
    {
        if (!hasRevived)
            FadeInPanel(loseCanvas);
        else
            FadeInPanel(reviveCanvas);
    }

    public void GoSceneMenu() => LoadScene("Menu");

    public void GoSceneGame()
    {
        ResetGameState();
        LoadScene("Game");
    }

    public void GoSceneGameRevive()
    {
        hasRevived = true;
        adsManager.ShowRewardedAd();
    }

    private void HandleUserEarnedReward()
    {
        hasRevived = true;
        PlayerPrefs.SetInt("AlreadyRevive", 1);
        PlayerPrefs.Save();

        LoadScene("Game");
    }

    public void OpenPausePanel() => FadeInPanel(pauseCanvas);
    public void ClosePausePanel() => FadeOutPanel(pauseCanvas);
    public void OpenLosePanel() => FadeInPanel(loseCanvas);
    public void CloseLosePanel() => FadeOutPanel(loseCanvas);
    public void OpenRevivePanel() => FadeInPanel(reviveCanvas);
    public void CloseRevivePanel() => FadeOutPanel(reviveCanvas);

    private void FadeInPanel(CanvasGroup cg)
    {
        cg.gameObject.SetActive(true);
        StartCoroutine(FadeCanvas(cg, 0f, 1f));
    }

    private void FadeOutPanel(CanvasGroup cg)
    {
        StartCoroutine(FadeOutAndDisable(cg));
    }

    private IEnumerator FadeCanvas(CanvasGroup cg, float start, float end)
    {
        float elapsed = 0f;
        cg.interactable = false;
        cg.blocksRaycasts = false;

        while (elapsed < panelFadeDuration)
        {
            elapsed += Time.unscaledDeltaTime;
            cg.alpha = Mathf.Lerp(start, end, elapsed / panelFadeDuration);
            yield return null;
        }

        cg.alpha = end;
        cg.interactable = end > 0.5f;
        cg.blocksRaycasts = end > 0.5f;
    }

    private IEnumerator FadeOutAndDisable(CanvasGroup cg)
    {
        yield return FadeCanvas(cg, cg.alpha, 0f);
        cg.gameObject.SetActive(false);
    }

    private void UpdateInfoText()
    {
        if (infoText != null)
            infoText.text = points.ToString();
    }

    private void UpdateRecordText()
    {
        if (recordText != null)
            recordText.text = recordPoints.ToString();
    }

    public void IncrementValue(int value)
    {
        points += value;
        PlayerPrefs.SetInt("Points", points);

        if (points > recordPoints)
        {
            recordPoints = points;
            PlayerPrefs.SetInt("RecordPoints", recordPoints);
        }

        PlayerPrefs.Save();
        UpdateInfoText();
        UpdateRecordText();
    }

    public void ResetValue()
    {
        points = 0;
        PlayerPrefs.SetInt("Points", points);
        PlayerPrefs.Save();
        UpdateInfoText();
        UpdateRecordText();
    }

    private void ResetGameState()
    {
        ResetValue();
        hasRevived = false;
        PlayerPrefs.SetInt("AlreadyRevive", 0);
        PlayerPrefs.Save();
    }

    private void LoadScene(string sceneName)
    {
        SceneManager.LoadScene(sceneName);
    }
}