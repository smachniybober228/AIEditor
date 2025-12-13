using AIEditor.Models;
using AIEditor.Windows;
using Microsoft.Win32;
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
            openFileDialog.Title = "Выберите изображение";
            openFileDialog.Filter = "Все поддерживаемые форматы|*.jpg;*.jpeg;*.png;*.bmp;*.gif|" +
                    "JPEG Images|*.jpg;*.jpeg|" +
                    "PNG Images|*.png|" +
                    "BMP Images|*.bmp|" +
                    "GIF Images|*.gif";

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
            openFileDialog.Title = "Выберите изображение";
            openFileDialog.Filter = "Все поддерживаемые форматы|*.jpg;*.jpeg;*.png;*.bmp;*.gif|" +
                    "JPEG Images|*.jpg;*.jpeg|" +
                    "PNG Images|*.png|" +
                    "BMP Images|*.bmp|" +
                    "GIF Images|*.gif";

            if (openFileDialog.ShowDialog() == true)
            {
                ImageSourceConverter converter = new();
                StyleImage.Source = (ImageSource)converter.ConvertFromString(openFileDialog.FileName);
                stylePath = openFileDialog.FileName;
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

                BitmapSource resultImage = await processingTask;
                ResultWindow resultWindow = new(resultImage);
                resultWindow.ShowDialog();
            }
        }
    }
}