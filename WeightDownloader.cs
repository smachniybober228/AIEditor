using AIEditor.Windows;
using System.IO;
using System.Net.Http;
using System.Security.Policy;
using System.Text.RegularExpressions;

namespace AIEditor
{
    public static class WeightDownloader
    {
        public static async Task DownloadWeights(string weightName)
        {
            string outputPath = @"..\..\..\Weights\" + weightName;
            if (!File.Exists(outputPath))
            {
                using var httpClient = new HttpClient();
                httpClient.DefaultRequestHeaders.Add("User-Agent", "AIEditor-App");
                httpClient.Timeout = TimeSpan.FromMinutes(10);

                string url = "https://github.com/smachniybober228/AIEditor/releases/download/v1.0.0/" + weightName;

                var response = await httpClient.GetAsync(url).ConfigureAwait(false);

                response.EnsureSuccessStatusCode();

                // Создаем папку
                Directory.CreateDirectory(Path.GetDirectoryName(outputPath));

                using var stream = await response.Content.ReadAsStreamAsync().ConfigureAwait(false);
                using var fileStream = new FileStream(outputPath, FileMode.Create);

                await stream.CopyToAsync(fileStream).ConfigureAwait(false);
            }
        }
    }
}
