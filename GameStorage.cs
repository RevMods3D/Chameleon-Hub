using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.Xml;

namespace Chameleon_Hub
{
    public static class GameStorage
    {
        private static readonly string SavePath = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
            "Chameleon_Hub",
            "games.json"
        );

        public static void Save(List<GameEntry> games)
        {
            Directory.CreateDirectory(Path.GetDirectoryName(SavePath)!);
            string json = JsonConvert.SerializeObject(games, Newtonsoft.Json.Formatting.Indented);
            File.WriteAllText(SavePath, json);
        }

        public static List<GameEntry> Load()
        {
            if (File.Exists(SavePath))
            {
                string json = File.ReadAllText(SavePath);
                return JsonConvert.DeserializeObject<List<GameEntry>>(json) ?? new List<GameEntry>();
            }

            return new List<GameEntry>();
        }
    }
}