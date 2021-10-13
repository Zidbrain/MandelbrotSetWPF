using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;

namespace MandelbrotSetWPF;

/// <summary>
/// Interaction logic for ColorPicker.xaml
/// </summary>
public partial class ColorPicker : UserControl
{
    public static readonly DependencyProperty ColorProperty = DependencyProperty.Register("Color", typeof(Color), typeof(ColorPicker), new PropertyMetadata(UpdateColorDependecies));
    public Color Color
    {
        get => (Color)GetValue(ColorProperty);
        set
        {
            if (value == Color)
                return;

            SetValue(ColorProperty, value);
        }
    }

    private static float Unlerp(float channel, float white, float black)
    {
        if (white == 1f || black == 1f)
            return channel;

        return (channel - white + black * white) / (1f - white - black + white * black);
    }

    private void UpdateColorDependecies(Color value)
    {
        A.Text = value.A.ToString();
        R.Text = value.R.ToString();
        G.Text = value.G.ToString();
        B.Text = value.B.ToString();

        var max = MathF.Max(value.R / 255f, MathF.Max(value.G / 255f, value.B / 255f));
        var min = MathF.Min(value.R / 255f, MathF.Min(value.G / 255f, value.B / 255f));
        var left = max == 0f ? 1f : 1f - min / max;
        ToneCircle.Margin = new Thickness(left * (ToneCircle.Parent as Grid).ActualWidth, (1.0f - max) * (ToneCircle.Parent as Grid).ActualHeight, 0f, 0f);

        var rgb = new float[3]
            {
                Unlerp(value.R / 255f, min, max),
                Unlerp(value.G / 255f, min, max),
                Unlerp(value.B / 255f, min, max)
            };

        float pos;
        if (rgb[0] == 1f)
            if (rgb[1] != 0f)
                pos = rgb[1] / 6f;
            else
                pos = 1f - rgb[2] / 6f;
        else if (rgb[1] == 1f)
            if (rgb[0] != 0f)
                pos = (2f - rgb[0]) / 6f;
            else
                pos = (2f + rgb[2]) / 6f;
        else
            if (rgb[0] != 0f)
            pos = (4f + rgb[0]) / 6f;
        else
            pos = (4f - rgb[1]) / 6f;

        ColorBar.Margin = new Thickness(0d, pos * (ColorBar.Parent as FrameworkElement).ActualHeight, 0d, 0d);
    }

    private static void UpdateColorDependecies(DependencyObject @object, DependencyPropertyChangedEventArgs args)
    {
        var colorPicker = @object as ColorPicker;
        colorPicker.UpdateColorDependecies((Color)args.NewValue);
    }

    public ColorPicker() => InitializeComponent();

    private void TextBox_TextChanged(object sender, TextChangedEventArgs e)
    {
        var source = e.Source as TextBox;
        switch (source.Name)
        {
            case "A":
                Color = Color.FromArgb(byte.Parse(source.Text), Color.R, Color.G, Color.B);
                break;
            case "R":
                Color = Color.FromArgb(Color.A, byte.Parse(source.Text), Color.G, Color.B);
                break;
            case "G":
                Color = Color.FromArgb(Color.A, Color.R, byte.Parse(source.Text), Color.B);
                break;
            case "B":
                Color = Color.FromArgb(Color.A, Color.R, Color.G, byte.Parse(source.Text));
                break;
            default:
                break;
        }
    }

    private void UserControl_Loaded(object sender, RoutedEventArgs e) => UpdateColorDependecies(Color);
}
