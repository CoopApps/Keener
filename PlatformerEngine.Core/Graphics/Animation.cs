using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System;
using System.Collections.Generic;

namespace PlatformerEngine.Core.Graphics
{
    /// <summary>
    /// Represents a single animation clip (sequence of frames)
    /// </summary>
    public class AnimationClip
    {
        public string Name { get; set; }
        public Rectangle[] Frames { get; set; }
        public float FrameDuration { get; set; } = 0.1f;  // Duration of each frame in seconds
        public bool Loop { get; set; } = true;
        public Action OnComplete { get; set; }  // Called when animation completes

        public AnimationClip(string name, int frameCount, Rectangle[] frames, float frameDuration = 0.1f, bool loop = true)
        {
            Name = name;
            Frames = frames;
            FrameDuration = frameDuration;
            Loop = loop;
        }

        /// <summary>
        /// Create animation from sprite sheet grid
        /// </summary>
        public static AnimationClip FromSpriteSheet(
            string name,
            int startX, int startY,
            int frameWidth, int frameHeight,
            int frameCount,
            float frameDuration = 0.1f,
            bool loop = true)
        {
            Rectangle[] frames = new Rectangle[frameCount];

            for (int i = 0; i < frameCount; i++)
            {
                frames[i] = new Rectangle(
                    startX + (i * frameWidth),
                    startY,
                    frameWidth,
                    frameHeight
                );
            }

            return new AnimationClip(name, frameCount, frames, frameDuration, loop);
        }

        /// <summary>
        /// Create animation from sprite sheet with row/column layout
        /// </summary>
        public static AnimationClip FromGrid(
            string name,
            int row, int column,
            int frameWidth, int frameHeight,
            int frameCount,
            int columns,
            float frameDuration = 0.1f,
            bool loop = true)
        {
            Rectangle[] frames = new Rectangle[frameCount];

            for (int i = 0; i < frameCount; i++)
            {
                int currentColumn = (column + i) % columns;
                int currentRow = row + ((column + i) / columns);

                frames[i] = new Rectangle(
                    currentColumn * frameWidth,
                    currentRow * frameHeight,
                    frameWidth,
                    frameHeight
                );
            }

            return new AnimationClip(name, frameCount, frames, frameDuration, loop);
        }
    }

    /// <summary>
    /// Manages and plays sprite sheet animations
    /// </summary>
    public class AnimationController
    {
        private Dictionary<string, AnimationClip> clips = new Dictionary<string, AnimationClip>();
        private AnimationClip currentClip;
        private int currentFrame;
        private float frameTimer;
        private bool isPlaying;

        public Texture2D SpriteSheet { get; set; }
        public string CurrentAnimation => currentClip?.Name;
        public int CurrentFrame => currentFrame;
        public bool IsPlaying => isPlaying;
        public bool IsFinished => currentClip != null && !currentClip.Loop && currentFrame >= currentClip.Frames.Length - 1;

        // Animation speed multiplier
        public float PlaybackSpeed { get; set; } = 1.0f;

        public AnimationController(Texture2D spriteSheet = null)
        {
            SpriteSheet = spriteSheet;
            isPlaying = true;
        }

        /// <summary>
        /// Add an animation clip
        /// </summary>
        public void AddClip(AnimationClip clip)
        {
            clips[clip.Name] = clip;

            // If this is the first clip, set it as current
            if (currentClip == null)
            {
                currentClip = clip;
                currentFrame = 0;
            }
        }

        /// <summary>
        /// Add multiple clips
        /// </summary>
        public void AddClips(params AnimationClip[] animationClips)
        {
            foreach (var clip in animationClips)
            {
                AddClip(clip);
            }
        }

        /// <summary>
        /// Play an animation by name
        /// </summary>
        public void Play(string animationName, bool restart = false)
        {
            if (!clips.TryGetValue(animationName, out AnimationClip clip))
            {
                return; // Animation not found
            }

            // If already playing this animation and not restarting, do nothing
            if (currentClip == clip && !restart && isPlaying)
            {
                return;
            }

            currentClip = clip;
            currentFrame = 0;
            frameTimer = 0;
            isPlaying = true;
        }

        /// <summary>
        /// Stop the current animation
        /// </summary>
        public void Stop()
        {
            isPlaying = false;
        }

        /// <summary>
        /// Resume the current animation
        /// </summary>
        public void Resume()
        {
            isPlaying = true;
        }

        /// <summary>
        /// Reset to first frame
        /// </summary>
        public void Reset()
        {
            currentFrame = 0;
            frameTimer = 0;
        }

        /// <summary>
        /// Update animation
        /// </summary>
        public void Update(GameTime gameTime)
        {
            if (!isPlaying || currentClip == null || currentClip.Frames.Length == 0)
                return;

            float deltaTime = (float)gameTime.ElapsedGameTime.TotalSeconds;
            frameTimer += deltaTime * PlaybackSpeed;

            // Check if it's time to advance frame
            if (frameTimer >= currentClip.FrameDuration)
            {
                frameTimer -= currentClip.FrameDuration;
                currentFrame++;

                // Check if animation finished
                if (currentFrame >= currentClip.Frames.Length)
                {
                    if (currentClip.Loop)
                    {
                        currentFrame = 0;
                    }
                    else
                    {
                        currentFrame = currentClip.Frames.Length - 1;
                        isPlaying = false;
                        currentClip.OnComplete?.Invoke();
                    }
                }
            }
        }

        /// <summary>
        /// Get current frame rectangle
        /// </summary>
        public Rectangle? GetCurrentFrame()
        {
            if (currentClip == null || currentFrame >= currentClip.Frames.Length)
                return null;

            return currentClip.Frames[currentFrame];
        }

        /// <summary>
        /// Draw the current animation frame
        /// </summary>
        public void Draw(SpriteBatch spriteBatch, Vector2 position, Color color, SpriteEffects effects = SpriteEffects.None, float layerDepth = 0f)
        {
            if (SpriteSheet == null) return;

            Rectangle? sourceRect = GetCurrentFrame();
            if (!sourceRect.HasValue) return;

            spriteBatch.Draw(
                SpriteSheet,
                position,
                sourceRect,
                color,
                0f,
                Vector2.Zero,
                1.0f,
                effects,
                layerDepth
            );
        }

        /// <summary>
        /// Check if a specific animation exists
        /// </summary>
        public bool HasAnimation(string animationName)
        {
            return clips.ContainsKey(animationName);
        }

        /// <summary>
        /// Get animation clip by name
        /// </summary>
        public AnimationClip GetClip(string animationName)
        {
            return clips.TryGetValue(animationName, out AnimationClip clip) ? clip : null;
        }

        /// <summary>
        /// Remove an animation clip
        /// </summary>
        public void RemoveClip(string animationName)
        {
            clips.Remove(animationName);
        }

        /// <summary>
        /// Clear all animation clips
        /// </summary>
        public void Clear()
        {
            clips.Clear();
            currentClip = null;
            currentFrame = 0;
            frameTimer = 0;
        }
    }

    /// <summary>
    /// Helper class for creating common animation patterns
    /// </summary>
    public static class AnimationHelper
    {
        /// <summary>
        /// Create a simple walk cycle animation
        /// </summary>
        public static AnimationClip CreateWalkCycle(string name, int frameWidth, int frameHeight, int frameCount, float speed = 0.1f)
        {
            return AnimationClip.FromSpriteSheet(name, 0, 0, frameWidth, frameHeight, frameCount, speed, true);
        }

        /// <summary>
        /// Create idle animation
        /// </summary>
        public static AnimationClip CreateIdle(string name, int frameWidth, int frameHeight, int startX = 0, int startY = 0)
        {
            Rectangle[] frames = new Rectangle[] {
                new Rectangle(startX, startY, frameWidth, frameHeight)
            };
            return new AnimationClip(name, 1, frames, 1.0f, true);
        }

        /// <summary>
        /// Create jump animation (single frame)
        /// </summary>
        public static AnimationClip CreateJump(string name, int frameWidth, int frameHeight, int startX, int startY)
        {
            Rectangle[] frames = new Rectangle[] {
                new Rectangle(startX, startY, frameWidth, frameHeight)
            };
            return new AnimationClip(name, 1, frames, 1.0f, false);
        }

        /// <summary>
        /// Create a standard platformer animation set
        /// </summary>
        public static void CreatePlatformerAnimations(
            AnimationController controller,
            int frameWidth,
            int frameHeight,
            int idleFrame = 0,
            int walkStartFrame = 1,
            int walkFrameCount = 4,
            int jumpFrame = 5,
            int fallFrame = 6)
        {
            // Idle
            controller.AddClip(new AnimationClip("Idle", 1, new[] {
                new Rectangle(idleFrame * frameWidth, 0, frameWidth, frameHeight)
            }, 1.0f, true));

            // Walk
            Rectangle[] walkFrames = new Rectangle[walkFrameCount];
            for (int i = 0; i < walkFrameCount; i++)
            {
                walkFrames[i] = new Rectangle((walkStartFrame + i) * frameWidth, 0, frameWidth, frameHeight);
            }
            controller.AddClip(new AnimationClip("Walk", walkFrameCount, walkFrames, 0.1f, true));

            // Jump
            controller.AddClip(new AnimationClip("Jump", 1, new[] {
                new Rectangle(jumpFrame * frameWidth, 0, frameWidth, frameHeight)
            }, 1.0f, false));

            // Fall
            controller.AddClip(new AnimationClip("Fall", 1, new[] {
                new Rectangle(fallFrame * frameWidth, 0, frameWidth, frameHeight)
            }, 1.0f, false));
        }
    }
}
