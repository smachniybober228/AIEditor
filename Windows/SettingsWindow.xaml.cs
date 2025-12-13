using AIEditor.Models.Parameters;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;

namespace AIEditor.Windows
{

    /// <summary>
    /// Логика взаимодействия для SettingsWindow.xaml
    /// </summary>
    public partial class SettingsWindow : Window
    {
        private void RefreshInput(StyleTransferParameters settings)
        {
            InputModel.SelectedItem = settings.Model;
            InputEpochs.Text = settings.NumEpochs.ToString();
            InputStyleScaling.Text = settings.StyleScaling.ToString();
            InputLR.Text = settings.LearningRate.ToString();
            InputOptimizer.SelectedItem = settings.Optimizer;
        }

        private bool CheckInput() // true если проблем нет
        {
            return int.TryParse(InputEpochs.Text, out int e) && e != 0 &&
                   int.TryParse(InputStyleScaling.Text, out int s) && s != 0 &&
                   double.TryParse(InputLR.Text, out double l) && l != 0;
        }
        public SettingsWindow()
        {
            InitializeComponent();

            RefreshInput(ParametersManager.LoadSettings());
        }

        private void BackToMainWindow(object sender, RoutedEventArgs e)
        {
            DialogResult = false;
        }

        private void SaveSettings(object sender, RoutedEventArgs e)
        {
            if (!CheckInput())
                CustomMessageBox.Show("Неправильно введены значения!");
            else
            {
                StyleTransferParameters settings = new StyleTransferParameters((ModelType)InputModel.SelectedItem,
                                                                                int.Parse(InputEpochs.Text),
                                                                                int.Parse(InputStyleScaling.Text),
                                                                                double.Parse(InputLR.Text),
                                                                                (OptimizerType)InputOptimizer.SelectedItem);
                ParametersManager.SaveSettings(settings);
                DialogResult = true;
            }
        }

        private void DefaultSettings(object sender, RoutedEventArgs e)
        {
            StyleTransferParameters defaultSettings = new();
            RefreshInput(defaultSettings);
        }

        private void InputInt_KeyDown(object sender, KeyEventArgs e)
        {
            TextBox textBox = sender as TextBox;

            bool isDigit = (e.Key >= Key.D0 && e.Key <= Key.D9) ||
                           (e.Key >= Key.NumPad0 && e.Key <= Key.NumPad9);
            bool isControl = e.Key == Key.Back || e.Key == Key.Delete ||
                                 e.Key == Key.Left || e.Key == Key.Right ||
                                 e.Key == Key.Tab || e.Key == Key.Enter;

            if (!(isDigit || isControl))
            {
                e.Handled = true;
            }
        }

        private void InputDouble_KeyDown(object sender, KeyEventArgs e)
        {
            bool isDigit = (e.Key >= Key.D0 && e.Key <= Key.D9) ||
               (e.Key >= Key.NumPad0 && e.Key <= Key.NumPad9);
            bool isControl = e.Key == Key.Back || e.Key == Key.Delete ||
                             e.Key == Key.Left || e.Key == Key.Right ||
                             e.Key == Key.Tab || e.Key == Key.Enter;
            bool isComma = e.Key == Key.OemComma;

            if (!(isDigit || isControl || isComma))
            {
                e.Handled = true;
            }
        }

        private void InputDouble_TextChanged(object sender, TextChangedEventArgs e)
        {
            var textBox = (TextBox)sender;

            if (double.TryParse(textBox.Text, out double value) && value != 0)
            {
                textBox.Background = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#010133"));
            }
            else
            {
                textBox.Background = Brushes.LightPink;
            }
        }

        private void InputInt_TextChanged(object sender, TextChangedEventArgs e)
        {
            TextBox textBox = (TextBox)sender;

            if (int.TryParse(textBox.Text, out int value) && value != 0)
            {
                textBox.Background = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#010133"));
            }
            else
            {
                textBox.Background = Brushes.LightPink;
            }
        }
    }
}
