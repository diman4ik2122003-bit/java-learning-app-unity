using UnityEngine;
using System.Collections.Generic;

public class AudioManager : MonoBehaviour
{
    public static AudioManager Instance;

    public AudioSource musicSource;
    public AudioSource sfxSource;

    public List<AudioClip> musicClips;
    public List<AudioClip> sfxClips;
    private Dictionary<string, AudioClip> musicDict = new Dictionary<string, AudioClip>();
    private Dictionary<string, AudioClip> sfxDict   = new Dictionary<string, AudioClip>();

    const string MUSIC_VOL_KEY = "MusicVolume";
    const string SFX_VOL_KEY   = "SfxVolume";

    void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);

            foreach (var clip in musicClips)
                musicDict[clip.name] = clip;
            foreach (var clip in sfxClips)
                sfxDict[clip.name] = clip;

            // загрузка сохранённых значений (0..1), по умолчанию 1
            float musicVol = PlayerPrefs.GetFloat(MUSIC_VOL_KEY, 1f);
            float sfxVol   = PlayerPrefs.GetFloat(SFX_VOL_KEY,   1f);

            if (musicSource) musicSource.volume = musicVol;
            if (sfxSource)   sfxSource.volume   = sfxVol;
        }
        else
        {
            Destroy(gameObject);
        }
    }

    public void SetMusicVolume(float volume)
    {
        if (musicSource != null)
            musicSource.volume = volume;

        PlayerPrefs.SetFloat(MUSIC_VOL_KEY, volume);
    }

    public void SetSFXVolume(float volume)
    {
        if (sfxSource != null)
            sfxSource.volume = volume;

        PlayerPrefs.SetFloat(SFX_VOL_KEY, volume);
    }

    public void PlayMusic(string name, bool loop = true)
    {
        if (musicDict.ContainsKey(name))
        {
            if (musicSource.clip == musicDict[name] && musicSource.isPlaying)
                return;

            musicSource.clip = musicDict[name];
            musicSource.loop = loop;
            musicSource.Play();
        }
    }

    public void PlaySFX(string name)
    {
        Debug.Log("PlaySFX " + name + " | AudioSource: " + (sfxSource ? sfxSource.name : "null"));
        if (sfxDict.ContainsKey(name))
            sfxSource.PlayOneShot(sfxDict[name]);
    }

    public void StopMusic()
    {
        if (musicSource) musicSource.Stop();
    }

    public void StopAllSFX()
    {
        if (sfxSource) sfxSource.Stop();
    }
    
}
