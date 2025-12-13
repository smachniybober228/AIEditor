using AIEditor.Models.Model;
using AIEditor.Models.Parameters;
using AIEditor.Windows;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.PixelFormats;
using System.Windows;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using TorchSharp.Modules;
using static TorchSharp.torch;
using static TorchSharp.torch.nn;
using static TorchSharp.torch.optim;
using static TorchSharp.torchvision;

namespace AIEditor.Models
{
    public static class Backend
    {
        private readonly static Device device;

        private readonly static Tensor imageForTarget;
        static Backend()
        {
            device = cuda.is_available() ? CUDA : CPU;

            imageForTarget = CreateRandomTarget();
        }

        private static Tensor ImageToTensor(Image<Rgb24> image)
        {
            int width = image.Width;
            int height = image.Height;

            Tensor tensor = zeros(new long[] { 1, 3, height, width }, float32).to(device);

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

        // ”старевший метод
        private static Image<Rgb24> TensorToImage(Tensor input)
        {
            int height = (int)input.shape[2];
            int width = (int)input.shape[3];

            var image = new Image<Rgb24>(width, height);
            Tensor sigma = sigmoid(input);

            for (int y = 0; y < height; y++)
            {
                for (int x = 0; x < width; x++)
                {
                    Rgb24 pixel = new Rgb24();

                    float r = (float)sigma[0, 0, y, x];
                    float g = (float)sigma[0, 1, y, x];
                    float b = (float)sigma[0, 2, y, x];

                    pixel.R = (byte)(r * 255.0f);
                    pixel.G = (byte)(g * 255.0f);
                    pixel.B = (byte)(b * 255.0f);

                    image[x, y] = pixel;
                }
            }
            return image;
        }

        private static BitmapSource TensorToBitmap(Tensor input)
        {
            int height = (int)input.shape[2];
            int width = (int)input.shape[3];

            byte[] pixelData = new byte[height * width * 4];
            Tensor sigma = sigmoid(input);

            int index = 0;
            for (int y = 0; y < height; y++)
            {
                for (int x = 0; x < width; x++) 
                {
                    float r = (float)sigma[0, 0, y, x];
                    float g = (float)sigma[0, 1, y, x];
                    float b = (float)sigma[0, 2, y, x];

                    pixelData[index++] = (byte)(b * 255);
                    pixelData[index++] = (byte)(g * 255);
                    pixelData[index++] = (byte)(r * 255);
                    pixelData[index++] = 255; // јльфа (полностью непрозрачный)
                }
            }

            BitmapSource result = BitmapSource.Create(
                width,                      // ширина
                height,                     // высота
                96,                         // DPI по горизонтали
                96,                         // DPI по вертикали
                PixelFormats.Bgra32,        // формат пикселей (BGRA)
                null,                       // палитра (не нужна)
                pixelData,                  // данные пикселей
                width * 4                   // stride (байт на строку)
            );
            result.Freeze();

            return result;
        }

        private static Tensor NormalizeTensor(Tensor input)
        {
            return transforms.functional.normalize(
                input,
                [0.485, 0.456, 0.406],
                [0.229, 0.224, 0.225]
            );
        }

        private static Tensor LoadAndProcessImage(string imagePath)
        {
            using Image<Rgb24> image = Image.Load<Rgb24>(imagePath);

            Tensor tensor = ImageToTensor(image);
            tensor = transforms.functional.resize(tensor, 256, 256);
            tensor = NormalizeTensor(tensor);

            return tensor;
        }

        public static (List<Tensor>, List<string>) GetFeatureMapsSequential(Tensor image, Module<Tensor, Tensor> net)
        {
            List<Tensor> featureMaps = new();
            List<string> featureNames = new();

            Tensor x = image.clone();

            var featuresBlock = net.named_children().First(s => s.name == "features").module as Sequential;
            if (featuresBlock is null)
                throw new InvalidOperationException($"Ѕлок 'features' не найден в модели");

            int convLayerIndex = 0;
            for (int i = 0; i < featuresBlock.Count; i++)
            {
                IModule<Tensor, Tensor> layer = featuresBlock[i];
                x = layer.call(x);
                if (layer.GetType().Name == "Conv2d")
                {
                    featureNames.Add("ConvLayer_" + convLayerIndex);
                    featureMaps.Add(x);
                    convLayerIndex++;
                }
            }

            return (featureMaps, featureNames);
        }

        public static (List<Tensor>, List<string>) GetFeatureMapsResnet18(Tensor image, Module<Tensor, Tensor> net)
        {
            List<Tensor> featureMaps = new();
            List<string> featureNames = new();

            Tensor x = image.clone();

            List<(string names, Module)> seqs = net.named_children().ToList();

            int convLayersIndex = 0;
            for (int i = 0; i < seqs.Count; i++)
            {
                var (name, seq) = seqs[i];
                if (seq.GetType().Name == "AdaptiveAvgPool2d" || seq.GetType().Name == "Flatten" || seq.GetType().Name == "Linear")
                    continue;
                if (seq is Sequential)
                {
                    Sequential layer = seq as Sequential;
                    x = layer.call(x);
                    featureNames.Add("Layer_" + convLayersIndex);
                    featureMaps.Add(x);
                    convLayersIndex++;
                }
                else
                {
                    IModule<Tensor, Tensor> module = seq as IModule<Tensor, Tensor>;
                    x = module.call(x);
                    if (module.GetType().Name == "Conv2d")
                    {
                        featureNames.Add("ConvLayer_" + convLayersIndex);
                        featureMaps.Add(x);
                        convLayersIndex++;
                    }
                }
            }

            return (featureMaps, featureNames);
        }

        private static Tensor GetGramMatrix(Tensor featureMap)
        {
            long chans = featureMap.shape[1];
            long height = featureMap.shape[2];
            long width = featureMap.shape[3];
            Tensor f = featureMap.reshape(chans, height * width);
            Tensor gram = mm(f, f.t()) / (chans * height * width);
            return gram;
        }

        private static Tensor CreateRandomTarget()
        {
            Tensor tensor = rand([1, 3, 256, 256], device: device);
            tensor = NormalizeTensor(tensor);
            return tensor;
        }

        private static Optimizer CreateOptimizer(Parameter targetParam, OptimizerType optType, double lr)
        {
            switch (optType)
            {
                case OptimizerType.RMSProp:
                    return RMSProp([targetParam], lr: lr);
                case OptimizerType.Adam:
                    return Adam([targetParam], lr: lr);
                case OptimizerType.Adagrad:
                    return Adagrad([targetParam], lr: lr);
                default:
                    throw new NotImplementedException();
            }
        }

        private static void FreezeModel(Module<Tensor, Tensor> model)
        {
            foreach (var param in model.parameters())
            {
                param.requires_grad = false;
            }
            model.eval();
        }

        public async static Task<BitmapSource> Process(string contentImagePath, string styleImagePath, ProgressWindow progressWindow)
        {
            StyleTransferParameters settings = ParametersManager.LoadSettings();

            ModelConfig modelConfig = ModelRegistry.Configs.Find(s => s.Type == settings.Model);
            Module<Tensor, Tensor> model = await modelConfig.ModelFactory(device);
            FreezeModel(model);

            Tensor imageForContent = LoadAndProcessImage(contentImagePath);
            Tensor imageForStyle = LoadAndProcessImage(styleImagePath);

            Parameter targetParam = new Parameter(imageForTarget.clone());
            Optimizer optimizer = CreateOptimizer(targetParam, settings.Optimizer, settings.LearningRate);

            var (contentFeatures, contentNames) = modelConfig.FeatureExtractor(imageForContent, model);
            var (styleFeatures, styleNames) = modelConfig.FeatureExtractor(imageForStyle, model);

            for (int i = 0; i < settings.NumEpochs; i++)
            {
                if (progressWindow.IsVisible)
                    progressWindow.UpdateProgress(i, settings.NumEpochs);
                else
                {
                    Application.Current.Dispatcher.Invoke(() => CustomMessageBox.Show("¬ы прервали обучение!"));
                    break;
                }

                var (targetFeatures, targetNames) = modelConfig.FeatureExtractor(targetParam, model);
                Tensor styleLoss = tensor(0.0f, device: device);
                Tensor contentLoss = tensor(0.0f, device: device);

                for (int layerI = 0; layerI < targetNames.Count; layerI++)
                {
                    if (modelConfig.ContentLayers.Contains(targetNames[layerI]))
                        contentLoss += mean((targetFeatures[layerI] - contentFeatures[layerI]).pow(2));
                    if (modelConfig.StyleLayers.Contains(targetNames[layerI]))
                    {
                        Tensor GTarget = GetGramMatrix(targetFeatures[layerI]);
                        Tensor GStyle = GetGramMatrix(styleFeatures[layerI]);
                        styleLoss += mean((GTarget - GStyle).pow(2)) * modelConfig.StyleLayerWeights[modelConfig.StyleLayers.IndexOf(targetNames[layerI])];
                    }
                }
                Tensor combiLoss = settings.StyleScaling * styleLoss + contentLoss;

                optimizer.zero_grad();
                combiLoss.backward();
                optimizer.step();
            }

            progressWindow.Complete();
            BitmapSource targetImage = TensorToBitmap(targetParam);
            return targetImage;
        }
    }
}
