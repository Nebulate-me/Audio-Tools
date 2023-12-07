using System;
using System.Collections.Generic;
using UnityEngine;
using Utilities;
using Utilities.Prefabs;
using Utilities.RandomService;
using Zenject;
using Object = UnityEngine.Object;

namespace AudioTools.Sound
{
    public class SoundManager<TSoundType> : ISoundManager<TSoundType>, IInitializable where TSoundType : Enum, IEquatable<TSoundType>
    {
        private const float DefaultThrottlingInterval = 0.05f;
        private readonly Dictionary<int, SoundSource> allActiveSounds = new();
        private readonly Dictionary<int, bool> manualHandlingSounds = new();
        private readonly Dictionary<TSoundType, float> playTimings = new();
        private readonly Dictionary<int, AudioClip> audioClips = new();
        private readonly IPrefabPool prefabPool;
        private readonly IRandomService randomService;
        private readonly ISoundIdGenerator soundIdGenerator = new SoundIdGenerator();
        private SoundsContainer soundsContainer;
        private readonly TSoundType _defaultSoundType;

        public SoundManager(IPrefabPool prefabPool, IRandomService randomService, TSoundType defaultSoundType)
        {
            this.prefabPool = prefabPool;
            this.randomService = randomService;
            _defaultSoundType = defaultSoundType; 

            foreach (var type in (TSoundType[])Enum.GetValues(typeof(TSoundType))) playTimings.Add(type, 0);
        }

        [Inject]
        public void Initialize()
        {
            soundsContainer = Object.FindObjectOfType<SoundsContainer>();
        }

        public void StopAll()
        {
            var list = ListPool<int>.Instance.Spawn();
            list.AddRange(allActiveSounds.Keys);

            foreach (var soundId in list)
            {
                if (!allActiveSounds.TryGetValue(soundId, out var value))
                    continue;

                if (manualHandlingSounds.SafeGet(soundId))
                    continue;

                value.Stop();
                allActiveSounds.Remove(soundId);
            }

            ListPool<int>.Instance.Despawn(list);
        }

        public void UnloadAllAudioClips()
        {
            foreach (var audioClip in audioClips.Values)
            {
                if (audioClip.loadState == AudioDataLoadState.Loaded)
                    audioClip.UnloadAudioData();
            }
        }

        public int Play(SoundSample<TSoundType> soundPrefab)
        {
            return Play(soundPrefab, null);
        }

        public int Play(SoundSample<TSoundType> soundPrefab, Transform transform, bool isTracking = false, float fadeInDuration = 0,
            float delay = 0)
        {
            return DoPlay(soundPrefab, transform, isTracking, fadeInDuration, delay);
        }

        public int PlayMusic(SoundSample<TSoundType> musicPrefab, float fadeInDuration = 0f, float delay = 0f)
        {
            // var playerData = playerRepository.PlayerData;
            //
            // if (!playerData.IsMusicOn)
            //     return -1;

            var soundId = DoPlay(musicPrefab, null, false, fadeInDuration, delay);
            SetSoundManualHandling(soundId);

            return soundId;
        }

        public void SetSoundManualHandling(int soundId)
        {
            if (!allActiveSounds.TryGetValue(soundId, out var value))
                return;

            manualHandlingSounds[soundId] = true;
        }

        // fade out and stop sound
        public void FadeOut(int soundId, float duration)
        {
            if (!allActiveSounds.TryGetValue(soundId, out var value))
                return;

            if (!value)
            {
                allActiveSounds.Remove(soundId);
                return;
            }

            value.FadeOut(duration);
        }

        public bool IsPlaying(int soundId)
        {
            allActiveSounds.TryGetValue(soundId, out var value);
            return value != null && value.IsPlaying();
        }

        public void Pause(int soundId)
        {
            if (allActiveSounds.TryGetValue(soundId, out var value))
                value.Pause();
        }

        public void Mute(int soundId, float muteRate = 0.1f, float fadeDuration = 0f)
        {
            if (allActiveSounds.TryGetValue(soundId, out var value))
                value.SetVolume(value.InitialVolume * muteRate, fadeDuration);
        }

        public void Unmute(int soundId, float fadeDuration = 0f)
        {
            if (allActiveSounds.TryGetValue(soundId, out var value))
                value.SetVolume(value.InitialVolume, fadeDuration);
        }

        public void Stop(int soundId)
        {
            if (allActiveSounds.TryGetValue(soundId, out var value))
                value.Stop();
        }

        public void Resume(int soundId)
        {
            if (allActiveSounds.TryGetValue(soundId, out var value))
                value.Resume();
        }

        private bool IsDefaultSoundType(TSoundType soundType)
        {
            return EqualityComparer<TSoundType>.Default.Equals(soundType, _defaultSoundType);
        }

        private int DoPlay(SoundSample<TSoundType> soundSample, Transform transform, bool isTracking, float fadeInDuration,
            float delay)
        {
            var throttlingInterval = soundSample.throttlingIntervalSeconds == 0
                ? DefaultThrottlingInterval
                : soundSample.throttlingIntervalSeconds;
            var soundType = soundSample.soundType;

            if (!IsDefaultSoundType(soundType) && playTimings[soundType] + throttlingInterval > Time.time)
                return -1;

            if (!randomService.Chance(soundSample.probability))
                return -1;

            if (!IsDefaultSoundType(soundType))
                playTimings[soundType] = Time.time;

            var instance = prefabPool.Spawn(soundSample.soundSource.gameObject, soundsContainer.transform);
            var soundSource = instance.GetComponent<SoundSource>();

            foreach (var audioClip in soundSample.audioClips)
            {
                if (audioClip.preloadAudioData || audioClip.loadType == AudioClipLoadType.Streaming)
                    continue;

                if (audioClip.loadState != AudioDataLoadState.Loaded)
                {
                    audioClip.LoadAudioData();
                    var instanceID = audioClip.GetInstanceID();

                    if (!audioClips.ContainsKey(instanceID))
                        audioClips.Add(instanceID, audioClip);
                }
            }

            var soundId = soundIdGenerator.GetNextId();
            soundSource.Configure(soundId, soundSample, transform, isTracking, fadeInDuration);
            soundSource.onStopped.AddListener(OnSoundStopped);
            allActiveSounds.Add(soundId, soundSource);
            soundSource.Play(delay);
            return soundId;
        }

        private void OnSoundStopped(int soundId)
        {
            if (!allActiveSounds.TryGetValue(soundId, out var value))
                return;

            prefabPool.Despawn(value.gameObject);
            allActiveSounds.Remove(soundId);

            if (manualHandlingSounds.SafeGet(soundId))
                manualHandlingSounds.Remove(soundId);
        }
    }
}