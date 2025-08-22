using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Controls.Primitives;
using Microsoft.UI.Xaml.Data;
using Microsoft.UI.Xaml.Input;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Navigation;
using Microsoft.UI.Xaml.Shapes;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using Windows.Foundation;
using Windows.Foundation.Collections;
using Windows.System;

// To learn more about WinUI, the WinUI project structure,
// and more about our project templates, see: http://aka.ms/winui-project-info.

namespace WinUISnake
{
    public sealed partial class MainWindow : Window
    {
        // Grid ve kare boyutu
        private const int GridSize = 20;
        private const int TileSize = 20;

        // Hýz kontrolü
        private const int FpsStart = 6;   // daha yavaþ baþlangýç
        private const int FpsMax = 12;    // üst sýnýr
        private int fps;
        private int score;
        private bool alive;

        // Oyun durumu
        private readonly DispatcherTimer timer;
        private List<(int x, int y)> snake;
        private (int x, int y) dir;
        private (int x, int y) food;
        private readonly Random rnd = new();

        public MainWindow()
        {
            InitializeComponent();

            // Tek timer: her reset’te yeniden yaratma yok, Tick tek sefer subscribe
            timer = new DispatcherTimer();
            timer.Tick += Tick;

            ResetGame();
        }

        // Pencere yüklenince odaðý köke ver (klavye için)
        private void Root_Loaded(object sender, RoutedEventArgs e)
        {
            Root.Focus(FocusState.Programmatic);
        }

        private void ResetGame()
        {
            // Timer’ý durdur, sonra konfigüre et
            timer.Stop();

            snake = new List<(int x, int y)> { (10, 10) };
            dir = (1, 0);
            food = RandFood();
            score = 0;
            fps = FpsStart;
            alive = true;

            UpdateHUD();
            SetTimerInterval();
            timer.Start();

            Draw();

            GameOverPanel.Visibility = Visibility.Collapsed;

            // Restart’tan sonra odaðý geri ver
            Root.Focus(FocusState.Programmatic);
        }

        private void SetTimerInterval()
        {
            // FPS aralýðýný clamp ederek aþýrý hýzlanmayý engelle
            int clamped = Math.Max(1, Math.Min(fps, FpsMax));
            timer.Interval = TimeSpan.FromMilliseconds(1000.0 / clamped);
            SpeedText.Text = $"Hýz: {clamped} fps";
        }

        private (int x, int y) RandFood()
        {
            while (true)
            {
                var f = (x: rnd.Next(0, GridSize), y: rnd.Next(0, GridSize));
                if (!snake.Contains(f)) return f;
            }
        }

        private void UpdateHUD()
        {
            ScoreText.Text = $"Skor: {score}";
            SpeedText.Text = $"Hýz: {Math.Min(fps, FpsMax)} fps";
        }

        private void ChangeDir(int nx, int ny)
        {
            // Ters yöne dönüþü engelle
            if (snake.Count > 1 && snake[0].x + nx == snake[1].x && snake[0].y + ny == snake[1].y) return;
            dir = (nx, ny);
        }

        // Klavye olaylarýný KÖK Grid yakalýyor
        private void Root_KeyDown(object sender, KeyRoutedEventArgs e)
        {
            if (!alive)
            {
                if (e.Key == VirtualKey.Enter && RestartButton.Visibility == Visibility.Visible)
                {
                    RestartButton_Click(RestartButton, null);
                    e.Handled = true;
                }
                return;
            }

            switch (e.Key)
            {
                case VirtualKey.Up:
                    ChangeDir(0, -1);
                    e.Handled = true;
                    break;
                case VirtualKey.Down:
                    ChangeDir(0, 1);
                    e.Handled = true;
                    break;
                case VirtualKey.Left:
                    ChangeDir(-1, 0);
                    e.Handled = true;
                    break;
                case VirtualKey.Right:
                    ChangeDir(1, 0);
                    e.Handled = true;
                    break;
            }
        }

        private void Tick(object sender, object e)
        {
            if (!alive) return;

            var head = (x: snake[0].x + dir.x, y: snake[0].y + dir.y);

            // Duvar çarpýþmasý
            if (head.x < 0 || head.x >= GridSize || head.y < 0 || head.y >= GridSize)
            {
                GameOver();
                return;
            }

            // Kendiyle çarpýþma
            if (snake.Contains(head))
            {
                GameOver();
                return;
            }

            snake.Insert(0, head);

            if (head == food)
            {
                score += 10;

                if (fps < FpsMax)
                {
                    fps++;
                    SetTimerInterval();
                }

                food = RandFood();
                UpdateHUD();
            }
            else
            {
                snake.RemoveAt(snake.Count - 1);
            }

            Draw();
        }

        private void GameOver()
        {
            alive = false;
            timer.Stop();
            GameOverPanel.Visibility = Visibility.Visible;
            RestartButton.Visibility = Visibility.Visible;
        }

        private void RestartButton_Click(object sender, RoutedEventArgs e)
        {
            ResetGame();
        }

        private void Draw()
        {
            GameCanvas.Children.Clear();

            // Izgara çizgileri
            var gridBrush = (SolidColorBrush)Root.Resources["GridLine"];
            for (int i = 1; i < GridSize; i++)
            {
                var v = new Line
                {
                    X1 = i * TileSize + 0.5,
                    Y1 = 0,
                    X2 = i * TileSize + 0.5,
                    Y2 = GridSize * TileSize,
                    Stroke = gridBrush,
                    StrokeThickness = 1
                };
                GameCanvas.Children.Add(v);

                var h = new Line
                {
                    X1 = 0,
                    Y1 = i * TileSize + 0.5,
                    X2 = GridSize * TileSize,
                    Y2 = i * TileSize + 0.5,
                    Stroke = gridBrush,
                    StrokeThickness = 1
                };
                GameCanvas.Children.Add(h);
            }

            // Yem
            var foodRect = new Rectangle
            {
                Width = TileSize,
                Height = TileSize,
                Fill = (SolidColorBrush)Root.Resources["FoodBrush"]
            };
            Canvas.SetLeft(foodRect, food.x * TileSize);
            Canvas.SetTop(foodRect, food.y * TileSize);
            GameCanvas.Children.Add(foodRect);

            // Yýlan
            var headBrush = (SolidColorBrush)Root.Resources["SnakeHeadBrush"];
            var bodyBrush = (SolidColorBrush)Root.Resources["SnakeBodyBrush"];

            for (int i = 0; i < snake.Count; i++)
            {
                var rect = new Rectangle
                {
                    Width = TileSize,
                    Height = TileSize,
                    Fill = i == 0 ? headBrush : bodyBrush
                };
                Canvas.SetLeft(rect, snake[i].x * TileSize);
                Canvas.SetTop(rect, snake[i].y * TileSize);
                GameCanvas.Children.Add(rect);
            }
        }
    }
}