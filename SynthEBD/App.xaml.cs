using System.Drawing;
using System.Windows;
using Autofac;
using Mutagen.Bethesda;
using Mutagen.Bethesda.Skyrim;
using Mutagen.Bethesda.Synthesis;

namespace SynthEBD;

/// <summary>
/// Interaction logic for App.xaml
/// </summary>
public partial class App : Application
{
    protected override void OnStartup(StartupEventArgs e)
    {
        base.OnStartup(e);

        try
        {
            SynthesisPipeline.Instance
                .AddPatch<ISkyrimMod, ISkyrimModGetter>(RunPatch)
                .SetTypicalOpen(TypicalOpen)
                .SetOpenForSettings(OpenForSettings)
                .SetForWpf()
                .Run(e.Args);
        }
        catch (Exception exception)
        {
            Console.WriteLine(exception);
            throw;
        }
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

    public static async Task RunPatch(IPatcherState<ISkyrimMod, ISkyrimModGetter> state)
    {
        // Do some other ContainerBuilder calls to only instantiate the bits needed to run the patcher:
        // Just the parts needed as if you pressed the "GO" button
        
        Console.WriteLine("Running patch");
        await Task.Delay(TimeSpan.FromSeconds(5));
        Console.WriteLine("I ran the patch?");
    }

    public static int OpenForSettings(Rectangle synthesisWindowLocation)
    {
        // Maybe open a version with no "GO" button?  Or just the normal version is fine too,
        // as long as people understand that typical usage would be to exit out after modifying
        // settings and then run their Synthesis Pipeline
        OpenNormally(synthesisWindowLocation);
        return 0;
    }

    public static async Task<int> TypicalOpen(Rectangle suggestedStartLocation)
    {
        // If the user just double clicked the exe from the desktop -> current behavior
        OpenNormally(null);
        return 0;
    }

    private static void OpenNormally(Rectangle? rectangle)
    {
        var window = new MainWindow();

        if (rectangle == null)
        {
            window.Height = (System.Windows.SystemParameters.FullPrimaryScreenHeight) + SystemParameters.WindowCaptionHeight;
            window.Width = (System.Windows.SystemParameters.MaximizedPrimaryScreenWidth * 0.5);
        }
        else
        {
            window.Left = rectangle.Value.X;
            window.Top = rectangle.Value.Y;
            window.Height = Math.Max(rectangle.Value.Height, 400);
            window.Width = Math.Max(rectangle.Value.Width, 400);
        }

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
}