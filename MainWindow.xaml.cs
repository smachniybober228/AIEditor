using AIEditor.Windows;
using Microsoft.Win32;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.PixelFormats;
using System.IO;
using System.Windows;
using System.Windows.Media;
using System.Windows.Media.Imaging;

namespace AIEditor
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        private string contentPath = "";
        private string stylePath = "";
        public MainWindow()
        {
            InitializeComponent();
        }

        private void SettingsClick(object sender, RoutedEventArgs e)
        {
            SettingsWindow settingsWindow = new();
            settingsWindow.ShowDialog();
        }

        private void LoadContentImage(object sender, RoutedEventArgs e)
        {
            OpenFileDialog openFileDialog = new OpenFileDialog();
            openFileDialog.Title = "K15@8B5 87>1@065=85";
            openFileDialog.Filter = "A5 ?>445@68205<K5 D>@<0BK|*.jpg;*.jpeg;*.png;*.bmp;*.gif|" +
                               "JPEG 7>1@065=8O|*.jpg;*.jpeg|" +
                               "PNG 7>1@065=8O|*.png|" +
                               "BMP 7>1@065=8O|*.bmp|" +
                               "GIF 7>1@065=8O|*.gif";

            if (openFileDialog.ShowDialog() == true)
            {
                ImageSourceConverter converter = new();
                ContentImage.Source = (ImageSource)converter.ConvertFromString(openFileDialog.FileName);
                contentPath = openFileDialog.FileName;
            }
        }

        private void LoadStyleImage(object sender, RoutedEventArgs e)
        {
            OpenFileDialog openFileDialog = new OpenFileDialog();
            openFileDialog.Title = "K15@8B5 87>1@065=85";
            openFileDialog.Filter = "A5 ?>445@68205<K5 D>@<0BK|*.jpg;*.jpeg;*.png;*.bmp;*.gif|" +
                               "JPEG 7>1@065=8O|*.jpg;*.jpeg|" +
                               "PNG 7>1@065=8O|*.png|" +
                               "BMP 7>1@065=8O|*.bmp|" +
                               "GIF 7>1@065=8O|*.gif";

            if (openFileDialog.ShowDialog() == true)
            {
                ImageSourceConverter converter = new();
                StyleImage.Source = (ImageSource)converter.ConvertFromString(openFileDialog.FileName);
                stylePath = openFileDialog.FileName;
            }
        }

        private BitmapSource ConvertToBitmapSource(Image<Rgb24> image) // логичнее сразу изображение делать BitMap, а не Rgb<24>
        {
            using (var ms = new MemoryStream())
            {
                image.SaveAsJpeg(ms);
                ms.Position = 0;

                var bitmap = new BitmapImage();
                bitmap.BeginInit();
                bitmap.CacheOption = BitmapCacheOption.OnLoad;
                bitmap.StreamSource = ms;
                bitmap.EndInit();
                bitmap.Freeze();

                return bitmap;
            }
        }

        private async void ProcessImages(object sender, RoutedEventArgs e)
        {
            if (string.IsNullOrEmpty(contentPath) || string.IsNullOrEmpty(stylePath))
                CustomMessageBox.Show("Укажите изображения!");
            else
            {
                ProgressWindow progressWindow = new();
                Task<BitmapSource> processingTask = Task.Run(() => Backend.Process(contentPath, stylePath, progressWindow));
                progressWindow.ShowDialog();

                BitmapSource resultImage = await processingTask;//Backend.Process(contentPath, stylePath, progressWindow);
                //BitmapSource bitmapResult = ConvertToBitmapSource(resultImage);
                ResultWindow resultWindow = new(resultImage);
                resultWindow.ShowDialog();
            }
        }
    }
}