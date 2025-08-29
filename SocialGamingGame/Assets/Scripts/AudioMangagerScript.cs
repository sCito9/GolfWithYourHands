using System;
using System.Collections.Generic;
using DedicatedServer;
using UI;
using UnityEngine;
using UnityEngine.UI;
using Random = UnityEngine.Random;

public class AudioMangagerScript : MonoBehaviour
{
    [SerializeField] private GameObject options;
    private bool _musicVolumeOverdrive;
    private bool _effectVolumeOverdrive;
    
    [Header("Audio Sources")]
    private AudioSource backgroundSource;
    [SerializeField] private AudioSource abschlagSource;
    [SerializeField] private AudioSource golfballCollisionSource;
    [SerializeField] private AudioSource finishCourseSource;
    
    [Header("Audio clips")]
    [SerializeField] private AudioClip backgroundMusic;
    [SerializeField] private AudioClip abschlagSound;
    [SerializeField] private List<AudioClip> collisionSounds;
    [SerializeField] private AudioClip finishCourseSound;
    
    [Header("Options stuff")]
    [SerializeField] private Toggle musicVolumeBoost;
    [SerializeField] private Toggle effectVolumeBoost;
    [SerializeField] private Slider musicVolumeSlider;
    [SerializeField] private Slider effectVolumeSlider;

    private void Start()
    {
        try
        {
            backgroundSource = Camera.main.GetComponent<AudioSource>();
            backgroundSource.clip = backgroundMusic;
            backgroundSource.volume = 0.05f;
            backgroundSource.playOnAwake = true;
            backgroundSource.loop = true;
            backgroundSource.Play();
        }
        catch(Exception e)
        {
            Debug.Log(e + "\nExcpetion caught, Camera doesn't have Audio Source");
        }

        PlayerSettings settings = JsonHandler.ReadPlayerSettings();
        initializeSettings(settings);
    }

    public void playAbchlag()
    {
        abschlagSource.PlayOneShot(abschlagSound);
    }
    
    public void playCollision()
    {
        if (collisionSounds == null || collisionSounds.Count == 0)
            return;
        
        AudioClip audioClip = collisionSounds[Random.Range(0, collisionSounds.Count)];
        golfballCollisionSource.PlayOneShot(audioClip);
    }

    public void playFinishCourse()
    {
        finishCourseSource.PlayOneShot(finishCourseSound);
    }

    public void activateOptions(bool active)
    {
        options.SetActive(active);
    }
    
    public void setMusicVolume(float volume)
    {
        if (backgroundSource == null)
            return;
        if (_musicVolumeOverdrive)
            backgroundSource.volume = Math.Clamp(volume, 0, 1);
        else
            backgroundSource.volume = Math.Clamp(volume, 0, 1) * 0.1f;
    }

    public void setEffectVolume(float volume)
    {
        if (_effectVolumeOverdrive)
        {
            abschlagSource.volume = Math.Clamp(volume, 0, 1);
            golfballCollisionSource.volume = Math.Clamp(volume, 0, 1);
            finishCourseSource.volume = Math.Clamp(volume, 0, 1);
        }
        else
        {
            abschlagSource.volume = Math.Clamp(volume, 0, 1) * 0.1f;
            golfballCollisionSource.volume = Math.Clamp(volume, 0, 1) * 0.1f;
            finishCourseSource.volume = Math.Clamp(volume, 0, 1)  * 0.1f;
        }
    }

    public void setEffectVolumeOverdrive(bool overdrive)
    {
        _effectVolumeOverdrive = overdrive;
        if (overdrive)
        {
            abschlagSource.volume *= 10;
            golfballCollisionSource.volume *= 10;
            finishCourseSource.volume *= 10;
        }
        else
        {
            abschlagSource.volume /= 10;
            golfballCollisionSource.volume /= 10;
            finishCourseSource.volume /= 10;
        }
    }

    public void setMusicVolumeOverdrive(bool overdrive)
    {
        _musicVolumeOverdrive = overdrive;
        if (backgroundSource == null)
            return;
        if (overdrive)
            backgroundSource.volume *= 10;
        else
            backgroundSource.volume /= 10;
    }
    
    public void initializeSettings(PlayerSettings settings)
    {
        if (backgroundSource != null)
        {
            backgroundSource.volume = settings.backgroundMusicVolume;
        }
        abschlagSource.volume = settings.effectVolume;
        golfballCollisionSource.volume = settings.effectVolume;
        finishCourseSource.volume = settings.effectVolume;
        _musicVolumeOverdrive = settings.backgroundMusicBoost;
        _effectVolumeOverdrive = settings.effectVolumeBoost;
        
        if ((musicVolumeBoost.isOn = settings.backgroundMusicBoost) == true)
            musicVolumeSlider.value = settings.backgroundMusicVolume;
        else
            musicVolumeSlider.value = settings.backgroundMusicVolume * 10;
        
        if ((effectVolumeBoost.isOn = settings.effectVolumeBoost) == true)
            effectVolumeSlider.value = settings.effectVolume;
        else
            effectVolumeSlider.value = settings.effectVolume * 10;
    }

    public void saveSettings()
    {
        PlayerSettings settings;
        if (backgroundSource == null)
            settings = new PlayerSettings(0, abschlagSource.volume, _musicVolumeOverdrive, _effectVolumeOverdrive);
        else
            settings = new PlayerSettings(backgroundSource.volume, abschlagSource.volume, _musicVolumeOverdrive, _effectVolumeOverdrive);
        JsonHandler.WritePlayerSettings(settings);
        activateOptions(false);
    }
}
