using System.Net.Http;
using System.Text.Json;
using System.Threading.Tasks;

namespace Kalendarz
{
    /// <summary>
    /// Połaczenie z API od pogody
    /// </summary>
    public class WeatherData
    {
        public Main main { get; set; }
        public Weather[] weather { get; set; }

        public class Main
        {
            public double temp { get; set; }
        }

        public class Weather
        {
            public string description { get; set; }
        }
    }

    public static class WeatherService
    {
        private static readonly HttpClient httpClient = new();
        private const string ApiKey = "f02324b88559b9619469593ea8413131"; 
        private const string BaseUrl = "https://api.openweathermap.org/data/2.5/weather";

        public static async Task<string> GetWeatherAsync(string city)
        {
            string url = $"{BaseUrl}?q={city}&appid={ApiKey}&units=metric&lang=pl";

            var response = await httpClient.GetAsync(url);
            if (!response.IsSuccessStatusCode)
                return "Nie udało się pobrać pogody";

            var json = await response.Content.ReadAsStringAsync();
            var weather = JsonSerializer.Deserialize<WeatherData>(json);

            return $"{weather.main.temp}°C, {weather.weather[0].description}";
        }
    }
}
