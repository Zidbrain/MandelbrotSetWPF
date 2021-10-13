using Microsoft.Win32;
using System.IO;
using System.Numerics;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Media.Imaging;
using static System.Math;

namespace MandelbrotSetWPF;

/// <summary>
/// Interaction logic for MainWindow.xaml
/// </summary>
public partial class MainWindow : Window
{
    public static readonly DependencyProperty ImageProperty = DependencyProperty.Register("Image", typeof(WriteableBitmap), typeof(MainWindow));
    public WriteableBitmap Image
    {
        get => GetValue(ImageProperty) as WriteableBitmap;
        private set => SetValue(ImageProperty, value);
    }

    public static readonly DependencyProperty IterationsProperty = DependencyProperty.Register("Iterations", typeof(int), typeof(MainWindow), new PropertyMetadata(
        static (d, e) =>
        {
            var window = d as MainWindow;
            if (window.LinkIterationsPalette.IsChecked ?? false && (int)e.NewValue != window.PaletteLength)
                window.PaletteLength = (int)e.NewValue;
        }));

    public int Iterations
    {
        get => (int)GetValue(IterationsProperty);
        set => SetValue(IterationsProperty, value);
    }

    public static readonly DependencyProperty PaletteLengthProperty = DependencyProperty.Register("PaletteLength", typeof(int), typeof(MainWindow), new PropertyMetadata(
        static (d, e) =>
        {
            var window = d as MainWindow;
            if (window.LinkIterationsPalette.IsChecked ?? false && (int)e.NewValue != window.Iterations)
                window.Iterations = (int)e.NewValue;
        }));

    public int PaletteLength
    {
        get => (int)GetValue(PaletteLengthProperty);
        set => SetValue(PaletteLengthProperty, value);
    }

    private static double Lerp(double a, double b, double value) =>
        a + (b - a) * value;

    private CancellationTokenSource _imageGenerationToken = new();
    private int _percentage = -1;

    private async void GenerateImage()
    {
        var pixels = new uint[Image.PixelWidth * Image.PixelHeight];

        static uint GetColor(in Complex c, int maxIterations)
        {
            var z = new Complex(0, 0);

            double iterations = 0;
            var i_iterations = 0;
            var sqrMagniture = 0d;

            for (var i = 0; i < maxIterations; i++)
            {
                iterations = i;
                i_iterations = i;
                z = z * z + c;
                sqrMagniture = z.Magnitude * z.Magnitude;
                if (sqrMagniture > 1 << 16)
                    break;
            }

            if (iterations == maxIterations - 1)
                return uint.MaxValue;

            if (iterations < maxIterations - 1)
                iterations += 2 - Log(Log(sqrMagniture, 2), 2);

            var color1 = (double)i_iterations / maxIterations;
            var color2 = (double)(i_iterations + 1) / maxIterations;

            var lerp = (uint)(Lerp(color1, color2, iterations % 1) * 255d);

            return lerp << 16 | lerp << 8 | lerp;
        }

        (var width, var height) = (Image.PixelWidth, Image.PixelHeight);
        var maxIterations = Iterations;
        var length = Image.PixelWidth * Image.PixelHeight;

        _imageGenerationToken.Cancel();
        _imageGenerationToken.Dispose();

        _imageGenerationToken = new CancellationTokenSource();

        var token = _imageGenerationToken.Token;

        ProgressText.Visibility = Visibility.Visible;
        Checkmark.Visibility = Visibility.Collapsed;

        await Task.Run(() =>
        {
            for (var pass = 0; pass < 4; pass++)
            {
                var completed = 0;

                _ = Parallel.For(0, length / 4, (i, state) =>
                {
                    if (token.IsCancellationRequested)
                        state.Break();

                    var index = i * 4 + pass;
                    var position = new Complex((double)(index % width) / width * _size.x + _position.x, (double)(index / width) / height * -_size.y + _position.y);
                    var color = GetColor(position, maxIterations);

                    for (var j = index; j < i * 4 + 4 && j < length; j++)
                        pixels[j] = color;

                    completed++;

                    var completeness = (int)((completed + 1f) / length * 4f * 100f);
                    if (completeness != _percentage)
                    {
                        _percentage = completeness;
                        _ = Dispatcher.Invoke(static void (MainWindow window, int pass) => window.ProgressText.Text = $@"Pass {pass + 1}/4 - {window._percentage}%", this, pass);
                    }
                });

                if (token.IsCancellationRequested)
                    break;

                Dispatcher.Invoke(() =>
                {
                    Image.WritePixels(new Int32Rect(0, 0, Image.PixelWidth, Image.PixelHeight), pixels, 4 * Image.PixelWidth, 0);
                });
            }
        }, _imageGenerationToken.Token);

        ProgressText.Visibility = Visibility.Collapsed;
        Checkmark.Visibility = Visibility.Visible;
    }

    public MainWindow()
    {
        InitializeComponent();

        Iterations = 100;
    }

    private void StackPanel_MouseDown(object sender, MouseButtonEventArgs e) => Window.DragMove();

    private void Button_Click(object sender, RoutedEventArgs e) => Close();

    private void Button_Click_1(object sender, RoutedEventArgs e) =>
        WindowState = WindowState is WindowState.Normal ? WindowState.Maximized : WindowState.Normal;

    private void Button_Click_2(object sender, RoutedEventArgs e) =>
        WindowState = WindowState.Minimized;

    private void Window_Loaded(object sender, RoutedEventArgs e)
    {
        Image = new WriteableBitmap(1920, 1080, 96, 96, PixelFormats.Bgr32, null);
        GenerateImage();
    }

    private void AddIterations(object sender, RoutedEventArgs e)
    {
        if (Keyboard.IsKeyDown(Key.LeftShift))
            Iterations *= 2;
        else
            Iterations++;
    }

    private void LinkIterationsPalette_Checked(object sender, RoutedEventArgs e) => SetValue(PaletteLengthProperty, Iterations);

    private (double x, double y) _size = (4d, 2.25d);
    private (double x, double y) _position = (-2.5d, 1.5d);

    private bool _imageMove;
    private System.Windows.Vector _imageMoveStart;

    private void Image_MouseMove(object sender, MouseEventArgs e)
    {
        if (_imageMove)
        {
            var offset = e.GetPosition(this) - _imageMoveStart;
            Canvas.SetLeft(DisplayImage, offset.X);
            Canvas.SetTop(DisplayImage, offset.Y);
            e.Handled = true;
        }
        else
        {
            var position = e.GetPosition(DisplayImage);
            PositionText.Text = $"X: {position.X / DisplayImage.ActualWidth * _size.x + _position.x:F5} Y: {position.Y / DisplayImage.ActualHeight * -_size.y + _position.y:F5}";
        }
    }

    private void DisplayImage_MouseDown(object sender, MouseButtonEventArgs e)
    {
        _imageMoveStart = e.GetPosition(this) - new Point(Canvas.GetLeft(DisplayImage), Canvas.GetTop(DisplayImage));
        _imageMove = true;
        e.Handled = true;
    }

    private void DisplayImage_MouseUp(object sender, MouseButtonEventArgs e) =>
        _imageMove = false;

    private void Canvas_MouseWheel(object sender, MouseWheelEventArgs e)
    {
        var factor = 1 + e.Delta / 120d * 0.1d;
        var position = e.GetPosition(Canvas);
        var offset = new Point(Canvas.GetLeft(DisplayImage), Canvas.GetTop(DisplayImage));
        offset = (offset - position) * factor + position;

        Canvas.SetLeft(DisplayImage, offset.X);
        Canvas.SetTop(DisplayImage, offset.Y);

        DisplayImage.MaxWidth *= factor;
        DisplayImage.MaxHeight *= factor;

        e.Handled = true;
    }

    private bool _collapsed;
    private void Button_Click_3(object sender, RoutedEventArgs e)
    {
        var to = _collapsed switch
        {
            true => new Thickness(0d, 0d, 0d, 0d),
            false => new Thickness(0d, 0d, -CollapseableColumn.ActualWidth, 0d)
        };

        var storyboard = new Storyboard();
        storyboard.Children.Add(new ThicknessAnimation(to, new Duration(System.TimeSpan.FromSeconds(0.25d))));
        Storyboard.SetTarget(storyboard, CollapseableColumn);
        Storyboard.SetTargetProperty(storyboard, new PropertyPath("Margin"));
        BeginStoryboard(storyboard);

        _collapsed = !_collapsed;

        var button = sender as Button;
        button.RenderTransform = new RotateTransform(_collapsed ? 180f : 0f);
    }

    private void Update(object sender, RoutedEventArgs e)
    {
        var newpos = (-Canvas.GetLeft(DisplayImage) / DisplayImage.ActualWidth * _size.x + _position.x, Canvas.GetTop(DisplayImage) / DisplayImage.ActualHeight * _size.y + _position.y);
        var newsize = ((-Canvas.GetLeft(DisplayImage) + Canvas.ActualWidth) / DisplayImage.ActualWidth * _size.x + _position.x, (Canvas.GetTop(DisplayImage) - Canvas.ActualHeight) / DisplayImage.ActualHeight * _size.y + _position.y);
        newsize = (newsize.Item1 - newpos.Item1, -newsize.Item2 + newpos.Item2);
        (_position, _size) = (newpos, newsize);

        Canvas.SetLeft(DisplayImage, 0);
        Canvas.SetTop(DisplayImage, 0);
        DisplayImage.MaxHeight = Canvas.ActualHeight;
        DisplayImage.MaxWidth = Canvas.ActualWidth;
        GenerateImage();
    }

    private void Reset(object sender, RoutedEventArgs e)
    {
        _size = (4d, 2.25d);
        _position = (-2.5d, 1.5d);

        Canvas.SetLeft(DisplayImage, 0);
        Canvas.SetTop(DisplayImage, 0);
        DisplayImage.MaxHeight = Canvas.ActualHeight;
        DisplayImage.MaxWidth = Canvas.ActualWidth;

        GenerateImage();
    }

    private void Save(object sender, RoutedEventArgs e)
    {
        var dialog = new SaveFileDialog
        {
            FileName = "MandelbrotSet",
            Filter = "PNG Images (.png)|*.png",
            DefaultExt = ".png"
        };
        if (dialog.ShowDialog().GetValueOrDefault())
        {
            var encoder = new PngBitmapEncoder();
            encoder.Frames.Add(BitmapFrame.Create(Image));

            using var stream = new FileStream(dialog.FileName, FileMode.Create);
            encoder.Save(stream);
        }
    }

    private void Button_Click_4(object sender, RoutedEventArgs e)
    {
        if (Keyboard.IsKeyDown(Key.LeftShift))
            PaletteLength *= 2;
        else
            PaletteLength++;
    }

    private void Button_Click_5(object sender, RoutedEventArgs e) => CollapseableColumn.Content = Resources["ColorSelect"];

    private void ColorSelect_ExitRequested(object sender, RoutedEventArgs e) => CollapseableColumn.Content = Settings;
}

