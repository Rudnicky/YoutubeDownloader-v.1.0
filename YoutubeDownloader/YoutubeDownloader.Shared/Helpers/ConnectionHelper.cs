using System.Net;
using YoutubeDownloader.Shared.Interfaces;

namespace YoutubeDownloader.Shared.Helpers
{
    public sealed class ConnectionHelper : IConnectionHelper
    {
        #region Ctor
        public ConnectionHelper()
        {

        }
        #endregion

        #region Methods
        public bool CheckForInternetConnection()
        {
            try
            {
                using (var client = new WebClient())
                {
                    using (client.OpenRead("http://clients3.google.com/generate_204"))
                    {
                        return true;
                    }
                }
            }
            catch
            {
                return false;
            }
        }
        #endregion
    }
}
