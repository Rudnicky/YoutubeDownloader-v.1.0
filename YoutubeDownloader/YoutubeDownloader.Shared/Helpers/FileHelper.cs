﻿using System;
using System.Diagnostics;
using System.IO;
using YoutubeDownloader.Shared.Interfaces;
using YoutubeDownloader.Shared.Utilities;

namespace YoutubeDownloader.Shared.Helpers
{
    public sealed class FileHelper : IFileHelper
    {
        #region Fields & Properties
        private static readonly string _folderPath = Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments);
        private const string _youtubeLastPartString = " - YouTube";
        private bool _isHidden = false;
        public string Path = System.IO.Path.Combine(_folderPath, Consts.DefaultDirectoryName);
        public string HiddenPath = System.IO.Path.Combine(_folderPath, Consts.TemporaryDirectoryName);

        public string DefaultTrackPath { get; set; }
        public string DefaultTrackHiddenPath { get; set; }
        public string DefaultTrackName { get; set; }
        public string TmpTrackHiddenPath { get; set; }
        public string TmpTrackPath { get; set; }
        #endregion

        #region Ctor
        public FileHelper()
        {

        }
        #endregion

        #region Methods
        public void CheckIfDirectoryExists()
        {
            try
            {
                if (!Directory.Exists(Path))
                {
                    DirectoryInfo directoryInfo = Directory.CreateDirectory(Path);
                }
                CreateHiddenFolder();
            }
            catch (IOException e)
            {
                Debug.WriteLine("Exception occured: {0}", e.ToString());
            }
        }

        public void WriteToFile(string fileName, byte[] bytes, bool isHidden)
        {
            try
            {
                if (!isHidden)
                {
                    File.WriteAllBytes(Path + "\\" + fileName, bytes);
                }
                else
                {
                    File.WriteAllBytes(HiddenPath + "\\" + fileName, bytes);
                }
            }
            catch (Exception e)
            {
                Debug.WriteLine("Exception occured: {0}", e.ToString());
            }
        }

        public void RemoveFile(string path)
        {
            try
            {
                File.Delete(path);
            }
            catch (IOException e)
            {

            }
        }

        public void RemoveContent(string path)
        {
            try
            {
                DirectoryInfo directoryInfo = new DirectoryInfo(path);

                foreach (FileInfo file in directoryInfo.GetFiles())
                {
                    file.Delete();
                }
            }
            catch (IOException e)
            {

            }
        }

        public void RenameFile(string oldNamePath, string newNamePath, string extension)
        {
            try
            {
                var newPath = CheckVideoFormat(newNamePath, extension);
                if (newPath.Contains(_youtubeLastPartString))
                {
                    File.Move(oldNamePath, newPath.Replace(_youtubeLastPartString, string.Empty));
                }
                else
                {
                    File.Move(oldNamePath, newPath);
                }
            }
            catch (IOException e)
            {

            }
        }

        public void CreateHiddenFolder()
        {
            try
            {
                if (!Directory.Exists(HiddenPath))
                {
                    DirectoryInfo directoryInfo = Directory.CreateDirectory(HiddenPath);
                    directoryInfo.Attributes = FileAttributes.Directory | FileAttributes.Hidden;
                }
                else
                {
                    CleanUpHiddenFolder();
                }
            }
            catch (Exception e)
            {
                Debug.WriteLine("Exception occured: {0}", e.ToString());
            }
        }

        public void CleanUpHiddenFolder()
        {
            DirectoryInfo directoryInfo = new DirectoryInfo(HiddenPath);
            foreach (var file in directoryInfo.GetFiles())
            {
                file.Delete();
            }
        }

        public bool CheckPossibleDuplicate(string fileName)
        {
            if (!_isHidden)
            {
                var firstReplace = fileName.Replace(".mp4", ".mp3");
                var finalReplace = firstReplace.Replace(_youtubeLastPartString, string.Empty);
                return File.Exists(System.IO.Path.Combine(Path, finalReplace));
            }
            else
            {
                return File.Exists(System.IO.Path.Combine(HiddenPath, fileName));
            }
        }

        public string CheckVideoFormat(string path, string extension)
        {
            if (path.Contains(".webm"))
            {
                return path.Replace(".webm", "." + extension)
                    .Replace(Consts.TemporaryDirectoryName, Consts.DefaultDirectoryName);
            }
            else if (path.Contains(".mp4"))
            {
                return path.Replace(".mp4", "." + extension)
                    .Replace(Consts.TemporaryDirectoryName, Consts.DefaultDirectoryName);
            }
            return string.Empty;
        }

        public string PreparePathForFFmpeg(string path)
        {
            return path.Replace(" ", string.Empty);
        }

        public string PrepareTrackForNotification(string trackName)
        {
            if (trackName.Contains(".mp3"))
            {
                return trackName.Replace(".mp3", string.Empty);
            }
            else if (trackName.Contains(".webm"))
            {
                return trackName.Replace(".webm", string.Empty);
            }
            else if (trackName.Contains(".mp4"))
            {
                return trackName.Replace(".mp4", string.Empty);
            }
            else if (trackName.Contains("- YouTube"))
            {
                return trackName.Replace("- YouTube", string.Empty);
            }
            return trackName;
        }
        #endregion
    }
}
