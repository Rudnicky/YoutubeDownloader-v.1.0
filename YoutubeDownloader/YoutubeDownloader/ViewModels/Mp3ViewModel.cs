﻿using System;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.IO;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;
using ToastNotifications.Messages;
using VideoLibrary;
using YoutubeDownloader.Shared.Helpers;
using YoutubeDownloader.Shared.Interfaces;
using YoutubeDownloader.Shared.Models;
using YoutubeDownloader.Shared.Utilities;

namespace YoutubeDownloader
{
    class Mp3ViewModel : BaseViewModel
    {
        #region Fields and Properties
        private IConnectionHelper _connectionHelper;
        private ICursorControl _cursor;
        private IConverter _converter;
        private IFileHelper _fileHelper;
        private Process _process;
        private int _currentLine;

        private ObservableCollection<Mp3Model> _mp3List;
        public ObservableCollection<Mp3Model> Mp3List
        {
            get
            {
                return _mp3List;
            }
            set
            {
                _mp3List = value;
                OnPropertyChanged(nameof(Mp3List));
            }
        }

        private ObservableCollection<QualityModel> _qualityList;
        public ObservableCollection<QualityModel> QualityList
        {
            get
            {
                return _qualityList;
            }
            set
            {
                _qualityList = value;
                OnPropertyChanged(nameof(QualityList));
            }
        }

        private QualityModel _qualityModel;
        public QualityModel QualityModel
        {
            get
            {
                return _qualityModel;
            }
            set
            {
                _qualityModel = value;
                OnPropertyChanged(nameof(QualityModel));
            }
        }

        private ObservableCollection<FormatModel> _formatList;
        public ObservableCollection<FormatModel> FormatList
        {
            get
            {
                return _formatList;
            }
            set
            {
                _formatList = value;
                OnPropertyChanged(nameof(FormatList));
            }
        }

        private FormatModel _formatModel;
        public FormatModel FormatModel
        {
            get
            {
                return _formatModel;
            }
            set
            {
                _formatModel = value;
                OnPropertyChanged(nameof(FormatModel));
            }
        }

        private string _youtubeLinkUrl;
        public string YoutubeLinkUrl
        {
            get
            {
                return _youtubeLinkUrl;
            }
            set
            {
                _youtubeLinkUrl = value;
                OnPropertyChanged(nameof(YoutubeLinkUrl));
            }
        }

        private bool _isFocused;
        public bool IsFocused
        {
            get
            {
                return _isFocused;
            }
            set
            {
                _isFocused = value;
                OnPropertyChanged("IsYouTubeTextBoxFocused");
                if (_isFocused)
                {
                    YoutubeLinkUrl = string.Empty;
                }
                else if (!_isFocused && YoutubeLinkUrl == string.Empty)
                {
                    YoutubeLinkUrl = Consts.DefaultTextBoxEntry;
                }
            }
        }

        private bool _isWholeListChecked;
        public bool IsWholeListChecked
        {
            get
            {
                return _isWholeListChecked;
            }
            set
            {
                _isWholeListChecked = value;
                OnPropertyChanged(nameof(IsWholeListChecked));
            }
        }
        #endregion

        #region Commands
        public ICommand GoButtonCommand
        {
            get
            {
                return new RelayCommand(GoButtonClicked, CanExecute);
            }
        }
        #endregion

        #region Constructor
        public Mp3ViewModel(IConnectionHelper connectionHelper, ICursorControl cursor, IConverter converter, IFileHelper fileHelper)
        {
            this._connectionHelper = connectionHelper;
            this._cursor = cursor;
            this._converter = converter;
            this._fileHelper = fileHelper;

            Initialize();
        }
        #endregion

        #region Events
        private void GoButtonClicked()
        {
            if (ValidateEditFieldString())
            {
                if (!CheckIfFileAlreadyExists(YoutubeLinkUrl))
                {
                    if (CheckIfInternetConnectivityIsOn())
                    {
                        SaveVideoToDisk();
                    }
                }
            }
        }
        #endregion

        #region Methods Private
        private void Initialize()
        {
            this._mp3List = new ObservableCollection<Mp3Model>();
            this.YoutubeLinkUrl = Consts.DefaultTextBoxEntry;

            InitializeQualityCollection();
            InitializeFormatCollection();
        }

        private void InitializeQualityCollection()
        {
            QualityList = new ObservableCollection<QualityModel>
            {
                new QualityModel() { Quality = "128k" },
                new QualityModel() { Quality = "192k" },
                new QualityModel() { Quality = "256k" },
                new QualityModel() { Quality = "320k" },
            };
            QualityModel = QualityList[3];
        }

        private void InitializeFormatCollection()
        {
            FormatList = new ObservableCollection<FormatModel>
            {
                new FormatModel() { Format = "mp3" },
                new FormatModel() { Format = "wav" },
                new FormatModel() { Format = "wma" }
            };
            FormatModel = FormatList[0];
        }

        private void SaveVideoToDisk()
        {
            Task.Factory.StartNew(() =>
            {
                var CurrentFile = new FileHelper();
                var Mp3Model = new Mp3Model();

                using (var service = Client.For(YouTube.Default))
                {
                    if (IsWholeListChecked)
                    {
                        var youtubePlaylist = new YoutubePlaylist();
                        var playlist = youtubePlaylist.GetVideosFromPlaylist(YoutubeLinkUrl);

                        if (playlist != null)
                        {
                            foreach (var audio in playlist)
                            {
                                using (var video = service.GetVideo(audio))
                                {
                                    CurrentFile.DefaultTrackName = video.FullName;
                                    CurrentFile.DefaultTrackPath = CurrentFile.Path + "\\" + CurrentFile.DefaultTrackName;
                                    CurrentFile.DefaultTrackHiddenPath = CurrentFile.HiddenPath + "\\" + CurrentFile.DefaultTrackName;
                                    CurrentFile.TmpTrackPath = CurrentFile.PreparePathForFFmpeg(CurrentFile.DefaultTrackHiddenPath);

                                    Mp3Model = new Mp3Model()
                                    {
                                        Name = CurrentFile.CheckVideoFormat(video.FullName, FormatModel.Format),
                                        IsProgressDownloadVisible = Visibility.Visible,
                                        IsPercentLabelVisible = Visibility.Visible,
                                        IsConvertingLabelVisible = Visibility.Hidden,
                                        IsOperationDoneLabelVisible = Visibility.Hidden,
                                        ConvertingLabelText = Consts.ConvertingPleaseWait,
                                        CurrentProgress = 0,
                                    };

                                    Application.Current.Dispatcher.BeginInvoke(new Action(() =>
                                    {
                                        this._mp3List.Add(Mp3Model);
                                    }));

                                    using (var outFile = File.OpenWrite(CurrentFile.TmpTrackPath))
                                    {
                                        using (var progressStream = new ProgressStream(outFile))
                                        {
                                            var streamLength = (long)video.StreamLength();

                                            progressStream.BytesMoved += (sender, args) =>
                                            {
                                                Mp3Model.CurrentProgress = args.StreamLength * 100 / streamLength;
                                                Debug.WriteLine($"{Mp3Model.CurrentProgress}% of video downloaded");
                                            };

                                            video.Stream().CopyTo(progressStream);
                                        }
                                    }
                                    BeforeConversion(Mp3Model);
                                    ExtractAudioFromVideo(CurrentFile);
                                    AfterConversion(Mp3Model, CurrentFile);
                                }
                            }
                        }
                    }
                    else
                    {
                        using (var video = service.GetVideo(YoutubeLinkUrl))
                        {
                            CurrentFile.DefaultTrackName = video.FullName;
                            CurrentFile.DefaultTrackPath = CurrentFile.Path + "\\" + CurrentFile.DefaultTrackName;
                            CurrentFile.DefaultTrackHiddenPath = CurrentFile.HiddenPath + "\\" + CurrentFile.DefaultTrackName;
                            CurrentFile.TmpTrackPath = CurrentFile.PreparePathForFFmpeg(CurrentFile.DefaultTrackHiddenPath);

                            Mp3Model = new Mp3Model()
                            {
                                Name = CurrentFile.CheckVideoFormat(video.FullName, FormatModel.Format),
                                IsProgressDownloadVisible = Visibility.Visible,
                                IsPercentLabelVisible = Visibility.Visible,
                                IsConvertingLabelVisible = Visibility.Hidden,
                                IsOperationDoneLabelVisible = Visibility.Hidden,
                                ConvertingLabelText = Consts.ConvertingPleaseWait,
                                CurrentProgress = 0,
                            };

                            Application.Current.Dispatcher.BeginInvoke(new Action(() =>
                            {
                                this._mp3List.Add(Mp3Model);
                            }));

                            using (var outFile = File.OpenWrite(CurrentFile.TmpTrackPath))
                            {
                                using (var progressStream = new ProgressStream(outFile))
                                {
                                    var streamLength = (long)video.StreamLength();

                                    progressStream.BytesMoved += (sender, args) =>
                                    {
                                        Mp3Model.CurrentProgress = args.StreamLength * 100 / streamLength;
                                        Debug.WriteLine($"{Mp3Model.CurrentProgress}% of video downloaded");
                                    };

                                    video.Stream().CopyTo(progressStream);
                                }
                            }
                            BeforeConversion(Mp3Model);
                            ExtractAudioFromVideo(CurrentFile);
                            AfterConversion(Mp3Model, CurrentFile);
                        }
                    }
                }
            });
        }

        private void ExtractAudioFromVideo(FileHelper fileHelper)
        {
            var videoToWorkWith = fileHelper.TmpTrackPath;
            var ffmpegExePath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "ffmpeg\\ffmpeg.exe");
            var output = fileHelper.CheckVideoFormat(videoToWorkWith, FormatModel.Format);
            var standardErrorOutput = string.Empty;
            var quality = QualityModel.Quality;

            fileHelper.DefaultTrackHiddenPath = videoToWorkWith;
            fileHelper.TmpTrackPath = output;

            try
            {
                _process = new Process();
                _process.StartInfo.UseShellExecute = false;
                _process.StartInfo.RedirectStandardInput = true;
                _process.StartInfo.RedirectStandardOutput = true;
                _process.StartInfo.RedirectStandardError = true;
                _process.StartInfo.CreateNoWindow = true;
                _process.StartInfo.WindowStyle = ProcessWindowStyle.Hidden;
                _process.StartInfo.FileName = ffmpegExePath;
                _process.StartInfo.Arguments = " -i " + videoToWorkWith + " -codec:a libmp3lame -b:a " + quality + " " + output;
                _process.Start();
                _process.EnableRaisingEvents = true;
                _process.ErrorDataReceived += new DataReceivedEventHandler(OnErrorDataReceived);
                _process.Exited += new EventHandler(OnConversionExited);
                _process.BeginOutputReadLine();
                _process.BeginErrorReadLine();
                _process.WaitForExit();
                _process.Close();
            }
            catch (Exception e)
            {
                Debug.WriteLine("Exception Occured: {0}", e);
            }
        }

        private void OnConversionExited(object sender, EventArgs e)
        {
            _process.ErrorDataReceived -= OnErrorDataReceived;
            _process.Exited -= OnConversionExited;
        }

        private void OnErrorDataReceived(object sender, DataReceivedEventArgs e)
        {
            // TODO: implement logger
            Debug.WriteLine("Input line: {0} ({1:m:s:fff})", _currentLine++, DateTime.Now);
        }

        private void BeforeConversion(Mp3Model model)
        {
            model.IsConvertingLabelVisible = Visibility.Visible;
            model.IsPercentLabelVisible = Visibility.Hidden;
            model.IsIndeterminate = true;

            DispatchService.Invoke(() =>
            {
                shortToastMessage.ShowInformation("Converting...");
            });
        }

        private void AfterConversion(Mp3Model model, FileHelper fileHelper)
        {
            DispatchService.Invoke(() =>
            {
                longToastMessage.ShowSuccess(fileHelper.PrepareTrackForNotification(fileHelper.DefaultTrackName));
            });

            fileHelper.RenameFile(fileHelper.TmpTrackPath, fileHelper.DefaultTrackPath, FormatModel.Format);
            fileHelper.RemoveFile(fileHelper.DefaultTrackHiddenPath);

            model.IsProgressDownloadVisible = Visibility.Hidden;
            model.IsPercentLabelVisible = Visibility.Hidden;
            model.IsConvertingLabelVisible = Visibility.Hidden;
            model.IsOperationDoneLabelVisible = Visibility.Visible;
            model.ConvertingLabelText = Consts.ConvertingPleaseWait;
            model.IsOperationDone = Consts.OperationDone;
            model.IsIndeterminate = false;
        }
        #endregion

        #region Validators
        private bool CheckIfFileAlreadyExists(string FileName)
        {
            var youTube = YouTube.Default;
            var video = youTube.GetVideo(FileName);

            if (fileHelper.CheckPossibleDuplicate(video.FullName))
            {
                shortToastMessage.ShowInformation(Consts.FileAlreadyExistsInfo);
                return true;
            }
            return false;
        }

        private bool CheckIfInternetConnectivityIsOn()
        {
            if (_connectionHelper != null)
            {
                if (_connectionHelper.CheckForInternetConnection())
                {
                    return true;
                }
                else
                {
                    shortToastMessage.ShowError(Consts.InternetConnectionError);
                }
            }
            return false;
        }

        private bool ValidateEditFieldString()
        {
            if (YoutubeLinkUrl == string.Empty)
            {
                shortToastMessage.ShowWarning(Consts.LinkValidatorEmpty);
                return false;
            }
            else if (!YoutubeLinkUrl.Contains(Consts.LinkPartValidation))
            {
                shortToastMessage.ShowWarning(Consts.LinkValidatorIsNotValid);
                YoutubeLinkUrl = Consts.DefaultTextBoxEntry;
                return false;
            }
            return true;
        }
        #endregion
    }
}
