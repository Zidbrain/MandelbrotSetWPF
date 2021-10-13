using System.Collections.Generic;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Windows.Markup;
using System;

namespace MandelbrotSetWPF;

public class StructRef<T> : INotifyPropertyChanged where T : struct
{
    private T _value;
    public T Value
    {
        get => _value;
        set
        {
            _value = value;
            OnPropertyChanged();
        }
    }

    public static implicit operator StructRef<T>(T value) => new() { Value = value};

    public event PropertyChangedEventHandler PropertyChanged;

    protected void OnPropertyChanged([CallerMemberName] string propertyName = null) =>
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
}

/// <summary>
/// Interaction logic for ColorSelect.xaml
/// </summary>
public partial class ColorSelect : UserControl
{
    public static readonly DependencyProperty AvailableColorsProperty = DependencyProperty.Register("AvailableColors", typeof(ObservableCollection<StructRef<Color>>), typeof(ColorSelect));
    public ObservableCollection<StructRef<Color>> AvailableColors
    {
        get => (ObservableCollection<StructRef<Color>>)GetValue(AvailableColorsProperty);
        private set => SetValue(AvailableColorsProperty, value);
    }

    public event RoutedEventHandler ExitRequested
    {
        add => AddHandler(ExitRequestedEvent, value);
        remove => RemoveHandler(ExitRequestedEvent, value);
    }

    public static readonly RoutedEvent ExitRequestedEvent = EventManager.RegisterRoutedEvent(nameof(ExitRequested), RoutingStrategy.Bubble, typeof(RoutedEventHandler), typeof(ColorSelect));
    public ColorSelect()
    {
        InitializeComponent();

        AvailableColors = new()
        {
            Colors.Black,
            Colors.Gray,
            Colors.Gray,
            Colors.White
        };
    }

    private void Add(object sender, RoutedEventArgs e)
    {
        AvailableColors.Add(Colors.White);
       // OnPropertyChanged(new DependencyPropertyChangedEventArgs(AvailableColorsProperty, AvailableColors, AvailableColors));
    }

    private void Button_Click(object sender, RoutedEventArgs e) => RaiseEvent(new RoutedEventArgs(ExitRequestedEvent));

    private void Grid_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        ColorPicker.Color = (Color)Grid.SelectedItem;
    }
}
