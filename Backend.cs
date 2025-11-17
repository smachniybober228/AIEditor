using SixLabors.ImageSharp;
using SixLabors.ImageSharp.PixelFormats;
using SkiaSharp;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media.Media3D;
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
            device = cuda.is_available() ? CUDA : CPU;
            model = models.vgg19();

            foreach (var param in model.parameters()) // заморозка весов
            {
                param.requires_grad = false;
            }

            model.eval();
            model.to(device);

            imageForContent = LoadAndProcessImage("./Images/National_Gallery.jpg");
            //imageForStyle = LoadAndProcessImage("./Images/Castle.jpg");
            // Создать реализацию заполнения target картинки белым шумом

            var (contentFeatures, contentNames) = GetFeatureMaps(imageForContent, model);
        }

        public static void InspectNET()
        {
            string name = model.GetName();
            Type netType = model.GetType();
            string text = "";

            Console.WriteLine($"=== {name} PROPERTIES ===");
            foreach (PropertyInfo prop in netType.GetProperties())
            {
                text += $"{prop.Name} : {prop.PropertyType.Name}" + "\n";
            }

            text += "\n";

            Console.WriteLine($"=== {name} METHODS ===");
            foreach (MethodInfo method in netType.GetMethods())
            {
                text += $"{method.Name} : {method.ReturnType.Name}" + "\n";
            }

            MessageBox.Show(text);
        }

        private static Tensor ImageToTensor(Image<Rgb24> image)
        {
            int width = image.Width;
            int height = image.Height;

            // Создаем тензор [1, 3, H, W]
            Tensor tensor = torch.zeros(new long[] { 1, 3, height, width }, torch.float32);
            tensor = tensor.to(device);

            // Копируем пиксели
            for (int y = 0; y < height; y++)
            {
                for (int x = 0; x < width; x++)
                {
                    Rgb24 pixel = image[x, y];
                    tensor[0, 0, y, x] = pixel.R / 255.0f; // R
                    tensor[0, 1, y, x] = pixel.G / 255.0f; // G
                    tensor[0, 2, y, x] = pixel.B / 255.0f; // B
                }
            }
            return tensor;
        }

        private static Tensor LoadAndProcessImage(string imagePath)
        {
            using Image<Rgb24> image = SixLabors.ImageSharp.Image.Load<Rgb24>(imagePath);
            Tensor tensor = ImageToTensor(image);
            tensor = transforms.functional.resize(tensor, 256, 256);
            tensor = transforms.functional.normalize(
                tensor,
                [0.485, 0.456, 0.406],  // 3 значения для 3 каналов
                [0.229, 0.224, 0.225]   // 3 значения для 3 каналов
            );
            return tensor;
        }

        /// <summary>Получает feature maps для VGG19 (другие модели планируются в дальнейшем)</summary>
        /// <param name="image">Изображение</param>
        /// <param name="net">Нейросеть</param>
        /// <returns>Списки FeatureMaps и FeatureNames</returns>
        private static (List<Tensor>, List<string>) GetFeatureMaps(Tensor image, nn.Module<Tensor, Tensor> net) 
        {
            //List<Tensor> featureMaps = new();
            //List<string> featureNames = new();
            //int convLayerIndex = 0;

            //Sequential features = GetFeaturesSequential(net);

            //for (int layerNum = 0; layerNum < features.Count; layerNum++)
            //{
            //    image = features[layerNum].call(image);

            //    if (IsConv2dLayer(features[layerNum]))
            //    {
            //        featureMaps.Add(image);
            //        featureNames.Add($"ConvLayer_{convLayerIndex}");
            //        convLayerIndex++;
            //    }
            //}
            //return (featureMaps, featureNames);

            List<Tensor> featureMaps = new();
            List<string> featureNames = new();

            Tensor x = image;

            List<(string names, nn.Module)> seqs = net.named_children().ToList();

            for (int i = 0; i < seqs.Count; i++)
            {
                var (name, seq) = seqs[i];
                if (name == "features")
                {
                    Sequential features = seq as Sequential;
                    int convLayerIndex = 0;
                    for (int j = 0; j < features.Count; j++)
                    {
                        IModule<Tensor, Tensor> layer = features[j];
                        x = layer.call(x);
                        if (layer.GetType().Name == "Conv2d")
                        {
                            featureNames.Add("ConvLayer_" + convLayerIndex);
                            featureMaps.Add(x);
                            convLayerIndex++;
                        }
                    }
                    break;
                }
            }

            return (featureMaps, featureNames);

        }

        private static Tensor GetGramMatrix(Tensor featureMap)
        {
            long chans = featureMap.shape[1];
            long height = featureMap.shape[2];
            long width = featureMap.shape[3];
            featureMap = featureMap.reshape(chans, height * width);
            Tensor gram = torch.mm(featureMap, featureMap.t()) / (chans * height * width);
            return gram;
        }

        //private static Sequential GetFeaturesSequential(Module<Tensor, Tensor> net)
        //{
        //

        //    // Для VGG19 features обычно доступны как net.features
        //    // Если net.features не доступен напрямую, используем рефлексию
        //    Type netType = net.GetType();
        //    PropertyInfo? featuresProperty = netType.GetProperty("features");

        //    if (featuresProperty != null)
        //    {
        //        return (Sequential)featuresProperty.GetValue(net);
        //    }

        //    // Альтернативный способ: если net сам является Sequential
        //    if (net is Sequential sequential)
        //    {
        //        return sequential;
        //    }

        //    throw new InvalidOperationException("Cannot find features sequential in the network");
        //}

        //private static bool IsConv2dLayer(IModule<Tensor, Tensor> layer)
        //{
        //    return layer.GetType().Name.Contains("Conv2d");
        //}

    }
}
