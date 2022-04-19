using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class SoundManager : MonoBehaviour
{
    public enum SoundChannel { BGM, BGS, SE, UISE }

    public static SoundManager Ins;
    [HideInInspector] public AudioSource[] Channels = new AudioSource[4];

    private float _universalBGMVol, _universalSEVol;

    public delegate void AudioFadedOut();
    public event AudioFadedOut AudioFadedOutHandler;

    void Awake()
    {
        if (Ins == null)
        {
            Ins = this;
            DontDestroyOnLoad(Ins);
        }
        else
            Destroy(this);
    }
    void Start()
    {
        Channels = this.GetComponentsInChildren<AudioSource>();
    }

    /// <summary>
    /// Playing Sound or Audio.
    /// </summary>
    /// <param name="channel"></param>
    /// <param name="audio"></param>
    /// <param name="pitch"></param>
    public void Play(SoundChannel channel, AudioClip audio, float pitch = 1f)
    {
        int channelIndex = (int)channel;
        if (channel == SoundChannel.BGM || channel == SoundChannel.BGS)
        {
            // playing audio on BGM or BGS channel
            Channels[channelIndex].clip = audio;
            Channels[channelIndex].pitch = pitch;
            Channels[channelIndex].Play();
        }
        else
        {
            Channels[channelIndex].pitch = pitch;
            Channels[channelIndex].PlayOneShot(audio);
        }
    }

    public IEnumerator AudioFadeOut(SoundChannel channel, float duration)
    {
        int channelIndex = (int)channel;
        if (channel == SoundChannel.SE || channel == SoundChannel.UISE)
        {
            Debug.LogError("You cant use audio_fade_out() on SE or UISE channel");
            yield break;
        }
        else
        {
            float timeElapsed = 0f;
            while (timeElapsed < duration)
            {
                Channels[channelIndex].volume = Mathf.Lerp(1, 0, timeElapsed / duration);
                timeElapsed += Time.deltaTime;
                yield return null;
            }
            Channels[channelIndex].volume = 1;    // default value

            if (AudioFadedOutHandler != null)  // if it has methods can be fired
                AudioFadedOutHandler();
        }
    }
}
