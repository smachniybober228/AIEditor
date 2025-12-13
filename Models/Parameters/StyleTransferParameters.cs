using System.Text.Json.Serialization;

namespace AIEditor.Models.Parameters
{
    public enum OptimizerType
    {
        RMSProp,
        Adam,
        Adagrad
    }

    public enum ModelType
    {
        VGG19,
        AlexNet,
        ResNet18
    }
    public class StyleTransferParameters
    {
        [JsonConstructor]
        public StyleTransferParameters(ModelType model, int numEpochs, int styleScaling, double learningRate, OptimizerType optimizer)
        {
            Model = model;
            NumEpochs = numEpochs;
            StyleScaling = styleScaling;
            LearningRate = learningRate;
            Optimizer = optimizer;
        }

        public ModelType Model { get; } = ModelType.VGG19;
        public int NumEpochs { get; } = 1500;
        public int StyleScaling { get; } = 100000;

        public double LearningRate { get; } = 0.005;
        public OptimizerType Optimizer { get; } = OptimizerType.RMSProp;

        public StyleTransferParameters() { }
    }
}
