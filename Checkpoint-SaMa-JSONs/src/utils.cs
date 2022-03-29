using Microsoft.Toolkit.Uwp.Notifications;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Net;
using System.Reflection;
using System.Security.Cryptography;
using System.Threading.Tasks;

namespace Checkpoint_SaMa_JSONs
{
    internal static class utils
    {
        public class JSONBuilderDate
        {
            public string Builder { get; set; }
            public DateTime Date { get; set; }
        }

        internal static string CalculateMD5(string filename)
        {
            if (!File.Exists(filename))
                return "";

            using (var md5 = MD5.Create())
            {
                using (var stream = File.OpenRead(filename))
                {
                    var hash = md5.ComputeHash(stream);
                    return BitConverter.ToString(hash).Replace("-", "").ToLowerInvariant();
                }
            }
        }

        internal static void SendToast(string text1, string text2, string image, int conversationID = 01)
        {
            new ToastContentBuilder()
                    .AddArgument("action", "viewConversation")
                    .AddArgument("conversationId", conversationID)
                    .AddText(text1)
                    .AddText(text2)
                    .AddAppLogoOverride(new Uri($"file://{Directory.GetCurrentDirectory()}/Resources/{image}"))
                    .Show(toast =>
                    {
                        toast.ExpirationTime = DateTime.Now.AddDays(1);
                    });
        }

        internal static string DownloadMD5(string restrequest)
        {
            try
            {
                using (WebClient client = new())
                {
                    string data = client.DownloadString(new Uri("https://raw.githubusercontent.com/Haruki1707/Checkpoint-SaMa-JSONs/main/src/" + restrequest));
                    System.Diagnostics.Trace.WriteLine(restrequest + " downloaded.");
                    return data.Replace("\n", "");
                }

            }
            catch (Exception e)
            {
                System.Diagnostics.Trace.WriteLine(e.Message + " | " + $"https://raw.githubusercontent.com/Haruki1707/Checkpoint-SaMa-JSONs/main/src/{restrequest}");
                return null;
            }
        }

        internal static void CreateShortcut()
        {
            string CurrentDir = Path.GetDirectoryName(Assembly.GetEntryAssembly().Location) + $"\\{AppDomain.CurrentDomain.FriendlyName}.exe";
            string Name = Path.GetFileNameWithoutExtension(CurrentDir);
            string WD = Path.GetDirectoryName(CurrentDir);
            string shortcutDir = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData) + @"\Microsoft\Windows\Start Menu\Programs\Startup\" + $"{Name}.lnk";

            if (File.Exists(shortcutDir)) return;

            object shDesktop = (object)"Desktop";
            IWshRuntimeLibrary.WshShell shell = new IWshRuntimeLibrary.WshShell();
            string shortcutAddress = shortcutDir;
            IWshRuntimeLibrary.IWshShortcut shortcut = (IWshRuntimeLibrary.IWshShortcut)shell.CreateShortcut(shortcutAddress);
            shortcut.TargetPath = WD + $"\\{Name}.exe";
            shortcut.WorkingDirectory = WD;
            shortcut.Save();
        }

        internal static async Task SaveImage(string imageUrl, string filename, ImageFormat format)
        {
            try
            {
                //Change SSL checks so that all checks pass
                ServicePointManager.ServerCertificateValidationCallback = delegate { return true; };
                using (WebClient client = new())
                {
                    Stream stream = await client.OpenReadTaskAsync(new Uri(imageUrl));
                    Bitmap bitmap; bitmap = new Bitmap(stream);

                    if (bitmap != null)
                        bitmap.Save(filename, format);

                    stream.Flush();
                    stream.Close();
                }

                System.Diagnostics.Trace.WriteLine(imageUrl + " downloaded.");
            }
            catch (Exception e)
            {
                System.Diagnostics.Trace.WriteLine(e.Message + " | " + imageUrl);
            }
        }

        private static string BDjsonFile = "BuilderDates.json";
        internal static async Task<DateTime> DateJSON(string Builder)
        {
            //System.Diagnostics.Trace.WriteLine("HOLA");
            if (!File.Exists(BDjsonFile))
                await GenerateDateJSON();
            else if (JSONDates == null)
                await ReadDateJSON();

            foreach (var date in JSONDates)
                if (date.Builder == Builder)
                    return date.Date;
            return DateTime.Now;
        }

        static List<JSONBuilderDate>? JSONDates = null;
        private static async Task ReadDateJSON()
        {
            string jsonText = await File.ReadAllTextAsync(BDjsonFile);
            JSONDates = JsonConvert.DeserializeObject<List<JSONBuilderDate>>(jsonText);
        }

        internal static async Task NowDateJSON(string Builder)
        {
            foreach (var date in JSONDates)
                if (date.Builder == Builder)
                    date.Date = DateTime.Now;
            string json = JsonConvert.SerializeObject(JSONDates, Formatting.None);
            await File.WriteAllTextAsync(BDjsonFile, json);
        }

        private static async Task GenerateDateJSON()
        {
            var now_8 = DateTime.Now.AddDays(-8);
            List<JSONBuilderDate> dates = new List<JSONBuilderDate>();
            dates.Add(new JSONBuilderDate
            {
                Builder = "wiiu-csm",
                Date = now_8
            });
            dates.Add(new JSONBuilderDate
            {
                Builder = "switch-csm",
                Date = now_8

            });
            dates.Add(new JSONBuilderDate
            {
                Builder = "3ds-csm",
                Date = now_8
            });

            JSONDates = dates;

            string json = JsonConvert.SerializeObject(dates, Formatting.None);
            await File.WriteAllTextAsync(BDjsonFile, json);
        }
    }
}
