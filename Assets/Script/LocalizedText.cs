using UnityEngine;
using TMPro;

public class LocalizedText : MonoBehaviour
{
    [TextArea(1, 3)]
    public string germanText;

    [TextArea(1, 3)]
    public string englishText;

    private TextMeshProUGUI textComponent;

    private void Awake()
    {
        textComponent = GetComponent<TextMeshProUGUI>();
    }

    private void OnEnable()
    {
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