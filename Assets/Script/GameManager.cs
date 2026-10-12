using System;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

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
    public GameObject mediumAnswerButton;
    public GameObject specialAnswerButton;
    public StoryManager storyManager;

    [ContextMenu("Reset Player Points")]
    public void ResetPlayerPrefs()
    {
        PlayerPrefs.DeleteKey("HasReceivedStartPoints");
        currentPoints = 10;
        PlayerPrefs.Save();
        UpdatePointsUI();
        Debug.Log("Punkte und Start-Flag wurden zurückgesetzt!");
    }

    [Header("Story Progress")]
    public string currentDayKey = "shop_1";

    private bool isRolling = false;
    private bool hasRolled = false;
    private List<DialogueOption> currentOptions;

    [Header("Settings UI Direct References")]
    public Button settingsBackButton;
    public Button settingsEnglishButton;
    public Button settingsGermanButton;
    public Button settingsFontSizeL;
    public Button settingsFontSizeR;
    public Button settingsResolutionL;
    public Button settingsResolutionR;

    [Header("Settings UI Extra References")]
    public TextMeshProUGUI settingsFontSizeDisplayText;
    public TextMeshProUGUI settingsResolutionDisplayText;

    [Header("Points UI Setup")]
    public GameObject pointsUIObject;

    private bool isDuplicate = false;

    private void Awake()
    {
        if (FindObjectsByType<GameManager>().Length > 1)
        {
            isDuplicate = true;
            Destroy(gameObject);
            return;
        }

        DontDestroyOnLoad(gameObject);
    }

    private void OnEnable()
    {
        if (isDuplicate) return;
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
        if (isDuplicate) return;

        if (PlayerPrefs.GetInt("HasReceivedStartPoints", 0) == 0 || currentPoints <= 0)
        {
            currentPoints = 10;
            PlayerPrefs.SetInt("HasReceivedStartPoints", 1);
            PlayerPrefs.Save();
        }

        currentSizeIndex = PlayerPrefs.GetInt("FontSizeIndex", 1);
        UpdateFontSizeUI();

        currentResIndex = PlayerPrefs.GetInt("ResolutionIndex", 2);
        ApplyResolution();

        UpdateLanguageUI();
        LoadUsername();

        hasRolled = false;
        UpdatePointsUI();

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

        PlayerPrefs.SetInt("PointsVisible", 0);
        PlayerPrefs.SetInt("HasHaruPin", 0);
        PlayerPrefs.Save();
        AffectionData.ResetAll();
        SceneManager.LoadScene("ApartmentScene");

        RebindSceneUI();
    }

    private void RebindSceneUI()
    {
        choicePanel = null;
        diceButton = null;
        standardAnswerButton = null;
        mediumAnswerButton = null;
        specialAnswerButton = null;
        pointsDisplayText = null;
        pointPopUpText = null;
        fontSizeDisplayText = null;
        resolutionDisplayText = null;
        menuPanel = null;
        menuCanvasGroup = null;
        isMenuOpen = false;

        englishHeart = null;
        germanHeart = null;

        string currentSceneName = SceneManager.GetActiveScene().name;

        foreach (GameObject obj in Resources.FindObjectsOfTypeAll<GameObject>())
        {
            if (obj.scene.name == currentSceneName)
            {
                if (obj.name == "ChoicePanel") choicePanel = obj;

                if (obj.name == "InGameMenuPanel")
                {
                    menuPanel = obj;
                    menuCanvasGroup = obj.GetComponent<CanvasGroup>();
                    obj.SetActive(false);
                }

                if (obj.name == "DiceButton")
                {
                    diceButton = obj;
                    diceImageDisplay = diceButton.GetComponent<UnityEngine.UI.Button>() != null ? diceButton.GetComponent<UnityEngine.UI.Image>() : null;
                }
                if (obj.name == "standardAnswer") standardAnswerButton = obj;
                if (obj.name == "MediumAnswer") mediumAnswerButton = obj;
                if (obj.name == "SpecialAnswer") specialAnswerButton = obj;

                if (obj.name == "EnglishHeartButton" || obj.name == "English")
                {
                    englishHeart = obj.GetComponent<Image>();
                }
                if (obj.name == "GermanHeartButton" || obj.name == "German")
                {
                    germanHeart = obj.GetComponent<Image>();
                }

                Button btn = obj.GetComponent<Button>();
                if (btn != null)
                {
                    if (obj.name == "GameStart" || obj.name == "Game Start" || obj.name == "StartButton" || obj.name == "GameStartButton")
                    {
                        btn.onClick.RemoveAllListeners();
                        btn.onClick.AddListener(() => StartGame());
                    }
                    else if (obj.name == "Settings" || obj.name == "SettingsButton")
                    {
                        btn.onClick.RemoveAllListeners();
                        btn.onClick.AddListener(() => OpenSettings());
                    }
                    else if (obj.name == "CreditsButton")
                    {
                        btn.onClick.RemoveAllListeners();
                        btn.onClick.AddListener(() => OpenCredits());
                    }
                    else if (obj.name == "BackButton")
                    {
                        btn.onClick.RemoveAllListeners();
                        btn.onClick.AddListener(() => OpenHomescreen());
                    }
                    else if (obj.name == "MenuButton")
                    {
                        btn.onClick.RemoveAllListeners();
                        btn.onClick.AddListener(() => ToggleMenu());
                    }
                    else if (obj.name == "EnglishHeartButton" || obj.name == "English")
                    {
                        btn.onClick.RemoveAllListeners();
                        btn.onClick.AddListener(() => SelectEnglish());
                    }
                    else if (obj.name == "GermanHeartButton" || obj.name == "German")
                    {
                        btn.onClick.RemoveAllListeners();
                        btn.onClick.AddListener(() => SelectGerman());
                    }
                    else if (obj.name == "FontSizeButtonL")
                    {
                        btn.onClick.RemoveAllListeners();
                        btn.onClick.AddListener(() => DecreaseFontSize());
                    }
                    else if (obj.name == "FontSizeButtonR")
                    {
                        btn.onClick.RemoveAllListeners();
                        btn.onClick.AddListener(() => IncreaseFontSize());
                    }
                    else if (obj.name == "ResolutionButtonL")
                    {
                        btn.onClick.RemoveAllListeners();
                        btn.onClick.AddListener(() => DecreaseResolution());
                    }
                    else if (obj.name == "ResolutionButtonR")
                    {
                        btn.onClick.RemoveAllListeners();
                        btn.onClick.AddListener(() => IncreaseResolution());
                    }
                    else if (obj.name == "BackToHomescreenButton")
                    {
                        btn.onClick.RemoveAllListeners();
                        btn.onClick.AddListener(() => OpenHomescreen());
                    }
                    else if (obj.name == "QuitButton")
                    {
                        btn.onClick.RemoveAllListeners();
                        btn.onClick.AddListener(() => QuitGame());
                    }
                }
            }
        }

        if (settingsBackButton != null)
        {
            settingsBackButton.onClick.RemoveAllListeners();
            settingsBackButton.onClick.AddListener(() => OpenHomescreen());
        }
        if (settingsEnglishButton != null)
        {
            settingsEnglishButton.onClick.RemoveAllListeners();
            settingsEnglishButton.onClick.AddListener(() => SelectEnglish());
        }
        if (settingsGermanButton != null)
        {
            settingsGermanButton.onClick.RemoveAllListeners();
            settingsGermanButton.onClick.AddListener(() => SelectGerman());
        }
        if (settingsFontSizeL != null)
        {
            settingsFontSizeL.onClick.RemoveAllListeners();
            settingsFontSizeL.onClick.AddListener(() => DecreaseFontSize());
        }
        if (settingsFontSizeR != null)
        {
            settingsFontSizeR.onClick.RemoveAllListeners();
            settingsFontSizeR.onClick.AddListener(() => IncreaseFontSize());
        }
        if (settingsResolutionL != null)
        {
            settingsResolutionL.onClick.RemoveAllListeners();
            settingsResolutionL.onClick.AddListener(() => DecreaseResolution());
        }
        if (settingsResolutionR != null)
        {
            settingsResolutionR.onClick.RemoveAllListeners();
            settingsResolutionR.onClick.AddListener(() => IncreaseResolution());
        }

        if (settingsFontSizeDisplayText != null) fontSizeDisplayText = settingsFontSizeDisplayText;
        if (settingsResolutionDisplayText != null) resolutionDisplayText = settingsResolutionDisplayText;

        TextMeshProUGUI[] allTexts = Resources.FindObjectsOfTypeAll<TextMeshProUGUI>();
        foreach (TextMeshProUGUI txt in allTexts)
        {
            if (txt.gameObject.scene.name == currentSceneName)
            {
                string objName = txt.gameObject.name;
                string parentName = txt.transform.parent != null ? txt.transform.parent.name : "";

                if (objName == "FontSizeValue" || parentName == "FontSizeValue")
                {
                    fontSizeDisplayText = txt;
                }
                else if (objName == "ResolutionValue" || parentName == "ResolutionValue")
                {
                    resolutionDisplayText = txt;
                }
                else if (objName.Contains("PermanentPoint") || (objName.Contains("Point") && !objName.Contains("PopUp")))
                {
                    pointsDisplayText = txt;
                }
                else if (objName.Contains("PopUp"))
                {
                    pointPopUpText = txt;
                    pointPopUpText.gameObject.SetActive(false);
                }
            }
        }

        if (diceButton != null)
        {
            Button btn = diceButton.GetComponent<Button>();
            if (btn != null)
            {
                btn.onClick.RemoveAllListeners();
                btn.onClick.AddListener(() => RollW20());
            }
        }
        Debug.Log("Rebind: menuPanel=" + (menuPanel != null) + " in Szene " + currentSceneName);
        UpdatePointsUI();
        UpdateFontSizeUI();
        UpdateResolutionUI();
        UpdateLanguageUI();
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
        Debug.Log("ToggleMenu aufgerufen, menuPanel = " + (menuPanel != null ? menuPanel.name : "NULL"));
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

        hasRolled = false;
        isRolling = false;

        if (choicePanel != null) choicePanel.SetActive(true);
        if (diceButton != null) diceButton.SetActive(true);

        if (standardAnswerButton != null) standardAnswerButton.SetActive(false);
        if (mediumAnswerButton != null) mediumAnswerButton.SetActive(false);
        if (specialAnswerButton != null) specialAnswerButton.SetActive(false);

        UpdatePointsUI();
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
        hasRolled = true;

        Debug.Log($"Final Gewürfelt: {finalRoll} | Gesamte Punkte: {currentPoints}");
        UpdatePointsUI();

        ShowPointPopUp($"+{finalRoll} Punkte", gainColor);

        yield return new WaitForSeconds(0.5f);

        if (currentOptions != null)
        {
            // Standard-Button: erste Option
            if (standardAnswerButton != null && currentOptions.Count > 0)
            {
                standardAnswerButton.SetActive(true);
                TextMeshProUGUI tmp = standardAnswerButton.GetComponentInChildren<TextMeshProUGUI>();
                if (tmp != null) tmp.text = currentOptions[0].text;

                Button btn = standardAnswerButton.GetComponent<Button>();
                if (btn != null)
                {
                    btn.onClick.RemoveAllListeners();
                    var opt = currentOptions[0];
                    btn.onClick.AddListener(() => OnAnswerOptionSelected(opt));
                    btn.interactable = currentPoints >= opt.cost;
                }
            }

            // Medium-Button: zweite Option (nur bei 3 Optionen)
            if (mediumAnswerButton != null)
            {
                if (currentOptions.Count >= 3)
                {
                    mediumAnswerButton.SetActive(true);
                    TextMeshProUGUI tmp = mediumAnswerButton.GetComponentInChildren<TextMeshProUGUI>();
                    if (tmp != null) tmp.text = currentOptions[1].text;

                    Button btn = mediumAnswerButton.GetComponent<Button>();
                    if (btn != null)
                    {
                        btn.onClick.RemoveAllListeners();
                        var opt = currentOptions[1];
                        btn.onClick.AddListener(() => OnAnswerOptionSelected(opt));
                        btn.interactable = currentPoints >= opt.cost;
                    }
                }
                else
                {
                    mediumAnswerButton.SetActive(false);
                }
            }

            // Special-Button: letzte Option
            if (specialAnswerButton != null)
            {
                int specialIndex = (currentOptions.Count >= 3) ? 2 : (currentOptions.Count == 2 ? 1 : -1);

                if (specialIndex >= 0 && specialIndex < currentOptions.Count)
                {
                    specialAnswerButton.SetActive(true);
                    TextMeshProUGUI tmp = specialAnswerButton.GetComponentInChildren<TextMeshProUGUI>();
                    if (tmp != null) tmp.text = currentOptions[specialIndex].text;

                    Button btn = specialAnswerButton.GetComponent<Button>();
                    if (btn != null)
                    {
                        btn.onClick.RemoveAllListeners();
                        var opt = currentOptions[specialIndex];
                        btn.onClick.AddListener(() => OnAnswerOptionSelected(opt));
                        btn.interactable = currentPoints >= opt.cost;
                    }
                }
                else
                {
                    specialAnswerButton.SetActive(false);
                }
            }
        }

        isRolling = false;
    }

    public void SetupChoiceButtons(List<DialogueOption> options)
    {
        currentOptions = options;

        if (standardAnswerButton != null) standardAnswerButton.SetActive(false);
        if (mediumAnswerButton != null) mediumAnswerButton.SetActive(false);
        if (specialAnswerButton != null) specialAnswerButton.SetActive(false);
    }

    public void SelectStandardAnswer()
    {
        if (choicePanel != null) choicePanel.SetActive(false);

        if (storyManager == null) storyManager = FindAnyObjectByType<StoryManager>();
        if (storyManager != null && storyManager.currentNode != null && storyManager.currentNode.options.Count > 0)
        {
            string nextId = storyManager.currentNode.options[0].nextId;
            storyManager.DisplayNode(nextId);
        }
    }

    public void SelectMediumAnswer()
    {
        if (choicePanel != null) choicePanel.SetActive(false);

        if (storyManager == null) storyManager = FindAnyObjectByType<StoryManager>();
        if (storyManager != null && storyManager.currentNode != null && storyManager.currentNode.options.Count > 1)
        {
            string nextId = storyManager.currentNode.options[1].nextId;
            storyManager.DisplayNode(nextId);
        }
    }

    public void SelectSpecialAnswer()
    {
        if (choicePanel != null) choicePanel.SetActive(false);

        if (storyManager == null) storyManager = FindAnyObjectByType<StoryManager>();
        if (storyManager != null && storyManager.currentNode != null)
        {
            int optionIndex = storyManager.currentNode.options.Count >= 3 ? 2 : 1;
            if (storyManager.currentNode.options.Count > optionIndex)
            {
                string nextId = storyManager.currentNode.options[optionIndex].nextId;
                storyManager.DisplayNode(nextId);
            }
        }
    }

    // Neue Methode: wendet Affection an und ruft dann die alte Methode auf
    public void OnAnswerOptionSelected(DialogueOption opt)
    {
        AffectionData.Add(opt.affectionTarget, opt.affectionChange);
        OnAnswerOptionSelected(opt.cost, opt.nextId);
    }

    public void OnAnswerOptionSelected(int cost, string nextId)
    {
        if (choicePanel != null) choicePanel.SetActive(false);

        if (cost > 0)
        {
            currentPoints -= cost;
            if (currentPoints < 0) currentPoints = 0;
            UpdatePointsUI();
            ShowPointPopUp($"-{cost} Punkte", spendColor);
        }

        if (storyManager == null) storyManager = FindAnyObjectByType<StoryManager>();
        if (storyManager != null)
        {
            storyManager.DisplayNode(nextId);
        }
    }

    public bool CanChooseSpecialAnswer() => currentPoints >= specialAnswerCost;

    private void UpdatePointsUI()
    {
        if (pointsDisplayText != null)
        {
            pointsDisplayText.text = "Punkte: " + currentPoints;
        }

        // Steuert die Sichtbarkeit des gesamten Punktesystems basierend darauf, ob gewürfelt wurde
        if (pointsUIObject != null)
        {
            pointsUIObject.SetActive(hasRolled);
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
        Debug.Log("FontSize +");
        if (currentSizeIndex < fontSizes.Length - 1)
        {
            currentSizeIndex++;
            SaveAndApplyFontSize();
        }
    }

    public void DecreaseFontSize()
    {
        Debug.Log("FontSize -");
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
        Debug.Log("Resolution +");
        if (currentResIndex < widths.Length - 1)
        {
            currentResIndex++;
            SaveAndApplyResolution();
        }
    }

    public void DecreaseResolution()
    {
        Debug.Log("Resolution -");
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