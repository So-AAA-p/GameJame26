using System.Collections;
using UnityEngine;
using UnityEngine.UI;

public class AffectionBarUI : MonoBehaviour
{
    [System.Serializable]
    public class Bar
    {
        public string character;          // "Haru" oder "Vincent"
        public RectTransform barArea;     // Rect der Leiste
        public Image fill;                // rote Leiste (BarFull)
        public RectTransform heart;       // Herz-Icon
        [HideInInspector] public float current;
        [HideInInspector] public Coroutine routine;
    }

    public GameObject content;            // Kind-Objekt, das alles enthält
    public Bar[] bars;

    [Header("Wertebereich")]
    [Tooltip("Bei diesem Wert ist die Leiste komplett rot. 0 oder weniger = komplett grau.")]
    public int maxValue = 25;
    [Tooltip("Herz nach oben (+) oder unten (-) verschieben")]
    public float heartYOffset = 0f;
    public float animTime = 0.6f;

    private void OnEnable() { AffectionData.OnChanged += OnAffectionChanged; }
    private void OnDisable() { AffectionData.OnChanged -= OnAffectionChanged; }

    private void Start()
    {
        foreach (var b in bars)
        {
            Setup(b);
            b.current = TargetFill(AffectionData.Get(b.character));
            Apply(b, b.current);
        }
        if (content != null) content.SetActive(AffectionData.Visible);
    }

    // Stellt Image und Herz selbst richtig ein
    private void Setup(Bar b)
    {
        if (b.fill != null)
        {
            b.fill.type = Image.Type.Filled;
            b.fill.fillMethod = Image.FillMethod.Vertical;
            b.fill.fillOrigin = (int)Image.OriginVertical.Bottom;
        }

        if (b.heart != null)
        {
            b.heart.anchorMin = new Vector2(0.5f, 0f);
            b.heart.anchorMax = new Vector2(0.5f, 0f);
            b.heart.pivot = new Vector2(0.5f, 0.5f);
        }
    }

    // 0 oder weniger = 0 (grau), maxValue = 1 (voll)
    private float TargetFill(int value)
    {
        if (maxValue <= 0) return 0f;
        return Mathf.Clamp01((float)value / maxValue);
    }

    private void Apply(Bar b, float fill)
    {
        if (b.fill != null) b.fill.fillAmount = fill;
        if (b.heart != null && b.barArea != null)
        {
            b.heart.anchoredPosition = new Vector2(0f, b.barArea.rect.height * fill + heartYOffset);
        }
    }

    private void OnAffectionChanged(string who, int value)
    {
        foreach (var b in bars)
        {
            if (b.character != who) continue;
            if (b.routine != null) StopCoroutine(b.routine);

            if (content != null && content.activeInHierarchy)
            {
                b.routine = StartCoroutine(Animate(b, TargetFill(value)));
            }
            else
            {
                b.current = TargetFill(value);
                Apply(b, b.current);
            }
        }
    }

    private IEnumerator Animate(Bar b, float target)
    {
        float start = b.current, t = 0f;
        while (t < 1f)
        {
            t += Time.deltaTime / animTime;
            float e = 1f - Mathf.Pow(1f - Mathf.Clamp01(t), 3f); // ease-out
            b.current = Mathf.Lerp(start, target, e);
            Apply(b, b.current);
            yield return null;
        }
        b.current = target;
        Apply(b, target);
    }

    // Einfach nur sichtbar machen, ohne Animation
    public void Reveal()
    {
        if (content != null) content.SetActive(true);
    }
}