using Microsoft.Win32;
using System;
using System.Diagnostics.Contracts;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Runtime.InteropServices;
using System.Security.AccessControl;

class NativeFunction
{
    const int SPI_SETDESKWALLPAPER = 20;
    const int SPIF_UPDATEINIFILE = 0x01;
    const int SPIF_SENDWININICHANGE = 0x02;

    [DllImport("user32.dll", CharSet = CharSet.Auto)]
    static extern int SystemParametersInfo(int uAction, int uParam, string lpvParam, int fuWinIni);
    public enum Style : int
    {
        Tile,
        Center,
        Stretch,
        Span,
        Fit,
        Fill
    }
    public static void ChangeBackground(string path, Style fill_style)
    {
        RegistryKey key = Registry.CurrentUser.OpenSubKey(@"Control Panel\Desktop", true);
        if (fill_style == Style.Fill)
        {
            key.SetValue(@"WallpaperStyle", 10.ToString());
            key.SetValue(@"TileWallpaper", 0.ToString());
        }
        if (fill_style == Style.Fit)
        {
            key.SetValue(@"WallpaperStyle", 6.ToString());
            key.SetValue(@"TileWallpaper", 0.ToString());
        }
        if (fill_style == Style.Span)
        {
            key.SetValue(@"WallpaperStyle", 22.ToString());
            key.SetValue(@"TileWallpaper", 0.ToString());
        }
        if (fill_style == Style.Stretch)
        {
            key.SetValue(@"WallpaperStyle", 2.ToString());
            key.SetValue(@"TileWallpaper", 0.ToString());
        }
        if (fill_style == Style.Tile)
        {
            key.SetValue(@"WallpaperStyle", 0.ToString());
            key.SetValue(@"TileWallpaper", 1.ToString());
        }
        if (fill_style == Style.Center)
        {
            key.SetValue(@"WallpaperStyle", 0.ToString());
            key.SetValue(@"TileWallpaper", 0.ToString());
        }

        SystemParametersInfo(SPI_SETDESKWALLPAPER,
            0,
            path,
            SPIF_UPDATEINIFILE | SPIF_SENDWININICHANGE);
    }
}

class nasa_img
{
    public class AstronomyPicture
    {
        public string date { get; set; }
        public string explanation { get; set; }
        public string media_type { get; set; }
        public string title { get; set; }
        public string url { get; set; }

        public void Save(string path)
        {
            Directory.CreateDirectory(Path.GetDirectoryName(path));
            using (WebClient client = new WebClient())
            {
                client.DownloadFile(url, path);
            }
        }

        public void SetWallpaper(string path)
        {
            Save(path);
            NativeFunction.ChangeBackground(path, NativeFunction.Style.Center);
        }
    }
    public string ApiKey { get; set; }
    public DateTime Date { get; set; }
    public bool HD { get; set; }
    public bool AllowOnlyPicture { get; set; }
    public Logger default_logger = new Logger(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile) + @"\DailyWall\Log\ServiceLog.txt");
    private HttpClient client;
    public nasa_img()
    {
        client = new HttpClient();
        client.BaseAddress = new Uri("https://api.nasa.gov");
        client.DefaultRequestHeaders.Accept.Clear();
        client.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
    }

    public AstronomyPicture GetAstronomyPicture()
    {
        AstronomyPicture picture = null;
        HttpResponseMessage response = client.GetAsync($"planetary/apod?api_key={ApiKey}&hd={HD.ToString()}&date={Date.ToString("yyyy-MM-dd")}").Result;

        if (response.IsSuccessStatusCode)
        {
            picture = response.Content.ReadAsAsync<AstronomyPicture>().Result;

            if (picture.media_type == "video" && AllowOnlyPicture)
            {
                Date = Date.AddDays(-1);
                return GetAstronomyPicture();
            }
        }
        return picture;
    }
    public bool TodayWallSet()
    {
        return File.Exists(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile) + @"\DailyWall\" + DateTime.Now.ToString("yyyy-MM-dd") + ".jpg");
    }

    public string GetTodayPath()
    {
        return Environment.GetFolderPath(Environment.SpecialFolder.UserProfile) + @"\DailyWall\" + DateTime.Now.ToString("yyyy-MM-dd") + ".jpg";
    }
    public void ChangeWallpaper(string save_path)
    {
        AstronomyPicture NewWall = GetAstronomyPicture();
        NewWall.SetWallpaper(save_path);
        default_logger.Log("[+][" + DateTime.Now.ToString() + "] Attempting to change => Success \"" + NewWall.title + "\"\n{\n    Url = " + NewWall.url + "\n    File = " + save_path + "\n}");


    }

}

class Logger
{
    private StreamWriter file;
    public Logger(string Path)
    {
        Directory.CreateDirectory(System.IO.Path.GetDirectoryName(Path));
        file = new StreamWriter(Path, true);
    }

    public void Log(string input)
    {
        file.WriteLine(input);
        file.Flush();
    }

}

class Program
{

    static void Main(string[] args)
    {

        nasa_img apot = new nasa_img

        {
            ApiKey = "75hJbORv7Rf4t3mPHsLXLlU8jeuLVT3az075xiSk",
            Date = DateTime.Now,
            HD = true,
            AllowOnlyPicture = true
        };

        if (!apot.TodayWallSet())
        {
            apot.ChangeWallpaper(apot.GetTodayPath());
        }
        else
        {
            apot.default_logger.Log("[*][" + DateTime.Now.ToString() + "] Attempting to change => No need");
        }

    }
}
