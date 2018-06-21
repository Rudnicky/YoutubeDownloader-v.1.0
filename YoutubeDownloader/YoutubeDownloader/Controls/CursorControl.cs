using System.Windows;
using System.Windows.Input;
using YoutubeDownloader.Interfaces;

namespace YoutubeDownloader
{
    public sealed class CursorControl : ICursorControl
    {
        #region Ctor
        public CursorControl()
        {
            Arrow();
        }
        #endregion

        #region Methods
        public void Wait()
        {
            Application.Current.Dispatcher.Invoke(() =>
            {
                Mouse.OverrideCursor = Cursors.Wait;
            });
        }

        public void Arrow()
        {
            Application.Current.Dispatcher.Invoke(() =>
            {
                Mouse.OverrideCursor = Cursors.Arrow;
            });
        }

        public void Cross()
        {
            Application.Current.Dispatcher.Invoke(() =>
            {
                Mouse.OverrideCursor = Cursors.Cross;
            });
        }
        #endregion
    }
}
