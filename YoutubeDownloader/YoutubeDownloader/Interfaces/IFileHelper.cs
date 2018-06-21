namespace YoutubeDownloader.Interfaces
{
    interface IFileHelper
    {
        void CheckIfDirectoryExists();

        void WriteToFile(string fileName, byte[] bytes, bool isHidden);

        void RemoveFile(string path);

        void RemoveContent(string path);

        void RenameFile(string oldNamePath, string newNamePath);

        void CreateHiddenFolder();

        bool CheckPossibleDuplicate(string fileName);

        string CheckVideoFormat(string path);

        string PreparePathForFFmpeg(string path);

        string PrepareTrackForNotification(string trackName);
    }
}
