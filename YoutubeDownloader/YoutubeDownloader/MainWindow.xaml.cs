using System.Windows;

namespace YoutubeDownloader
{
    public partial class MainWindow : Window
    {
        #region Ctor
        public MainWindow()
        {
            InitializeComponent();
        }
        #endregion

        #region Events
        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            this.MinWidth = this.Width;
            this.MinHeight = this.Height;
        }
        #endregion
    }
}
