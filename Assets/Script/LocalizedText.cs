using UnityEngine;
using TMPro;

public class LocalizedText : MonoBehaviour
{
    [TextArea(1, 3)]
    public string germanText; // Deutscher Text

    [TextArea(1, 3)]
    public string englishText; // Englischer Text

    private TextMeshProUGUI textComponent;

    private void Awake()
    {
        textComponent = GetComponent<TextMeshProUGUI>();
    }

    private void OnEnable()
    {
        // Registriert den Text beim Sprachwechsel-System
        GameManager.OnLanguageChanged += UpdateText;
        UpdateText();
    }

    private void OnDisable()
    {
        GameManager.OnLanguageChanged -= UpdateText;
    }

    public void UpdateText()
    {
        if (textComponent == null) return;

        // Liest die Sprache aus den PlayerPrefs ("DE" oder "EN")
        string lang = PlayerPrefs.GetString("Language", "EN");

        if (lang == "DE")
        {
            textComponent.text = germanText;
        }
        else
        {
            textComponent.text = englishText;
        }
    }
}