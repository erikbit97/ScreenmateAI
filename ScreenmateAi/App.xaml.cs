using ChatGPTWPF;
using System.IO;
using System.Windows;

namespace ScreenmateAi
{
    public partial class App : System.Windows.Application
    {
        protected override void OnStartup(StartupEventArgs e)
        {
            base.OnStartup(e);

            string apiKeyPath = "apikey.txt";

            Window startWindow;

            if (File.Exists(apiKeyPath))
            {
                string apiKey = File.ReadAllText(apiKeyPath);

                if (!string.IsNullOrWhiteSpace(apiKey))
                {
                    startWindow = new MainWindow();
                }
                else
                {
                    startWindow = new Anmelden();
                }
            }
            else
            {
                startWindow = new Anmelden();
            }

            startWindow.Show();
        }
    }
}
