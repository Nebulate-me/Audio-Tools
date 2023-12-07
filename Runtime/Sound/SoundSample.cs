using System;
using System.ComponentModel;
using UnityEngine;
using UnityEngine.Audio;

namespace AudioTools.Sound
{
    public class SoundSample<SoundType> : MonoBehaviour where SoundType : Enum
    {
        public AudioClip[] audioClips;
        public AudioMixerGroup outputAudioMixerGroup;
        public SoundSource soundSource;
        public SoundType soundType;
        public bool isLooped = false;
        public bool pauseWhenGameIsPaused = false;
        public int lastPlayedIndex = 0;

        [Range(0, 1), DefaultValue(0)]
        public float pitchDelta = 0;
        
        [Range(0, 1), DefaultValue(0)]
        public float volumeDeltaPercent = 0;

        [Range(0, 1), DefaultValue(0.4f)]
        public float spatialBlend = 0.4f;
        
        [Range(-3, 3), DefaultValue(1)]
        public float pitch = 1;

        [Range(0, 1), DefaultValue(1)]
        public float volume;
        
        [Range(0, 1), DefaultValue(1)]
        public float probability;
        
        [DefaultValue(0.05f)]
        public float throttlingIntervalSeconds;
    }
}