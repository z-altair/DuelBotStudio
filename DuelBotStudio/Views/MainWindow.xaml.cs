using System.Windows;
using Microsoft.Win32;
using System.IO;
using System.Windows.Controls;
using System.Windows.Media.Imaging;
using DuelBotStudio.Services;

namespace DuelBotStudio
{
    public partial class MainWindow : Window
    {
        private readonly YdkParserService _ydkParserService;
        public MainWindow()
        {
            InitializeComponent();
        }

        private void onButtonClick(object sender, RoutedEventArgs e)
        {
            OpenFileDialog dialog = new OpenFileDialog
            {
                Title = "Select your deck.",
                Filter = "Deck files (*.ydk)|*.ydk",
            };

            if (dialog.ShowDialog() != true)
            {
                return;
            }

            var cardIds = _ydkParserService.ParseFile(dialog.FileName);
            CardPanel.Children.Clear();

            foreach(int id in cardIds)
            {
                Image card = new Image
                {
                    Width = 120,
                    Height = 175,
                    Margin = new Thickness(5),
                    Stretch = System.Windows.Media.Stretch.Uniform,
                    Source = new BitmapImage(
                        new Uri($"https://images.ygoprodeck.com/images/cards/{id}.jpg"))
                };

                CardPanel.Children.Add(card);
            }
        }
    }
}