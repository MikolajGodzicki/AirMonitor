using AirMonitor.Models;
using AirMonitor.Models.Geolocalization;
using CsvHelper;
using Microsoft.Win32;
using OxyPlot;
using OxyPlot.Axes;
using OxyPlot.Series;
using System.Globalization;
using System.IO;
using System.Net.Http;
using System.Text.Json;
using System.Windows;
using System.Windows.Controls;

namespace AirMonitor.Views {
    /// <summary>
    /// Logika interakcji dla klasy MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window {

        private string _importedFilePath;
        private string _location;
        private double _latitude;
        private double _longitude;
        private string _selectedCompound;
        List<AirSample>? _airSamples;

        public MainWindow() {
            InitializeComponent();
        }

        private async void ImportData_Click(object sender, RoutedEventArgs e) {
            OpenFileDialog openFileDialog = new OpenFileDialog();
            openFileDialog.Filter = "CSV files (*.csv)|*.csv|All files (*.*)|*.*";
            openFileDialog.Title = "Wybierz plik CSV";

            bool? result = openFileDialog.ShowDialog();

            if (result == true) {
                _importedFilePath = openFileDialog.FileName;

                _airSamples = GetAirSamples();
                if (_airSamples.Count == 0) {
                    MessageBox.Show("Brak danych w pliku CSV.", "Błąd", MessageBoxButton.OK, MessageBoxImage.Error);
                    return;
                }

                _latitude = _airSamples.First().Latitude;
                _longitude = _airSamples.First().Longitude;
                _location = await GetLocation(_latitude, _longitude);

                UpdateView();
            }
        }

        private List<AirSample> GetAirSamples() {
            var compounds = new List<ChemicalCompund> {
                    new ChemicalCompund { Name = "HCHO(ppm)", Unit = "ppm", Limit = 50 },
                    new ChemicalCompund { Name = "HCL(ppm)", Unit = "ppm", Limit = 50 },
                    new ChemicalCompund { Name = "H2S(ppm)", Unit = "ppm", Limit = 50 },
                    new ChemicalCompund { Name = "NH3(ppm)", Unit = "ppm", Limit = 50 },
                    new ChemicalCompund { Name = "PM1(ug/m3)", Unit = "µg/m³", Limit = 20 },
                    new ChemicalCompund { Name = "PM2.5(ug/m3)", Unit = "µg/m³", Limit = 20 },
                    new ChemicalCompund { Name = "PM10(ug/m3)", Unit = "µg/m³", Limit = 40 }
                };

            using var reader = new StreamReader(_importedFilePath);
            using var csv = new CsvReader(reader, CultureInfo.InvariantCulture);

            csv.Read();
            csv.ReadHeader();

            var headers = csv.HeaderRecord;
            var chemicalColumns = headers
                .Where(h => compounds.Any(c => c.Name == h))
                .ToList();

            var samples = new List<AirSample>();

            while (csv.Read()) {
                var sample = new AirSample {
                    Timestamp = csv.GetField<DateTime>("Date&Time"),
                    Latitude = csv.GetField<double>("Latitude"),
                    Longitude = csv.GetField<double>("Longitude"),
                    Measurements = new List<ChemicalMeasurement>()
                };

                foreach (var col in chemicalColumns) {
                    var compound = compounds.First(c => c.Name == col);
                    var value = csv.GetField<double>(col);

                    var measurement = new ChemicalMeasurement {
                        ChemicalCompund = compound,
                        Value = value
                    };

                    sample.Measurements.Add(measurement);
                }

                samples.Add(sample);
            }

            return samples;
        }

        private async Task<string> GetLocation(double latitude, double longitude) {
            using var client = new HttpClient();
            client.DefaultRequestHeaders.Add("User-Agent", "MyAirApp/1.0");

            string latStr = latitude.ToString(CultureInfo.InvariantCulture);
            string lonStr = longitude.ToString(CultureInfo.InvariantCulture);


            string url = $"https://nominatim.openstreetmap.org/reverse?lat={latStr}&lon={lonStr}&format=json";

            string json = await client.GetStringAsync(url);

            var options = new JsonSerializerOptions {
                PropertyNameCaseInsensitive = true
            };

            var location = JsonSerializer.Deserialize<NominatimResponse>(json, options);

            string village = location.address.village;           // "Złotniki"
            string municipality = location.address.municipality; // "gmina Igołomia-Wawrzeńczyce"
            return $"{village} - {municipality}";
        }

        private void CreateDataPlot(List<AirSample>? _airSamples) {
            var plotModel = new PlotModel { Title = _location };

            plotModel.Axes.Add(new LinearAxis { Position = AxisPosition.Bottom, Title = "Numer próbki" });
            plotModel.Axes.Add(new LinearAxis { Position = AxisPosition.Left, Title = "Stężenie" });

            var lineSeries = new LineSeries {
                Title = "Mierzona Wartość",
                MarkerType = MarkerType.Circle,
                MarkerSize = 4,
                MarkerStroke = OxyColors.Yellow
            };

            double i = 0;
            foreach (AirSample sample in _airSamples) {
                var measurement = sample.Measurements.FirstOrDefault(m => m.ChemicalCompund.Name == MapCompound(_selectedCompound));
                if (measurement != null) {
                    double x = i;
                    double y = measurement.Value;
                    lineSeries.Points.Add(new DataPoint(x, y));
                    i++;
                }
            }

            plotModel.Series.Add(lineSeries);

            DataPlot.Model = plotModel;
        }

        private string MapCompound(string compound) {
            return compound switch {
                "HCHO" => "HCHO(ppm)",
                "HCL" => "HCL(ppm)",
                "H2S" => "H2S(ppm)",
                "NH3" => "NH3(ppm)",
                "PM1" => "PM1(ug/m3)",
                "PM2.5" => "PM2.5(ug/m3)",
                "PM10" => "PM10(ug/m3)",
                _ => compound,
            };
        }

        private void UpdateView() {
            _selectedCompound = ChemicalRadioPanel.Children
                        .OfType<RadioButton>()
                        .FirstOrDefault(r => r.IsChecked == true)?.Content?.ToString();

            if (string.IsNullOrEmpty(_importedFilePath))
                return;

            CreateDataPlot(_airSamples);
        }

        private void ExportPDF_Click(object sender, RoutedEventArgs e) {

        }

        private void Print_Click(object sender, RoutedEventArgs e) {

        }

        private void RadioButton_Checked(object sender, RoutedEventArgs e) {
            UpdateView();
        }

        private void MenuItemConfiguration_Click(object sender, RoutedEventArgs e) {
            ConfigurationWindow confWindow = new ConfigurationWindow();
            confWindow.ShowDialog();
        }
    }
}
