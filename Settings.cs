using System.IO;
using System.Text.Json;

namespace Kalendarz
{
    /// <summary>
    /// Zapisane ustawienie miasta, potrzebne do pobrania pogody
    /// </summary>
    public class Settings
    {
        public string City { get; set; } = "Wrocław";

        private static readonly string FilePath = "ustawienia.json";

        public static Settings Load()
        {
            if (!File.Exists(FilePath))
                return new Settings();

            var json = File.ReadAllText(FilePath);
            return JsonSerializer.Deserialize<Settings>(json) ?? new Settings();
        }

        public void Save()
        {
            var json = JsonSerializer.Serialize(this, new JsonSerializerOptions { WriteIndented = true });
            File.WriteAllText(FilePath, json);
        }
    }
}
