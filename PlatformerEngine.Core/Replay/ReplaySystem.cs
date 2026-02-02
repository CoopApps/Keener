using Microsoft.Xna.Framework;
using PlatformerEngine.Core.Input;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace PlatformerEngine.Core.Replay
{
    /// <summary>
    /// Recorded input frame
    /// </summary>
    public class InputFrame
    {
        [JsonPropertyName("frame")]
        public int FrameNumber { get; set; }

        [JsonPropertyName("horizontalAxis")]
        public float HorizontalAxis { get; set; }

        [JsonPropertyName("verticalAxis")]
        public float VerticalAxis { get; set; }

        [JsonPropertyName("actions")]
        public List<InputAction> PressedActions { get; set; } = new List<InputAction>();

        [JsonPropertyName("releasedActions")]
        public List<InputAction> ReleasedActions { get; set; } = new List<InputAction>();
    }

    /// <summary>
    /// Complete replay data
    /// </summary>
    public class ReplayData
    {
        [JsonPropertyName("version")]
        public string Version { get; set; } = "1.0";

        [JsonPropertyName("levelName")]
        public string LevelName { get; set; }

        [JsonPropertyName("recordDate")]
        public DateTime RecordDate { get; set; }

        [JsonPropertyName("totalFrames")]
        public int TotalFrames { get; set; }

        [JsonPropertyName("duration")]
        public double DurationSeconds { get; set; }

        [JsonPropertyName("playerName")]
        public string PlayerName { get; set; } = "Player";

        [JsonPropertyName("randomSeed")]
        public int RandomSeed { get; set; }

        [JsonPropertyName("frames")]
        public List<InputFrame> Frames { get; set; } = new List<InputFrame>();

        [JsonPropertyName("metadata")]
        public Dictionary<string, string> Metadata { get; set; } = new Dictionary<string, string>();
    }

    /// <summary>
    /// Records and plays back gameplay for demos and replays
    /// </summary>
    public class ReplaySystem
    {
        private ReplayData currentReplay;
        private int currentFrame;
        private bool isRecording;
        private bool isPlayingBack;
        private InputManager input;
        private Random recordingRandom;

        // Recording state
        private HashSet<InputAction> lastFrameActions = new HashSet<InputAction>();

        // Playback state
        private Dictionary<InputAction, bool> simulatedActions = new Dictionary<InputAction, bool>();
        private float simulatedHorizontal;
        private float simulatedVertical;

        public bool IsRecording => isRecording;
        public bool IsPlayingBack => isPlayingBack;
        public ReplayData CurrentReplay => currentReplay;
        public int CurrentFrame => currentFrame;
        public float Progress => currentReplay != null && currentReplay.TotalFrames > 0
            ? (float)currentFrame / currentReplay.TotalFrames : 0f;

        public event Action OnReplayComplete;

        public ReplaySystem()
        {
            input = InputManager.Instance;
        }

        /// <summary>
        /// Start recording a replay
        /// </summary>
        public void StartRecording(string levelName, int? randomSeed = null)
        {
            if (isRecording || isPlayingBack)
                return;

            currentReplay = new ReplayData
            {
                LevelName = levelName,
                RecordDate = DateTime.Now,
                RandomSeed = randomSeed ?? Environment.TickCount
            };

            recordingRandom = new Random(currentReplay.RandomSeed);
            currentFrame = 0;
            isRecording = true;
            lastFrameActions.Clear();
        }

        /// <summary>
        /// Stop recording
        /// </summary>
        public void StopRecording()
        {
            if (!isRecording)
                return;

            currentReplay.TotalFrames = currentFrame;
            currentReplay.DurationSeconds = currentFrame / 60.0; // Assuming 60 FPS
            isRecording = false;
        }

        /// <summary>
        /// Start playing back a replay
        /// </summary>
        public void StartPlayback(ReplayData replay)
        {
            if (isRecording || isPlayingBack)
                return;

            currentReplay = replay;
            currentFrame = 0;
            isPlayingBack = true;
            simulatedActions.Clear();

            // Initialize simulated action states
            foreach (InputAction action in Enum.GetValues(typeof(InputAction)))
            {
                simulatedActions[action] = false;
            }
        }

        /// <summary>
        /// Stop playback
        /// </summary>
        public void StopPlayback()
        {
            if (!isPlayingBack)
                return;

            isPlayingBack = false;
            simulatedActions.Clear();
        }

        /// <summary>
        /// Update replay system (call every frame)
        /// </summary>
        public void Update(GameTime gameTime)
        {
            if (isRecording)
            {
                RecordFrame();
            }
            else if (isPlayingBack)
            {
                PlaybackFrame();
            }
        }

        private void RecordFrame()
        {
            var frame = new InputFrame
            {
                FrameNumber = currentFrame,
                HorizontalAxis = input.GetHorizontalAxis(),
                VerticalAxis = input.GetVerticalAxis()
            };

            // Record action presses and releases
            HashSet<InputAction> currentActions = new HashSet<InputAction>();

            foreach (InputAction action in Enum.GetValues(typeof(InputAction)))
            {
                bool isDown = input.IsActionDown(action);

                if (isDown)
                {
                    currentActions.Add(action);

                    // Newly pressed this frame
                    if (!lastFrameActions.Contains(action))
                    {
                        frame.PressedActions.Add(action);
                    }
                }
                else
                {
                    // Released this frame
                    if (lastFrameActions.Contains(action))
                    {
                        frame.ReleasedActions.Add(action);
                    }
                }
            }

            currentReplay.Frames.Add(frame);
            lastFrameActions = currentActions;
            currentFrame++;
        }

        private void PlaybackFrame()
        {
            if (currentFrame >= currentReplay.Frames.Count)
            {
                StopPlayback();
                OnReplayComplete?.Invoke();
                return;
            }

            var frame = currentReplay.Frames[currentFrame];

            // Update simulated input
            simulatedHorizontal = frame.HorizontalAxis;
            simulatedVertical = frame.VerticalAxis;

            // Apply pressed actions
            foreach (var action in frame.PressedActions)
            {
                simulatedActions[action] = true;
            }

            // Apply released actions
            foreach (var action in frame.ReleasedActions)
            {
                simulatedActions[action] = false;
            }

            currentFrame++;
        }

        /// <summary>
        /// Get horizontal axis (real or simulated)
        /// </summary>
        public float GetHorizontalAxis()
        {
            return isPlayingBack ? simulatedHorizontal : input.GetHorizontalAxis();
        }

        /// <summary>
        /// Get vertical axis (real or simulated)
        /// </summary>
        public float GetVerticalAxis()
        {
            return isPlayingBack ? simulatedVertical : input.GetVerticalAxis();
        }

        /// <summary>
        /// Check if action is down (real or simulated)
        /// </summary>
        public bool IsActionDown(InputAction action)
        {
            if (isPlayingBack)
            {
                return simulatedActions.TryGetValue(action, out bool isDown) && isDown;
            }
            return input.IsActionDown(action);
        }

        /// <summary>
        /// Check if action was pressed this frame (real or simulated)
        /// </summary>
        public bool IsActionPressed(InputAction action)
        {
            if (isPlayingBack)
            {
                if (currentFrame > 0 && currentFrame <= currentReplay.Frames.Count)
                {
                    var frame = currentReplay.Frames[currentFrame - 1];
                    return frame.PressedActions.Contains(action);
                }
                return false;
            }
            return input.IsActionPressed(action);
        }

        /// <summary>
        /// Save replay to file
        /// </summary>
        public bool SaveReplay(string filePath)
        {
            if (currentReplay == null)
                return false;

            try
            {
                var options = new JsonSerializerOptions
                {
                    WriteIndented = true
                };

                string json = JsonSerializer.Serialize(currentReplay, options);
                File.WriteAllText(filePath, json);
                return true;
            }
            catch
            {
                return false;
            }
        }

        /// <summary>
        /// Load replay from file
        /// </summary>
        public static ReplayData LoadReplay(string filePath)
        {
            try
            {
                string json = File.ReadAllText(filePath);

                var options = new JsonSerializerOptions
                {
                    PropertyNameCaseInsensitive = true
                };

                return JsonSerializer.Deserialize<ReplayData>(json, options);
            }
            catch
            {
                return null;
            }
        }

        /// <summary>
        /// Get replay metadata
        /// </summary>
        public void SetMetadata(string key, string value)
        {
            if (currentReplay != null)
            {
                currentReplay.Metadata[key] = value;
            }
        }

        /// <summary>
        /// Get seeded random for deterministic replay
        /// </summary>
        public Random GetReplayRandom()
        {
            return recordingRandom;
        }
    }

    /// <summary>
    /// Replay manager for organizing multiple replays
    /// </summary>
    public class ReplayManager
    {
        private const string REPLAY_FOLDER = "Replays";
        private Dictionary<string, ReplayData> replays = new Dictionary<string, ReplayData>();

        public ReplayManager()
        {
            EnsureReplayFolderExists();
        }

        /// <summary>
        /// Save replay with auto-generated name
        /// </summary>
        public bool SaveReplay(ReplayData replay, string customName = null)
        {
            string fileName = customName ?? GenerateReplayFileName(replay);
            string filePath = Path.Combine(REPLAY_FOLDER, fileName + ".replay");

            try
            {
                var options = new JsonSerializerOptions { WriteIndented = true };
                string json = JsonSerializer.Serialize(replay, options);
                File.WriteAllText(filePath, json);
                return true;
            }
            catch
            {
                return false;
            }
        }

        /// <summary>
        /// Load all replays from folder
        /// </summary>
        public List<ReplayData> LoadAllReplays()
        {
            var replayList = new List<ReplayData>();

            try
            {
                string[] files = Directory.GetFiles(REPLAY_FOLDER, "*.replay");

                foreach (var file in files)
                {
                    var replay = ReplaySystem.LoadReplay(file);
                    if (replay != null)
                    {
                        replayList.Add(replay);
                    }
                }
            }
            catch { }

            return replayList;
        }

        /// <summary>
        /// Delete replay file
        /// </summary>
        public bool DeleteReplay(string fileName)
        {
            try
            {
                string filePath = Path.Combine(REPLAY_FOLDER, fileName);
                if (File.Exists(filePath))
                {
                    File.Delete(filePath);
                    return true;
                }
            }
            catch { }

            return false;
        }

        private string GenerateReplayFileName(ReplayData replay)
        {
            string datePart = replay.RecordDate.ToString("yyyyMMdd_HHmmss");
            string levelPart = replay.LevelName.Replace(" ", "_");
            return $"{levelPart}_{datePart}";
        }

        private void EnsureReplayFolderExists()
        {
            if (!Directory.Exists(REPLAY_FOLDER))
            {
                Directory.CreateDirectory(REPLAY_FOLDER);
            }
        }
    }
}
