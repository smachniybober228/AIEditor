using AIEditor.Models.Parameters;
using static TorchSharp.torchvision;

namespace AIEditor.Models.Model
{
    public static class ModelRegistry
    {
        public static List<ModelConfig> Configs { get; } = new();
        static ModelRegistry()
        {
            Configs.Add(new ModelConfig(ModelType.VGG19,
                                        new List<string> { "ConvLayer_1", "ConvLayer_4" },
                                        new List<string> { "ConvLayer_1", "ConvLayer_2", "ConvLayer_3", "ConvLayer_4", "ConvLayer_5" },
                                        new List<double> { 1, 0.5, 0.5, 0.2, 0.1 },
                                        Backend.GetFeatureMapsSequential,
                                        async (device) =>
                                            {
                                                await WeightDownloader.DownloadWeights("vgg19_weights.dat");
                                                return models.vgg19(weights_file: @"..\..\..\Weights\vgg19_weights.dat", device: device); 
                                            }));

            Configs.Add(new ModelConfig(ModelType.AlexNet,
                                        new List<string> { "ConvLayer_1", "ConvLayer_3" },
                                        new List<string> { "ConvLayer_0", "ConvLayer_1", "ConvLayer_2", "ConvLayer_3", "ConvLayer_4" },
                                        new List<double> { 1, 0.5, 0.5, 0.2, 0.1 },
                                        Backend.GetFeatureMapsSequential,
                                        async (device) => {
                                            await WeightDownloader.DownloadWeights("alexnet_weights.dat");
                                            return models.alexnet(weights_file: @"..\..\..\Weights\alexnet_weights.dat", device: device);
                                        }));

            Configs.Add(new ModelConfig(ModelType.ResNet18,
                                        new List<string> { "Layer_1", "Layer_2" },
                                        new List<string> { "ConvLayer_0", "Layer_1", "Layer_2", "Layer_3" },
                                        new List<double> { 1, 0.8, 0.6, 0.3 },
                                        Backend.GetFeatureMapsResnet18,
                                        async (device) => {
                                            await WeightDownloader.DownloadWeights("resnet18_weights.dat");
                                            return models.resnet18(weights_file: @"..\..\..\Weights\resnet18_weights.dat", device: device);
                                        }));
        }
    }
}
