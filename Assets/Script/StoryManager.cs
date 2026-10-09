using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

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
    public bool isNameInput; // Zeigt Name Input-Feld an
    public float autoAdvanceDelay = 0f; // Pause nach dem Text vor automatischem Laden
    public List<DialogueOption> options;
    public string nextId;
}

[System.Serializable]
public class StoryData
{
    public string startNodeId;
    public List<DialogueNode> nodes;
}

public class StoryManager : MonoBehaviour
{
    [Header("UI References")]
    public TextMeshProUGUI dialogueText;
    public GameManager gameManager;

    [Header("Name Input UI")]
    public GameObject nameInputPanel; // Panel mit dem InputField und Bestätigen-Button
    public TMP_InputField nameInputField;
    public Button nameSubmitButton;

    [Header("Typewriter Settings")]
    public float typingSpeed = 0.05f;

    [Header("Dialogue File")]
    public TextAsset dialogueJsonFile;

    private Dictionary<string, DialogueNode> nodeMap = new Dictionary<string, DialogueNode>();
    private DialogueNode currentNode;
    private Coroutine typingCoroutine;
    private Coroutine autoAdvanceCoroutine;
    private bool isTyping = false;
    private bool isWaitingForChoice = false;
    private bool isWaitingForNameInput = false;

    private void Start()
    {
        if (gameManager == null)
        {
            gameManager = FindAnyObjectByType<GameManager>();
        }

        if (nameInputPanel != null)
        {
            nameInputPanel.SetActive(false);
        }

        if (nameSubmitButton != null)
        {
            nameSubmitButton.onClick.AddListener(OnNameSubmitted);
        }

        if (dialogueJsonFile != null)
        {
            LoadStoryFromJson(dialogueJsonFile.text);
        }
    }

    private void Update()
    {
        if (isWaitingForChoice || isWaitingForNameInput) return;

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

        // Platzhalter {Name} durch den gespeicherten Benutzernamen ersetzen
        string formattedText = currentNode.text.Replace("{Name}", GameManager.GetUsername());

        typingCoroutine = StartCoroutine(TypeSentence(formattedText));
    }

    public void AdvanceDialogue()
    {
        if (autoAdvanceCoroutine != null)
        {
            StopCoroutine(autoAdvanceCoroutine);
        }

        if (isTyping)
        {
            StopCoroutine(typingCoroutine);
            dialogueText.text = currentNode.text.Replace("{Name}", GameManager.GetUsername());
            isTyping = false;
            OnNodeTextFinished();
            return;
        }

        if (currentNode != null && !currentNode.isChoice && !currentNode.isNameInput)
        {
            DisplayNode(currentNode.nextId);
        }
    }

    private IEnumerator TypeSentence(string sentence)
    {
        isTyping = true;
        dialogueText.text = "";

        foreach (char letter in sentence.ToCharArray())
        {
            dialogueText.text += letter;
            yield return new WaitForSeconds(typingSpeed);
        }

        isTyping = false;
        OnNodeTextFinished();
    }

    private void OnNodeTextFinished()
    {
        // 1. Namens-Eingabe
        if (currentNode != null && currentNode.isNameInput)
        {
            isWaitingForNameInput = true;
            if (nameInputPanel != null)
            {
                nameInputPanel.SetActive(true);
            }
            return;
        }

        // 2. Entscheidungs-Knoten
        if (currentNode != null && currentNode.isChoice)
        {
            isWaitingForChoice = true;

            if (gameManager == null)
            {
                gameManager = FindAnyObjectByType<GameManager>();
            }

            if (gameManager != null && currentNode.options != null && currentNode.options.Count >= 2)
            {
                gameManager.specialAnswerCost = currentNode.options[1].cost;
                gameManager.SetupAnswerButtonTexts(currentNode.options[0].text, currentNode.options[1].text);
                gameManager.ShowChoiceUI();
            }
            return;
        }

        // 3. Automatisches Weitergehen nach Verzögerung (2 Sekunden Pause)
        if (currentNode != null && currentNode.autoAdvanceDelay > 0f)
        {
            autoAdvanceCoroutine = StartCoroutine(AutoAdvanceAfterDelay(currentNode.autoAdvanceDelay));
        }
    }

    private IEnumerator AutoAdvanceAfterDelay(float delay)
    {
        yield return new WaitForSeconds(delay);
        dialogueText.text = ""; // Text kurz leeren für die 2 Sek Pause
        yield return new WaitForSeconds(0.5f);
        DisplayNode(currentNode.nextId);
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

            if (nameInputPanel != null)
            {
                nameInputPanel.SetActive(false);
            }

            isWaitingForNameInput = false;

            // Weiter zum nächsten Knoten
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

    private void EndDialogue()
    {
        Debug.Log("Intro / Dialog beendet!");
    }
}