using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System;
using System.Collections.Generic;

namespace PlatformerEngine.Core.Scenes
{
    /// <summary>
    /// Base class for all game scenes/states
    /// </summary>
    public abstract class Scene
    {
        public string Name { get; set; }
        public bool IsInitialized { get; private set; }
        public bool IsActive { get; set; } = true;
        public bool DrawBelow { get; set; } = false;  // Draw scenes below this one?
        public bool UpdateBelow { get; set; } = false;  // Update scenes below this one?

        protected Game game;
        protected SceneManager sceneManager;

        public Scene()
        {
        }

        /// <summary>
        /// Initialize the scene (called once)
        /// </summary>
        public virtual void Initialize(Game game, SceneManager sceneManager)
        {
            this.game = game;
            this.sceneManager = sceneManager;
            IsInitialized = true;
        }

        /// <summary>
        /// Load content for this scene
        /// </summary>
        public virtual void LoadContent()
        {
        }

        /// <summary>
        /// Unload content when scene is removed
        /// </summary>
        public virtual void UnloadContent()
        {
        }

        /// <summary>
        /// Called when scene becomes active
        /// </summary>
        public virtual void OnEnter()
        {
        }

        /// <summary>
        /// Called when scene becomes inactive
        /// </summary>
        public virtual void OnExit()
        {
        }

        /// <summary>
        /// Update scene logic
        /// </summary>
        public abstract void Update(GameTime gameTime);

        /// <summary>
        /// Draw the scene
        /// </summary>
        public abstract void Draw(SpriteBatch spriteBatch, GameTime gameTime);
    }

    /// <summary>
    /// Manages game scenes and transitions
    /// </summary>
    public class SceneManager
    {
        private static SceneManager instance;
        public static SceneManager Instance => instance;

        private Stack<Scene> sceneStack = new Stack<Scene>();
        private Dictionary<string, Scene> scenes = new Dictionary<string, Scene>();

        private Scene currentScene;
        private Game game;

        // Transitions
        private SceneTransition activeTransition;
        private bool isTransitioning;
        private Action transitionComplete;

        public Scene CurrentScene => currentScene;
        public bool IsTransitioning => isTransitioning;
        public GraphicsDevice GraphicsDevice { get; private set; }

        public SceneManager(Game game)
        {
            this.game = game;
            this.GraphicsDevice = game.GraphicsDevice;
            instance = this;
        }

        /// <summary>
        /// Register a scene with a name
        /// </summary>
        public void RegisterScene(string name, Scene scene)
        {
            scene.Name = name;
            scenes[name] = scene;

            if (!scene.IsInitialized)
            {
                scene.Initialize(game, this);
            }
        }

        /// <summary>
        /// Register a scene (uses scene's Name property)
        /// </summary>
        public void RegisterScene(Scene scene)
        {
            if (string.IsNullOrEmpty(scene.Name))
            {
                throw new ArgumentException("Scene must have a Name set");
            }

            scenes[scene.Name] = scene;

            if (!scene.IsInitialized)
            {
                scene.Initialize(game, this);
            }
        }

        /// <summary>
        /// Change to a different scene (replaces current scene)
        /// </summary>
        public void ChangeScene(string sceneName, SceneTransition transition = null)
        {
            if (!scenes.TryGetValue(sceneName, out Scene newScene))
            {
                throw new ArgumentException($"Scene '{sceneName}' not registered");
            }

            if (transition != null)
            {
                StartTransition(transition, () =>
                {
                    PerformSceneChange(newScene);
                });
            }
            else
            {
                PerformSceneChange(newScene);
            }
        }

        /// <summary>
        /// Push a scene onto the stack (keeps current scene)
        /// </summary>
        public void PushScene(string sceneName)
        {
            if (!scenes.TryGetValue(sceneName, out Scene newScene))
            {
                throw new ArgumentException($"Scene '{sceneName}' not registered");
            }

            if (currentScene != null)
            {
                sceneStack.Push(currentScene);
                currentScene.OnExit();
            }

            currentScene = newScene;
            currentScene.OnEnter();
            currentScene.LoadContent();
        }

        /// <summary>
        /// Pop the current scene and return to the previous one
        /// </summary>
        public void PopScene()
        {
            if (sceneStack.Count == 0)
            {
                throw new InvalidOperationException("Cannot pop scene - stack is empty");
            }

            if (currentScene != null)
            {
                currentScene.OnExit();
                currentScene.UnloadContent();
            }

            currentScene = sceneStack.Pop();
            currentScene.OnEnter();
        }

        /// <summary>
        /// Internal scene change
        /// </summary>
        private void PerformSceneChange(Scene newScene)
        {
            // Exit and unload current scene
            if (currentScene != null)
            {
                currentScene.OnExit();
                currentScene.UnloadContent();
            }

            // Clear scene stack
            sceneStack.Clear();

            // Enter and load new scene
            currentScene = newScene;
            currentScene.LoadContent();
            currentScene.OnEnter();
        }

        /// <summary>
        /// Start a scene transition
        /// </summary>
        private void StartTransition(SceneTransition transition, Action onComplete)
        {
            activeTransition = transition;
            activeTransition.Reset();
            isTransitioning = true;
            transitionComplete = onComplete;
        }

        /// <summary>
        /// Update the scene manager
        /// </summary>
        public void Update(GameTime gameTime)
        {
            // Update transition
            if (isTransitioning && activeTransition != null)
            {
                activeTransition.Update(gameTime);

                // Check if transition reached midpoint (scene change happens here)
                if (activeTransition.IsAtMidpoint && transitionComplete != null)
                {
                    transitionComplete.Invoke();
                    transitionComplete = null;
                }

                // Check if transition complete
                if (activeTransition.IsComplete)
                {
                    isTransitioning = false;
                    activeTransition = null;
                }
            }

            // Update current scene
            if (currentScene != null && currentScene.IsActive && !isTransitioning)
            {
                currentScene.Update(gameTime);
            }

            // Update scenes below if requested
            if (currentScene != null && currentScene.UpdateBelow)
            {
                foreach (var scene in sceneStack)
                {
                    if (scene.IsActive)
                    {
                        scene.Update(gameTime);
                    }
                }
            }
        }

        /// <summary>
        /// Draw the scene manager
        /// </summary>
        public void Draw(SpriteBatch spriteBatch, GameTime gameTime)
        {
            // Draw scenes below if requested
            if (currentScene != null && currentScene.DrawBelow)
            {
                var scenesToDraw = new List<Scene>(sceneStack);
                scenesToDraw.Reverse();

                foreach (var scene in scenesToDraw)
                {
                    scene.Draw(spriteBatch, gameTime);
                }
            }

            // Draw current scene
            if (currentScene != null)
            {
                currentScene.Draw(spriteBatch, gameTime);
            }

            // Draw transition effect
            if (isTransitioning && activeTransition != null)
            {
                activeTransition.Draw(spriteBatch);
            }
        }
    }

    /// <summary>
    /// Base class for scene transitions
    /// </summary>
    public abstract class SceneTransition
    {
        public float Duration { get; set; } = 1.0f;
        public float Progress { get; protected set; }
        public bool IsComplete => Progress >= 1.0f;
        public bool IsAtMidpoint { get; protected set; }

        protected bool midpointReached;

        public virtual void Reset()
        {
            Progress = 0f;
            IsAtMidpoint = false;
            midpointReached = false;
        }

        public virtual void Update(GameTime gameTime)
        {
            float deltaTime = (float)gameTime.ElapsedGameTime.TotalSeconds;
            Progress += deltaTime / Duration;

            if (Progress >= 0.5f && !midpointReached)
            {
                IsAtMidpoint = true;
                midpointReached = true;
            }
            else
            {
                IsAtMidpoint = false;
            }

            if (Progress > 1.0f)
                Progress = 1.0f;
        }

        public abstract void Draw(SpriteBatch spriteBatch);
    }

    /// <summary>
    /// Fade transition (fade to black)
    /// </summary>
    public class FadeTransition : SceneTransition
    {
        private Texture2D pixelTexture;
        private Rectangle screenBounds;
        public Color FadeColor { get; set; } = Color.Black;

        public FadeTransition(float duration = 1.0f)
        {
            Duration = duration;
        }

        private void EnsureTextureCreated(GraphicsDevice graphicsDevice)
        {
            if (pixelTexture == null)
            {
                pixelTexture = new Texture2D(graphicsDevice, 1, 1);
                pixelTexture.SetData(new[] { Color.White });
                screenBounds = graphicsDevice.Viewport.Bounds;
            }
        }

        public override void Draw(SpriteBatch spriteBatch)
        {
            if (spriteBatch.GraphicsDevice != null)
            {
                EnsureTextureCreated(spriteBatch.GraphicsDevice);

                // Fade out first half, fade in second half
                float alpha = Progress < 0.5f ? Progress * 2f : (1f - Progress) * 2f;

                spriteBatch.Begin();
                spriteBatch.Draw(pixelTexture, screenBounds, FadeColor * alpha);
                spriteBatch.End();
            }
        }
    }

    /// <summary>
    /// Slide transition (slide scenes left/right)
    /// </summary>
    public class SlideTransition : SceneTransition
    {
        public enum Direction
        {
            Left,
            Right,
            Up,
            Down
        }

        public Direction SlideDirection { get; set; }
        private int screenWidth;
        private int screenHeight;
        private bool initialized;

        public SlideTransition(Direction direction, float duration = 0.5f)
        {
            SlideDirection = direction;
            Duration = duration;
        }

        private void EnsureInitialized(GraphicsDevice graphicsDevice)
        {
            if (!initialized)
            {
                screenWidth = graphicsDevice.Viewport.Width;
                screenHeight = graphicsDevice.Viewport.Height;
                initialized = true;
            }
        }

        public override void Draw(SpriteBatch spriteBatch)
        {
            if (spriteBatch.GraphicsDevice != null)
            {
                EnsureInitialized(spriteBatch.GraphicsDevice);
                // Slide transition would manipulate the SpriteBatch transform
                // This is a simplified version - full implementation would be more complex
            }
        }
    }

    /// <summary>
    /// Crossfade transition
    /// </summary>
    public class CrossfadeTransition : SceneTransition
    {
        public CrossfadeTransition(float duration = 1.0f)
        {
            Duration = duration;
        }

        public override void Draw(SpriteBatch spriteBatch)
        {
            // Crossfade is handled by drawing both scenes with different alphas
            // This would be implemented by the SceneManager
        }
    }
}
