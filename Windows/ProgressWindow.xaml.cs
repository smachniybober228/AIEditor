using System.Windows;

namespace AIEditor.Windows
{
    public partial class ProgressWindow : Window
    {
        public ProgressWindow()
        {
            InitializeComponent();
        }

        // Обновление прогресса
        public void UpdateProgress(int current, int total, string message = null)
        {
            Dispatcher.Invoke(() =>
            {
                double percentage = (double)current / total * 100;
                ProgressBarControl.Value = percentage;

                if (!string.IsNullOrEmpty(message))
                    ProgressText.Text = message;
                else
                    ProgressText.Text = $"Обработка: {current}/{total} эпох";
            });
        }

        // Завершение
        public void Complete(string message = "Обработка завершена!")
        {
            Dispatcher.Invoke(() =>
            {
                ProgressBarControl.Value = 100;
                ProgressText.Text = message;

                // Автоматическое закрытие через 1 секунду
                Task.Delay(1000).ContinueWith(_ =>
                {
                    Dispatcher.Invoke(Close);
                });
            });
        }
    }
}
