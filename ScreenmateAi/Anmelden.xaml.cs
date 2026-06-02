using ChatGPTWPF;
using System;
using System.Collections.Generic;
using System.IO;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;



namespace ScreenmateAi
{
    /// <summary>
    /// Interaktionslogik für Anmelden.xaml
    /// </summary>
    public partial class Anmelden : Window
    {
        public Anmelden()
        {
            InitializeComponent();
        }
        private async Task<bool> TestApiKeyAsync(string apiKey)
        {
            try
            {
                using HttpClient httpClient = new HttpClient();

                httpClient.DefaultRequestHeaders.Authorization =
                    new AuthenticationHeaderValue("Bearer", apiKey);

                var requestBody = new
                {
                    model = "gpt-4.1-mini",
                    input = "Antworte nur mit OK."
                };

                string json = JsonSerializer.Serialize(requestBody);

                using StringContent content = new StringContent(
                    json,
                    Encoding.UTF8,
                    "application/json"
                );

                using HttpResponseMessage response = await httpClient.PostAsync(
                    "https://api.openai.com/v1/responses",
                    content
                );

                return response.IsSuccessStatusCode;
            }
            catch
            {
                return false;
            }
        }
        private async void AnmeldeButton_Click(object sender, RoutedEventArgs e)
        {
            string apiKey = ApiKeyTextBox.Text.Trim();

            if (string.IsNullOrWhiteSpace(apiKey))
            {
                ErrorTextBlock.Text = "Bitte gib einen API-Key ein.";
                return;
            }

            bool isValid = await TestApiKeyAsync(apiKey);

            if (!isValid)
            {
                ErrorTextBlock.Text = "Ungültiger API-Key oder kein API-Guthaben vorhanden.";
                return;
            }

            File.WriteAllText("apikey.txt", apiKey);

            MainWindow mainWindow = new MainWindow();
            mainWindow.Show();

            Close();
        }
        private void OpenApiPage_Click(object sender, RoutedEventArgs e)
        {
            System.Diagnostics.Process.Start(
                new System.Diagnostics.ProcessStartInfo
                {
                    FileName = "https://platform.openai.com/api-keys",
                    UseShellExecute = true
                });
        }
    }
}
