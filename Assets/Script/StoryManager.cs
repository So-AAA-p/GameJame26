using System.Collections;
using System.Collections.Generic;
using UnityEngine;
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

    [Header("Typewriter Settings")]
    public float typingSpeed = 0.05f;

    [Header("Dialogue File")]
    public TextAsset dialogueJsonFile;

    private Dictionary<string, DialogueNode> nodeMap = new Dictionary<string, DialogueNode>();
    private DialogueNode currentNode;
    private Coroutine typingCoroutine;
    private bool isTyping = false;
    private bool isWaitingForChoice = false;

    private void Start()
    {
        if (gameManager == null)
        {
            gameManager = FindAnyObjectByType<GameManager>();
        }

        if (dialogueJsonFile != null)
        {
            LoadStoryFromJson(dialogueJsonFile.text);
        }
    }

    private void Update()
    {
        if (isWaitingForChoice) return;

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
        typingCoroutine = StartCoroutine(TypeSentence(currentNode.text));
    }

    public void AdvanceDialogue()
    {
        if (isTyping)
        {
            StopCoroutine(typingCoroutine);
            dialogueText.text = currentNode.text;
            isTyping = false;
            OnNodeTextFinished();
            return;
        }

        if (currentNode != null && !currentNode.isChoice)
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
        // Sobald der Satz zu Ende getippt ist: Ist es ein Entscheidungs-Knoten?
        if (currentNode != null && currentNode.isChoice)
        {
            isWaitingForChoice = true;

            if (gameManager == null)
            {
                gameManager = FindAnyObjectByType<GameManager>();
            }

            if (gameManager != null && currentNode.options != null && currentNode.options.Count >= 2)
            {
                // Setzt Kosten und Texte für die Buttons dynamisch
                gameManager.specialAnswerCost = currentNode.options[1].cost;
                gameManager.SetupAnswerButtonTexts(currentNode.options[0].text, currentNode.options[1].text);
                gameManager.ShowChoiceUI();
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
        Debug.Log("Dialog beendet!");
    }
}