using System.Windows;
using SdrRadioWpf.ViewModels;

namespace SdrRadioWpf;

public partial class MainWindow : Window
{
    public MainWindow()
    {
        InitializeComponent();
    }

    private void Window_Closing(object sender, System.ComponentModel.CancelEventArgs e)
    {
        if (DataContext is MainViewModel vm)
            vm.Dispose();
    }
}
