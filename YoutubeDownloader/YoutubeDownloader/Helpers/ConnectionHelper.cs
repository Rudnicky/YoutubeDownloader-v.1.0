using System;
using System.Net;
using System.Net.NetworkInformation;
using YoutubeDownloader.Interfaces;

namespace YoutubeDownloader.Helpers
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
