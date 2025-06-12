using System;
using System.Collections.Generic;
using System.IO;
using System.Text.Json;

namespace Kalendarz
{
    public static class TaskStorage
    {
        private static readonly string FilePath = "zadania.json";

        public static void Save(Dictionary<DateTime, List<string>> tasks)
        {
            var serializable = new Dictionary<string, List<string>>();
            foreach (var kv in tasks)
            {
                serializable[kv.Key.ToString("yyyy-MM-dd")] = kv.Value;
            }

            var options = new JsonSerializerOptions { WriteIndented = true };
            File.WriteAllText(FilePath, JsonSerializer.Serialize(serializable, options));
        }

        public static Dictionary<DateTime, List<string>> Load()
        {
            if (!File.Exists(FilePath))
                return new Dictionary<DateTime, List<string>>();

            var content = File.ReadAllText(FilePath);
            var deserialized = JsonSerializer.Deserialize<Dictionary<string, List<string>>>(content);

            var tasks = new Dictionary<DateTime, List<string>>();
            foreach (var kv in deserialized)
            {
                if (DateTime.TryParse(kv.Key, out var date))
                    tasks[date.Date] = kv.Value;
            }

            return tasks;
        }
    }
}
