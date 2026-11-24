using System.Collections.Generic;
using System.Runtime.Serialization;

namespace ClimaOpenMeteo.Models
{
    // ---------- Geocodificación: GET geocoding-api.open-meteo.com/v1/search ----------
    [DataContract]
    public class GeocodingResponse
    {
        [DataMember(Name = "results")] public List<GeoLocation> Results { get; set; }
    }

    [DataContract]
    public class GeoLocation
    {
        [DataMember(Name = "name")] public string Name { get; set; }
        [DataMember(Name = "country")] public string Country { get; set; }
        [DataMember(Name = "admin1")] public string Region { get; set; }
        [DataMember(Name = "latitude")] public double Latitude { get; set; }
        [DataMember(Name = "longitude")] public double Longitude { get; set; }
    }

    // ---------- Pronóstico: GET api.open-meteo.com/v1/forecast ----------
    [DataContract]
    public class ForecastResponse
    {
        [DataMember(Name = "current")] public CurrentData Current { get; set; }
        [DataMember(Name = "daily")] public DailyData Daily { get; set; }
    }

    [DataContract]
    public class CurrentData
    {
        [DataMember(Name = "temperature_2m")] public double Temperature { get; set; }
        [DataMember(Name = "apparent_temperature")] public double FeelsLike { get; set; }
        [DataMember(Name = "relative_humidity_2m")] public double Humidity { get; set; }
        [DataMember(Name = "pressure_msl")] public double Pressure { get; set; }
        [DataMember(Name = "wind_speed_10m")] public double WindSpeed { get; set; }
        [DataMember(Name = "weather_code")] public int WeatherCode { get; set; }
    }

    // La API devuelve listas paralelas: el índice i de cada lista corresponde al día i.
    [DataContract]
    public class DailyData
    {
        [DataMember(Name = "time")] public List<string> Dates { get; set; }
        [DataMember(Name = "weather_code")] public List<int> WeatherCode { get; set; }
        [DataMember(Name = "temperature_2m_max")] public List<double> TempMax { get; set; }
        [DataMember(Name = "temperature_2m_min")] public List<double> TempMin { get; set; }
        [DataMember(Name = "precipitation_probability_max")] public List<double?> RainProbability { get; set; }
        [DataMember(Name = "wind_speed_10m_max")] public List<double> WindMax { get; set; }
        [DataMember(Name = "sunrise")] public List<string> Sunrise { get; set; }
        [DataMember(Name = "sunset")] public List<string> Sunset { get; set; }
    }
}
