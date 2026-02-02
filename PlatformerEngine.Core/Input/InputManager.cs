using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Input;
using System;
using System.Collections.Generic;

namespace PlatformerEngine.Core.Input
{
    /// <summary>
    /// Input actions that can be mapped to keys/buttons
    /// </summary>
    public enum InputAction
    {
        MoveLeft,
        MoveRight,
        Jump,
        Duck,
        Run,
        Shoot,
        Interact,
        Pause,
        Confirm,
        Cancel
    }

    /// <summary>
    /// Comprehensive input manager supporting keyboard, gamepad, and action mappings
    /// </summary>
    public class InputManager
    {
        // Singleton pattern for easy access
        private static InputManager instance;
        public static InputManager Instance => instance ??= new InputManager();

        // Current and previous states
        private KeyboardState currentKeyboardState;
        private KeyboardState previousKeyboardState;
        private GamePadState currentGamePadState;
        private GamePadState previousGamePadState;
        private MouseState currentMouseState;
        private MouseState previousMouseState;

        // Input configuration
        public PlayerIndex GamePadIndex { get; set; } = PlayerIndex.One;
        public bool GamePadEnabled { get; set; } = true;
        public float DeadZone { get; set; } = 0.2f;

        // Action mappings (keyboard)
        private Dictionary<InputAction, Keys[]> keyboardMappings = new Dictionary<InputAction, Keys[]>
        {
            { InputAction.MoveLeft, new[] { Keys.Left, Keys.A } },
            { InputAction.MoveRight, new[] { Keys.Right, Keys.D } },
            { InputAction.Jump, new[] { Keys.Space, Keys.W, Keys.Up } },
            { InputAction.Duck, new[] { Keys.Down, Keys.S, Keys.LeftControl } },
            { InputAction.Run, new[] { Keys.LeftShift, Keys.RightShift } },
            { InputAction.Shoot, new[] { Keys.Z, Keys.X } },
            { InputAction.Interact, new[] { Keys.E, Keys.Enter } },
            { InputAction.Pause, new[] { Keys.Escape, Keys.P } },
            { InputAction.Confirm, new[] { Keys.Enter, Keys.Space } },
            { InputAction.Cancel, new[] { Keys.Escape, Keys.Back } }
        };

        // Action mappings (gamepad)
        private Dictionary<InputAction, Buttons[]> gamepadMappings = new Dictionary<InputAction, Buttons[]>
        {
            { InputAction.Jump, new[] { Buttons.A } },
            { InputAction.Duck, new[] { Buttons.B } },
            { InputAction.Run, new[] { Buttons.X } },
            { InputAction.Shoot, new[] { Buttons.Y } },
            { InputAction.Interact, new[] { Buttons.B } },
            { InputAction.Pause, new[] { Buttons.Start } },
            { InputAction.Confirm, new[] { Buttons.A } },
            { InputAction.Cancel, new[] { Buttons.B } }
        };

        private InputManager()
        {
            // Initialize with default states
            currentKeyboardState = Keyboard.GetState();
            previousKeyboardState = currentKeyboardState;
            currentGamePadState = GamePad.GetState(GamePadIndex);
            previousGamePadState = currentGamePadState;
            currentMouseState = Mouse.GetState();
            previousMouseState = currentMouseState;
        }

        /// <summary>
        /// Update input states - call once per frame at the start of Update
        /// </summary>
        public void Update()
        {
            previousKeyboardState = currentKeyboardState;
            previousGamePadState = currentGamePadState;
            previousMouseState = currentMouseState;

            currentKeyboardState = Keyboard.GetState();
            currentGamePadState = GamePad.GetState(GamePadIndex);
            currentMouseState = Mouse.GetState();
        }

        #region Action-Based Input

        /// <summary>
        /// Check if an action is currently pressed
        /// </summary>
        public bool IsActionDown(InputAction action)
        {
            // Check keyboard
            if (keyboardMappings.TryGetValue(action, out Keys[] keys))
            {
                foreach (var key in keys)
                {
                    if (currentKeyboardState.IsKeyDown(key))
                        return true;
                }
            }

            // Check gamepad buttons
            if (GamePadEnabled && gamepadMappings.TryGetValue(action, out Buttons[] buttons))
            {
                foreach (var button in buttons)
                {
                    if (currentGamePadState.IsButtonDown(button))
                        return true;
                }
            }

            // Check gamepad analog for movement
            if (GamePadEnabled)
            {
                switch (action)
                {
                    case InputAction.MoveLeft:
                        return currentGamePadState.ThumbSticks.Left.X < -DeadZone;
                    case InputAction.MoveRight:
                        return currentGamePadState.ThumbSticks.Left.X > DeadZone;
                    case InputAction.Jump:
                        return currentGamePadState.ThumbSticks.Left.Y > DeadZone ||
                               currentGamePadState.IsButtonDown(Buttons.A);
                    case InputAction.Duck:
                        return currentGamePadState.ThumbSticks.Left.Y < -DeadZone ||
                               currentGamePadState.IsButtonDown(Buttons.B);
                }
            }

            return false;
        }

        /// <summary>
        /// Check if an action was just pressed this frame
        /// </summary>
        public bool IsActionPressed(InputAction action)
        {
            // Check keyboard
            if (keyboardMappings.TryGetValue(action, out Keys[] keys))
            {
                foreach (var key in keys)
                {
                    if (currentKeyboardState.IsKeyDown(key) && previousKeyboardState.IsKeyUp(key))
                        return true;
                }
            }

            // Check gamepad
            if (GamePadEnabled && gamepadMappings.TryGetValue(action, out Buttons[] buttons))
            {
                foreach (var button in buttons)
                {
                    if (currentGamePadState.IsButtonDown(button) && previousGamePadState.IsButtonUp(button))
                        return true;
                }
            }

            return false;
        }

        /// <summary>
        /// Check if an action was just released this frame
        /// </summary>
        public bool IsActionReleased(InputAction action)
        {
            // Check keyboard
            if (keyboardMappings.TryGetValue(action, out Keys[] keys))
            {
                foreach (var key in keys)
                {
                    if (currentKeyboardState.IsKeyUp(key) && previousKeyboardState.IsKeyDown(key))
                        return true;
                }
            }

            // Check gamepad
            if (GamePadEnabled && gamepadMappings.TryGetValue(action, out Buttons[] buttons))
            {
                foreach (var button in buttons)
                {
                    if (currentGamePadState.IsButtonUp(button) && previousGamePadState.IsButtonDown(button))
                        return true;
                }
            }

            return false;
        }

        /// <summary>
        /// Get horizontal movement axis (-1 to 1)
        /// </summary>
        public float GetHorizontalAxis()
        {
            float axis = 0f;

            // Keyboard
            if (IsActionDown(InputAction.MoveLeft)) axis -= 1f;
            if (IsActionDown(InputAction.MoveRight)) axis += 1f;

            // Gamepad (if no keyboard input)
            if (GamePadEnabled && axis == 0f)
            {
                float stickValue = currentGamePadState.ThumbSticks.Left.X;
                if (Math.Abs(stickValue) > DeadZone)
                    axis = stickValue;
            }

            return MathHelper.Clamp(axis, -1f, 1f);
        }

        /// <summary>
        /// Get vertical movement axis (-1 to 1)
        /// </summary>
        public float GetVerticalAxis()
        {
            float axis = 0f;

            // Keyboard (inverted - down is negative, up is positive)
            if (IsActionDown(InputAction.Duck)) axis -= 1f;
            if (IsActionDown(InputAction.Jump)) axis += 1f;

            // Gamepad (if no keyboard input)
            if (GamePadEnabled && axis == 0f)
            {
                float stickValue = currentGamePadState.ThumbSticks.Left.Y;
                if (Math.Abs(stickValue) > DeadZone)
                    axis = stickValue;
            }

            return MathHelper.Clamp(axis, -1f, 1f);
        }

        #endregion

        #region Raw Keyboard Input

        public bool IsKeyDown(Keys key) => currentKeyboardState.IsKeyDown(key);
        public bool IsKeyUp(Keys key) => currentKeyboardState.IsKeyUp(key);
        public bool IsKeyPressed(Keys key) => currentKeyboardState.IsKeyDown(key) && previousKeyboardState.IsKeyUp(key);
        public bool IsKeyReleased(Keys key) => currentKeyboardState.IsKeyUp(key) && previousKeyboardState.IsKeyDown(key);

        #endregion

        #region Raw GamePad Input

        public bool IsButtonDown(Buttons button) => GamePadEnabled && currentGamePadState.IsButtonDown(button);
        public bool IsButtonUp(Buttons button) => !GamePadEnabled || currentGamePadState.IsButtonUp(button);
        public bool IsButtonPressed(Buttons button) => GamePadEnabled && currentGamePadState.IsButtonDown(button) && previousGamePadState.IsButtonUp(button);
        public bool IsButtonReleased(Buttons button) => GamePadEnabled && currentGamePadState.IsButtonUp(button) && previousGamePadState.IsButtonDown(button);

        public Vector2 GetLeftStick() => GamePadEnabled ? currentGamePadState.ThumbSticks.Left : Vector2.Zero;
        public Vector2 GetRightStick() => GamePadEnabled ? currentGamePadState.ThumbSticks.Right : Vector2.Zero;
        public float GetLeftTrigger() => GamePadEnabled ? currentGamePadState.Triggers.Left : 0f;
        public float GetRightTrigger() => GamePadEnabled ? currentGamePadState.Triggers.Right : 0f;

        public bool IsGamePadConnected() => GamePadEnabled && currentGamePadState.IsConnected;

        #endregion

        #region Mouse Input

        public Vector2 MousePosition => new Vector2(currentMouseState.X, currentMouseState.Y);
        public Vector2 MouseDelta => new Vector2(
            currentMouseState.X - previousMouseState.X,
            currentMouseState.Y - previousMouseState.Y
        );

        public bool IsLeftMouseDown() => currentMouseState.LeftButton == ButtonState.Pressed;
        public bool IsRightMouseDown() => currentMouseState.RightButton == ButtonState.Pressed;
        public bool IsMiddleMouseDown() => currentMouseState.MiddleButton == ButtonState.Pressed;

        public bool IsLeftMousePressed() => currentMouseState.LeftButton == ButtonState.Pressed &&
                                           previousMouseState.LeftButton == ButtonState.Released;
        public bool IsRightMousePressed() => currentMouseState.RightButton == ButtonState.Pressed &&
                                            previousMouseState.RightButton == ButtonState.Released;
        public bool IsMiddleMousePressed() => currentMouseState.MiddleButton == ButtonState.Pressed &&
                                             previousMouseState.MiddleButton == ButtonState.Released;

        public int GetScrollWheelDelta() => currentMouseState.ScrollWheelValue - previousMouseState.ScrollWheelValue;

        #endregion

        #region Custom Mappings

        /// <summary>
        /// Add or update a keyboard mapping for an action
        /// </summary>
        public void SetKeyboardMapping(InputAction action, params Keys[] keys)
        {
            keyboardMappings[action] = keys;
        }

        /// <summary>
        /// Add or update a gamepad mapping for an action
        /// </summary>
        public void SetGamePadMapping(InputAction action, params Buttons[] buttons)
        {
            gamepadMappings[action] = buttons;
        }

        /// <summary>
        /// Get current keyboard mapping for an action
        /// </summary>
        public Keys[] GetKeyboardMapping(InputAction action)
        {
            return keyboardMappings.TryGetValue(action, out Keys[] keys) ? keys : Array.Empty<Keys>();
        }

        /// <summary>
        /// Get current gamepad mapping for an action
        /// </summary>
        public Buttons[] GetGamePadMapping(InputAction action)
        {
            return gamepadMappings.TryGetValue(action, out Buttons[] buttons) ? buttons : Array.Empty<Buttons>();
        }

        #endregion

        #region Utility

        /// <summary>
        /// Vibrate the gamepad
        /// </summary>
        public void Vibrate(float leftMotor, float rightMotor, float duration = 0.1f)
        {
            if (GamePadEnabled && currentGamePadState.IsConnected)
            {
                GamePad.SetVibration(GamePadIndex, leftMotor, rightMotor);
                // Note: You'll need to create a timer system to stop vibration after duration
            }
        }

        /// <summary>
        /// Stop gamepad vibration
        /// </summary>
        public void StopVibration()
        {
            if (GamePadEnabled)
            {
                GamePad.SetVibration(GamePadIndex, 0f, 0f);
            }
        }

        #endregion
    }
}
