using AIEditor.Windows;
using Microsoft.Win32;
using System.IO;
using System.Windows.Media.Imaging;

namespace AIEditor
{
    public static class ImageSaver
    {
        public static bool SaveBitmapSource(BitmapSource bitmapSource)
        {
            if (bitmapSource == null)
            {
                CustomMessageBox.Show("Нет изображения для сохранения", "Ошибка");
                return false;
            }

            // Настройки диалога
            SaveFileDialog saveDialog = new()
            {
                Title = "Сохранить изображение",
                Filter = GetImageFilters(),
                FilterIndex = 1,
                DefaultExt = ".png",
                AddExtension = true,
                OverwritePrompt = true,
                ValidateNames = true
            };

            // Показываем диалог
            if (saveDialog.ShowDialog() != true)
                return false;

            string filePath = saveDialog.FileName;
            string extension = Path.GetExtension(filePath).ToLower();

            try
            {
                // Сохраняем в зависимости от формата
                switch (extension)
                {
                    case ".png":
                        SaveAsPng(bitmapSource, filePath);
                        break;

                    case ".jpg":
                    case ".jpeg":
                        SaveAsJpeg(bitmapSource, filePath);
                        break;

                    case ".bmp":
                        SaveAsBmp(bitmapSource, filePath);
                        break;

                    case ".tiff":
                    case ".tif":
                        SaveAsTiff(bitmapSource, filePath);
                        break;

                    default:
                        // По умолчанию сохраняем как PNG
                        SaveAsPng(bitmapSource, Path.ChangeExtension(filePath, ".png"));
                        break;
                }

                CustomMessageBox.Show($"Изображение сохранено:\n{filePath}", "Успех");
                return true;
            }
            catch (Exception ex)
            {
                CustomMessageBox.Show($"Ошибка сохранения:\n{ex.Message}", "Ошибка");
                return false;
            }
        }

        private static string GetImageFilters()
        {
            return "PNG Image (*.png)|*.png|" +
                   "JPEG Image (*.jpg;*.jpeg)|*.jpg;*.jpeg|" +
                   "Bitmap Image (*.bmp)|*.bmp|" +
                   "TIFF Image (*.tiff;*.tif)|*.tiff;*.tif|" +
                   "Все файлы (*.*)|*.*";
        }

        // Методы сохранения в разных форматах
        private static void SaveAsPng(BitmapSource bitmapSource, string filePath)
        {
            using (var fileStream = new FileStream(filePath, FileMode.Create))
            {
                var encoder = new PngBitmapEncoder();
                encoder.Frames.Add(BitmapFrame.Create(bitmapSource));
                encoder.Save(fileStream);
            }
        }

        private static void SaveAsJpeg(BitmapSource bitmapSource, string filePath, int quality = 100)
        {
            using (var fileStream = new FileStream(filePath, FileMode.Create))
            {
                var encoder = new JpegBitmapEncoder();
                encoder.QualityLevel = Math.Clamp(quality, 1, 100);
                encoder.Frames.Add(BitmapFrame.Create(bitmapSource));
                encoder.Save(fileStream);
            }
        }

        private static void SaveAsBmp(BitmapSource bitmapSource, string filePath)
        {
            using (var fileStream = new FileStream(filePath, FileMode.Create))
            {
                var encoder = new BmpBitmapEncoder();
                encoder.Frames.Add(BitmapFrame.Create(bitmapSource));
                encoder.Save(fileStream);
            }
        }

        private static void SaveAsTiff(BitmapSource bitmapSource, string filePath)
        {
            using (var fileStream = new FileStream(filePath, FileMode.Create))
            {
                var encoder = new TiffBitmapEncoder();
                encoder.Frames.Add(BitmapFrame.Create(bitmapSource));
                encoder.Save(fileStream);
            }
        }
    }
}
