using SixLabors.ImageSharp;
using SixLabors.ImageSharp.PixelFormats;
using System.Reflection;
using System.Windows;
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
            model = models.vgg19().to(device);

            foreach (var param in model.parameters()) // заморозка весов
            {
                param.requires_grad = false;
            }

            model.eval();

            //imageForContent = LoadAndProcessImage("./Images/National_Gallery.jpg");
            //imageForStyle = LoadAndProcessImage("./Images/Castle.jpg");
            //imageForTarget = CreateRandomTarget();

            layersForContent = new List<string>() { "ConvLayer_1", "ConvLayer_4" };
            layersForStyle = new List<string>() { "ConvLayer_1", "ConvLayer_2", "ConvLayer_3", "ConvLayer_4", "ConvLayer_5" };
            weightsForStyle = new List<double>() { 1, 0.5, 0.5, 0.2, 0.1 };

            //Tensor target = Process();
            //Image<Rgb24> targetImage = TensorToImage(target);
            //targetImage.Save("newImage.jpg");
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

        public static Image<Rgb24> TensorToImage(Tensor tensor)
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
                    float a = (float)tensor[0, 0, y, x];
                    float b = (float)tensor[0, 1, y, x];
                    float c = (float)tensor[0, 2, y, x];
                    pixel.R = (byte)(a * 255.0f);
                    pixel.G = (byte)(b * 255.0f);
                    pixel.B = (byte)(c * 255.0f);
                    image[x, y] = pixel;
                }
            }
            return image;
        }

        public static Tensor LoadAndProcessImage(string imagePath)
        {
            using Image<Rgb24> image = SixLabors.ImageSharp.Image.Load<Rgb24>(imagePath);

            Tensor tensor = ImageToTensor(image);
            tensor = transforms.functional.resize(tensor, 256, 256);

            //tensor = transforms.functional.normalize(
            //    tensor,
            //    [0.485, 0.456, 0.406],  // 3 значения для 3 каналов
            //    [0.229, 0.224, 0.225]   // 3 значения для 3 каналов
            //);
            return tensor;
        }

        /// <summary>Получает feature maps для VGG19 (другие модели планируются в дальнейшем)</summary>
        /// <param name="image">Изображение</param>
        /// <param name="net">Нейросеть</param>
        /// <returns>Списки FeatureMaps и FeatureNames</returns>
        public static (List<Tensor>, List<string>) GetFeatureMaps(Tensor image, nn.Module<Tensor, Tensor> net)
        {
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
            Tensor f = featureMap.reshape(chans, height * width);
            Tensor gram = torch.mm(f, f.t()) / (chans * height * width);
            return gram;
        }

        private static Tensor CreateRandomTarget()
        {
            long height = imageForContent.shape[2];
            long width = imageForContent.shape[3];

            Tensor tensor = torch.rand(new long[] { 1, 3, height, width }, device: device);

            // Нормализуем — но mean/std ДОЛЖНЫ быть на том же устройстве!

            //double[] mean = new double[] { 0.485, 0.456, 0.406 };
            //double[] std = new double[] { 0.229, 0.224, 0.225 };

            //tensor = transforms.functional.normalize(tensor, mean, std);

            // После нормализации устанавливаем requires_grad
            tensor.requires_grad = true;

            // ⚠️ ДОПОЛНИТЕЛЬНО: сделайте clone(), чтобы избежать "view"-тензора
            return tensor;
        }
        //private static Parameter CreateRandomTargetParam()
        //{
        //    long height = imageForContent.shape[2];
        //    long width = imageForContent.shape[3];
        //    var tensor = torch.rand(new long[] { 1, 3, height, width }, device: device);
        //    tensor = transforms.functional.normalize(
        //        tensor,
        //        new double[] { 0.485, 0.456, 0.406 },
        //        new double[] { 0.229, 0.224, 0.225 }
        //    );
        //    tensor.requires_grad = true;
        //    return new Parameter(tensor.clone()); // clone() для безопасности
        //}

        // deepseek-version
        //private static Tensor Process()
        //{
        //    Parameter targetParam = new Parameter(imageForTarget.clone()); // проблема с параметрами и .clone()
        //    targetParam.requires_grad = true;
        //    RMSProp optimizer = torch.optim.RMSProp([targetParam], lr: 0.005); // тут ошибка

        //    string text = "";

        //    int numepochs = 100;
        //    double styleScaling = 1e5;

        //    List<Tensor> contentFeatures;
        //    List<string> contentNames;
        //    List<Tensor> styleFeatures;
        //    List<string> styleNames;

        //    using (no_grad())
        //    {
        //        (contentFeatures, contentNames) = GetFeatureMaps(imageForContent, model);
        //        (styleFeatures, styleNames) = GetFeatureMaps(imageForStyle, model);
        //    }

        //    for (int i = 0; i < numepochs; i++)
        //    {
        //        using var gradScope = torch.enable_grad();

        //        var (targetFeatures, targetNames) = GetFeatureMaps(targetParam, model);
        //        Tensor styleLoss = torch.tensor(0.0f, device: device);
        //        Tensor contentLoss = torch.tensor(0.0f, device: device);
        //        for (int layerI = 0; layerI < targetNames.Count; layerI++)
        //        {
        //            if (layersForContent.Contains(targetNames[layerI]))
        //            {
        //                using (torch.enable_grad())
        //                {
        //                    var contentTarget = GetFeatureMaps(targetParam, model);
        //                    contentLoss += torch.mean((targetFeatures[layerI] - contentFeatures[layerI]).pow(2));
        //                }
        //            }
        //            if (layersForStyle.Contains(targetNames[layerI]))
        //            {
        //                Tensor GTarget = GetGramMatrix(targetFeatures[layerI]);
        //                Tensor GStyle = GetGramMatrix(styleFeatures[layerI]);
        //                styleLoss += torch.mean((GTarget - GStyle).pow(2)) * weightsForStyle[layersForStyle.IndexOf(targetNames[layerI])];
        //            }
        //        }
        //        Tensor combiLoss = styleScaling * styleLoss + contentLoss;
        //        optimizer.zero_grad();
        //        combiLoss.backward();
        //        optimizer.step();

        //        // Выводим прогресс для отладки
        //        if (i % 10 == 0)
        //        {
        //            text += $"Epoch {i}, Loss: {combiLoss.item<float>()}\n" +
        //                    $"Content Loss = {contentLoss.item<float>()}\n" +
        //                    $"Style Loss = {styleLoss.item<float>()}\n\n";

        //            if (targetParam.grad is not null && !targetParam.grad.isnan().any().item<bool>())
        //            {
        //                float gradNorm = targetParam.grad.norm().item<float>();
        //                text += $"Gradient norm: {gradNorm}\n";

        //                optimizer.step();

        //                // Если градиенты слишком маленькие, увеличим learning rate
        //                if (gradNorm < 1e-8)
        //                {
        //                    text += "Gradients are too small, adjusting learning rate...\n";
        //                    foreach (var param_group in optimizer.ParamGroups)
        //                    {
        //                        param_group.LearningRate *= 2;
        //                    }
        //                }
        //            }
        //            else
        //            {
        //                text += "No gradients! This is the problem.\n";
        //                break;
        //            }
        //        }
        //    }
        //    MessageBox.Show(text);
        //    return targetParam.detach().clone();
        //}

        private static Tensor Process()
        {
            string text = "";

            Parameter targetParam = new Parameter(imageForTarget.clone());
            targetParam.requires_grad = true;

            RMSProp optimizer = torch.optim.RMSProp([targetParam], lr: 0.005);
            string str = $"{targetParam.grad}";
            text += $"targetParam.requires_grad: {targetParam.requires_grad}\n";

            int numepochs = 5;
            double styleScaling = 1e5;

            var (contentFeatures, contentNames) = GetFeatureMaps(imageForContent, model);
            var (styleFeatures, styleNames) = GetFeatureMaps(imageForStyle, model);

            for (int i = 0; i < numepochs; i++)
            {
                var (targetFeatures, targetNames) = GetFeatureMaps(targetParam, model);
                Tensor styleLoss = torch.tensor(0.0f, device: device);
                Tensor contentLoss = torch.tensor(0.0f, device: device);

                for (int layerI = 0; layerI < targetNames.Count; layerI++)
                {
                    if (layersForContent.Contains(targetNames[layerI]))
                        contentLoss += torch.mean((targetFeatures[layerI] - contentFeatures[layerI]).pow(2));
                    if (layersForStyle.Contains(targetNames[layerI]))
                    {
                        Tensor GTarget = GetGramMatrix(targetFeatures[layerI]);
                        Tensor GStyle = GetGramMatrix(styleFeatures[layerI]);
                        styleLoss += torch.mean((GTarget - GStyle).pow(2)) * weightsForStyle[layersForStyle.IndexOf(targetNames[layerI])];
                    }
                }
                Tensor combiLoss = styleScaling * styleLoss + contentLoss;

                optimizer.zero_grad();
                combiLoss.backward();
                optimizer.step();

                text += $"Epoch {i}, Loss: {combiLoss.item<float>()}, Grad: {targetParam.grad?.item<float>() ?? float.NaN}\n";
            }

            MessageBox.Show(text);
            return targetParam;
        }

        private static Tensor ProcessOtladka()
        {
            string diagnosticText = "";

            // Создаем Parameter
            Parameter targetParam = new Parameter(imageForTarget.clone());
            targetParam.requires_grad = true;

            var optimizer = torch.optim.Adam(new[] { targetParam }, lr: 0.01);
            int numepochs = 3;
            double styleScaling = 1e6;

            // Получаем content/style features один раз (без градиентов)
            using (no_grad())
            {
                var (contentFeatures, contentNames) = GetFeatureMaps(imageForContent, model);
                var (styleFeatures, styleNames) = GetFeatureMaps(imageForStyle, model);

                for (int i = 0; i < numepochs; i++)
                {
                    diagnosticText += $"=== EPOCH {i} ===\n";

                    // Каждую эпоху создаем НОВЫЙ граф вычислений
                    using (var scope = torch.enable_grad())
                    {
                        var (targetFeatures, targetNames) = GetFeatureMaps(targetParam, model);

                        Tensor styleLoss = torch.tensor(0.0f, device: device);
                        Tensor contentLoss = torch.tensor(0.0f, device: device);

                        for (int layerI = 0; layerI < targetNames.Count; layerI++)
                        {
                            if (layersForContent.Contains(targetNames[layerI]))
                                contentLoss += torch.mean((targetFeatures[layerI] - contentFeatures[layerI]).pow(2));
                            if (layersForStyle.Contains(targetNames[layerI]))
                            {
                                Tensor GTarget = GetGramMatrix(targetFeatures[layerI]);
                                Tensor GStyle = GetGramMatrix(styleFeatures[layerI]);
                                styleLoss += torch.mean((GTarget - GStyle).pow(2)) * weightsForStyle[layersForStyle.IndexOf(targetNames[layerI])];
                            }
                        }

                        Tensor combiLoss = styleScaling * styleLoss + contentLoss;

                        optimizer.zero_grad();
                        combiLoss.backward(); // БЕЗ retain_graph - граф создается заново каждый раз

                        diagnosticText += $"Gradient: {(targetParam.grad is null ? "NULL" : targetParam.grad.norm().item<float>().ToString())}\n";
                        diagnosticText += $"Loss: {combiLoss.item<float>()}\n";

                        optimizer.step();
                    }

                    diagnosticText += "\n";
                }
            }

            MessageBox.Show(diagnosticText);
            return targetParam.detach().clone();
        }

        //private static string TraceGraph(Tensor tensor, string name, int depth = 0)
        //{
        //    string indent = new string(' ', depth * 2);
        //    string text = "";

        //    text += $"{indent}{name}:\n";
        //    text += $"{indent}  Shape: [{string.Join(", ", tensor.shape)}]\n";
        //    text += $"{indent}  Requires grad: {tensor.requires_grad}\n";
        //    text += $"{indent}  Is leaf: {tensor.is_leaf}\n";
        //    text += $"{indent}  Grad: {(tensor.grad is null ? "null" : "exists")}\n";

        //    if (tensor.grad_fn is not null)
        //    {
        //        text += $"{indent}  Grad function: {tensor.grad_fn.GetType().Name}\n";

        //       // Рекурсивно обходим предков в графе
        //       var next_functions = tensor.grad_fn.NextFunctions;
        //        if (next_functions is not null)
        //        {
        //            for (int i = 0; i < next_functions.Count; i++)
        //            {
        //                var next_tensor = next_functions[i];
        //                if (next_tensor is not null)
        //                {
        //                    text += TraceGraph(next_tensor, $"Input_{i}", depth + 1);
        //                }
        //            }
        //        }
        //    }
        //    else
        //    {
        //        text += $"{indent}  Grad function: null\n";
        //    }

        //    return text;
        //}
    }
}
