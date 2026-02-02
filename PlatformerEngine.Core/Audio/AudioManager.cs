using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Audio;
using Microsoft.Xna.Framework.Media;
using System;
using System.Collections.Generic;

namespace PlatformerEngine.Core.Audio
{
    /// <summary>
    /// Centralized audio management for music and sound effects
    /// </summary>
    public class AudioManager
    {
        // Singleton
        private static AudioManager instance;
        public static AudioManager Instance => instance ??= new AudioManager();

        // Sound effect library
        private Dictionary<string, SoundEffect> soundEffects = new Dictionary<string, SoundEffect>();
        private Dictionary<string, SoundEffectInstance> loopingSounds = new Dictionary<string, SoundEffectInstance>();

        // Music library
        private Dictionary<string, Song> songs = new Dictionary<string, Song>();
        private string currentSongName;

        // Volume settings
        public float MasterVolume { get; set; } = 1.0f;
        public float MusicVolume { get; set; } = 0.7f;
        public float SoundEffectVolume { get; set; } = 0.8f;

        // State
        public bool IsMusicEnabled { get; set; } = true;
        public bool AreSoundEffectsEnabled { get; set; } = true;
        public bool IsMusicPaused { get; private set; }

        // Fade
        private float musicFadeSpeed = 1.0f;
        private float targetMusicVolume = 1.0f;
        private float currentMusicVolume = 1.0f;

        private AudioManager()
        {
            // Initialize with system defaults
            MediaPlayer.Volume = MusicVolume * MasterVolume;
            MediaPlayer.IsRepeating = true;
        }

        #region Sound Effects

        /// <summary>
        /// Load a sound effect
        /// </summary>
        public void LoadSoundEffect(string name, SoundEffect soundEffect)
        {
            soundEffects[name] = soundEffect;
        }

        /// <summary>
        /// Play a sound effect
        /// </summary>
        public void PlaySound(string name, float volume = 1.0f, float pitch = 0f, float pan = 0f)
        {
            if (!AreSoundEffectsEnabled)
                return;

            if (soundEffects.TryGetValue(name, out SoundEffect sound))
            {
                float finalVolume = volume * SoundEffectVolume * MasterVolume;
                sound.Play(finalVolume, pitch, pan);
            }
        }

        /// <summary>
        /// Play a sound effect with random pitch variation
        /// </summary>
        public void PlaySoundVaried(string name, float volume = 1.0f, float pitchVariation = 0.2f)
        {
            Random random = new Random();
            float pitch = (float)(random.NextDouble() * 2.0 - 1.0) * pitchVariation;
            PlaySound(name, volume, pitch, 0f);
        }

        /// <summary>
        /// Play a looping sound effect
        /// </summary>
        public void PlayLoopingSound(string name, float volume = 1.0f)
        {
            if (!AreSoundEffectsEnabled)
                return;

            if (soundEffects.TryGetValue(name, out SoundEffect sound))
            {
                // Stop existing instance if playing
                StopLoopingSound(name);

                // Create new instance
                var instance = sound.CreateInstance();
                instance.IsLooped = true;
                instance.Volume = volume * SoundEffectVolume * MasterVolume;
                instance.Play();

                loopingSounds[name] = instance;
            }
        }

        /// <summary>
        /// Stop a looping sound effect
        /// </summary>
        public void StopLoopingSound(string name)
        {
            if (loopingSounds.TryGetValue(name, out SoundEffectInstance instance))
            {
                instance.Stop();
                instance.Dispose();
                loopingSounds.Remove(name);
            }
        }

        /// <summary>
        /// Stop all looping sounds
        /// </summary>
        public void StopAllLoopingSounds()
        {
            foreach (var instance in loopingSounds.Values)
            {
                instance.Stop();
                instance.Dispose();
            }
            loopingSounds.Clear();
        }

        /// <summary>
        /// Check if a sound effect is loaded
        /// </summary>
        public bool HasSound(string name)
        {
            return soundEffects.ContainsKey(name);
        }

        #endregion

        #region Music

        /// <summary>
        /// Load a music track
        /// </summary>
        public void LoadMusic(string name, Song song)
        {
            songs[name] = song;
        }

        /// <summary>
        /// Play music
        /// </summary>
        public void PlayMusic(string name, bool loop = true, float fadeInDuration = 0f)
        {
            if (!IsMusicEnabled || !songs.TryGetValue(name, out Song song))
                return;

            // Don't restart if already playing
            if (currentSongName == name && MediaPlayer.State == MediaState.Playing)
                return;

            currentSongName = name;
            MediaPlayer.IsRepeating = loop;

            if (fadeInDuration > 0)
            {
                // Start at 0 volume and fade in
                currentMusicVolume = 0f;
                targetMusicVolume = MusicVolume;
                musicFadeSpeed = 1.0f / fadeInDuration;
                MediaPlayer.Volume = 0f;
            }
            else
            {
                currentMusicVolume = MusicVolume;
                MediaPlayer.Volume = MusicVolume * MasterVolume;
            }

            MediaPlayer.Play(song);
            IsMusicPaused = false;
        }

        /// <summary>
        /// Stop music
        /// </summary>
        public void StopMusic(float fadeOutDuration = 0f)
        {
            if (fadeOutDuration > 0)
            {
                targetMusicVolume = 0f;
                musicFadeSpeed = 1.0f / fadeOutDuration;
            }
            else
            {
                MediaPlayer.Stop();
                currentSongName = null;
                IsMusicPaused = false;
            }
        }

        /// <summary>
        /// Pause music
        /// </summary>
        public void PauseMusic()
        {
            if (MediaPlayer.State == MediaState.Playing)
            {
                MediaPlayer.Pause();
                IsMusicPaused = true;
            }
        }

        /// <summary>
        /// Resume music
        /// </summary>
        public void ResumeMusic()
        {
            if (IsMusicPaused)
            {
                MediaPlayer.Resume();
                IsMusicPaused = false;
            }
        }

        /// <summary>
        /// Check if music is playing
        /// </summary>
        public bool IsMusicPlaying()
        {
            return MediaPlayer.State == MediaState.Playing;
        }

        /// <summary>
        /// Get currently playing music name
        /// </summary>
        public string GetCurrentMusic()
        {
            return currentSongName;
        }

        #endregion

        #region Volume Control

        /// <summary>
        /// Set master volume (affects both music and sound effects)
        /// </summary>
        public void SetMasterVolume(float volume)
        {
            MasterVolume = MathHelper.Clamp(volume, 0f, 1f);
            UpdateVolumes();
        }

        /// <summary>
        /// Set music volume
        /// </summary>
        public void SetMusicVolume(float volume)
        {
            MusicVolume = MathHelper.Clamp(volume, 0f, 1f);
            UpdateVolumes();
        }

        /// <summary>
        /// Set sound effect volume
        /// </summary>
        public void SetSoundEffectVolume(float volume)
        {
            SoundEffectVolume = MathHelper.Clamp(volume, 0f, 1f);
            UpdateVolumes();
        }

        /// <summary>
        /// Update all volume levels
        /// </summary>
        private void UpdateVolumes()
        {
            // Update music volume
            MediaPlayer.Volume = currentMusicVolume * MasterVolume;

            // Update looping sound volumes
            foreach (var instance in loopingSounds.Values)
            {
                instance.Volume = SoundEffectVolume * MasterVolume;
            }
        }

        #endregion

        #region Update

        /// <summary>
        /// Update audio manager (handles fades, etc.)
        /// </summary>
        public void Update(GameTime gameTime)
        {
            float deltaTime = (float)gameTime.ElapsedGameTime.TotalSeconds;

            // Handle music fade
            if (Math.Abs(currentMusicVolume - targetMusicVolume) > 0.01f)
            {
                if (currentMusicVolume < targetMusicVolume)
                {
                    currentMusicVolume += musicFadeSpeed * deltaTime;
                    if (currentMusicVolume > targetMusicVolume)
                        currentMusicVolume = targetMusicVolume;
                }
                else
                {
                    currentMusicVolume -= musicFadeSpeed * deltaTime;
                    if (currentMusicVolume < targetMusicVolume)
                        currentMusicVolume = targetMusicVolume;
                }

                MediaPlayer.Volume = currentMusicVolume * MasterVolume;

                // Stop music when faded out
                if (currentMusicVolume <= 0f && targetMusicVolume <= 0f)
                {
                    MediaPlayer.Stop();
                    currentSongName = null;
                }
            }
        }

        #endregion

        #region Cleanup

        /// <summary>
        /// Clean up all audio resources
        /// </summary>
        public void Dispose()
        {
            StopAllLoopingSounds();
            MediaPlayer.Stop();
            soundEffects.Clear();
            songs.Clear();
        }

        #endregion

        #region Helper Methods

        /// <summary>
        /// Quick access - play common sounds
        /// </summary>
        public void PlayJumpSound() => PlaySound("jump", 0.7f);
        public void PlayLandSound() => PlaySound("land", 0.5f);
        public void PlayHurtSound() => PlaySound("hurt", 0.8f);
        public void PlayDeathSound() => PlaySound("death", 1.0f);
        public void PlayCollectSound() => PlaySoundVaried("collect", 0.6f, 0.15f);
        public void PlayShootSound() => PlaySound("shoot", 0.7f);
        public void PlayExplosionSound() => PlaySound("explosion", 1.0f);
        public void PlayPowerUpSound() => PlaySound("powerup", 0.8f);

        #endregion
    }

    /// <summary>
    /// Simple audio configuration for saving/loading settings
    /// </summary>
    public class AudioSettings
    {
        public float MasterVolume { get; set; } = 1.0f;
        public float MusicVolume { get; set; } = 0.7f;
        public float SoundEffectVolume { get; set; } = 0.8f;
        public bool MusicEnabled { get; set; } = true;
        public bool SoundEffectsEnabled { get; set; } = true;

        public void Apply()
        {
            var audio = AudioManager.Instance;
            audio.SetMasterVolume(MasterVolume);
            audio.SetMusicVolume(MusicVolume);
            audio.SetSoundEffectVolume(SoundEffectVolume);
            audio.IsMusicEnabled = MusicEnabled;
            audio.AreSoundEffectsEnabled = SoundEffectsEnabled;
        }

        public static AudioSettings FromCurrent()
        {
            var audio = AudioManager.Instance;
            return new AudioSettings
            {
                MasterVolume = audio.MasterVolume,
                MusicVolume = audio.MusicVolume,
                SoundEffectVolume = audio.SoundEffectVolume,
                MusicEnabled = audio.IsMusicEnabled,
                SoundEffectsEnabled = audio.AreSoundEffectsEnabled
            };
        }
    }
}
