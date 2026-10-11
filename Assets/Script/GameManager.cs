using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using TMPro;
using System;
using System.Collections;

public class GameManager : MonoBehaviour
{
    public static event Action OnLanguageChanged;
    public string nextSceneStartNode = "";

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
    public GameObject menuPanel;
    public CanvasGroup menuCanvasGroup;
    public float animSpeed = 5f;

    private Coroutine menuAnimationCoroutine;
    private bool isMenuOpen = false;

    [Header("Würfel & Punkte System (DnD W20)")]
    public int currentPoints = 0;
    public int specialAnswerCost = 13;
    public TextMeshProUGUI pointsDisplayText;

    [Header("Points Pop-Up Visuals")]
    public TextMeshProUGUI pointPopUpText;
    public Color gainColor = Color.green;
    public Color spendColor = Color.red;
    private Coroutine popUpCoroutine;

    [Header("Dice UI Visuals")]
    public UnityEngine.UI.Image diceImageDisplay;
    public Sprite[] diceSprites;

    [Header("Choice & UI Elements")]
    public GameObject choicePanel;
    public GameObject diceButton;
    public GameObject standardAnswerButton;
    public GameObject specialAnswerButton;
    public StoryManager storyManager;

    [Header("Story Progress")]
    public string currentDayKey = "shop_1";

    private bool isRolling = false;
    private bool hasRolled = false; // Hier deklarieren

    private void Awake()
    {
        int numGameManagers = FindObjectsByType<GameManager>(FindObjectsSortMode.None).Length;
        if (numGameManagers > 1)
        {
            Destroy(gameObject);
            return;
        }

        DontDestroyOnLoad(gameObject);
    }

    private void OnEnable()
    {
        SceneManager.sceneLoaded += OnSceneLoaded;
    }

    private void OnDisable()
    {
        SceneManager.sceneLoaded -= OnSceneLoaded;
    }

    private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        RebindSceneUI();
    }

    private void Start()
    {
        // 1. Prüfen, ob der Spieler bereits Startpunkte erhalten hat
        if (PlayerPrefs.GetInt("HasReceivedStartPoints", 0) == 0)
        {
            currentPoints = 10; // Startguthaben vergeben
            PlayerPrefs.SetInt("HasReceivedStartPoints", 1);
            PlayerPrefs.Save();
        }

        currentSizeIndex = PlayerPrefs.GetInt("FontSizeIndex", 1);
        UpdateFontSizeUI();

        currentResIndex = PlayerPrefs.GetInt("ResolutionIndex", 2);
        ApplyResolution();

        UpdateLanguageUI();
        LoadUsername();

        UpdatePointsUI(); // Zeigt die 10 Punkte sofort oben in der UI an

        if (pointPopUpText != null)
        {
            pointPopUpText.gameObject.SetActive(false);
        }

        if (storyManager == null)
        {
            storyManager = FindAnyObjectByType<StoryManager>();
        }

        if (menuPanel != null)
        {
            menuPanel.SetActive(false);
        }

        RebindSceneUI();
    }

    private void RebindSceneUI()
    {
        choicePanel = null;
        diceButton = null;
        standardAnswerButton = null;
        specialAnswerButton = null;
        pointsDisplayText = null;
        pointPopUpText = null;

        // 1. Alle GameObjects in der aktiven Szene durchsuchen (inklusive inaktiver)
        foreach (GameObject obj in Resources.FindObjectsOfTypeAll<GameObject>())
        {
            if (obj.scene.name == SceneManager.GetActiveScene().name)
            {
                if (obj.name == "ChoicePanel") choicePanel = obj;
                if (obj.name == "DiceButton")
                {
                    diceButton = obj;
                    diceImageDisplay = diceButton.GetComponent<UnityEngine.UI.Image>();
                }
                if (obj.name == "AnswerButton1") standardAnswerButton = obj;
                if (obj.name == "AnswerButton2") specialAnswerButton = obj;
            }
        }

        // 2. TextMeshPro-Komponenten direkt über die Szene finden, unabhängig vom exakten GameObject-Namen
        TextMeshProUGUI[] allTexts = Resources.FindObjectsOfTypeAll<TextMeshProUGUI>();
        foreach (TextMeshProUGUI txt in allTexts)
        {
            if (txt.gameObject.scene.name == SceneManager.GetActiveScene().name)
            {
                if (txt.gameObject.name.Contains("PermanentPoint") || txt.gameObject.name.Contains("Point"))
                {
                    if (txt.gameObject.name.Contains("PopUp"))
                    {
                        pointPopUpText = txt;
                        pointPopUpText.gameObject.SetActive(false);
                    }
                    else
                    {
                        pointsDisplayText = txt;
                    }
                }
            }
        }

        // 3. Klick-Listener für den Würfel neu binden
        if (diceButton != null)
        {
            Button btn = diceButton.GetComponent<Button>();
            if (btn != null)
            {
                btn.onClick.AddListener(() => RollW20());
            }
        }

        // 4. Klick-Listener für die Antwort-Buttons neu binden
        if (standardAnswerButton != null)
        {
            Button btn = standardAnswerButton.GetComponent<Button>();
            if (btn != null)
            {
                btn.onClick.AddListener(() => OnAnswerSelected(false));
            }
        }

        if (specialAnswerButton != null)
        {
            Button btn = specialAnswerButton.GetComponent<Button>();
            if (btn != null)
            {
                btn.onClick.AddListener(() => OnAnswerSelected(true));
            }
        }

        UpdatePointsUI();
    }

    #region Scene Navigation
    public void OpenHomescreen() => SceneManager.LoadScene("Homescreen");
    public void OpenSettings() => SceneManager.LoadScene("Settings");
    public void OpenCredits() => SceneManager.LoadScene("Credits");

    public void StartGame()
    {
        SceneManager.LoadScene("ApartmentScene");
    }

    public void QuitGame()
    {
        Debug.Log("Spiel wird beendet...");
        Application.Quit();
    }
    #endregion

    #region Menu Animation Logic
    public void ToggleMenu()
    {
        if (menuPanel == null) return;

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
            menuPanel.transform.localScale = Vector3.Lerp(startScale, targetScale, t);

            if (menuCanvasGroup != null)
            {
                menuCanvasGroup.alpha = Mathf.Lerp(startAlpha, targetAlpha, t);
            }

            yield return null;
        }

        menuPanel.transform.localScale = targetScale;
        if (menuCanvasGroup != null) menuCanvasGroup.alpha = targetAlpha;

        if (disableAtEnd)
        {
            menuPanel.SetActive(false);
        }
    }
    #endregion

    #region Dice & Choice System
    public void SetupAnswerButtonTexts(string standardText, string specialText)
    {
        if (standardAnswerButton != null)
        {
            TextMeshProUGUI tmp = standardAnswerButton.GetComponentInChildren<TextMeshProUGUI>();
            if (tmp != null) tmp.text = standardText;
        }

        if (specialAnswerButton != null)
        {
            TextMeshProUGUI tmp = specialAnswerButton.GetComponentInChildren<TextMeshProUGUI>();
            if (tmp != null) tmp.text = specialText;
        }
    }

    public void ShowChoiceUI()
    {
        if (choicePanel == null || diceButton == null)
        {
            RebindSceneUI();
        }

        // Status zurücksetzen, damit man beim nächsten Mal wieder würfeln kann
        hasRolled = false;
        isRolling = false;

        // Panels und Würfel aktivieren
        if (choicePanel != null) choicePanel.SetActive(true);
        if (diceButton != null) diceButton.SetActive(true);

        // Antworten erst nach dem Würfeln zeigen
        if (standardAnswerButton != null) standardAnswerButton.SetActive(false);
        if (specialAnswerButton != null) specialAnswerButton.SetActive(false);
    }

    public void RollW20()
    {
        if (isRolling) return;
        StartCoroutine(AnimateAndRollDice());
    }

    private IEnumerator AnimateAndRollDice()
    {
        isRolling = true;

        int finalRoll = UnityEngine.Random.Range(1, 21);

        float animationDuration = 1.0f;
        float elapsed = 0f;
        float interval = 0.05f;

        Vector3 originalScale = diceButton != null ? diceButton.transform.localScale : Vector3.one;
        Quaternion originalRotation = diceButton != null ? diceButton.transform.localRotation : Quaternion.identity;

        while (elapsed < animationDuration)
        {
            elapsed += interval;

            if (diceSprites != null && diceSprites.Length >= 20 && diceImageDisplay != null)
            {
                int randomSpriteIndex = UnityEngine.Random.Range(0, diceSprites.Length);
                diceImageDisplay.sprite = diceSprites[randomSpriteIndex];
            }

            if (diceButton != null)
            {
                float randomAngle = UnityEngine.Random.Range(-15f, 15f);
                diceButton.transform.localRotation = Quaternion.Euler(0, 0, randomAngle);

                float scalePulse = 1f + UnityEngine.Random.Range(-0.08f, 0.08f);
                diceButton.transform.localScale = originalScale * scalePulse;
            }

            interval = Mathf.Lerp(0.05f, 0.15f, elapsed / animationDuration);
            yield return new WaitForSeconds(interval);
        }

        if (diceButton != null)
        {
            diceButton.transform.localRotation = originalRotation;
            diceButton.transform.localScale = originalScale;
        }

        if (diceImageDisplay != null && diceSprites != null && diceSprites.Length >= 20)
        {
            diceImageDisplay.sprite = diceSprites[finalRoll - 1];
        }

        currentPoints += finalRoll;
        Debug.Log($"Final Gewürfelt: {finalRoll} | Gesamte Punkte: {currentPoints}");
        UpdatePointsUI();

        ShowPointPopUp($"+{finalRoll} Punkte", gainColor);

        yield return new WaitForSeconds(0.5f);

        if (standardAnswerButton != null) standardAnswerButton.SetActive(true);

        if (specialAnswerButton != null)
        {
            specialAnswerButton.SetActive(true);

            Button btn = specialAnswerButton.GetComponent<Button>();
            if (btn != null)
            {
                btn.interactable = CanChooseSpecialAnswer();
            }
        }

        isRolling = false;
    }

    public void OnAnswerSelected(bool isSpecial)
    {
        // 1. Choice-Panel sofort wieder schließen
        if (choicePanel != null) choicePanel.SetActive(false);

        // 2. Punkte abziehen, wenn die Spezial-Antwort gewählt wurde
        if (isSpecial)
        {
            currentPoints -= specialAnswerCost;
            if (currentPoints < 0) currentPoints = 0; // Sicherheitshalber nicht unter 0 fallen lassen

            UpdatePointsUI();
            ShowPointPopUp($"-{specialAnswerCost} Punkte", spendColor);
        }

        // 3. StoryManager sagen, dass es im entsprechenden Pfad weitergeht
        if (storyManager == null) storyManager = FindAnyObjectByType<StoryManager>();
        if (storyManager != null)
        {
            storyManager.ResumeAfterChoice(isSpecial);
        }
    }

    public bool CanChooseSpecialAnswer() => currentPoints >= specialAnswerCost;

    public void SelectSpecialAnswer()
    {
        if (choicePanel != null) choicePanel.SetActive(false);

        StoryManager sm = FindAnyObjectByType<StoryManager>();
        if (sm != null)
        {
            sm.ResumeAfterChoice(true);
        }
    }

    public void SelectStandardAnswer()
    {
        Debug.Log("Standard-Antwort gewählt. Punkte bleiben erhalten.");

        if (choicePanel != null) choicePanel.SetActive(false);

        StoryManager sm = FindAnyObjectByType<StoryManager>();
        if (sm != null)
        {
            sm.ResumeAfterChoice(false);
        }
    }

    private void UpdatePointsUI()
    {
        if (pointsDisplayText != null)
        {
            pointsDisplayText.text = "Punkte: " + currentPoints;
        }
    }

    private void ShowPointPopUp(string text, Color color)
    {
        if (pointPopUpText == null) return;

        if (popUpCoroutine != null)
        {
            StopCoroutine(popUpCoroutine);
        }

        popUpCoroutine = StartCoroutine(AnimatePointPopUp(text, color));
    }

    private IEnumerator AnimatePointPopUp(string text, Color color)
    {
        pointPopUpText.text = text;
        pointPopUpText.color = color;
        pointPopUpText.gameObject.SetActive(true);

        RectTransform rectTransform = pointPopUpText.rectTransform;
        Vector3 startPos = rectTransform.anchoredPosition;
        Vector3 targetPos = startPos + new Vector3(0, 30f, 0);

        float duration = 1.2f;
        float elapsed = 0f;

        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float t = elapsed / duration;

            rectTransform.anchoredPosition = Vector3.Lerp(startPos, targetPos, t);

            Color c = color;
            c.a = Mathf.Lerp(1f, 0f, t);
            pointPopUpText.color = c;

            yield return null;
        }

        pointPopUpText.gameObject.SetActive(false);
        rectTransform.anchoredPosition = startPos;
    }
    #endregion

    #region Settings & Preferences
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
            fontSizeDisplayText.text = (lang == "DE") ? fontSizeNamesDE[currentSizeIndex] : fontSizeNamesEN[currentSizeIndex];
        }
    }

    public int GetCurrentFontSize() => fontSizes[PlayerPrefs.GetInt("FontSizeIndex", 1)];

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

    public static string GetUsername() => PlayerPrefs.GetString("PlayerUsername", "Spieler");
    #endregion
}