using System.Windows;
using Unity;
using YoutubeDownloader.Helpers;
using YoutubeDownloader.Interfaces;

// Check IoC -> http://www.technical-recipes.com/2017/a-simple-example-of-using-dependency-injection-using-unity-in-wpf/
// Check IoC -> http://ikeptwalking.com/microsoft-unity-tutorial/

namespace YoutubeDownloader
{
    /// <summary>
    /// Interaction logic for App.xaml
    /// </summary>
    public partial class App : Application
    {
        protected override void OnStartup(StartupEventArgs e)
        {
            base.OnStartup(e);
            ConfigureContainer();
        }

        private void ConfigureContainer()
        {
            IUnityContainer Container = new UnityContainer();
            Container.RegisterType<IFileHelper, FileHelper>();
            Container.RegisterType<IConnectionHelper, ConnectionHelper>();
            Container.RegisterType<ICursorControl, CursorControl>();
            Container.RegisterType<IConverter, Converter>();

            var navigationViewModel = Container.Resolve<NavigationViewModel>();
            var mainWindow = new MainWindow() { DataContext = navigationViewModel };
            mainWindow.Show();
        }
    }
}
