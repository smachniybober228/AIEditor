using System.Windows;
using System.Windows.Media;
using System.Windows.Media.Imaging;

namespace AIEditor.Windows
{
    /// <summary>
    /// Логика взаимодействия для ResultWindow.xaml
    /// </summary>
    public partial class ResultWindow : Window
    {
        private BitmapSource bitmap { get; }
        public ResultWindow(BitmapSource resultImage)
        {
            InitializeComponent();

            bitmap = resultImage;
            ResultImage.Source = resultImage;
        }

        private void BackToMainWindow(object sender, RoutedEventArgs e)
        {
            DialogResult = false;
        }

        private void SaveImage(object sender, RoutedEventArgs e)
        {
            if (ImageSaver.SaveBitmapSource(bitmap))
                DialogResult = true;
        }
    }
}
