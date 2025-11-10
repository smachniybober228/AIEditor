using SixLabors.ImageSharp;
using SixLabors.ImageSharp.PixelFormats;
using SkiaSharp;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using TorchSharp;
using TorchSharp.Modules;
using static TorchSharp.torch;
using static TorchSharp.torch.nn;
using static TorchSharp.torchvision;

namespace AIEditor
{
    public static class Backend
    {
        private static Device device;
        private static Module<Tensor, Tensor> model;
        private static Tensor imageForContent;
        private static Tensor imageForStyle;
        private static Tensor imageForTarget;

        static Backend()
        {
            device = torch.cuda.is_available() ? torch.CUDA : torch.CPU;
            model = models.vgg19();
            model.eval();
            model.to(device);

            imageForContent = LoadAndProcessImage("./Images/National_Gallery.jpg");
            imageForStyle = LoadAndProcessImage("./Images/Castle.jpg");
        }

        public static void InspectNET()
        {
            var name = model.GetName();
            var netType = model.GetType();
            string text = "";

            Console.WriteLine($"=== {name} PROPERTIES ===");
            foreach (var prop in netType.GetProperties())
            {
                text += $"{prop.Name} : {prop.PropertyType.Name}" + "\n";
            }

            text += "\n";

            Console.WriteLine($"=== {name} METHODS ===");
            foreach (var method in netType.GetMethods().Where(m => !m.Name.StartsWith("get_")))
            {
                text += $"{method.Name} : {method.ReturnType.Name}" + "\n";
            }
            MessageBox.Show(text);
        }

        private static Tensor ImageToTensor(Image<Rgb24> image)
        {
            var width = image.Width;
            var height = image.Height;

            // Создаем тензор [3, H, W]
            var tensor = torch.zeros(new long[] { 3, height, width }, torch.float32);

            // Копируем пиксели
            for (int y = 0; y < height; y++)
            {
                for (int x = 0; x < width; x++)
                {
                    var pixel = image[x, y];
                    tensor[0, y, x] = pixel.R / 255.0f; // R
                    tensor[1, y, x] = pixel.G / 255.0f; // G
                    tensor[2, y, x] = pixel.B / 255.0f; // B
                }
            }
            tensor.to(device);
            return tensor;
        }

        private static Tensor LoadAndProcessImage(string imagePath)
        {
            using var image = SixLabors.ImageSharp.Image.Load<Rgb24>(imagePath);

            if (image.PixelType.BitsPerPixel != 24)
            {
                throw new InvalidOperationException($"Expected 24bpp RGB image, got {image.PixelType.BitsPerPixel}bpp");
            }

            var tensor = ImageToTensor(image);

            if (tensor.shape.Length != 3 || tensor.shape[0] != 3)
            {
                throw new InvalidOperationException($"Expected tensor shape [3, H, W], got [{string.Join(", ", tensor.shape)}]");
            }

            tensor = transforms.functional.resize(tensor, 256, 256);

            if (tensor.shape[0] != 3)
            {
                throw new InvalidOperationException(
                    $"Expected 3 channels, but got {tensor.shape[0]} channels. " +
                    $"Full shape: [{string.Join(", ", tensor.shape)}]");
            }

            tensor = transforms.functional.normalize(
                tensor,
                new double[] { 0.485, 0.456, 0.406 },  // 3 значения для 3 каналов
                new double[] { 0.229, 0.224, 0.225 }   // 3 значения для 3 каналов
            );
            return tensor.unsqueeze(0).to(device);
        }

        private static void GetFeatureMapActs(Tensor image, nn.Module<Tensor, Tensor> net) 
        {
            List<Tensor> featureMaps = new();
            List<string> featureNames = new();
            int convLayerIndex = 0;

            var features = GetFeaturesSequential(net);
            for (int layerNum = 0; layerNum < features.Count; layerNum++)
            {
                image = features[layerNum].call(image);

                if (IsConv2dLayer(features[layerNum]))
                {
                    featureMaps.Add(image);
                    featureNames.Add($"ConvLayer_{convLayerIndex}");
                    convLayerIndex++;
                }
            }
        }

        private static Sequential GetFeaturesSequential(Module<Tensor, Tensor> net)
        {
            // Для VGG19 features обычно доступны как net.features
            // Если net.features не доступен напрямую, используем рефлексию
            var netType = net.GetType();
            var featuresProperty = netType.GetProperty("features");

            if (featuresProperty != null)
            {
                return (Sequential)featuresProperty.GetValue(net);
            }

            // Альтернативный способ: если net сам является Sequential
            if (net is Sequential sequential)
            {
                return sequential;
            }

            throw new InvalidOperationException("Cannot find features sequential in the network");
        }

        private static bool IsConv2dLayer(IModule<Tensor, Tensor> layer)
        {
            return layer.GetType().Name.Contains("Conv2d");
        }

    }
}
