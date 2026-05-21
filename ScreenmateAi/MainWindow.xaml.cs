using ChatGPTWPF.Services;
using ScreenmateAi;
using System.IO;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Threading;
using Bitmap = System.Drawing.Bitmap;
using Color = System.Drawing.Color;

namespace ChatGPTWPF
{
    public partial class MainWindow : Window
    {
        private List<Conversation> _conversations = new List<Conversation>();
        private Conversation? _currentConversation;
        private ChatStorageService _chatStorageService;
        private Conversation? _conversationToRename;
        private List<ChatMessage> _chatHistory = new List<ChatMessage>();
        private DispatcherTimer _liveTimer;

        private bool _isLiveAnalyzing = false;
        private bool _isLiveModeActive = false;

        private List<string> _liveScreenshotPaths = new List<string>();

        private const int MaxStoredScreenshots = 100;
        private const int MaxImagesToSend = 5;

        private OpenAIService _aiService;
        private ScreenCaptureService _screenService;
        public MainWindow()
        {
            InitializeComponent();
            _screenService = new ScreenCaptureService();
            _aiService = new OpenAIService();
            _liveTimer = new DispatcherTimer();
            _chatStorageService = new ChatStorageService();

            LoadConversations();
            _liveTimer.Interval = TimeSpan.FromSeconds(5);

            _liveTimer.Tick += LiveTimer_Tick;

        }

        private void LoadConversations()
        {
            _conversations = _chatStorageService.Load();

            ConversationListBox.Items.Clear();

            foreach (Conversation conversation in _conversations)
            {
                ConversationListBox.Items.Add(conversation);
            }

            if (_conversations.Count > 0)
            {
                ConversationListBox.SelectedItem = _conversations[0];
            }
        }

        private void SaveConversations()
        {
            _chatStorageService.Save(_conversations);
        }

        private void ConversationListBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (ConversationListBox.SelectedItem is Conversation selectedConversation)
            {
                
                _currentConversation = selectedConversation;

                ChatPanel.Children.Clear();

                foreach (var message in _currentConversation.Messages)
                {
                    string Sender = message.Role == "user" ? "Du" : "KI";

                    AddMessage($"{Sender}: {message.Content}");
                }
            }
        }
        private void CreateNewConversation()
        {
            Conversation conversation = new Conversation
            {
                Title = $"Neuer Chat {_conversations.Count + 1}"
            };

            _conversations.Add(conversation);

            ConversationListBox.Items.Add(conversation);

            ConversationListBox.SelectedItem = conversation;

            _currentConversation = conversation;

            ChatPanel.Children.Clear();

            AddMessage("System: Neuer Chat gestartet.");
            SaveConversations();
        }
        private void ShowSentScreenshots(List<string> imagePaths)
        {
            SentScreenshotsPanel.Children.Clear();

            foreach (string imagePath in imagePaths)
            {
                System.Windows.Controls.Image image = new System.Windows.Controls.Image
                {
                    Width = 180,
                    Height = 90,
                    Stretch = Stretch.UniformToFill
                };

                BitmapImage bitmap = new BitmapImage();

                bitmap.BeginInit();
                bitmap.UriSource = new Uri(Path.GetFullPath(imagePath));
                bitmap.CacheOption = BitmapCacheOption.OnLoad;
                bitmap.EndInit();

                image.Source = bitmap;
                image.Cursor = System.Windows.Input.Cursors.Hand;

                image.MouseLeftButtonUp += (s, e) =>
                {
                    System.Diagnostics.Process.Start(
                        new System.Diagnostics.ProcessStartInfo
                        {
                            FileName = Path.GetFullPath(imagePath),
                            UseShellExecute = true
                        });
                };
                Border border = new Border
                {
                    BorderBrush = System.Windows.Media.Brushes.DarkRed,
                    BorderThickness = new Thickness(3),
                    Margin = new Thickness(0, 0, 0, 12),
                    Child = image
                };

                SentScreenshotsPanel.Children.Add(border);
            }
        }

        private async void SendButton_Click(object sender, RoutedEventArgs e)
        {

            string userText = InputTextBox.Text;

            if (_currentConversation == null)
            {
                CreateNewConversation();
            }

            if (!string.IsNullOrWhiteSpace(userText))
            {
                AddMessage("Du: " + userText);

                InputTextBox.Clear();

                List<string> relevantImages;

                if (_isLiveModeActive)
                {
                    relevantImages = SelectRelevantScreenshots();

                }
                else
                {
                    string imagePath = _screenService.CaptureScreen();

                    relevantImages = new List<string>
            {
                imagePath
            };
                }
                ShowSentScreenshots(relevantImages);
                if (_currentConversation != null)
                {
                    _currentConversation.Messages.Add(new ChatMessage
                    {
                        Role = "user",
                        Content = userText
                    });

                    var response =
                        await _aiService.SendMessageWithImagesAndHistoryAsync(
                            _currentConversation.Messages,
                            relevantImages
                        );

                    if (_currentConversation.Title.StartsWith("Neuer Chat"))
                    {
                        _currentConversation.Title =
                            userText.Length > 25
                                ? userText.Substring(0, 25) + "..."
                                : userText;

                        ConversationListBox.Items.Refresh();
                    }

                    AddMessage("KI: " + response);

                    _currentConversation.Messages.Add(new ChatMessage
                    {
                        Role = "assistant",
                        Content = response
                    });
                    SaveConversations();
                }
            }
        }

        public void AddMessage(string text)
        {
            TextBlock message = new TextBlock
            {
                Text = text,
                Margin = new Thickness(5),
                TextWrapping = TextWrapping.Wrap
            };

            ChatPanel.Children.Add(message);
        }

        private void LiveToggleButton_Click(object sender, RoutedEventArgs e)
        {

            if (!_isLiveModeActive)
            {
                _isLiveModeActive = true;
                _liveTimer.Start();

                LiveToggleButton.Content = "Live Stop";
                AddMessage("System: Live-Modus gestartet.");
            }
            else
            {
                _isLiveModeActive = false;
                _liveTimer.Stop();

                LiveToggleButton.Content = "Live Start";
                AddMessage("System: Live-Modus gestoppt.");
            }
        }
       
        private async void LiveTimer_Tick(object? sender, EventArgs e)
        {
            string imagePath = _screenService.CaptureScreen();

            _liveScreenshotPaths.Add(imagePath);

            if (_liveScreenshotPaths.Count > MaxStoredScreenshots)
            {
                File.Delete(_liveScreenshotPaths[0]);

                _liveScreenshotPaths.RemoveAt(0);
            }

            
        }

        private List<string> SelectRelevantScreenshots()
        {
            if (_liveScreenshotPaths.Count <= MaxImagesToSend)
            {
                return _liveScreenshotPaths.ToList();
            }

            List<(string Path, double Difference)> scoredImages = new List<(string, double)>();

            scoredImages.Add((_liveScreenshotPaths[0], double.MaxValue));

            for (int i = 1; i < _liveScreenshotPaths.Count; i++)
            {
                double difference = CalculateImageDifference(
                    _liveScreenshotPaths[i - 1],
                    _liveScreenshotPaths[i]
                );

                scoredImages.Add((_liveScreenshotPaths[i], difference));
            }

            List<string> selected = scoredImages
                .OrderByDescending(x => x.Difference)
                .Take(MaxImagesToSend - 1)
                .Select(x => x.Path)
                .ToList();

            string lastImage = _liveScreenshotPaths[^1];

            selected = selected
            .Where(x => x != lastImage)
            .Distinct()
            .Take(MaxImagesToSend - 1)
            .ToList();

            selected.Add(lastImage);

            return selected;
        }

        private double CalculateImageDifference(string firstImagePath, string secondImagePath)
        {
            using Bitmap firstOriginal = new Bitmap(firstImagePath);
            using Bitmap secondOriginal = new Bitmap(secondImagePath);

            using Bitmap firstSmall = new Bitmap(firstOriginal, new System.Drawing.Size(32, 18));
            using Bitmap secondSmall = new Bitmap(secondOriginal, new System.Drawing.Size(32, 18));

            double difference = 0;

            for (int x = 0; x < firstSmall.Width; x++)
            {
                for (int y = 0; y < firstSmall.Height; y++)
                {
                    Color firstPixel = firstSmall.GetPixel(x, y);
                    Color secondPixel = secondSmall.GetPixel(x, y);

                    difference += Math.Abs(firstPixel.R - secondPixel.R);
                    difference += Math.Abs(firstPixel.G - secondPixel.G);
                    difference += Math.Abs(firstPixel.B - secondPixel.B);
                }
            }

            return difference;
        }

        private void NewChatButton_Click(object sender, RoutedEventArgs e)
        {
            CreateNewConversation();
        }

        private void RenameConversation_Click(object sender, RoutedEventArgs e)
        {
            if (ConversationListBox.SelectedItem is Conversation conversation)
            {
                conversation.IsRenaming = true;

                ConversationListBox.Items.Refresh();
            }
        }
        private void RenameInlineTextBox_KeyDown(object sender,System.Windows.Input.KeyEventArgs e)
        {
            if (e.Key == System.Windows.Input.Key.Enter)
            {
                if (sender is System.Windows.Controls.TextBox textBox &&
                    ConversationListBox.SelectedItem is Conversation conversation)
                {
                    string newTitle = textBox.Text.Trim();

                    if (!string.IsNullOrWhiteSpace(newTitle))
                    {
                        conversation.Title = newTitle;
                    }

                    conversation.IsRenaming = false;

                    ConversationListBox.Items.Refresh();

                    SaveConversations();
                }
            }
        }

        private void DeleteConversation_Click(object sender, RoutedEventArgs e)
        {
            if (ConversationListBox.SelectedItem is Conversation conversation)
            {
                _conversations.Remove(conversation);

                ConversationListBox.Items.Remove(conversation);
                ChatPanel.Children.Clear();

                SaveConversations();
            }
        }
    }
}