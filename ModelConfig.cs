using static TorchSharp.torch;
using static TorchSharp.torch.nn;

namespace AIEditor
{
    public class ModelConfig
    {
        public ModelType Type { get; set; }
        public List<string> ContentLayers { get; set; }
        public List<string> StyleLayers { get; set; }
        public List<double> StyleLayerWeights { get; set; }
        public Func<Tensor, Module<Tensor, Tensor>, (List<Tensor>, List<string>)> FeatureExtractor { get; set; }
        public Func<Device, Task<Module<Tensor, Tensor>>> ModelFactory { get; set; }

        public ModelConfig(ModelType type, List<string> contentLayers, List<string> styleLayers, List<double> styleLayerWeights, Func<Tensor, 
            Module<Tensor, Tensor>, (List<Tensor>, List<string>)> featureExtractor, Func<Device, Task<Module<Tensor, Tensor>>> modelFactory)
        {
            Type = type;
            ContentLayers = contentLayers;
            StyleLayers = styleLayers;
            StyleLayerWeights = styleLayerWeights;
            FeatureExtractor = featureExtractor;
            ModelFactory = modelFactory;
        }
    }
}
