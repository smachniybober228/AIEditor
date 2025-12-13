using System.Windows;

namespace AIEditor.Windows
{
    /// <summary>
    /// Логика взаимодействия для CustomMessageBox.xaml
    /// </summary>
    public partial class CustomMessageBox : Window
    {
        public MessageBoxResult Result { get; private set; }

        public CustomMessageBox()
        {
            InitializeComponent();
        }

        // Статический метод для показа (как MessageBox.Show)
        public static MessageBoxResult Show(
            string message,
            string caption = "",
            Window owner = null)
        {
            var dialog = new CustomMessageBox();

            if (owner != null)
            {
                dialog.Owner = owner;
                dialog.WindowStartupLocation = WindowStartupLocation.CenterOwner;
            }

            dialog.Setup(message, caption);
            dialog.ShowDialog();

            return dialog.Result;
        }

        private void Setup(string message, string caption)
        {
            MessageText.Text = message;
            TitleText.Text = string.IsNullOrEmpty(caption) ? "Сообщение" : caption;
        }

        private void Button1_Click(object sender, RoutedEventArgs e)
        {
            Result = MessageBoxResult.OK;
            Close();
        }
    }
}
