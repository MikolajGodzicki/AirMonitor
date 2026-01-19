using AirMonitor.Models;
using System.Windows;

namespace AirMonitor.Views {
    /// <summary>
    /// Logika interakcji dla klasy ConfigurationWindow.xaml
    /// </summary>
    public partial class ConfigurationWindow : Window {
        private Configuration configuration;

        public ConfigurationWindow(Configuration configuration) {
            InitializeComponent();
            this.configuration = configuration;
            LoadSettings();
        }

        private void LoadSettings() {
            HCHO_Limit.Value = configuration.HCHO_Limit;
            HCL_Limit.Value = configuration.HCL_Limit;
            H2S_Limit.Value = configuration.H2S_Limit;
            NH3_Limit.Value = configuration.NH3_Limit;
            PM1_Limit.Value = configuration.PM1_Limit;
            PM2_5_Limit.Value = configuration.PM2_5_Limit;
            PM10_Limit.Value = configuration.PM10_Limit;
        }

        private void SaveSettings_Click(object sender, RoutedEventArgs e) {

            if (HCHO_Limit.Value == null || HCL_Limit.Value == null || H2S_Limit.Value == null ||
                NH3_Limit.Value == null || PM1_Limit.Value == null || PM2_5_Limit.Value == null ||
                PM10_Limit.Value == null) {
                MessageBox.Show("Proszę wprowadzić wszystkie limity.");
                return;
            }

            configuration.HCHO_Limit = (double)HCHO_Limit.Value;
            configuration.HCL_Limit = (double)HCL_Limit.Value;
            configuration.H2S_Limit = (double)H2S_Limit.Value;
            configuration.NH3_Limit = (double)NH3_Limit.Value;
            configuration.PM1_Limit = (double)PM1_Limit.Value;
            configuration.PM2_5_Limit = (double)PM2_5_Limit.Value;
            configuration.PM10_Limit = (double)PM10_Limit.Value;
            configuration.Save();

            MessageBox.Show("Ustawienia zostały zapisane.");
            this.Close();
        }
    }
}
