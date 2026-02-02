using System;
using System.IO;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace PlatformerEngine.Core.Persistence
{
    /// <summary>
    /// Game save data
    /// </summary>
    public class SaveData
    {
        [JsonPropertyName("playerName")]
        public string PlayerName { get; set; } = "Player";

        [JsonPropertyName("currentLevel")]
        public string CurrentLevel { get; set; }

        [JsonPropertyName("checkpointLevel")]
        public string CheckpointLevel { get; set; }

        [JsonPropertyName("checkpointX")]
        public float CheckpointX { get; set; }

        [JsonPropertyName("checkpointY")]
        public float CheckpointY { get; set; }

        [JsonPropertyName("health")]
        public int Health { get; set; } = 100;

        [JsonPropertyName("maxHealth")]
        public int MaxHealth { get; set; } = 100;

        [JsonPropertyName("lives")]
        public int Lives { get; set; } = 3;

        [JsonPropertyName("coins")]
        public int Coins { get; set; }

        [JsonPropertyName("score")]
        public int Score { get; set; }

        [JsonPropertyName("collectedItems")]
        public System.Collections.Generic.List<string> CollectedItems { get; set; } = new System.Collections.Generic.List<string>();

        [JsonPropertyName("completedLevels")]
        public System.Collections.Generic.List<string> CompletedLevels { get; set; } = new System.Collections.Generic.List<string>();

        [JsonPropertyName("unlockedAbilities")]
        public System.Collections.Generic.List<string> UnlockedAbilities { get; set; } = new System.Collections.Generic.List<string>();

        [JsonPropertyName("playTimeSeconds")]
        public double PlayTimeSeconds { get; set; }

        [JsonPropertyName("saveDate")]
        public DateTime SaveDate { get; set; }

        [JsonPropertyName("gameVersion")]
        public string GameVersion { get; set; } = "1.0.0";

        [JsonPropertyName("customData")]
        public System.Collections.Generic.Dictionary<string, string> CustomData { get; set; } = new System.Collections.Generic.Dictionary<string, string>();
    }

    /// <summary>
    /// Game settings/configuration
    /// </summary>
    public class GameSettings
    {
        [JsonPropertyName("masterVolume")]
        public float MasterVolume { get; set; } = 1.0f;

        [JsonPropertyName("musicVolume")]
        public float MusicVolume { get; set; } = 0.7f;

        [JsonPropertyName("sfxVolume")]
        public float SfxVolume { get; set; } = 0.8f;

        [JsonPropertyName("fullscreen")]
        public bool Fullscreen { get; set; } = false;

        [JsonPropertyName("screenWidth")]
        public int ScreenWidth { get; set; } = 960;

        [JsonPropertyName("screenHeight")]
        public int ScreenHeight { get; set; } = 600;

        [JsonPropertyName("vsync")]
        public bool VSync { get; set; } = true;

        [JsonPropertyName("showFPS")]
        public bool ShowFPS { get; set; } = false;

        [JsonPropertyName("keyBindings")]
        public System.Collections.Generic.Dictionary<string, string> KeyBindings { get; set; } = new System.Collections.Generic.Dictionary<string, string>();
    }

    /// <summary>
    /// Save/Load manager
    /// </summary>
    public class SaveManager
    {
        private static SaveManager instance;
        public static SaveManager Instance => instance ??= new SaveManager();

        private const string SAVE_FOLDER = "Saves";
        private const string SETTINGS_FILE = "settings.json";

        public SaveData CurrentSave { get; private set; }
        public GameSettings Settings { get; private set; }

        private SaveManager()
        {
            Settings = new GameSettings();
        }

        /// <summary>
        /// Save game to file
        /// </summary>
        public bool SaveGame(string slotName, SaveData saveData)
        {
            try
            {
                EnsureSaveFolderExists();

                saveData.SaveDate = DateTime.Now;
                string filePath = GetSaveFilePath(slotName);

                var options = new JsonSerializerOptions
                {
                    WriteIndented = true
                };

                string json = JsonSerializer.Serialize(saveData, options);
                File.WriteAllText(filePath, json);

                CurrentSave = saveData;
                return true;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Save failed: {ex.Message}");
                return false;
            }
        }

        /// <summary>
        /// Load game from file
        /// </summary>
        public SaveData LoadGame(string slotName)
        {
            try
            {
                string filePath = GetSaveFilePath(slotName);

                if (!File.Exists(filePath))
                    return null;

                string json = File.ReadAllText(filePath);

                var options = new JsonSerializerOptions
                {
                    PropertyNameCaseInsensitive = true
                };

                CurrentSave = JsonSerializer.Deserialize<SaveData>(json, options);
                return CurrentSave;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Load failed: {ex.Message}");
                return null;
            }
        }

        /// <summary>
        /// Check if save file exists
        /// </summary>
        public bool SaveExists(string slotName)
        {
            return File.Exists(GetSaveFilePath(slotName));
        }

        /// <summary>
        /// Delete save file
        /// </summary>
        public bool DeleteSave(string slotName)
        {
            try
            {
                string filePath = GetSaveFilePath(slotName);
                if (File.Exists(filePath))
                {
                    File.Delete(filePath);
                    return true;
                }
                return false;
            }
            catch
            {
                return false;
            }
        }

        /// <summary>
        /// Get all save slots
        /// </summary>
        public string[] GetAllSaveSlots()
        {
            try
            {
                EnsureSaveFolderExists();
                string[] files = Directory.GetFiles(SAVE_FOLDER, "*.json");

                for (int i = 0; i < files.Length; i++)
                {
                    files[i] = Path.GetFileNameWithoutExtension(files[i]);
                }

                return files;
            }
            catch
            {
                return new string[0];
            }
        }

        /// <summary>
        /// Save settings
        /// </summary>
        public bool SaveSettings()
        {
            try
            {
                var options = new JsonSerializerOptions
                {
                    WriteIndented = true
                };

                string json = JsonSerializer.Serialize(Settings, options);
                File.WriteAllText(SETTINGS_FILE, json);
                return true;
            }
            catch
            {
                return false;
            }
        }

        /// <summary>
        /// Load settings
        /// </summary>
        public bool LoadSettings()
        {
            try
            {
                if (!File.Exists(SETTINGS_FILE))
                {
                    Settings = new GameSettings();
                    SaveSettings();
                    return true;
                }

                string json = File.ReadAllText(SETTINGS_FILE);

                var options = new JsonSerializerOptions
                {
                    PropertyNameCaseInsensitive = true
                };

                Settings = JsonSerializer.Deserialize<GameSettings>(json, options);
                return true;
            }
            catch
            {
                Settings = new GameSettings();
                return false;
            }
        }

        private void EnsureSaveFolderExists()
        {
            if (!Directory.Exists(SAVE_FOLDER))
            {
                Directory.CreateDirectory(SAVE_FOLDER);
            }
        }

        private string GetSaveFilePath(string slotName)
        {
            return Path.Combine(SAVE_FOLDER, $"{slotName}.json");
        }

        /// <summary>
        /// Quick save to default slot
        /// </summary>
        public bool QuickSave(SaveData saveData)
        {
            return SaveGame("quicksave", saveData);
        }

        /// <summary>
        /// Quick load from default slot
        /// </summary>
        public SaveData QuickLoad()
        {
            return LoadGame("quicksave");
        }

        /// <summary>
        /// Create new save data
        /// </summary>
        public SaveData CreateNewSave()
        {
            return new SaveData
            {
                SaveDate = DateTime.Now,
                GameVersion = "1.0.0"
            };
        }
    }
}
