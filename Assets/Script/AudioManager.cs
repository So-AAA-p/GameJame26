using System.Collections.Generic;
using UnityEngine;

public class AudioManager : MonoBehaviour
{
    public static AudioManager Instance;

    [Header("Audio Sources")]
    public AudioSource bgmSource;
    public AudioSource voiceSource;

    [Header("BGM Track")]
    public AudioClip backgroundMusic;

    [Header("Voice Clips Collection")]
    // Hier ziehst du alle 18 Sounds einfach in den Inspector in Unity!
    public List<AudioClip> voiceClips;

    private Dictionary<string, AudioClip> voiceDictionary = new Dictionary<string, AudioClip>();

    private void Awake()
    {
        // Singleton-Muster, damit die Musik beim Szenenwechsel durchläuft:
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }
        else
        {
            Destroy(gameObject);
            return;
        }

        // Dictionary für schnellen Zugriff per Name aufbauen:
        foreach (AudioClip clip in voiceClips)
        {
            if (clip != null && !voiceDictionary.ContainsKey(clip.name))
            {
                voiceDictionary.Add(clip.name, clip);
            }
        }
    }

    private void Start()
    {
        // Hintergrundmusik starten, falls vorhanden
        if (bgmSource != null && backgroundMusic != null)
        {
            if (!bgmSource.isPlaying)
            {
                bgmSource.clip = backgroundMusic;
                bgmSource.loop = true;
                bgmSource.Play();
            }
        }
    }

    public void PlayVoice(string clipName)
    {
        if (string.IsNullOrEmpty(clipName)) return;

        if (voiceDictionary.ContainsKey(clipName))
        {
            // Bisherige Sprachausgabe stoppen, falls noch eine läuft
            if (voiceSource.isPlaying)
            {
                voiceSource.Stop();
            }

            voiceSource.PlayOneShot(voiceDictionary[clipName]);
        }
        else
        {
            Debug.LogWarning("AudioClip nicht im AudioManager gefunden: " + clipName);
        }
    }
}