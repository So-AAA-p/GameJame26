using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using TMPro;
using System;
using System.Collections;

public class GameManager : MonoBehaviour
{
    public static event Action OnLanguageChanged;

    [Header("Language Settings")]
    public Image englishHeart;
    public Image germanHeart;
    public Color activeColor = Color.red;
    public Color inactiveColor = Color.white;

    [Header("Username Settings")]
    public TMP_InputField usernameInput;

    [Header("Font Size Settings")]
    public TextMeshProUGUI fontSizeDisplayText;
    private int[] fontSizes = { 24, 32, 40 };
    private string[] fontSizeNamesEN = { "Small", "Medium", "Large" };
    private string[] fontSizeNamesDE = { "Klein", "Mittel", "Groß" };
    private int currentSizeIndex = 1;

    [Header("Resolution Settings")]
    public TextMeshProUGUI resolutionDisplayText;
    private int[] widths = { 1280, 1600, 1920, 2560 };
    private int[] heights = { 720, 900, 1080, 1440 };
    private int currentResIndex = 2;

    [Header("In-Game Menu & Animation")]
    public GameObject menuPanel; // Dein InGameMenuPanel
    public CanvasGroup menuCanvasGroup; // CanvasGroup auf dem InGameMenuPanel
    public float animSpeed = 5f;

    private Coroutine menuAnimationCoroutine;
    private bool isMenuOpen = false;

    [Header("Würfel & Punkte System (DnD W20)")]
    public int currentPoints = 0; // Aktueller Punktestand
    public int specialAnswerCost = 13; // Kosten/Schwelle für Spezial-Antwort
    public TextMeshProUGUI pointsDisplayText;

    [Header("Dice UI Visuals")]
    public UnityEngine.UI.Image diceImageDisplay; // Das Image-Objekt auf deinem Canvas
    public Sprite[] diceSprites;
    private void Start()
    {
        currentSizeIndex = PlayerPrefs.GetInt("FontSizeIndex", 1);
        UpdateFontSizeUI();

        currentResIndex = PlayerPrefs.GetInt("ResolutionIndex", 2);
        ApplyResolution();

        UpdateLanguageUI();
        LoadUsername();

        if (menuPanel != null)
        {
            menuPanel.SetActive(false);
        }
    }
    public void OpenHomescreen()
    {
        SceneManager.LoadScene("Homescreen");
    }

    public void OpenSettings()
    {
        SceneManager.LoadScene("Settings");
    }

    public void OpenCredits()
    {
        SceneManager.LoadScene("Credits");
    }

    public void StartGame()
    {
        SceneManager.LoadScene("GameScene");
    }

    public void QuitGame()
    {
        Debug.Log("Spiel wird beendet...");
        Application.Quit();
    }

    public void ToggleMenu()
    {
        if (menuPanel == null) return;

        // Falls bereits eine Animation läuft, wird sie gestoppt
        if (menuAnimationCoroutine != null)
        {
            StopCoroutine(menuAnimationCoroutine);
        }

        isMenuOpen = !isMenuOpen;

        if (isMenuOpen)
        {
            menuPanel.SetActive(true);
            menuAnimationCoroutine = StartCoroutine(AnimateMenu(Vector3.zero, Vector3.one, 0f, 1f, false));
        }
        else
        {
            float currentAlpha = menuCanvasGroup != null ? menuCanvasGroup.alpha : 1f;
            menuAnimationCoroutine = StartCoroutine(AnimateMenu(menuPanel.transform.localScale, Vector3.zero, currentAlpha, 0f, true));
        }
    }

    private IEnumerator AnimateMenu(Vector3 startScale, Vector3 targetScale, float startAlpha, float targetAlpha, bool disableAtEnd)
    {
        float t = 0f;

        menuPanel.transform.localScale = startScale;
        if (menuCanvasGroup != null) menuCanvasGroup.alpha = startAlpha;

        while (t < 1f)
        {
            t += Time.deltaTime * animSpeed;

            // Sanftes Vergrößern / Verkleinern
            menuPanel.transform.localScale = Vector3.Lerp(startScale, targetScale, t);

            // Sanftes Ein- / Ausblenden
            if (menuCanvasGroup != null)
            {
                menuCanvasGroup.alpha = Mathf.Lerp(startAlpha, targetAlpha, t);
            }

            yield return null;
        }

        menuPanel.transform.localScale = targetScale;
        if (menuCanvasGroup != null) menuCanvasGroup.alpha = targetAlpha;

        // Deaktiviert das Panel komplett, sobald das Einkleinern fertig ist
        if (disableAtEnd)
        {
            menuPanel.SetActive(false);
        }
    }
    public void RollW20()
    {
        int roll = UnityEngine.Random.Range(1, 21); // Random 1 bis 20
        currentPoints += roll;
        Debug.Log($"Gewürfelt: {roll} | Gesamte Gesprächspunkte: {currentPoints}");

        // Würfel-Bild im UI aktualisieren (falls Sprite-Array vorhanden)
        if (diceImageDisplay != null && diceSprites != null && diceSprites.Length >= 20)
        {
            diceImageDisplay.sprite = diceSprites[roll - 1];
        }

        UpdatePointsUI();
    }
    public bool CanChooseSpecialAnswer()
    {
        return currentPoints >= specialAnswerCost;
    }

    // 3. Aufrufen, wenn Spezial-Antwort gedrückt wird
    public void SelectSpecialAnswer()
    {
        if (CanChooseSpecialAnswer())
        {
            currentPoints -= specialAnswerCost;
            Debug.Log($"Spezial-Antwort gewählt! Verbleibende Punkte: {currentPoints}");
            UpdatePointsUI();
        }
    }

    // 4. Aufrufen, wenn Standard-Antwort gedrückt wird (kostenlos)
    public void SelectStandardAnswer()
    {
        Debug.Log("Standard-Antwort gewählt. Punkte bleiben erhalten.");
    }

    // 5. Punkte-Anzeige auf dem UI aktualisieren
    private void UpdatePointsUI()
    {
        if (pointsDisplayText != null)
        {
            pointsDisplayText.text = "Punkte: " + currentPoints;
        }
    }
    public void IncreaseFontSize()
    {
        if (currentSizeIndex < fontSizes.Length - 1)
        {
            currentSizeIndex++;
            SaveAndApplyFontSize();
        }
    }

    public void DecreaseFontSize()
    {
        if (currentSizeIndex > 0)
        {
            currentSizeIndex--;
            SaveAndApplyFontSize();
        }
    }

    private void SaveAndApplyFontSize()
    {
        PlayerPrefs.SetInt("FontSizeIndex", currentSizeIndex);
        PlayerPrefs.Save();
        UpdateFontSizeUI();
    }

    private void UpdateFontSizeUI()
    {
        if (fontSizeDisplayText != null)
        {
            string lang = PlayerPrefs.GetString("Language", "EN");

            if (lang == "DE")
            {
                fontSizeDisplayText.text = fontSizeNamesDE[currentSizeIndex];
            }
            else
            {
                fontSizeDisplayText.text = fontSizeNamesEN[currentSizeIndex];
            }
        }
    }
    public int GetCurrentFontSize()
    {
        return fontSizes[PlayerPrefs.GetInt("FontSizeIndex", 1)];
    }


    public void IncreaseResolution()
    {
        if (currentResIndex < widths.Length - 1)
        {
            currentResIndex++;
            SaveAndApplyResolution();
        }
    }

    public void DecreaseResolution()
    {
        if (currentResIndex > 0)
        {
            currentResIndex--;
            SaveAndApplyResolution();
        }
    }

    private void SaveAndApplyResolution()
    {
        PlayerPrefs.SetInt("ResolutionIndex", currentResIndex);
        PlayerPrefs.Save();
        ApplyResolution();
    }

    private void ApplyResolution()
    {
        Screen.SetResolution(widths[currentResIndex], heights[currentResIndex], Screen.fullScreen);
        UpdateResolutionUI();
    }

    private void UpdateResolutionUI()
    {
        if (resolutionDisplayText != null)
        {
            resolutionDisplayText.text = widths[currentResIndex] + " x " + heights[currentResIndex];
        }
    }

    public void SelectEnglish()
    {
        PlayerPrefs.SetString("Language", "EN");
        PlayerPrefs.Save();
        UpdateLanguageUI();
        UpdateFontSizeUI();
        OnLanguageChanged?.Invoke();
    }

    public void SelectGerman()
    {
        PlayerPrefs.SetString("Language", "DE");
        PlayerPrefs.Save();
        UpdateLanguageUI();
        UpdateFontSizeUI();
        OnLanguageChanged?.Invoke();
    }

    private void UpdateLanguageUI()
    {
        string currentLang = PlayerPrefs.GetString("Language", "EN");

        if (englishHeart != null && germanHeart != null)
        {
            if (currentLang == "DE")
            {
                germanHeart.color = activeColor;
                englishHeart.color = inactiveColor;
            }
            else
            {
                englishHeart.color = activeColor;
                germanHeart.color = inactiveColor;
            }
        }
    }
    private void LoadUsername()
    {
        if (usernameInput != null)
        {
            string savedName = PlayerPrefs.GetString("PlayerUsername", "");
            usernameInput.text = savedName;

            usernameInput.onValueChanged.AddListener(SaveUsername);
        }
    }
    public void SaveUsername(string newName)
    {
        PlayerPrefs.SetString("PlayerUsername", newName);
        PlayerPrefs.Save();
    }

    public static string GetUsername()
    {
        return PlayerPrefs.GetString("PlayerUsername", "Spieler");
    }
}
