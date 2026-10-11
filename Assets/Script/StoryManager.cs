using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

[System.Serializable]
public class DialogueOption
{
    public string text;
    public int cost;
    public string nextId;
}

[System.Serializable]
public class DialogueNode
{
    public string id;
    public string text;
    public bool isChoice;
    public bool isNameInput;
    public bool useMCBubble;
    public bool isNarration;
    public bool useIntroPanel;
    public bool isThought;
    public bool showElderlyLadys;
    public bool showCustomer;
    public bool showLoveInterestH;
    public bool showLoveInterestV;
    public bool showMafiaMen;
    public bool useSideCharacterBubble;
    public bool removeBlur;
    public bool clearBeforeTyping;
    public string loadScene;
    public List<DialogueOption> options;
    public string nextId;
    public bool fadeToBlack;
    public string overrideStartNode;
    public bool showPoints;

    // Flag-Felder für Weichen & Speicherwerte:
    public string resetFlag;
    public string setFlag;
    public string checkFlag;
    public string flagTrueNextId;
    public string flagFalseNextId;
}

[System.Serializable]
public class StoryData
{
    public string startNodeId;
    public List<DialogueNode> nodes;
}

public class StoryManager : MonoBehaviour
{
    [Header("UI References - Narrative / Intro")]
    public GameObject introBubblePanel;
    public TextMeshProUGUI introText;

    [Header("UI References - Narrator (Scene Descriptions)")]
    public GameObject narratorBubblePanel;
    public TextMeshProUGUI narratorText;

    [Header("UI References - Main Dialogue (MC)")]
    public GameObject speechBubblePanel;
    public TextMeshProUGUI dialogueText;

    [Header("UI References - Side Characters (Elderly Ladies)")]
    public GameObject sideCharacterBubblePanel;
    public TextMeshProUGUI sideCharacterText;

    public Image backgroundImage;
    public GameManager gameManager;

    [Header("Name Input UI")]
    public GameObject nameInputPanel;
    public TMP_InputField nameInputField;
    public Button nameSubmitButton;

    [Header("Typewriter Settings")]
    public float typingSpeed = 0.05f;

    [Header("Dialogue File")]
    public TextAsset dialogueJsonFile;

    [Header("Fade UI")]
    public GameObject blackScreenPanel;

    [Header("UI References - Character Visuals")]
    public GameObject elderlyLadysCharacterImage;
    public GameObject loveInterestCharacterImage;
    public GameObject customerCharacterImage;
    public GameObject mafiaMenImage;
    public GameObject vinkCharacterImage;

    [Header("UI References - Points System")]
    public GameObject pointsPanel;

    private Dictionary<string, DialogueNode> nodeMap = new Dictionary<string, DialogueNode>();
    public DialogueNode currentNode;
    private TextMeshProUGUI currentActiveTextComponent;

    private Coroutine typingCoroutine;
    private Coroutine blurCoroutine;

    private bool isTyping = false;
    private bool isWaitingForChoice = false;
    private bool isWaitingForNameInput = false;
    private bool isPointsPanelVisible = false;
    private string formattedCurrentText;

    private void Start()
    {
        if (gameManager == null)
        {
            gameManager = FindAnyObjectByType<GameManager>();
        }

        if (nameInputPanel != null) nameInputPanel.SetActive(false);

        if (nameSubmitButton != null)
        {
            nameSubmitButton.onClick.AddListener(OnNameSubmitted);
        }

        if (introBubblePanel != null) introBubblePanel.SetActive(true);
        if (speechBubblePanel != null) speechBubblePanel.SetActive(false);
        if (sideCharacterBubblePanel != null) sideCharacterBubblePanel.SetActive(false);

        currentActiveTextComponent = introText;

        if (dialogueJsonFile != null)
        {
            LoadStoryFromJson(dialogueJsonFile.text);
        }
    }

    private void Update()
    {
        // Blockiert Klicks zum Weiterblättern, wenn eine Choice oder Namenseingabe aktiv ist:
        if (isWaitingForChoice || isWaitingForNameInput || (currentNode != null && currentNode.isChoice)) return;

        if (Input.GetMouseButtonDown(0) || Input.GetKeyDown(KeyCode.Space))
        {
            AdvanceDialogue();
        }


        if (Input.GetKeyDown(KeyCode.R))
        {
            PlayerPrefs.SetInt("HasHaruPin", 0);
            PlayerPrefs.Save();
            Debug.Log("Pin manuell entfernt!");
        }
    }

    public void LoadStoryFromJson(string jsonText)
    {
        StoryData storyData = JsonUtility.FromJson<StoryData>(jsonText);

        if (storyData != null && storyData.nodes != null)
        {
            nodeMap.Clear();
            foreach (DialogueNode node in storyData.nodes)
            {
                if (!nodeMap.ContainsKey(node.id))
                {
                    nodeMap.Add(node.id, node);
                }
            }

            string targetStartId = storyData.startNodeId;

            string savedStartNode = PlayerPrefs.GetString("NextStartNode", "");
            if (!string.IsNullOrEmpty(savedStartNode))
            {
                if (nodeMap.ContainsKey(savedStartNode))
                {
                    targetStartId = savedStartNode;
                }
                PlayerPrefs.DeleteKey("NextStartNode");
                PlayerPrefs.Save();
            }

            if (!string.IsNullOrEmpty(targetStartId) && nodeMap.ContainsKey(targetStartId))
            {
                DisplayNode(targetStartId);
            }
            else
            {
                Debug.LogError("StartNode konnte nicht gefunden werden: " + targetStartId);
            }
        }
        else
        {
            Debug.LogError("Fehler beim Laden des JSONs!");
        }
    }

    public void DisplayNode(string nodeId)
    {
        isWaitingForChoice = false;
        if (gameManager != null && gameManager.choicePanel != null)
        {
            gameManager.choicePanel.SetActive(false);
        }

        if (string.IsNullOrEmpty(nodeId) || !nodeMap.ContainsKey(nodeId))
        {
            EndDialogue();
            return;
        }

        currentNode = nodeMap[nodeId];

        // 1. Charakterbilder setzen:
        if (elderlyLadysCharacterImage != null) elderlyLadysCharacterImage.SetActive(currentNode.showElderlyLadys);
        if (loveInterestCharacterImage != null) loveInterestCharacterImage.SetActive(currentNode.showLoveInterestH);
        if (vinkCharacterImage != null) vinkCharacterImage.SetActive(currentNode.showLoveInterestV);
        if (customerCharacterImage != null) customerCharacterImage.SetActive(currentNode.showCustomer);
        if (mafiaMenImage != null) mafiaMenImage.SetActive(currentNode.showMafiaMen);

        /// 2. FLAG-PRÜFUNG (Weiche):
        if (!string.IsNullOrEmpty(currentNode.checkFlag))
        {
            int hasFlag = PlayerPrefs.GetInt(currentNode.checkFlag, 0);
            
            // --- HIER SEHEN WIR GENAU, WAS PASSIERT ---
            Debug.Log("CHECK-FLAG: " + currentNode.checkFlag + " | Aktueller Wert in PlayerPrefs: " + hasFlag);
            // ------------------------------------------

            if (hasFlag == 1 && !string.IsNullOrEmpty(currentNode.flagTrueNextId))
            {
                Debug.Log("-> Springe zu TRUE: " + currentNode.flagTrueNextId);
                DisplayNode(currentNode.flagTrueNextId);
                return;
            }
            else if (hasFlag == 0 && !string.IsNullOrEmpty(currentNode.flagFalseNextId))
            {
                Debug.Log("-> Springe zu FALSE: " + currentNode.flagFalseNextId);
                DisplayNode(currentNode.flagFalseNextId);
                return;
            }
            if (!string.IsNullOrEmpty(currentNode.resetFlag))
            {
                PlayerPrefs.SetInt(currentNode.resetFlag, 0);
                PlayerPrefs.Save();
                Debug.Log("Flag auf 0 zurückgesetzt: " + currentNode.resetFlag);
            }
        }

        // 3. UI Panels umschalten:
        if (introBubblePanel != null) introBubblePanel.SetActive(false);
        if (narratorBubblePanel != null) narratorBubblePanel.SetActive(false);
        if (speechBubblePanel != null) speechBubblePanel.SetActive(false);
        if (sideCharacterBubblePanel != null) sideCharacterBubblePanel.SetActive(false);

        // Textblase zuweisen:
        if (currentNode.isNarration)
        {
            if (narratorBubblePanel != null) narratorBubblePanel.SetActive(true);
            currentActiveTextComponent = narratorText;
        }
        else if (currentNode.useMCBubble)
        {
            if (speechBubblePanel != null) speechBubblePanel.SetActive(true);
            currentActiveTextComponent = dialogueText;
        }
        else if (currentNode.useSideCharacterBubble)
        {
            if (sideCharacterBubblePanel != null) sideCharacterBubblePanel.SetActive(true);
            currentActiveTextComponent = sideCharacterText;
        }
        else if (currentNode.useIntroPanel || (SceneManager.GetActiveScene().name == "ApartmentScene" && currentNode.id.StartsWith("intro_")))
        {
            if (introBubblePanel != null) introBubblePanel.SetActive(true);
            currentActiveTextComponent = introText;
        }
        else
        {
            if (sideCharacterBubblePanel != null) sideCharacterBubblePanel.SetActive(true);
            currentActiveTextComponent = sideCharacterText;
        }

        if (currentNode.removeBlur)
        {
            if (blurCoroutine != null) StopCoroutine(blurCoroutine);
            blurCoroutine = StartCoroutine(FadeOutBlur());
        }

        if (currentNode.clearBeforeTyping && currentActiveTextComponent != null)
        {
            currentActiveTextComponent.text = "";
        }

        formattedCurrentText = currentNode.text.Replace("{Name}", GameManager.GetUsername());
        if (currentNode.isThought)
        {
            formattedCurrentText = "<i>" + formattedCurrentText + "</i>";
        }

        if (typingCoroutine != null) StopCoroutine(typingCoroutine);
        typingCoroutine = StartCoroutine(TypeSentence(formattedCurrentText));

        if (currentNode.showPoints || isPointsPanelVisible)
        {
            isPointsPanelVisible = true;
        }

        if (pointsPanel != null)
        {
            pointsPanel.SetActive(isPointsPanelVisible);
        }

        // FLAG SETZEN (falls im Knoten definiert):
        if (!string.IsNullOrEmpty(currentNode.setFlag))
        {
            PlayerPrefs.SetInt(currentNode.setFlag, 1);
            PlayerPrefs.Save();
            Debug.Log("Flag gesetzt: " + currentNode.setFlag);
        }
    }

    public void AdvanceDialogue()
    {
        if (isTyping)
        {
            StopCoroutine(typingCoroutine);
            if (currentActiveTextComponent != null)
            {
                currentActiveTextComponent.text = formattedCurrentText;
            }
            isTyping = false;
            OnNodeTextFinished();
            return;
        }

        if (currentNode != null && !currentNode.isChoice && !currentNode.isNameInput)
        {
            if (!string.IsNullOrEmpty(currentNode.loadScene))
            {
                if (!string.IsNullOrEmpty(currentNode.overrideStartNode))
                {
                    PlayerPrefs.SetString("NextStartNode", currentNode.overrideStartNode);
                    PlayerPrefs.Save();
                }

                if (currentNode.fadeToBlack)
                {
                    StartCoroutine(FadeOutAndLoadNextScene(currentNode.loadScene));
                }
                else
                {
                    SceneManager.LoadScene(currentNode.loadScene);
                }
                return;
            }

            if (!string.IsNullOrEmpty(currentNode.nextId))
            {
                DisplayNode(currentNode.nextId);
            }
            else
            {
                EndDialogue();
            }
        }
    }

    private IEnumerator TypeSentence(string sentence)
    {
        isTyping = true;
        if (currentActiveTextComponent != null) currentActiveTextComponent.text = "";

        foreach (char letter in sentence.ToCharArray())
        {
            if (currentActiveTextComponent != null)
            {
                currentActiveTextComponent.text += letter;
            }
            yield return new WaitForSeconds(typingSpeed);
        }

        isTyping = false;
        OnNodeTextFinished();
    }

    private void OnNodeTextFinished()
    {
        if (currentNode != null && currentNode.isNameInput)
        {
            isWaitingForNameInput = true;
            if (nameInputPanel != null) nameInputPanel.SetActive(true);
            return;
        }

        if (currentNode != null && currentNode.isChoice)
        {
            isWaitingForChoice = true;
            if (gameManager == null) gameManager = FindAnyObjectByType<GameManager>();

            if (gameManager != null && currentNode.options != null)
            {
                gameManager.SetupChoiceButtons(currentNode.options);
                gameManager.ShowChoiceUI();
            }
            return;
        }
    }

    private IEnumerator FadeOutBlur()
    {
        if (backgroundImage != null && backgroundImage.material != null)
        {
            Material mat = backgroundImage.material;

            if (mat.HasProperty("_BlurAmount"))
            {
                float startBlur = mat.GetFloat("_BlurAmount");
                float duration = 1.2f;
                float elapsed = 0f;

                while (elapsed < duration)
                {
                    elapsed += Time.deltaTime;
                    float currentBlur = Mathf.Lerp(startBlur, 0f, elapsed / duration);
                    mat.SetFloat("_BlurAmount", currentBlur);
                    yield return null;
                }
                mat.SetFloat("_BlurAmount", 0f);
            }
            else
            {
                backgroundImage.material = null;
            }
        }
    }

    public void OnNameSubmitted()
    {
        if (nameInputField != null && !string.IsNullOrEmpty(nameInputField.text))
        {
            string enteredName = nameInputField.text.Trim();

            if (gameManager != null)
            {
                gameManager.SaveUsername(enteredName);
            }
            else
            {
                PlayerPrefs.SetString("PlayerUsername", enteredName);
                PlayerPrefs.Save();
            }

            if (nameInputPanel != null) nameInputPanel.SetActive(false);

            isWaitingForNameInput = false;

            if (currentNode != null)
            {
                DisplayNode(currentNode.nextId);
            }
        }
    }

    public void ResumeAfterChoice(bool isSpecial)
    {
        isWaitingForChoice = false;

        if (currentNode != null && currentNode.isChoice && currentNode.options != null)
        {
            int optionIndex = isSpecial ? 1 : 0;
            if (optionIndex < currentNode.options.Count)
            {
                string targetNodeId = currentNode.options[optionIndex].nextId;
                DisplayNode(targetNodeId);
            }
        }
    }

    private IEnumerator FadeOutAndLoadNextScene(string sceneName)
    {
        if (blackScreenPanel != null)
        {
            blackScreenPanel.SetActive(true);
            CanvasGroup canvasGroup = blackScreenPanel.GetComponent<CanvasGroup>();
            if (canvasGroup == null)
            {
                canvasGroup = blackScreenPanel.AddComponent<CanvasGroup>();
            }

            canvasGroup.alpha = 0f;
            float duration = 1.5f;
            float elapsed = 0f;

            while (elapsed < duration)
            {
                elapsed += Time.deltaTime;
                canvasGroup.alpha = Mathf.Lerp(0f, 1f, elapsed / duration);
                yield return null;
            }
            canvasGroup.alpha = 1f;
        }

        if (!string.IsNullOrEmpty(sceneName))
        {
            SceneManager.LoadScene(sceneName);
        }
    }

    private void EndDialogue()
    {
        Debug.Log("Dialog beendet!");
    }
}