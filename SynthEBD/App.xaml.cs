using System.Windows;
using Autofac;

namespace SynthEBD;

/// <summary>
/// Interaction logic for App.xaml
/// </summary>
public partial class App : Application
{
    protected override void OnStartup(StartupEventArgs e)
    {
        base.OnStartup(e);
        var window = new MainWindow();
        window.Height = (System.Windows.SystemParameters.FullPrimaryScreenHeight) + SystemParameters.WindowCaptionHeight;
        window.Width = (System.Windows.SystemParameters.MaximizedPrimaryScreenWidth * 0.5);

        var builder = new ContainerBuilder();
        builder.RegisterModule<MainModule>();
        builder.RegisterInstance(window).AsSelf();
        var container = builder.Build();
        PatcherEnvironmentProvider.Instance = container.Resolve<PatcherEnvironmentProvider>();
        var mvm = container.Resolve<MainWindow_ViewModel>();
        mvm.Init();
        
        window.DataContext = mvm;
        window.Show();
    }

    private void Application_DispatcherUnhandledException(object sender, System.Windows.Threading.DispatcherUnhandledExceptionEventArgs e)
    {
        string errorMessage = "SynthEBD has crashed with the following error:" + Environment.NewLine + ExceptionLogger.GetExceptionStack(e.Exception, "") + Environment.NewLine + Environment.NewLine;
        errorMessage += "Patcher Settings Creation Log:" + Environment.NewLine + PatcherSettingsProvider.SettingsLog + Environment.NewLine + Environment.NewLine;
        errorMessage += "Patcher Environment Creation Log:" + Environment.NewLine + PatcherEnvironmentProvider.Instance.EnvironmentLog;
        CustomMessageBox.DisplayNotificationOK("SynthEBD has crashed.", errorMessage);

        var path = System.IO.Path.Combine(PatcherSettings.Paths.LogFolderPath, "Crash Logs", DateTime.Now.ToString("yyyy-MM-dd-HH-mm", System.Globalization.CultureInfo.InvariantCulture) + ".txt");
        PatcherIO.WriteTextFile(path, errorMessage).Wait();

        e.Handled = true;
    }

}