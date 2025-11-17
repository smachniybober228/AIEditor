using SixLabors.ImageSharp;
using SixLabors.ImageSharp.Formats;
using SixLabors.ImageSharp.PixelFormats;
using SkiaSharp;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.IO.Pipes;
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

        private static List<string> layersForContent;
        private static List<string> layersForStyle;
        private static List<double> weightsForStyle;

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
            imageForStyle = LoadAndProcessImage("./Images/Castle.jpg");
            imageForTarget = CreateRandomTarget();

            layersForContent = new List<string>() { "ConvLayer_1", "ConvLayer_4" };
            layersForStyle = new List<string>() { "ConvLayer_1", "ConvLayer_2", "ConvLayer_3", "ConvLayer_4", "ConvLayer_5" };
            weightsForStyle = new List<double>() { 1, 0.5, 0.5, 0.2, 0.1 };

            Tensor target = Process();
            Image<Rgb24> targetImage = TensorToImage(target);
            targetImage.Save("newImage.jpg");
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
            Tensor tensor = torch.zeros(new long[] { 1, 3, height, width }, torch.float32).to(device);

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

        private static Image<Rgb24> TensorToImage(Tensor tensor)
        {
            // с помощью bitmap
            int height = (int)tensor.shape[2];
            int width = (int)tensor.shape[3];

            var image = new Image<Rgb24>(width, height);

            for (int y = 0; y < height; y++) 
            {
                for (int x = 0; x < width; x++)
                {
                    Rgb24 pixel = new Rgb24();
                    var a = tensor[0, 0, y, x];
                    var b = tensor[0, 1, y, x];
                    var c = tensor[0, 2, y, x];
                    pixel.R = (tensor[0, 0, y, x] * 255.0f).ToByte();
                    pixel.G = (tensor[0, 1, y, x] * 255.0f).ToByte();
                    pixel.B = (tensor[0, 2, y, x] * 255.0f).ToByte();
                    image[x, y] = pixel;
                }
            }
            return image;
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

        private static Tensor CreateRandomTarget()
        {
            long height = imageForContent.shape[2];
            long width = imageForContent.shape[3];

            Tensor tensor = torch.rand(new long[] { 1, 3, height, width }, device: device);

            // Нормализуем — но mean/std ДОЛЖНЫ быть на том же устройстве!
            double[] mean = new double[] { 0.485, 0.456, 0.406 };
            double[] std = new double[] { 0.229, 0.224, 0.225 };

            tensor = transforms.functional.normalize(tensor, mean, std);

            // После нормализации устанавливаем requires_grad
            tensor.requires_grad = true;

            // ⚠️ ДОПОЛНИТЕЛЬНО: сделайте clone(), чтобы избежать "view"-тензора
            return tensor;
        }
        private static Parameter CreateRandomTargetParam()
        {
            long height = imageForContent.shape[2];
            long width = imageForContent.shape[3];
            var tensor = torch.rand(new long[] { 1, 3, height, width }, device: device);
            tensor = transforms.functional.normalize(
                tensor,
                new double[] { 0.485, 0.456, 0.406 },
                new double[] { 0.229, 0.224, 0.225 }
            );
            tensor.requires_grad = true;
            return new Parameter(tensor.clone()); // clone() для безопасности
        }

        private static Tensor Process()
        {
            Tensor target = imageForTarget;
            Parameter targetParam = new Parameter(target.clone()); // проблема с параметрами и .clone()
            RMSProp optimizer = torch.optim.RMSProp([targetParam], lr: 0.005); // тут ошибка

            int numepochs = 1;
            double styleScaling = 1e5;

            var (contentFeatures, contentNames) = GetFeatureMaps(imageForContent, model);
            var (styleFeatures, styleNames) = GetFeatureMaps(imageForStyle, model);

            for (int i = 0; i < numepochs; i++)
            {
                var (targetFeatures, targetNames) = GetFeatureMaps(target, model);
                Tensor styleLoss = 0;
                Tensor contentLoss = 0;
                for (int layerI = 0; layerI < targetNames.Count; layerI++) 
                {
                    if (layersForContent.Contains(targetNames[layerI]))
                        contentLoss += torch.mean((targetFeatures[layerI] - contentFeatures[layerI]).pow(2));
                    //if targetFeatureNames[layeri] in layers4style
                    if (layersForStyle.Contains(targetNames[layerI]))
                    {
                        //Gtarget = gram_matrix(targetFeatureMaps[layeri]);
                        //Gstyle = gram_matrix(styleFeatureMaps[layeri]);
                        //styleLoss += torch.mean((Gtarget - Gstyle) * *2) * weights4style[layers4style.index(targetFeatureNames[layeri])];
                        Tensor GTarget = GetGramMatrix(targetFeatures[layerI]);
                        Tensor GStyle = GetGramMatrix(styleFeatures[layerI]);
                        styleLoss += torch.mean((GTarget - GStyle).pow(2)) * weightsForStyle[layersForStyle.IndexOf(targetNames[layerI])];
                    }
                }
                Tensor combiLoss = styleScaling * styleLoss + contentLoss;
                optimizer.zero_grad();
                combiLoss.backward();
                optimizer.step();
            }

            return target;
        }
    }
}
