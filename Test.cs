using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using TorchSharp;
using TorchSharp.Modules;
using static TorchSharp.torch;
using static TorchSharp.torchvision;

namespace AIEditor
{
    public static class Test
    {
        public static void Do()
        {
            var net = models.vgg19();

            Parameter a = new Parameter(torch.rand(10));
            RMSProp optim = new RMSProp([a], lr: 0.005);
            Parameter b = new Parameter(torch.rand(10));
            float[] array = a.data<float>().ToArray();
            string text = "Parameter a:\n";
            foreach (float x in array)
            {
                text += x + "\t";
            }
            text += "\nAfter grad:\n";

            var (aMaps, _a) = Backend.GetFeatureMaps(a, net);
            var (bMaps, _b) = Backend.GetFeatureMaps(b, net);

            Tensor loss = torch.mean((aMaps[0] - bMaps[0]).pow(2));

            optim.zero_grad();
            loss.backward();
            optim.step();
            //text += "Tensor c:\n";

            array = a.data<float>().ToArray();
            foreach (float x in array)
            {
                text += x + "\t";
            }
            text += "\n";
            text += a.grad is not null;
            MessageBox.Show(text);
        }
    }
}
