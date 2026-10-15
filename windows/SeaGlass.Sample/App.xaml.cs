using Microsoft.UI.Xaml;

namespace SeaGlass.Sample;

public partial class App : Application
{
    public App()
    {
        this.InitializeComponent();
    }

    protected override void OnLaunched(LaunchActivatedEventArgs args)
    {
        MainWindow window = new();
        window.Activate();
    }
}
