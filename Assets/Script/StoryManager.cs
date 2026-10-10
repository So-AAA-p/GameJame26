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
    public bool removeBlur;
    public bool clearBeforeTyping;
    public string loadScene;
    public List<DialogueOption> options;
    public string nextId;
    public bool fadeToBlack;
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

    [Header("UI References - Main Dialogue")]
    public GameObject speechBubblePanel;
    public TextMeshProUGUI dialogueText;

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

    private Dictionary<string, DialogueNode> nodeMap = new Dictionary<string, DialogueNode>();
    private DialogueNode currentNode;
    private TextMeshProUGUI currentActiveTextComponent;
    
    private Coroutine typingCoroutine;
    private Coroutine blurCoroutine;

    private bool isTyping = false;
    private bool isWaitingForChoice = false;
    private bool isWaitingForNameInput = false;

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

        currentActiveTextComponent = introText;

        if (dialogueJsonFile != null)
        {
            LoadStoryFromJson(dialogueJsonFile.text);
        }
    }

    private void Update()
    {
        if (isWaitingForChoice || isWaitingForNameInput) return;

        // Per Mausklick oder Leertaste weiterschalten
        if (Input.GetMouseButtonDown(0) || Input.GetKeyDown(KeyCode.Space))
        {
            AdvanceDialogue();
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

            if (!string.IsNullOrEmpty(storyData.startNodeId) && nodeMap.ContainsKey(storyData.startNodeId))
            {
                DisplayNode(storyData.startNodeId);
            }
        }
        else
        {
            Debug.LogError("Fehler beim Laden des JSONs!");
        }
    }

    public void DisplayNode(string nodeId)
    {
        if (string.IsNullOrEmpty(nodeId) || !nodeMap.ContainsKey(nodeId))
        {
            EndDialogue();
            return;
        }

        currentNode = nodeMap[nodeId];

        // 1. Panel & Text-Komponente umschalten
        if (currentNode.useMCBubble)
        {
            if (introBubblePanel != null) introBubblePanel.SetActive(false);
            if (speechBubblePanel != null) speechBubblePanel.SetActive(true);
            currentActiveTextComponent = dialogueText;
        }
        else
        {
            if (introBubblePanel != null) introBubblePanel.SetActive(true);
            if (speechBubblePanel != null) speechBubblePanel.SetActive(false);
            currentActiveTextComponent = introText;
        }

        // 2. Blur ausblenden, falls getriggert
        if (currentNode.removeBlur)
        {
            if (blurCoroutine != null) StopCoroutine(blurCoroutine);
            blurCoroutine = StartCoroutine(FadeOutBlur());
        }

        // 3. Text davor leeren
        if (currentNode.clearBeforeTyping && currentActiveTextComponent != null)
        {
            currentActiveTextComponent.text = "";
        }

        string formattedText = currentNode.text.Replace("{Name}", GameManager.GetUsername());
        typingCoroutine = StartCoroutine(TypeSentence(formattedText));
    }

    public void AdvanceDialogue()
    {
        // 1. Wenn der Text noch tippt: Sofort vollst‰ndig anzeigen
        if (isTyping)
        {
            StopCoroutine(typingCoroutine);
            if (currentActiveTextComponent != null)
            {
                currentActiveTextComponent.text = currentNode.text.Replace("{Name}", GameManager.GetUsername());
            }
            isTyping = false;
            OnNodeTextFinished();
            return;
        }

        // 2. Wenn der Text fertig getippt ist und geklickt wird:
        if (currentNode != null && !currentNode.isChoice && !currentNode.isNameInput)
        {
            // Falls eine neue Szene geladen werden soll
            if (!string.IsNullOrEmpty(currentNode.loadScene))
            {
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

            DisplayNode(currentNode.nextId);
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
        // 1. Namensfeld aktivieren
        if (currentNode != null && currentNode.isNameInput)
        {
            isWaitingForNameInput = true;
            if (nameInputPanel != null) nameInputPanel.SetActive(true);
            return;
        }

        // 2. Entscheidungs-Buttons aktivieren
        if (currentNode != null && currentNode.isChoice)
        {
            isWaitingForChoice = true;
            if (gameManager == null) gameManager = FindAnyObjectByType<GameManager>();

            if (gameManager != null && currentNode.options != null && currentNode.options.Count >= 2)
            {
                gameManager.specialAnswerCost = currentNode.options[1].cost;
                gameManager.SetupAnswerButtonTexts(currentNode.options[0].text, currentNode.options[1].text);
                gameManager.ShowChoiceUI();
            }
            return;
        }

        // (Hinweis: loadScene lassen wir hier drauﬂen, damit es erst beim echten Weiterklick passiert!)
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

        // Szene laden
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