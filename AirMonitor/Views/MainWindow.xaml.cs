using AirMonitor.Models;
using AirMonitor.Models.Geolocalization;
using CsvHelper;
using Microsoft.Win32;
using OxyPlot;
using OxyPlot.Annotations;
using OxyPlot.Axes;
using OxyPlot.Series;
using OxyPlot.Wpf;
using PdfSharp.Drawing;
using PdfSharp.Fonts;
using PdfSharp.Pdf;
using System.Globalization;
using System.IO;
using System.Net;
using System.Net.Http;
using System.Reflection;
using System.Text.Json;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;

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

        private Configuration configuration;

        private List<ChemicalCompund> chemicalCompunds;

        public MainWindow() {
            InitializeComponent();
            configuration = new Configuration();
            configuration.Load();

            chemicalCompunds = GetChemicalCompunds();
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

        private List<ChemicalCompund> GetChemicalCompunds() {
            var compounds = new List<ChemicalCompund> {
                    new ChemicalCompund { Name = "HCHO(ppm)", Unit = "ppm", Limit = configuration.HCHO_Limit },
                    new ChemicalCompund { Name = "HCL(ppm)", Unit = "ppm", Limit = configuration.HCL_Limit },
                    new ChemicalCompund { Name = "H2S(ppm)", Unit = "ppm", Limit = configuration.H2S_Limit },
                    new ChemicalCompund { Name = "NH3(ppm)", Unit = "ppm", Limit = configuration.NH3_Limit },
                    new ChemicalCompund { Name = "PM1(ug/m3)", Unit = "µg/m³", Limit = configuration.PM1_Limit },
                    new ChemicalCompund { Name = "PM2.5(ug/m3)", Unit = "µg/m³", Limit = configuration.PM2_5_Limit },
                    new ChemicalCompund { Name = "PM10(ug/m3)", Unit = "µg/m³", Limit = configuration.PM10_Limit }
                };
            
            return compounds;
        }

        private List<AirSample> GetAirSamples() {
            var compounds = chemicalCompunds;

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

        private PlotModel CreateDataPlot(List<AirSample>? _airSamples, string customCompound = "") {
            string mappedCompound = customCompound == "" ? MapCompound(_selectedCompound) : MapCompound(customCompound);

            var plotModel = new PlotModel { Title = $"{_location} - {mappedCompound}"  };

            ChemicalCompund compoundInfo = _airSamples
                .SelectMany(s => s.Measurements)
                .FirstOrDefault(m => m.ChemicalCompund.Name == mappedCompound)?
                .ChemicalCompund;

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
                var measurement = sample.Measurements.FirstOrDefault(m => m.ChemicalCompund.Name == mappedCompound);
                if (measurement != null) {
                    double x = i;
                    double y = measurement.Value;
                    lineSeries.Points.Add(new DataPoint(x, y));
                    i++;
                }
            }

            plotModel.Series.Add(lineSeries);

            var exceedanceRect = new RectangleAnnotation {
                MinimumY = compoundInfo.Limit,           // dolna granica przekroczenia
                MaximumY = 1000,         // górna granica, np. duża wartość
                Fill = OxyColor.FromAColor(80, OxyColors.Red),
                Layer = AnnotationLayer.BelowSeries
            };

            plotModel.Annotations.Add(exceedanceRect);


            DataPlot.Model = plotModel;

            return plotModel;
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

            chemicalCompunds = GetChemicalCompunds();
            _airSamples = GetAirSamples();
            CreateDataPlot(_airSamples);
        }

        private void ExportPDF_Click(object sender, RoutedEventArgs e) {
            if (_airSamples is null || _airSamples.Count == 0) {
                MessageBox.Show("Brak danych w pliku CSV.", "Błąd", MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }

            GlobalFontSettings.UseWindowsFontsUnderWindows = true;

            var document = new PdfDocument();
            var page = document.AddPage();
            page.Size = PdfSharp.PageSize.A4;

            XGraphics gfx = XGraphics.FromPdfPage(page);

            double margin = 40;
            double currentY = margin;

            double availableWidth = page.Width - 2 * margin;
            double availableHeight = page.Height - margin;

            double spacing = 20;

            // Tytuł
            gfx.DrawString(
                "Raport z wykrycia cząstek chemicznych w powietrzu",
                new XFont("Arial", 18, XFontStyleEx.Bold),
                XBrushes.Black,
                new XRect(0, currentY, page.Width, 30),
                XStringFormats.TopCenter);

            currentY += 40;

            // Metadane
            var metaFont = new XFont("Arial", 10, XFontStyleEx.Regular);

            gfx.DrawString($"Raport stworzono z aplikacji:                                 Mikołaj Godzicki", metaFont,
                XBrushes.Black, margin, currentY);
            using var logo = XImage.FromFile($"{Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location)}\\Assets\\logo.png");
            double logoWidth = 100; // szerokość logo
            double logoHeight = logoWidth * logo.PixelHeight / logo.PixelWidth;

            gfx.DrawImage(logo, margin + 125, currentY - 20, logoWidth, logoHeight);
            currentY += 25;

            gfx.DrawString($"Data utworzenia: {DateTime.Now:yyyy-MM-dd HH:mm}", metaFont,
                XBrushes.Black, margin, currentY);
            currentY += 20;

            gfx.DrawString($"Osoba tworząca raport: Przemysław Polakiewicz", metaFont,
                XBrushes.Black, margin, currentY);
            currentY += 20;

            gfx.DrawString($"Szerokość geograficzna: {_latitude}", metaFont,
                XBrushes.Black, margin, currentY);
            currentY += 15;

            gfx.DrawString($"Długość geograficzna: {_longitude}", metaFont,
                XBrushes.Black, margin, currentY);
            currentY += 25;

            // Adres
            gfx.DrawString("Adres lokalizacji:", metaFont,
                XBrushes.Black, margin, currentY);
            currentY += 15;

            // Ramka adresu
            double addressHeight = 30;
            gfx.DrawRectangle(
                XPens.Black,
                margin,
                currentY,
                page.Width - 2 * margin,
                addressHeight);


            currentY += addressHeight + 20;



            List<PlotModel> plotModels = new() {
                CreateDataPlot(_airSamples, "HCHO"),
                CreateDataPlot(_airSamples, "HCL"),
                CreateDataPlot(_airSamples, "H2S"),
                CreateDataPlot(_airSamples, "NH3"),
                CreateDataPlot(_airSamples, "PM1"),
                CreateDataPlot(_airSamples, "PM2.5"),
                CreateDataPlot(_airSamples, "PM10"),
            };

            foreach (var plotModel in plotModels) {
                byte[] img = ExportPlotToPngBytes(plotModel);

                using var ms = new MemoryStream(img);
                using var image = XImage.FromStream(ms);

                // zachowanie proporcji
                double scale = availableWidth / image.PixelWidth;
                double plotHeight = image.PixelHeight * scale;

                // sprawdzenie czy się mieści
                if (currentY + plotHeight > availableHeight) {
                    gfx.Dispose();

                    page = document.AddPage();
                    page.Size = PdfSharp.PageSize.A4;
                    gfx = XGraphics.FromPdfPage(page);

                    currentY = margin;
                }

                gfx.DrawImage(
                    image,
                    margin,
                    currentY,
                    availableWidth,
                    plotHeight
                );


                currentY += plotHeight + spacing;
            }

            gfx.Dispose();
            LastRadioButton.IsChecked = true;

            SaveFileDialog saveFileDialog = new SaveFileDialog();
            saveFileDialog.Filter = "PDF files (*.pdf)|*.pdf|All files (*.*)|*.*";
            saveFileDialog.Title = "Zapisz raport jako PDF";
            bool? result = saveFileDialog.ShowDialog();
            if (result == true) {
                string filePath = saveFileDialog.FileName;
                document.Save(filePath);
                MessageBox.Show("Plik PDF został zapisany pomyślnie.", "Sukces", MessageBoxButton.OK, MessageBoxImage.Information);
            }
        }

        public byte[] ExportPlotToPngBytes(PlotModel model) {
            using var stream = new MemoryStream();

            var exporter = new PngExporter {
                Width = 1200,
                Height = 800
            };

            exporter.Export(model, stream);
            return stream.ToArray();
        }

        private void RadioButton_Checked(object sender, RoutedEventArgs e) {
            UpdateView();
        }

        private void MenuItemConfiguration_Click(object sender, RoutedEventArgs e) {
            ConfigurationWindow confWindow = new ConfigurationWindow(configuration);
            confWindow.ShowDialog();
            UpdateView();
        }
    }
}
