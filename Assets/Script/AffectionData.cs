using System;
using UnityEngine;

public static class AffectionData
{
    public static event Action<string, int> OnChanged;

    public static int Get(string who) => PlayerPrefs.GetInt("Affection" + who, 0);

    public static void Add(string who, int amount)
    {
        if (string.IsNullOrEmpty(who) || amount == 0) return;
        PlayerPrefs.SetInt("Affection" + who, Get(who) + amount);
        PlayerPrefs.Save();
        OnChanged?.Invoke(who, Get(who));
    }

    public static bool Visible
    {
        get => PlayerPrefs.GetInt("AffectionVisible", 0) == 1;
        set { PlayerPrefs.SetInt("AffectionVisible", value ? 1 : 0); PlayerPrefs.Save(); }
    }

    public static void ResetAll()
    {
        PlayerPrefs.DeleteKey("AffectionHaru");
        PlayerPrefs.DeleteKey("AffectionVincent");
        PlayerPrefs.SetInt("AffectionVisible", 0);
        PlayerPrefs.Save();
    }
}