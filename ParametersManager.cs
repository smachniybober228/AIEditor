using AIEditor.Windows;
using System.IO;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace AIEditor
{
    public static class ParametersManager
    {
        private static readonly string SettingsPath = "style_transfer_parameters.json";
        public static StyleTransferParameters LoadSettings()
        {
            if (File.Exists(SettingsPath))
            {
                try
                {
                    string json = File.ReadAllText(SettingsPath);
                    return JsonSerializer.Deserialize<StyleTransferParameters>(json);
                }
                catch (Exception ex)
                {
                    CustomMessageBox.Show($"Ошибка загрузки настроек: {ex.Message}. Использую настройки по умолчанию.");
                }
            }

            StyleTransferParameters defaultParameters = new StyleTransferParameters();
            return defaultParameters;
        }

        public static void SaveSettings(StyleTransferParameters settings)
        {
            try
            {
                var options = new JsonSerializerOptions
                {
                    WriteIndented = true,
                    DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
                };

                string json = JsonSerializer.Serialize(settings, options);
                File.WriteAllText(SettingsPath, json);
                CustomMessageBox.Show("Настройки сохранены!");
            }
            catch (Exception ex)
            {
                CustomMessageBox.Show($"Ошибка сохранения настроек: {ex.Message}");
            }
        }
    }
}
