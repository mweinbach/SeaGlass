using Microsoft.UI.Xaml;

namespace SeaGlass.ConsumerSample;

public partial class App : Application
{
    public static MainWindow? MainWindowInstance { get; set; }
    private Window? _window;

    public App()
    {
        this.InitializeComponent();
    }

    protected override void OnLaunched(LaunchActivatedEventArgs args)
    {
        _window = new MainWindow();
        MainWindowInstance = (MainWindow)_window;
        _window.Activate();
    }
}
