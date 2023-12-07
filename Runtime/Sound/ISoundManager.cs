using System;
using UnityEngine;

namespace AudioTools.Sound
{
    public interface ISoundManager<SoundType> where SoundType : Enum
    {
        int Play(SoundSample<SoundType> soundPrefab);
        int Play(SoundSample<SoundType> soundPrefab, Transform transform, bool isTracking = false, float fadeInDuration = 0, float delay = 0);
        void FadeOut(int soundId, float duration);
        void Mute(int soundId, float muteRate = 0.1f, float fadeDuration = 0f);
        void Unmute(int soundId, float fadeDuration = 0f);
        bool IsPlaying(int soundId);
        void Stop(int soundId);
        void Resume(int soundId);
        void Pause(int soundId);
        void StopAll();
        void UnloadAllAudioClips();
        void SetSoundManualHandling(int soundId);
        int PlayMusic(SoundSample<SoundType> musicPrefab, float fadeInDuration = 0f, float delay = 0f);
    };
}