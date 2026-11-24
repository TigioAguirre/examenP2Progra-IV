using System;
using System.Drawing;
using System.Globalization;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Forms;
using ClimaOpenMeteo.Models;
using ClimaOpenMeteo.Services;

namespace ClimaOpenMeteo
{
    public class MainForm : Form
    {
        private static readonly CultureInfo Es = new CultureInfo("es-ES");

        private readonly TextBox txtCity = new TextBox { Width = 220, Text = "Lima" };
        private readonly Button btnSearch = new Button { Text = "Consultar clima", AutoSize = true };
        private readonly Label lblStatus = new Label { AutoSize = true, ForeColor = Color.DimGray };
        private readonly Label lblIcon = new Label { AutoSize = true, Font = new Font("Segoe UI Emoji", 44) };
        private readonly Label lblCity = new Label { AutoSize = true, Font = new Font("Segoe UI", 18, FontStyle.Bold) };
        private readonly Label lblTemp = new Label { AutoSize = true, Font = new Font("Segoe UI", 28, FontStyle.Bold) };
        private readonly Label lblDesc = new Label { AutoSize = true, Font = new Font("Segoe UI", 11) };
        private readonly Label lblDetails = new Label { AutoSize = true, Font = new Font("Segoe UI", 10) };
        private readonly DataGridView grid = new DataGridView();

        private readonly OpenMeteoService _service = new OpenMeteoService();

        public MainForm()
        {
            Text = "Clima - Open-Meteo API";
            ClientSize = new Size(760, 560);
            StartPosition = FormStartPosition.CenterScreen;
            Font = new Font("Segoe UI", 9.5f);

            BuildLayout();
            btnSearch.Click += async (s, e) => await SearchAsync();
            txtCity.KeyDown += async (s, e) =>
            {
                if (e.KeyCode == Keys.Enter) { e.SuppressKeyPress = true; await SearchAsync(); }
            };
            FormClosed += (s, e) => _service.Dispose();
            Shown += async (s, e) => await SearchAsync();
        }

        private void BuildLayout()
        {
            var top = new FlowLayoutPanel { Dock = DockStyle.Top, Height = 50, Padding = new Padding(12, 12, 12, 0) };
            top.Controls.Add(new Label { Text = "Ciudad:", AutoSize = true, Margin = new Padding(0, 6, 4, 0) });
            top.Controls.Add(txtCity);
            top.Controls.Add(btnSearch);
            lblStatus.AutoSize = false;
            lblStatus.Dock = DockStyle.Bottom;
            lblStatus.Height = 26;
            lblStatus.Padding = new Padding(12, 4, 0, 0);
            lblStatus.BackColor = SystemColors.ControlLight;

            var current = new Panel { Dock = DockStyle.Top, Height = 160, Padding = new Padding(12) };
            lblIcon.Location = new Point(16, 30);
            lblCity.Location = new Point(150, 14);
            lblTemp.Location = new Point(150, 48);
            lblDesc.Location = new Point(152, 100);
            lblDetails.Location = new Point(420, 30);
            current.Controls.AddRange(new Control[] { lblIcon, lblCity, lblTemp, lblDesc, lblDetails });

            var forecastTitle = new Label
            {
                Text = "Pronóstico de los próximos 7 días",
                Dock = DockStyle.Top, Height = 28, Font = new Font("Segoe UI", 11, FontStyle.Bold),
                Padding = new Padding(12, 4, 0, 0)
            };

            grid.Dock = DockStyle.Fill;
            grid.ReadOnly = true;
            grid.AllowUserToAddRows = false;
            grid.RowHeadersVisible = false;
            grid.SelectionMode = DataGridViewSelectionMode.FullRowSelect;
            grid.AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill;
            grid.BackgroundColor = SystemColors.Window;
            grid.Columns.Add("fecha", "Fecha");
            grid.Columns.Add("desc", "Condición");
            grid.Columns.Add("min", "Mín (°C)");
            grid.Columns.Add("max", "Máx (°C)");
            grid.Columns.Add("lluvia", "Prob. lluvia");
            grid.Columns.Add("viento", "Viento máx (m/s)");
            grid.Columns["desc"].FillWeight = 210;

            // El Fill se agrega primero; luego los Top (el último queda más arriba).
            Controls.Add(grid);
            Controls.Add(lblStatus);
            Controls.Add(forecastTitle);
            Controls.Add(current);
            Controls.Add(top);
        }

        private async Task SearchAsync()
        {
            string city = txtCity.Text.Trim();
            if (city.Length == 0) { lblStatus.Text = "Escribe una ciudad."; return; }

            btnSearch.Enabled = false;
            lblStatus.ForeColor = Color.DimGray;
            lblStatus.Text = "Consultando...";
            try
            {
                GeoLocation loc = await _service.FindCityAsync(city);
                ForecastResponse data = await _service.GetForecastAsync(loc.Latitude, loc.Longitude);

                ShowCurrent(loc, data);
                ShowForecast(data.Daily);
                lblStatus.Text = "Datos actualizados " + DateTime.Now.ToString("HH:mm:ss");
            }
            catch (Exception ex)
            {
                lblStatus.ForeColor = Color.Firebrick;
                lblStatus.Text = ex.Message;
            }
            finally
            {
                btnSearch.Enabled = true;
            }
        }

        private void ShowCurrent(GeoLocation loc, ForecastResponse data)
        {
            CurrentData c = data.Current;
            DailyData d = data.Daily;
            var (text, emoji) = OpenMeteoService.Describe(c.WeatherCode);

            lblIcon.Text = emoji;
            lblCity.Text = string.IsNullOrEmpty(loc.Country) ? loc.Name : $"{loc.Name}, {loc.Country}";
            lblTemp.Text = $"{c.Temperature:0.#} °C";
            lblDesc.Text = text;
            lblDetails.Text =
                $"Sensación térmica: {c.FeelsLike:0.#} °C\r\n" +
                $"Mín / Máx hoy: {d.TempMin[0]:0.#} / {d.TempMax[0]:0.#} °C\r\n" +
                $"Humedad: {c.Humidity} %\r\n" +
                $"Presión: {c.Pressure:0} hPa\r\n" +
                $"Viento: {c.WindSpeed:0.#} m/s\r\n" +
                $"Amanecer: {DateTime.Parse(d.Sunrise[0], CultureInfo.InvariantCulture):HH:mm}   " +
                $"Atardecer: {DateTime.Parse(d.Sunset[0], CultureInfo.InvariantCulture):HH:mm}";
        }

        private void ShowForecast(DailyData d)
        {
            grid.Rows.Clear();
            for (int i = 0; i < d.Dates.Count; i++)
            {
                DateTime date = DateTime.Parse(d.Dates[i], CultureInfo.InvariantCulture);
                var (text, emoji) = OpenMeteoService.Describe(d.WeatherCode[i]);
                grid.Rows.Add(
                    Capitalize(date.ToString("dddd dd/MM", Es)),
                    $"{emoji} {text}",
                    d.TempMin[i].ToString("0.#"),
                    d.TempMax[i].ToString("0.#"),
                    (d.RainProbability[i] ?? 0) + " %",
                    d.WindMax[i].ToString("0.#"));
            }
        }

        private static string Capitalize(string s) =>
            string.IsNullOrEmpty(s) ? s : char.ToUpper(s[0]) + s.Substring(1);
    }
}
