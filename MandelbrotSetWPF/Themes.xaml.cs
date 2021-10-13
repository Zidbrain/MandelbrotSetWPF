using System.Windows;
using System.Windows.Input;
using System.Windows.Controls;

namespace MandelbrotSetWPF;

public partial class Themes : ResourceDictionary
{
    public Themes()
    {
        InitializeComponent();
    }

    private void NumberInput(object sender, TextCompositionEventArgs e)
    {
        if (e.Text[0] is < '0' or > '9')
            e.Handled = true;
    }

}
