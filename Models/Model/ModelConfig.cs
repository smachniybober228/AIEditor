using AIEditor.Models.Parameters;
using static TorchSharp.torch;
using static TorchSharp.torch.nn;

namespace AIEditor.Models.Model
{
    public class ModelConfig
    {
        public ModelType Type { get; }
        public List<string> ContentLayers { get; }
        public List<string> StyleLayers { get; }
        public List<double> StyleLayerWeights { get; }
        public Func<Tensor, Module<Tensor, Tensor>, (List<Tensor>, List<string>)> FeatureExtractor { get; }
        public Func<Device, Task<Module<Tensor, Tensor>>> ModelFactory { get; }

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
