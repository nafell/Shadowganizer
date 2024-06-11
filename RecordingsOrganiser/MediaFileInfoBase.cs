using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO;
using Alturos;
using System.Windows.Media.Imaging;
using Microsoft.WindowsAPICodePack.Shell;
using System.Windows.Threading;
using System.Windows;
using Alturos.VideoInfo;

namespace RecordingsOrganiser
{
    public abstract class MediaFileInfoBase
    {
        public string Path { get; set; }
        public string Name { get; set; }
        public string Extention { get; set; }
        public long Length { get; set; }
        public DateTime CreationTime { get; set; }
        public BitmapSource ShellThumbanil { get; set; }
        protected MediaFileInfoBase(FileInfo fileInfo)
        {
            Path = fileInfo.FullName;
            Name = fileInfo.Name;
            Extention = fileInfo.Extension;
            Length = fileInfo.Length;
            CreationTime = fileInfo.CreationTime;

            Application.Current.Dispatcher.Invoke(() =>
            {
                ShellThumbanil = ObtainShellThumbnail(fileInfo.FullName);
            });
        }

        public static BitmapSource ObtainShellThumbnail(string path)
        {
            return ShellFile.FromFilePath(path).Thumbnail.BitmapSource;
        }

        public override bool Equals(object obj)
        {
            if (obj == null || GetType() != obj.GetType())
            {
                return false;
            }

            var target = (MediaFileInfoBase)obj;
            return Path == target.Path;
        }

        public override int GetHashCode()
        {
            return Path.GetHashCode();
        }
    }

    abstract class VideoFileBase : MediaFileInfoBase
    {
        public string IsDvr { get; set; }
        public double Bitrate { get; set; }
        public TimeSpan Duration { get; set; }
        public int Height { get; set; }
        public int Width { get; set; }
        public int Framerate { get; set; }
        public string VideoCodec { get; set; }
        public string AudioCodec { get; set; }
        protected VideoFileBase(FileInfo fileInfo, VideoInfoResult videoInfo) : base(fileInfo)
        {
            IsDvr = System.IO.Path.GetFileNameWithoutExtension(fileInfo.Name).EndsWith("DVR") ? "DVR" : "rec";


            if (videoInfo == null)
                return;
            InflateVideoInfo(videoInfo);
        }

        public void InflateVideoInfo(VideoInfoResult videoInfo)
        {
            var videoStream = videoInfo.Streams[0];
            var audioStream = videoInfo.Streams[1];

            Duration = new TimeSpan(0, 0, (int) videoInfo.Format.Duration);
            Bitrate = videoInfo.Format.BitRate;

            Height = videoStream.Height;
            Width = videoStream.Width;
            Framerate = int.Parse(videoStream.RFrameRate.Split('/')[0]);
            VideoCodec = videoStream.CodecLongName;
            AudioCodec = audioStream.CodecName;
        }
    }

    class Mp4VideoFile : VideoFileBase
    {
        public Mp4VideoFile(FileInfo fileInfo, VideoInfoResult videoInfo) : base(fileInfo, videoInfo)
        {
        }
    }


    class GenericFile : MediaFileInfoBase
    {
        public GenericFile(FileInfo fileInfo) : base (fileInfo)
        {

        }
    }
}
