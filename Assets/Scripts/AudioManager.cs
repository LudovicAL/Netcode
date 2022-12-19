using System.Collections;
using System.Collections.Generic;
using UnityEngine;

//This scripts makes it possible to play all of your game AudioClips from a single GameObject.
//If at a given time there are not enough AudioSource to play simultaneous AudioClips, the script simply instantiate more AudioSources components.
//
//This script maintains 2 list of AudioSources:
//	(1) availableAudioSourceList
//	(2) playingAudioSourceList
//All AudioSources start in the first list.
//	When an AudioClip is played, its AudioSource is moved to the second list.
//	When an AudioClip is paused, its AudioSource stays in the second list.
//	When an AudioClip is stopped, its AudioSource is moved back to the first list.
//On every Update, the script searches the second list for AudioSource that are done playing their AudioClip and move those back to the first list. 
//
//Here is an example of the commands that can be passed from another script to play, pause, unpause and stop an AudioClip. This example is one where the reference to the AudioSource is kept.
//	AudioSource audioSource = AudioManager.Instance.PlayClip(AudioManager.Instance.menuThemeClip);
//  AudioManager.Instance.PauseAudioSource(audioSource);
//	audioSource.Play();
//  AudioManager.Instance.StopAudioSource(audioSource);
//
//Here is an example of the commands that can be passed from another script to play, pause, unpause and stop an AudioClip. This example is one where the reference to the AudioSource is not kept.
//	AudioManager.Instance.PlayClip(AudioManager.Instance.menuThemeClip);
//  AudioSource audioSource = AudioManager.Instance.PauseClip(AudioManager.Instance.menuThemeClip);
//	audioSource.Play();
//  AudioManager.Instance.StopClip(AudioManager.Instance.menuThemeClip);

[RequireComponent(typeof(AudioListener))]
public class AudioManager : MonoBehaviour {

    public static AudioManager Instance { get; private set; }

    [SerializeField]
    private int maxNbAudioSources = 15;

    public AudioClip menuClickClip;
    public AudioClip playerJoiningClip;
    public AudioClip playerLeavingClip;
    public AudioClip warningClip;

    private List<AudioSource> availableAudioSourceList;
    private List<AudioSource> playingAudioSourceList;
    private int currentAudioSourceCount;

    private void Awake() {
        if (Instance == null) {
            Instance = this;
        } else if (Instance != this) {
            Destroy(gameObject);
        }
    }

    void Start() {
        availableAudioSourceList = new List<AudioSource>();
        playingAudioSourceList = new List<AudioSource>();

        availableAudioSourceList.AddRange(GetComponents<AudioSource>());
        currentAudioSourceCount = availableAudioSourceList.Count;

        //GameStatesManager.Instance.GameStateChanged.AddListener(OnGameStateChange);
        //OnGameStateChange();
    }

    void Update() {
        for (int i = playingAudioSourceList.Count - 1; i >= 0; i--) {
            if (!playingAudioSourceList[i].isPlaying) {
                TransferAudioSource(playingAudioSourceList[i], playingAudioSourceList, availableAudioSourceList);
            }
        }
    }

    /*
	private void OnGameStateChange() {
		switch (GameStatesManager.gameState) {
			case (StaticData.AvailableGameStates.Menu):
				
				break;
			case (StaticData.AvailableGameStates.Starting):
				
				break;
			case (StaticData.AvailableGameStates.Playing):
				
				break;
			case (StaticData.AvailableGameStates.Pausing):
				
				break;
			case (StaticData.AvailableGameStates.Ending):
				
				break;
		}
	}
	*/

    #region Play

    /// <summary>
    /// Plays an AudioClip, returns the AudioSource used to play it
    /// </summary>
    public AudioSource PlayClip(AudioClip clip) {
        return PlayClip(clip, false, 0.0f, 1.0f);
    }

    /// <summary>
    /// Plays an AudioClip and loops it, returns the AudioSource used to play it
    /// </summary>
    public AudioSource PlayClip(AudioClip clip, bool loop) {
        return PlayClip(clip, loop, 0.0f, 1.0f);
    }

    /// <summary>
    /// Plays an AudioClip with a fade in, returns the AudioSource used to play it
    /// </summary>
    public AudioSource PlayClip(AudioClip clip, float fadeInLength) {
        return PlayClip(clip, false, fadeInLength, 1.0f);
    }

    /// <summary>
    /// Plays an AudioClip with a fade in and loops it, returns the AudioSource used to play it
    /// </summary>
    public AudioSource PlayClip(AudioClip clip, bool loop, float fadeInLength, float desiredVolume) {
        if (clip) {
            AudioSource audioSource = GetAudioSource();
            if (audioSource) {
                TransferAudioSource(audioSource, availableAudioSourceList, playingAudioSourceList);
                audioSource.loop = loop;
                audioSource.clip = clip;
                audioSource.Play();
                if (fadeInLength > 0.0f) {
                    StartCoroutine(AudioSourceFadeIn(audioSource, fadeInLength, desiredVolume));
                } else {
                    audioSource.volume = desiredVolume;
                }
            }
            return audioSource;
        } else {
            Debug.Log("You passed a null AudioClip. What is wrong with you?");
            return null;
        }
    }

    #endregion

    #region Pause

    /// <summary>
    /// Pauses an AudioClip, returns its AudioSource
    /// </summary>
    public AudioSource PauseClip(AudioClip clip) {
        return PauseClip(clip, 0.0f);
    }

    /// <summary>
    /// Finds and pauses all the AudioClips in a list and return the last AudioSource found playing it
    /// </summary>
    public AudioSource PauseAllClipsInList(List<AudioClip> listOfClips) {
        return PauseAllClipsInList(listOfClips, 0.0f);
    }

    /// <summary>
    /// Finds and pauses all the AudioClips in a list with a fade out and returns the last AudioSource found playing it
    /// </summary>
    public AudioSource PauseAllClipsInList(List<AudioClip> listOfClips, float fadeOutLength) {
        AudioSource audioSource = null;
        foreach (AudioClip clip in listOfClips) {
            AudioSource temp = PauseClip(clip, fadeOutLength);
            if (temp) {
                audioSource = temp;
            }
        }
        return audioSource;
    }

    /// <summary>
    /// Pauses all AudioClips with a fade out
    /// </summary>
    public void PauseAllAudioSource(float fadeOutLength) {
        foreach (AudioSource audioSource in playingAudioSourceList) {
            PauseAudioSource(audioSource, fadeOutLength);
        }
    }

    /// <summary>
    /// Pauses an AudioClip with a fadeout, returns its AudioSource
    /// </summary>
    public AudioSource PauseClip(AudioClip clip, float fadeOutLength) {
        AudioSource audioSource = FindAudioSourcePlayingClip(clip);
        PauseAudioSource(audioSource, fadeOutLength);
        return audioSource;
    }

    /// <summary>
    /// Pauses an AudioSource
    /// </summary>
    public void PauseAudioSource(AudioSource audioSource) {
        PauseAudioSource(audioSource, 0.0f);
    }

    /// <summary>
    /// Pauses an AudioSource with fade out
    /// </summary>
    public void PauseAudioSource(AudioSource audioSource, float fadeOutLength) {
        if (audioSource.isPlaying) {
            if (fadeOutLength > 0f) {
                StartCoroutine(AudioSourceFadeOut(audioSource, fadeOutLength, true));
            } else {
                audioSource.Pause();
            }
        }
    }

    #endregion

    #region Stop

    /// <summary>
    /// Stops an AudioClip
    /// </summary>
    public void StopClip(AudioClip clip) {
        StopClip(clip, 0f);
    }

    /// <summary>
    /// Stops an AudioClip with a fadeout
    /// </summary>
    public void StopClip(AudioClip clip, float fadeOutLength) {
        AudioSource audioSource = FindAudioSourcePlayingClip(clip);
        StopAudioSource(audioSource, fadeOutLength);
    }

    /// <summary>
    /// Stops all AudioClips
    /// </summary>
    public void StopAllClips() {
        StopAllClips(0.0f);
    }

    /// <summary>
    /// Finds and stops all the AudioClips in a list
    /// </summary>
    public void StopAllClipsInList(List<AudioClip> listOfClips) {
        foreach (AudioClip clip in listOfClips) {
            StopClip(clip);
        }
    }

    /// <summary>
    /// Finds and stops all the AudioClips in a list
    /// </summary>
    public void StopAllClipsInList(List<AudioClip> listOfClips, float fadeOutLength) {
        foreach (AudioClip clip in listOfClips) {
            StopClip(clip, fadeOutLength);
        }
    }

    /// <summary>
    /// Stops all AudioClips with fade out
    /// </summary>
    public void StopAllClips(float fadeOutLength) {
        foreach (AudioSource audioSource in playingAudioSourceList) {
            StopAudioSource(audioSource, fadeOutLength);
        }
    }

    /// <summary>
    /// Stops an AudioSource
    /// </summary>
    public void StopAudioSource(AudioSource audioSource) {
        StopAudioSource(audioSource, 0.0f);
    }

    /// <summary>
    /// Stops an AudioSource with fade out
    /// </summary>
    public void StopAudioSource(AudioSource audioSource, float fadeOutLength) {
        if (audioSource.isPlaying) {
            if (fadeOutLength > 0.0f) {
                AudioSourceFadeOut(audioSource, fadeOutLength, false);
            } else {
                audioSource.Stop();
                TransferAudioSource(audioSource, playingAudioSourceList, availableAudioSourceList);
            }
        }
    }

    #endregion

    #region Utilities

    /// <summary>
    /// Returns a random AudioClip from a passed list
    /// </summary>
    public AudioClip GetRandomClipFromList(List<AudioClip> clipList) {
        return clipList[Random.Range(0, clipList.Count)];
    }

    // Fades in an AudioSource
    private IEnumerator AudioSourceFadeIn(AudioSource audioSource, float fadeInLength, float desiredVolume) {
        while (audioSource.volume < desiredVolume) {
            audioSource.volume = Mathf.Clamp(audioSource.volume + (Time.deltaTime * (desiredVolume / fadeInLength)), 0.0f, desiredVolume);
            yield return null;
        }
        audioSource.Stop();
    }

    //Fades out an AudioSource
    private IEnumerator AudioSourceFadeOut(AudioSource audioSource, float fadeOutLength, bool pause) {
        while (!Mathf.Approximately(audioSource.volume, 0.0f)) {
            audioSource.volume = Mathf.Clamp(audioSource.volume - (Time.deltaTime * (1.0f / fadeOutLength)), 0.0f, 1.0f);
            yield return null;
        }
        if (pause) {
            audioSource.Pause();
        } else {
            audioSource.Stop();
            TransferAudioSource(audioSource, playingAudioSourceList, availableAudioSourceList);
        }
    }

    //Returns an available AudioSource, null if the max was reached already
    private AudioSource GetAudioSource() {
        AudioSource audioSource = null;
        bool success = true;
        if (availableAudioSourceList.Count == 0) {
            success = AddAudioSource();
        }
        if (success) {
            audioSource = availableAudioSourceList[0];
        }
        return audioSource;
    }

    //Adds a new AudioSource; returns true if successful, false if not
    private bool AddAudioSource() {
        if (currentAudioSourceCount < maxNbAudioSources) {
            AudioSource audioSource = gameObject.AddComponent(typeof(AudioSource)) as AudioSource;
            audioSource.playOnAwake = false;
            availableAudioSourceList.Add(audioSource);
            currentAudioSourceCount++;
            return true;
        } else {
            Debug.Log("Max number of AudioSource reached. Raise the max number of AudioSources or diminishe your concurrent number of required AudioSource at a given time.");
            return false;
        }
    }

    //Moves an AudioSource from one list to another
    private void TransferAudioSource(AudioSource audioSource, List<AudioSource> fromList, List<AudioSource> toList) {
        fromList.Remove(audioSource);
        if (!toList.Contains(audioSource)) {
            toList.Add(audioSource);
        }
    }

    //Finds an AudioSource playing the passed AudioClip
    private AudioSource FindAudioSourcePlayingClip(AudioClip clip) {
        foreach (AudioSource audioSource in playingAudioSourceList) {
            if (audioSource.clip.Equals(clip)) {
                return audioSource;
            }
        }
        return null;
    }

    #endregion
}