using System;
using DG.Tweening;
using DG.Tweening.Custom;
using UnityEngine;
using UnityEngine.Events;
using Utilities.Prefabs;
using Random = UnityEngine.Random;

namespace AudioTools.Sound
{
    public class SoundSource : MonoBehaviour, IPoolableResource
    {
        [SerializeField] private AudioSource audioSource;

        public UnityEvent<int> onStopped = new();

        private bool isPaused = true;
        private bool pauseSoundWhenGameIsPaused;
        private Transform trackingTransform;
        private int id;

        private float initialVolume;
        public float InitialVolume => initialVolume;

        public void Configure<TSoundType>(int soundId, SoundSample<TSoundType> soundSample, Transform newTransform, bool isTracking,
            float newFadeInDuration) where TSoundType : Enum
        {
            id = soundId;
            if (newTransform)
                gameObject.transform.position = newTransform.position;

            if (isTracking)
                trackingTransform = newTransform;

            audioSource.outputAudioMixerGroup = soundSample.outputAudioMixerGroup;

            if (soundSample.audioClips == null || soundSample.audioClips.Length == 0)
                return;

            var index = 0;

            if (soundSample.audioClips.Length > 1)
            {
                index = (soundSample.lastPlayedIndex + 1) % soundSample.audioClips.Length;
                soundSample.lastPlayedIndex = index;
            }

            var pitch = soundSample.pitch;
            var volume = soundSample.volume;

            if (soundSample.pitchDelta > 0)
                pitch = Random.Range(pitch - soundSample.pitchDelta, pitch + soundSample.pitchDelta);

            if (soundSample.volumeDeltaPercent > 0)
                volume *= 1 + Random.Range(-soundSample.volumeDeltaPercent, soundSample.volumeDeltaPercent);

            audioSource.clip = soundSample.audioClips[index];
            audioSource.volume = volume;
            audioSource.pitch = pitch;
            audioSource.loop = soundSample.isLooped;
            audioSource.spatialBlend = soundSample.spatialBlend;

            pauseSoundWhenGameIsPaused = soundSample.pauseWhenGameIsPaused;

            initialVolume = soundSample.volume;
            
            if (newFadeInDuration > 0)
            {
                audioSource.volume = 0;

                audioSource.DOKill();
                audioSource.DOFade(soundSample.volume, newFadeInDuration);
            }
        }

        public void Play(float delay)
        {
            if (delay > 0)
                audioSource.PlayDelayed(delay);
            else
                audioSource.Play();

            isPaused = false;
        }

        public void Resume()
        {
            isPaused = false;
            audioSource.UnPause();
        }

        public void Pause()
        {
            isPaused = true;
            audioSource.Pause();
        }

        private void Update()
        {
            if (isPaused) return;

            if (trackingTransform)
                gameObject.transform.position = trackingTransform.position;

            if (!audioSource.isPlaying && !isPaused)
                Stop();
        }

        public void Stop()
        {
            isPaused = true;
            audioSource.Stop();
            onStopped.Invoke(id);
        }

        public bool IsPlaying()
        {
            return audioSource.isPlaying;
        }

        public void FadeOut(float duration)
        {
            audioSource.DOKill();
            audioSource.DOFade(0f, duration)
                .SetUpdate(UpdateType.Normal, true)
                .OnComplete(Stop);
        }

        public void SetVolume(float volume, float fadeDuration = 0f)
        {
            if (fadeDuration > float.Epsilon)
            {
                audioSource.DOFade(volume, fadeDuration);
            }
            else
            {
                audioSource.volume = volume;
            }
        }

        public void OnSpawn()
        {
        }

        public void OnDespawn()
        {
            id = -1;

            isPaused = true;
            trackingTransform = default;

            onStopped.RemoveAllListeners();
        }
    }
}