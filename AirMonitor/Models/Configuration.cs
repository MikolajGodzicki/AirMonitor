using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;

namespace AirMonitor.Models {
    public class Configuration {

        public double HCHO_Limit;
        public double HCL_Limit;
        public double H2S_Limit;
        public double NH3_Limit;
        public double PM1_Limit;
        public double PM2_5_Limit;
        public double PM10_Limit;

        public void Load() {
            string filePath = getPath();

            if (Directory.Exists(Path.GetDirectoryName(filePath)) == false) {
                Directory.CreateDirectory(Path.GetDirectoryName(filePath)!);
            }

            if (File.Exists(filePath)) {
                string json = File.ReadAllText(filePath);
                var options = new JsonSerializerOptions {
                    PropertyNameCaseInsensitive = true
                };
                var config = JsonSerializer.Deserialize<ConfigurationModel>(json, options);
                if (config != null) {
                    HCHO_Limit = config.HCHO_Limit;
                    HCL_Limit = config.HCL_Limit;
                    H2S_Limit = config.H2S_Limit;
                    NH3_Limit = config.NH3_Limit;
                    PM1_Limit = config.PM1_Limit;
                    PM2_5_Limit = config.PM2_5_Limit;
                    PM10_Limit = config.PM10_Limit;
                }
            } else {
                File.WriteAllText(filePath, JsonSerializer.Serialize(this, new JsonSerializerOptions { WriteIndented = true }));
            }
        }

        public void Save() {
            string filePath = getPath();
            if (Directory.Exists(Path.GetDirectoryName(filePath)) == false) {
                Directory.CreateDirectory(Path.GetDirectoryName(filePath)!);
            }

            var ConfigurationModel = new ConfigurationModel {
                HCHO_Limit = this.HCHO_Limit,
                HCL_Limit = this.HCL_Limit,
                H2S_Limit = this.H2S_Limit,
                NH3_Limit = this.NH3_Limit,
                PM1_Limit = this.PM1_Limit,
                PM2_5_Limit = this.PM2_5_Limit,
                PM10_Limit = this.PM10_Limit
            };

            string json = JsonSerializer.Serialize(ConfigurationModel, new JsonSerializerOptions { WriteIndented = true });

            File.WriteAllText(filePath, json);
        }

        private string getPath() {
            return $"{Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments)}\\AirMonitor\\config.json";
        }
    }
}
