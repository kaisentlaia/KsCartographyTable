using System;
using System.IO;
using Vintagestory.API.Common;

namespace Kaisentlaia.KsCartographyTableMod.API.Common
{

    /// <summary>
    /// Represents the settings.json file structure.
    /// </summary>
    internal class SettingsFile
    {
        public bool ImmersiveMode { get; set; } = false;
        public int ChunksPerPacket { get; set; } = 25;
        public double PacketDelay { get; set; } = 0.2;
        public bool VerboseDebug { get; set; } = false;
        public bool WaypointUpload { get; set; } = true;
        public bool WaypointDownload { get; set; } = true;
    }
    public static class Settings
    {
        private static SettingsFile SettingsFile { get; set; }
        public static bool ImmersiveMode { 
            get { return SettingsFile.ImmersiveMode; } 
            set { SettingsFile.ImmersiveMode = value; Save(); } 
        }
        public static int ChunksPerPacket { 
            get {
                return SettingsFile.ChunksPerPacket;
            } 
            set { SettingsFile.ChunksPerPacket = value; Save(); } 
        }
        public static double PacketDelay { 
            get { return SettingsFile.PacketDelay; } 
            set { SettingsFile.PacketDelay = value; Save(); } 
        }
        public static bool VerboseDebug { 
            get { return SettingsFile.VerboseDebug; } 
            set { SettingsFile.VerboseDebug = value; Save(); } 
        }
        public static bool WaypointUpload { 
            get { return SettingsFile.WaypointUpload; } 
            set { SettingsFile.WaypointUpload = value; Save(); } 
        }
        public static bool WaypointDownload { 
            get { return SettingsFile.WaypointDownload; } 
            set { SettingsFile.WaypointDownload = value; Save(); } 
        }

        private static ICoreAPI Api;
        private static string SettingsPath;

        public static void Init(ICoreAPI api, string modId)
        {
            Api = api;
            SettingsPath = Path.Combine(api.GetOrCreateDataPath("ModConfig"), $"{modId}.json");
        }
        public static void Load()
        {
            SettingsFile settingsFile = null;
            SettingsFile = new();
            if (File.Exists(SettingsPath))
            {
                try
                {
                    string json = File.ReadAllText(SettingsPath);
                    settingsFile = JsonUtil.FromString<SettingsFile>(json);
                }
                catch (Exception ex)
                {
                    Api.Logger.Error($"{CartographyTableConstants.MAP_EVENT} Failed to load {SettingsPath}: {ex.Message}. Using defaults.");
                }
            }

            if (settingsFile != null)
            {
                SettingsFile = settingsFile;
                return;
            }

            SettingsFile.ImmersiveMode = false;
            SettingsFile.ChunksPerPacket = 25;
            SettingsFile.PacketDelay = 0.2;
            SettingsFile.VerboseDebug = false;
            SettingsFile.WaypointDownload = true;
            SettingsFile.WaypointUpload = true;
            Save();
        }

        public static void Save()
        {
            File.WriteAllText(SettingsPath, JsonUtil.ToString(SettingsFile));
        }
    }
}