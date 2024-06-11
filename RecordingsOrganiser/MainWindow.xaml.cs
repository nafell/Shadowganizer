using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using System.IO;
using Alturos.VideoInfo;
using System.Xml;
using System.Globalization;
using Microsoft.WindowsAPICodePack.Shell;

namespace RecordingsOrganiser
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        private readonly TimeSpan days = new TimeSpan(10, 0, 0, 0);
        private const string mediaPath = @"C:\Users\Pathemia\Videos\Final Fantasy XIV  A Realm Reborn";
        private const string ffprobePath = @"C:\Users\Pathemia\source\repos\RecordingsOrganiser\RecordingsOrganiser\bin\Debug\ffprobe.exe";

        private MediaFileInfoBase LastSelectedFile { get; set; }
        private IEnumerable<MediaFileInfoBase> SelectedFiles { get; set; }
        public long DiskSize { get; set; }
        public long InitialDiskAvailableSpace { get; set; }
        public long CurrentDiskAvailableSpace { get; set; }

        public MainWindow()
        {
            InitializeComponent();

        }

        private async void Window_ContentRendered(object sender, EventArgs e)
        {
            await Task.Run(() => InitializeList()).ConfigureAwait(false);
        }

        private IEnumerable<MediaFileInfoBase> _backingFiles;
        public IEnumerable<MediaFileInfoBase> Files 
        {
            get
            {
                return _backingFiles;
            }
            set 
            {
                if (_backingFiles == value)
                    return;

                //update UI
                VideoListBox.ItemsSource = value;
                currentFileCount.Text = value.Count().ToString();
                currentSizeSumText.Text = FileSizeSumGigabytes(value);

                _backingFiles = value;
            }
        }
        private void UpdateList(IEnumerable<MediaFileInfoBase> files)
        {
            Files = files;
            SortListView(SortOption.Date, SortDirection.LargerToSmaller);
        }


        private async Task InitializeList()
        {
            var files = await IndexMediaFiles();

            await Task.Run(() => { 
                Dispatcher.Invoke(() =>
                {
                    initialFileCount.Text = "/" + files.Count().ToString();
                    initialSizeSumText.Text = "/" + FileSizeSumGigabytes(files) + " GB";

                    UpdateList(files);


                    var allDrives = DriveInfo.GetDrives();
                    var targetDrive = allDrives.First(
                        drive => drive.IsReady && drive.Name == System.IO.Path.GetPathRoot(mediaPath));


                    //DiskSize
                    {
                        DiskSize = targetDrive.TotalSize;
                        InitialDiskAvailableSpace = targetDrive.AvailableFreeSpace;
                        CurrentDiskAvailableSpace = targetDrive.AvailableFreeSpace;

                        var usedSpace = DiskSize - CurrentDiskAvailableSpace;

                        DiskTotalSizeText.Text = "/" + FileLengthToGigabytes(DiskSize, true) + "GB";
                        DiskFreeSpaceText.Text = "" + FileLengthToGigabytes(CurrentDiskAvailableSpace);
                        DiskFreeSpacePercentage.Text = "(" + (((double)usedSpace / DiskSize) * 100).ToString("0.#") + "%)";

                        var fileSizeSum = FileSizeSum(Files);

                        DiskInitialSizeProgressBar.Maximum = DiskSize;
                        DiskCurrentSizeProgressBar.Maximum = DiskSize;
                        StakeProgressBar.Maximum = DiskSize;

                        DiskInitialSizeProgressBar.Value = InitialDiskAvailableSpace;
                        DiskCurrentSizeProgressBar.Value = usedSpace;
                        StakeProgressBar.Value = fileSizeSum;

                        StakeText.Text = (((double)fileSizeSum / usedSpace) * 100).ToString("0.#");
                    }

                    LoadingFileListIndicator.Visibility = Visibility.Collapsed;
                    LoadingFileListIndicator.IsEnabled = false;
                });
            });


            //Console.WriteLine("Started videoinfo inflation...");
            //var videoAnalyer = new VideoAnalyzer(ffprobePath);

            ////inflate VideoInfo
            //foreach (var file in files)
            //{
            //    if (!(file is VideoFileBase))
            //        continue;

            //    var videoFile = file as VideoFileBase;

            //    var analyzeResult = await videoAnalyer.GetVideoInfoAsync(file.Path);
            //    var videoInfo = analyzeResult.VideoInfo;

            //    videoFile.InflateVideoInfo(videoInfo);

            //}
            //Console.WriteLine("videoinfo inflation complete");

            Dispatcher.Invoke(() => UpdateList(files));
        }

        //Idea: return all files and filter it later on UI
        private async Task<IEnumerable<FileInfo>> IndexFiles(string path, string extention, 
            DateTime since = default, DateTime until = default, bool subfolder = false)
        {
            until = until == default ? DateTime.Now : until;


            var filePaths = await Task.Run(() =>
                Directory.EnumerateFiles(path, "*." + extention, SearchOption.AllDirectories)
            );

            var files = new List<FileInfo>();
            foreach (var filepath in filePaths)
            {
                files.Add(new FileInfo(filepath));
            }

            return files.Where(file => file.CreationTime > since);
        }
        private async Task<IEnumerable<MediaFileInfoBase>> IndexMediaFiles()
        {
            var since = DateTime.Now - days;
            var fileInfos = await IndexFiles(mediaPath, "mp4", since);

            var videoAnalyer = new VideoAnalyzer(ffprobePath);



            var mediaInfos = new List<MediaFileInfoBase>();
            foreach (var fileInfo in fileInfos)
            {
                var analyzeResult = await videoAnalyer.GetVideoInfoAsync(fileInfo.FullName);
                var videoInfo = analyzeResult.VideoInfo;

                mediaInfos.Add(new Mp4VideoFile(fileInfo, videoInfo));
            }


            return mediaInfos;
        }
        private long FileSizeSum(IEnumerable<MediaFileInfoBase> files)
        {
            return files.Sum(file => file.Length);
        }
        private string FileSizeSumGigabytes(IEnumerable<MediaFileInfoBase> files)
        {
            return FileLengthToGigabytes(FileSizeSum(files));
        }
        private string FileLengthToGigabytes(long length, bool inInteger = false)
        {
            var format = "#,0.##";
            if (inInteger) format = "#,0";

            return (length / Math.Pow(1024, 3)).ToString(format);
        }


        private void SortListView(SortOption sortOption, SortDirection sortDirection)
        {

            if (sortOption == SortOption.Date)
            {
                Files = Files.OrderByDescending(file => file.CreationTime);
            }
            else if (sortOption == SortOption.Size)
            {
                Files = Files.OrderByDescending(file => file.Length);
            }
        }
        public enum SortOption 
        {
            Date, Size
        }
        public enum SortDirection
        {
            LargerToSmaller, SmallerToLarger
        }

        private void VideoListBox_MouseDoubleClick(object sender, MouseButtonEventArgs e)
        {
            var selectedFile = (MediaFileInfoBase)VideoListBox.SelectedItem;
            System.Diagnostics.Process.Start(selectedFile.Path);
        }

        private void VideoListBox_KeyUp(object sender, KeyEventArgs eventArgs)
        {
            if (eventArgs.Key == Key.Delete)
            {
                var selectedObjects = VideoListBox.SelectedItems;
                var selecetdFiles = selectedObjects.Cast<MediaFileInfoBase>();


                var dialog = new DeleteConfirmationDialog();
                dialog.Owner = this;

                if (dialog.ShowDialog() == false)
                {
                    return;
                }

                var succeededFiles = new List<MediaFileInfoBase>();

                try
                {
                    foreach (var file in selecetdFiles)
                    {
                        File.Delete(file.Path);
                        succeededFiles.Add(file);
                    }
                }
                catch (Exception exception)
                {
                    //notify error
                }

                //Does not update the listbox UI when every deletion fails.
                if (succeededFiles.Any())
                {
                    var newFiles = Files.Where(file => !succeededFiles.Contains(file));
                    UpdateList(newFiles);
                }
            }
        }

        private void VideoListBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            //status bar left
            var selectedObjects = VideoListBox.SelectedItems;
            SelectedFiles = selectedObjects.Cast<MediaFileInfoBase>();

            selectedFileCountText.Text = SelectedFiles.Count().ToString();

            selectedSizeSumText.Text = FileSizeSumGigabytes(SelectedFiles);


            //preview panel
            var selectedFile = (MediaFileInfoBase)VideoListBox.SelectedItem;

            if (selectedFile != null)
            {
                SelectedFileNameText.Text = selectedFile.Name;
                SelectedFileDateText.Text = selectedFile.CreationTime.ToString();
                SelectedFileLengthText.Text =
                    new ReadableFileSizeConverter().Convert(
                        selectedFile.Length, typeof(string), null, CultureInfo.CurrentCulture).ToString();

                SelectedMediaThumbnailContainer.Source = MediaFileInfoBase.ObtainShellThumbnail(selectedFile.Path);

                if (selectedFile is Mp4VideoFile)
                {
                    var selectedVideo = selectedFile as Mp4VideoFile;

                    SelectedMediaTypeText.Text = selectedVideo.VideoCodec;
                    SelectedMediaDimentionText.Text = $"{selectedVideo.Width} * {selectedVideo.Height}";
                    SelectedMediaDurationText.Text = new ReadableDurationConverter().Convert(
                        selectedVideo.Duration, typeof(string), null, CultureInfo.CurrentCulture).ToString();

                    SelectedVideoFramerateText.Text = selectedVideo.Framerate.ToString() + " fps";
                    SelectedVideoBitrateText.Text =
                    new ReadableFileSizeConverter().Convert(
                        (long)selectedVideo.Bitrate, typeof(string), null, CultureInfo.CurrentCulture).ToString();

                }
            }





        }
    }
}
