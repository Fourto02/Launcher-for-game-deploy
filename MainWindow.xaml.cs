using System;
using System.Runtime;
using System.Net.Http;
using System.Windows;
using System.Windows.Input;
using System.IO;
using System.Net;
using System.ComponentModel;
using System.IO.Compression;
using System.Diagnostics;
using System.Threading.Tasks;
using System.Windows.Controls;
using System.Threading;
using System.Data;

namespace LASTHOPE
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    enum LauncherStatus
    {
        Download,
        Updates,
        ready,
        failed,
        downloadingGame,
        downloadingUpdate
    }
    public partial class MainWindow : Window
    {
        //create variable
        private string rootPath;
        private string updatePath;
        private string versionGameFile;
        private string versionUpdate;
        private string gameZip;
        private string updateZip;
        private string gameExe;
        private int fileSize;
        private int downloadingSize;
        private string gameUrl;
        private string updateUrl;
        private string urlVersion;
        private string urlVersionUpdate;
        private string gameFolder;
        private string homepage;
        private double maxsize;
        private LauncherStatus _status;
        static readonly HttpClient htc = new HttpClient();
        static readonly string[] SizeSuffixes = { "bytes", "KB", "MB", "GB", "TB", "PB", "EB", "ZB", "YB" };
        static readonly string[] SizeType = { "bytes", "KB", "MB", "GB", "TB", "PB", "EB", "ZB", "YB" };
        private bool _stop;
        internal LauncherStatus Status
        {

            get => _status;
            set
            {
                _status = value;
                switch (_status)
                {
                    case LauncherStatus.Download:
                        PlayButton.Content = "ดาวน์โหลดเลย!";
                        break;
                    case LauncherStatus.Updates:
                        PlayButton.Content = "อัปเดท";
                        break;
                    case LauncherStatus.ready:
                        PlayButton.Content = "เล่น";
                        break;
                    case LauncherStatus.failed:
                        PlayButton.Content = "อัปเดทไม่สำเร็จ ลองใหม่อีกครั้ง";
                        break;
                    case LauncherStatus.downloadingGame:
                        PlayButton.Content = "กำลังดาวน์โหลด";
                        break;
                    case LauncherStatus.downloadingUpdate:
                        PlayButton.Content = "กำลังอัปเดท";
                        break;
                    default:
                        break;
                }
            }
        }
        public MainWindow()
        {
            InitializeComponent();
            //set variable
            rootPath = Directory.GetCurrentDirectory();
            homepage = "https://www.dropbox.com/scl/fi/5aerp7mspw6rojqyl2iwy/updatelist.txt?rlkey=9aohf07aw3vaffhrprrauhusk&st=cns32cdl&dl=0&raw=1";
            String folder = "TTG";
            String ContentF = "TTG";
            String ExcName = "TTG";
            updatePath = Path.Combine(rootPath, folder + "/" + ContentF + "/Content/Paks");
            versionGameFile = Path.Combine(rootPath, folder, "Version.txt");
            versionUpdate = Path.Combine(rootPath, folder, "Update.txt");
            gameZip = Path.Combine(rootPath, folder + ".zip");
            updateZip = Path.Combine(rootPath, "Update.zip");
            gameFolder = folder;
            gameExe = Path.Combine(rootPath, folder, ExcName + ".exe");
            updateUrl = "https://www.dropbox.com/scl/fi/baebt1bsshoerjn0emnvm/Update.zip?rlkey=47jpniv7mkjufj879xwplo7x5&st=mfpt5lzx&dl=1";
            gameUrl = "https://www.dropbox.com/scl/fi/4l705xdoyklb5wi6u2nqp/TTG.zip?rlkey=m4fqjlwo57kra3k8nf8gox3l5&st=si8geo7g&dl=1";
            urlVersion = "https://www.dropbox.com/scl/fi/w221522e4jv7fqdxkx5b0/Version.txt?rlkey=wm6vsgm9vqszq57yap3jgs5kf&st=2einb223&dl=1";
            urlVersionUpdate = "https://www.dropbox.com/scl/fi/dljjyqr7l8feg245ifwzn/Update.txt?rlkey=7dqvasjgc6ng71x8245dnatc5&st=32wwi5zt&dl=1";
        }
        static string SizeSuffix(Int64 value, int decimalPlaces = 1)
        {
            if (decimalPlaces < 0) { throw new ArgumentOutOfRangeException("decimalPlaces"); }
            if (value < 0) { return "-" + SizeSuffix(-value, decimalPlaces); }
            if (value == 0) { return string.Format("{0:n" + decimalPlaces + "} bytes", 0); }

            // mag is 0 for bytes, 1 for KB, 2, for MB, etc.
            int mag = (int)Math.Log(value, 1024);

            // 1L << (mag * 10) == 2 ^ (10 * mag) 
            // [i.e. the number of bytes in the unit corresponding to mag]
            decimal adjustedSize = (decimal)value / (1L << (mag * 10));

            // make adjustment when the value is large enough that
            // it would round up to 1000 or more
            if (Math.Round(adjustedSize, decimalPlaces) >= 1000)
            {
                mag += 1;
                adjustedSize /= 1024;
            }

            return string.Format("{0:n" + decimalPlaces + "} {1}",
                adjustedSize,
                SizeSuffixes[mag]);
        }
        static string SizeLoading(Int64 value, int decimalPlaces = 1)
        {
            if (decimalPlaces < 0) { throw new ArgumentOutOfRangeException("decimalPlaces"); }
            if (value < 0) { return "-" + SizeLoading(-value, decimalPlaces); }
            if (value == 0) { return string.Format("{0:n" + decimalPlaces + "} bytes", 0); }

            // mag is 0 for bytes, 1 for KB, 2, for MB, etc.
            int mag = (int)Math.Log(value, 1024);

            // 1L << (mag * 10) == 2 ^ (10 * mag) 
            // [i.e. the number of bytes in the unit corresponding to mag]
            decimal adjustedSize = (decimal)value / (1L << (mag * 10));

            // make adjustment when the value is large enough that
            // it would round up to 1000 or more
            if (Math.Round(adjustedSize, decimalPlaces) >= 1000)
            {
                mag += 1;
                adjustedSize /= 1024;
            }

            return string.Format("{0:n" + decimalPlaces + "} {1}",
                adjustedSize,
                SizeType[mag]);
        }




        private void Window_ContentRendered(object sender, EventArgs e)
        {
            CheckVersion();
        }


        private void Install()
        {
            PlayButton.IsEnabled = false;
            if (File.Exists(versionUpdate))
            {
                Version localVersion = new Version(File.ReadAllText(versionUpdate));

                try
                {
                    WebClient webClient = new WebClient();
                    Version onlineVersion = new Version(webClient.DownloadString(urlVersionUpdate));

                    if (onlineVersion.IsDifferentThan(localVersion))
                    {
                        PlayButton.IsEnabled = false;
                        InstallGameFiles(true, onlineVersion);
                    }
                    else
                    {
                        Status = LauncherStatus.ready;
                        Downloadinfo_DB.Visibility = Visibility.Hidden;
                    }
                }
                catch (Exception ex)
                {
                    Status = LauncherStatus.failed;
                    MessageBox.Show($"Error checking for game updates: {ex}");
                }
            }
            else
            {
                InstallGameFiles(false, Version.zero);
            }
        }

        private void InstallGameFiles(bool _isUpdate, Version _onlineVersion)
        {
            try
            {
                WebClient wc = new WebClient();
                if (_isUpdate)
                { 
                    PlayButton.IsEnabled = false;
                    Status = LauncherStatus.downloadingUpdate;
                    wc.DownloadFileCompleted += new AsyncCompletedEventHandler(UpdateGameCompletedCallback);
                    wc.DownloadProgressChanged += new DownloadProgressChangedEventHandler(Wc_DownloadProgressChanged);
                    wc.DownloadFileAsync(new Uri(updateUrl), updateZip, _onlineVersion);
                }
                else
                {
                    PlayButton.IsEnabled = false;
                    Status = LauncherStatus.downloadingGame;
                    _onlineVersion = new Version(wc.DownloadString(urlVersion));
                    wc.DownloadFileCompleted += new AsyncCompletedEventHandler(DownloadGameCompletedCallback);
                    wc.DownloadProgressChanged += new DownloadProgressChangedEventHandler(Wc_DownloadProgressChanged);
                    wc.DownloadFileAsync(new Uri(gameUrl), gameZip, _onlineVersion);
                }
            }

            catch (Exception ex)
            {
                Status = LauncherStatus.failed;
                MessageBox.Show($"Error installing game files: {ex}");
            }
        }

        private void DownloadGameCompletedCallback(object sender, AsyncCompletedEventArgs e)
        {
            try
            {
                string onlineVersion = ((Version)e.UserState).ToString();
                unzip(gameFolder+".zip", rootPath);
                File.Delete(gameZip);
                File.WriteAllText(versionGameFile, onlineVersion);
                Status = LauncherStatus.ready;
                PlayButton.IsEnabled = true;
                CheckUpdate();
            }
            catch (Exception ex)
            {
                Status = LauncherStatus.failed;
                MessageBox.Show($"Error finishing download: {ex}");
                PlayButton.IsEnabled = true;
            }
        }

        void Wc_DownloadProgressChanged(object sender, DownloadProgressChangedEventArgs e)
        {
            DownloadStatus_txt.Text = "กำลังดาวน์โหลด";
            downloadingSize = ((int)e.BytesReceived);
            int Bytes = 0;
            SizeLoading(downloadingSize, Bytes);
            DownloadingSize_Text.Text = SizeSuffix(downloadingSize);
            DownloadPercent_Text.Text = e.ProgressPercentage.ToString();
            DownloadPercent_PgBar.Value = ((float)downloadingSize / e.TotalBytesToReceive);

        }

        private void UpdateGameCompletedCallback(object sender, AsyncCompletedEventArgs e)
        {
            try
            {
                string onlineVersion = ((Version)e.UserState).ToString();
                unzip("Update.zip", updatePath);
                File.Delete(updateZip);
                File.WriteAllText(versionUpdate, onlineVersion);
                Status = LauncherStatus.ready;
                PlayButton.IsEnabled = true;
                Downloadinfo_DB.Visibility = Visibility.Hidden;
            }
            catch (Exception ex)
            {
                Status = LauncherStatus.failed;
                MessageBox.Show($"Error finishing download: {ex}");
                Downloadinfo_DB.Visibility = Visibility.Hidden;
                PlayButton.IsEnabled = true;
            }
        }

        private void unzip(string filename, string path) {
            using (ZipArchive source = ZipFile.Open(filename, ZipArchiveMode.Read, null))
            {
                foreach (ZipArchiveEntry entry in source.Entries)
                {
                    string fullPath = Path.GetFullPath(Path.Combine(path, entry.FullName));

                    if (Path.GetFileName(fullPath).Length != 0)
                    {
                        Directory.CreateDirectory(Path.GetDirectoryName(fullPath));
                        // The boolean parameter determines whether an existing file that has the same name as the destination file should be overwritten
                        entry.ExtractToFile(fullPath, true);
                    }
                }
            }
        }
        private async void CheckVersion()
        {
            DownloadPercent_PgBar.IsIndeterminate = true;
            PlayButton.IsEnabled = false;
            if (Status == LauncherStatus.downloadingUpdate || Status == LauncherStatus.downloadingGame)
            { }
            else
            {
                htc.CancelPendingRequests();

                if (File.Exists(versionGameFile) && File.Exists(gameExe))
                {
                    try
                    {
                        HttpResponseMessage response = await htc.GetAsync(urlVersion);
                        response.EnsureSuccessStatusCode();
                        string responseBody = await response.Content.ReadAsStringAsync();
                        Version OnlineVer = new Version(responseBody);
                        Version localVersion = new Version(File.ReadAllText(versionGameFile));

                        if (OnlineVer.IsDifferentThan(localVersion))
                        {
                            Status = LauncherStatus.Download;
                            CheckSizeDownload(gameUrl);
                            DownloadStatus_txt.Text = "อัปเดทใหญ่";
                            Downloadinfo_DB.Visibility = Visibility.Visible;
                            PlayButton.IsEnabled = false;
                        }
                        else
                        {
                            Status = LauncherStatus.ready;
                            Downloadinfo_DB.Visibility = Visibility.Hidden;
                            DownloadPercent_PgBar.IsIndeterminate = false;
                            PlayButton.IsEnabled = true;
                            CheckUpdate();

                        }
                    }
                    catch (Exception EMS)
                    {
                        {
                            Status = LauncherStatus.Download;
                            DownloadStatus_txt.Text = "ดาวน์โหลด";
                            CheckSizeDownload(gameUrl);
                            DownloadPercent_PgBar.IsIndeterminate = false;
                        }
                    }
                }
                else
                {
                    Status = LauncherStatus.Download;
                    DownloadStatus_txt.Text = "ดาวน์โหลด";
                    CheckSizeDownload(gameUrl);
                    DownloadPercent_PgBar.IsIndeterminate = true;
                }
            }
        }

        private async void CheckUpdate()
        {
            PlayButton.IsEnabled = false;
            if (Status == LauncherStatus.downloadingUpdate || Status == LauncherStatus.downloadingGame)
            {
            }
            else
            {
                htc.CancelPendingRequests();

                if (File.Exists(versionGameFile) && File.Exists(gameExe))
                {
                    try
                    {
                        HttpResponseMessage response = await htc.GetAsync(urlVersionUpdate);
                        response.EnsureSuccessStatusCode();
                        string responseBody = await response.Content.ReadAsStringAsync();
                        Version OnlineVer = new Version(responseBody);
                        Version localVersion = new Version(File.ReadAllText(versionUpdate));

                        if (OnlineVer.IsDifferentThan(localVersion))
                        {
                            Status = LauncherStatus.Updates;
                            CheckSizeDownload(updateUrl);
                            DownloadStatus_txt.Text = "อัปเดท";
                            Downloadinfo_DB.Visibility = Visibility.Visible;
                            PlayButton.IsEnabled = false;
                            DownloadPercent_PgBar.IsIndeterminate = false;

                        }
                        else
                        {
                            //CheckforUpdates();
                            Status = LauncherStatus.ready;
                            Downloadinfo_DB.Visibility = Visibility.Collapsed;
                            PlayButton.IsEnabled = true;
                            Mouse.OverrideCursor = Cursors.Arrow;
                            DownloadPercent_PgBar.IsIndeterminate = false;
                        }
                    }
                    catch (Exception EMS)
                    {
                        Status = LauncherStatus.Updates;
                        CheckSizeDownload(updateUrl);
                        DownloadStatus_txt.Text = "อัปเดท";
                        Downloadinfo_DB.Visibility = Visibility.Visible;
                    }
                }
                else
                {
                    Status = LauncherStatus.Download;
                    CheckSizeDownload(gameUrl);
                    PlayButton.IsEnabled = true;
                }
            }

        }

        private void texttest_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            System.Diagnostics.Process.Start("https://www.facebook.com/profile.php?id=61572355677034");
        }

        private void texttest_MouseEnter(object sender, MouseEventArgs e)
        {
        }

        private void Close_bt_Click(object sender, RoutedEventArgs e)
        {
            Close();
        }

        private void minisize_bt_Click(object sender, RoutedEventArgs e)
        {
            this.WindowState = WindowState.Minimized;
        }

        async void CheckSizeDownload(string Url)
        {
            try
            {
                var request = new HttpRequestMessage(HttpMethod.Head, Url);
                var response = await htc.SendAsync(request);
                if (response.IsSuccessStatusCode)
                {
                    Int64 total_bytes = Convert.ToInt64(response.Content.Headers.ContentLength);
                    fileSize = ((int)total_bytes);
                    int I = 0;
                    DownloadSize_txt.Text = fileSize.ToString();
                    DownloadSize_txt.Text = SizeSuffix(fileSize);
                }
                DownloadPercent_PgBar.IsIndeterminate = false;

                PlayButton.IsEnabled = true;
            }
            catch (Exception ex)
            {
                MessageBox.Show("กรุณาตรวจสอบการเชื่อมต่อ Internet");
            }
        }

        private async void PlayButton_Click(object sender, RoutedEventArgs e)
        {
            if (File.Exists(gameExe) && Status == LauncherStatus.ready)
            {
                ProcessStartInfo startInfo = new ProcessStartInfo(gameExe);
                startInfo.WorkingDirectory = Path.Combine(rootPath, gameFolder);
                Process.Start(startInfo);


                this.WindowState = WindowState.Minimized;
                Mouse.OverrideCursor = Cursors.Arrow;
            }
            else if (Status == LauncherStatus.Download)
            {
                try
                {
                    HttpResponseMessage response = await htc.GetAsync(urlVersion);
                    response.EnsureSuccessStatusCode();
                    string responseBody = await response.Content.ReadAsStringAsync();
                    Version onlineVersion = new Version(responseBody);
                    InstallGameFiles(false, onlineVersion);
                    PlayButton.IsEnabled = false;
                }
                catch
                {

                }
            }
            else if (Status == LauncherStatus.Updates)
            {
                Install();
                PlayButton.IsEnabled = false;
            }
            else if (Status == LauncherStatus.failed)
            {
                Install();
                PlayButton.IsEnabled = false;
            }
            else if (Status == LauncherStatus.ready){
                CheckVersion();
                Downloadinfo_DB.Visibility = Visibility.Visible;
                PlayButton.IsEnabled = false;
            }
        }

        struct Version
        {
            internal static Version zero = new Version(0, 0, 0);

            private short major;
            private short minor;
            private short subMinor;

            internal Version(short _major, short _minor, short _subMinor)
            {
                major = _major;
                minor = _minor;
                subMinor = _subMinor;
            }
            internal Version(string _version)
            {
                string[] versionStrings = _version.Split('.');
                if (versionStrings.Length != 3)
                {
                    major = 0;
                    minor = 0;
                    subMinor = 0;
                    return;
                }

                major = short.Parse(versionStrings[0]);
                minor = short.Parse(versionStrings[1]);
                subMinor = short.Parse(versionStrings[2]);
            }

            internal bool IsDifferentThan(Version _otherVersion)
            {
                if (major != _otherVersion.major)
                {
                    return true;
                }
                else
                {
                    if (minor != _otherVersion.minor)
                    {
                        return true;
                    }
                    else
                    {
                        if (subMinor != _otherVersion.subMinor)
                        {
                            return true;
                        }
                    }
                }
                return false;
            }
            public override string ToString()
            {
                return $"{major}.{minor}.{subMinor}";
            }
        }
    }
}
