using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;

[System.Serializable]
public class DialogueData
{
    public List<string> lines;
    public string standardOptionText;
    public string specialOptionText;
    public string followUpLine;
}

public class StoryManager : MonoBehaviour
{
    [Header("UI References")]
    public TextMeshProUGUI dialogueText;
    public GameManager gameManager;

    [Header("Typewriter Settings")]
    public float typingSpeed = 0.05f;

    [Header("Dialogue File")]
    public TextAsset dialogueJsonFile; // Hier ziehst du einfach deine JSON-Datei rein!

    private Queue<string> sentences = new Queue<string>();
    private Coroutine typingCoroutine;
    private bool isTyping = false;
    private string currentSentence;
    private DialogueData currentDialogueData;
    private bool isWaitingForChoice = false;

    private void Start()
    {
        if (gameManager == null)
        {
            gameManager = FindObjectOfType<GameManager>();
        }

        if (dialogueJsonFile != null)
        {
            LoadDialogueFromJson(dialogueJsonFile.text);
        }
    }

    private void Update()
    {
        if (isWaitingForChoice) return;
        // Bei Linksklick oder Leertaste -> Nächster Satz
        if (Input.GetMouseButtonDown(0) || Input.GetKeyDown(KeyCode.Space))
        {
            DisplayNextSentence();
        }
    }

    public void LoadDialogueFromJson(string jsonText)
    {
        DialogueData loadedData = JsonUtility.FromJson<DialogueData>(jsonText);
        StartDialogue(loadedData.lines);
    }

    public void StartDialogue(List<string> lines)
    {
        sentences.Clear();
        isWaitingForChoice = false;

        foreach (string line in lines)
        {
            sentences.Enqueue(line);
        }

        DisplayNextSentence();
    }

    public void DisplayNextSentence()
    {
        // Wenn aktuell noch getippt wird, springe direkt zum vollständigen Text
        if (isTyping)
        {
            StopCoroutine(typingCoroutine);
            dialogueText.text = currentSentence;
            isTyping = false;
            CheckIfSentenceTriggersChoice();
            return;
        }

        // Keine Sätze mehr in der Queue? Dialog beendet!
        if (sentences.Count == 0)
        {
            EndDialogue();
            return;
        }

        currentSentence = sentences.Dequeue();
        typingCoroutine = StartCoroutine(TypeSentence(currentSentence));
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
        CheckIfSentenceTriggersChoice();
    }
    private void CheckIfSentenceTriggersChoice()
    {
        // Sobald die Frage "do you want to be my gop" getippt wurde, Wahl-UI öffnen
        if (currentSentence == "do you want to be my gop" && sentences.Count == 0)
        {
            isWaitingForChoice = true;
            if (gameManager != null)
            {
                gameManager.SetupAnswerButtonTexts(currentDialogueData.standardOptionText, currentDialogueData.specialOptionText);
                gameManager.ShowChoiceUI();
            }
        }
    }
    public void ResumeAfterChoice()
    {
        isWaitingForChoice = false;

        if (currentDialogueData != null && !string.IsNullOrEmpty(currentDialogueData.followUpLine))
        {
            sentences.Enqueue(currentDialogueData.followUpLine);
            DisplayNextSentence();
        }
    }

    private void EndDialogue()
    {
        Debug.Log("Dialog beendet!");
    }
}