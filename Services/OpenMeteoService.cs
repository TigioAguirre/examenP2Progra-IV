using System;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Runtime.Serialization.Json;
using System.Text;
using System.Threading.Tasks;
using ClimaOpenMeteo.Models;

namespace ClimaOpenMeteo.Services
{
    /// <summary>Cliente HTTP para la API pública de Open-Meteo (no requiere API key).</summary>
    public class OpenMeteoService : IDisposable
    {
        private const string GeoUrl = "https://geocoding-api.open-meteo.com/v1/search";
        private const string ForecastUrl = "https://api.open-meteo.com/v1/forecast";

        private readonly HttpClient _http;

        public OpenMeteoService()
        {
            ServicePointManager.SecurityProtocol |= SecurityProtocolType.Tls12;
            _http = new HttpClient { Timeout = TimeSpan.FromSeconds(15) };
        }

        /// <summary>Convierte el nombre de una ciudad en coordenadas.</summary>
        public async Task<GeoLocation> FindCityAsync(string city)
        {
            string url = $"{GeoUrl}?name={Uri.EscapeDataString(city)}&count=1&language=es&format=json";
            var data = await GetAsync<GeocodingResponse>(url).ConfigureAwait(false);

            GeoLocation loc = data.Results?.FirstOrDefault();
            if (loc == null)
                throw new Exception("Ciudad no encontrada. Prueba con otro nombre (ej. Lima).");
            return loc;
        }

        /// <summary>Clima actual + pronóstico de 7 días para unas coordenadas.</summary>
        public Task<ForecastResponse> GetForecastAsync(double lat, double lon)
        {
            string la = lat.ToString(CultureInfo.InvariantCulture);
            string lo = lon.ToString(CultureInfo.InvariantCulture);
            string url = $"{ForecastUrl}?latitude={la}&longitude={lo}" +
                "&current=temperature_2m,apparent_temperature,relative_humidity_2m,pressure_msl,wind_speed_10m,weather_code" +
                "&daily=weather_code,temperature_2m_max,temperature_2m_min,precipitation_probability_max,wind_speed_10m_max,sunrise,sunset" +
                "&timezone=auto&forecast_days=7&wind_speed_unit=ms";
            return GetAsync<ForecastResponse>(url);
        }

        /// <summary>Solicitud GET + deserialización del JSON al tipo T.</summary>
        private async Task<T> GetAsync<T>(string url)
        {
            using (HttpResponseMessage response = await _http.GetAsync(url).ConfigureAwait(false))
            {
                string json = await response.Content.ReadAsStringAsync().ConfigureAwait(false);

                if (!response.IsSuccessStatusCode)
                    throw new Exception($"Error {(int)response.StatusCode}: {json}");

                // JSON -> objetos C# con el serializador incluido en .NET Framework (sin NuGet)
                using (var ms = new MemoryStream(Encoding.UTF8.GetBytes(json)))
                    return (T)new DataContractJsonSerializer(typeof(T)).ReadObject(ms);
            }
        }

        /// <summary>Traduce el código WMO de Open-Meteo a texto en español y un emoji.</summary>
        public static (string Text, string Emoji) Describe(int code)
        {
            switch (code)
            {
                case 0: return ("Despejado", "☀");
                case 1: return ("Mayormente despejado", "🌤");
                case 2: return ("Parcialmente nublado", "⛅");
                case 3: return ("Nublado", "☁");
                case 45: case 48: return ("Niebla", "🌫");
                case 51: case 53: case 55: return ("Llovizna", "🌦");
                case 56: case 57: return ("Llovizna helada", "🌧");
                case 61: return ("Lluvia ligera", "🌦");
                case 63: return ("Lluvia moderada", "🌧");
                case 65: return ("Lluvia fuerte", "🌧");
                case 66: case 67: return ("Lluvia helada", "🌧");
                case 71: case 73: case 75: case 77: return ("Nieve", "❄");
                case 80: case 81: case 82: return ("Chubascos", "🌧");
                case 85: case 86: return ("Chubascos de nieve", "❄");
                case 95: return ("Tormenta", "⛈");
                case 96: case 99: return ("Tormenta con granizo", "⛈");
                default: return ("Desconocido", "❓");
            }
        }

        public void Dispose() => _http.Dispose();
    }
}
