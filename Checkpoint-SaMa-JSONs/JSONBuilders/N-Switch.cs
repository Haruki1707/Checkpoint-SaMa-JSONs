using Newtonsoft.Json;
using RestSharp;
using System;
using System.Collections.Generic;
using System.Drawing.Imaging;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace Checkpoint_SaMa_JSONs.JSONBuilders
{
    public class GeneratedJSONEventArgs
    {
        public GeneratedJSONEventArgs(string Json, string md5) { json = Json; MD5 = md5; }
        public string json { get; set; }
        public string MD5 { get; set; }
    }

    public class ProgressedJSONEventArgs
    {
        public ProgressedJSONEventArgs(int NTotal, int NActual) { Total = NTotal; Actual = NActual; }
        public int Total { get; set; }
        public int Actual { get; set; }
    }

    struct TinfoilJSON
    {
        public TinfoilGame[] data { get; set; }
    }

    struct TinfoilGame
    {
        public string id { get; set; }
        public string name { get; set; }
        public string release_date { get; set; }
    }

    struct CheckpointSwitchGame
    {
        public string id { get; set; }
        public string title { get; set; }
    }

    internal class N_Switch
    {
        public static event GenerateJSONfinishEvent? GenerationFinished;
        public delegate void GenerateJSONfinishEvent(object? sender, GeneratedJSONEventArgs args);
        protected static void OnGenerationFinishes(string json, string md5)
        {
            GenerationFinished?.Invoke(null, new GeneratedJSONEventArgs(json, md5));
        }

        static readonly RestClient client = new RestClient("https://tinfoil.media/Title/ApiJson");
        private static bool AlreadyFetch = false;
        private static int NTotal = 0;
        private static int NActual = 0;
        private static List<CheckpointSwitchGame>? Games = new List<CheckpointSwitchGame>();
        private static string imgfolder = "";
        private static string imagesFolder(string img = "") => $"{imgfolder}\\img\\Switch\\{img}";

        public static async Task<string?> GenerateCheckpointJSON(string file, GenerateJSONfinishEvent? eventg = null)
        {
            imgfolder = Path.GetDirectoryName(file);
            if (await gameDBApiFetch() == false)
                return null;

            string json = JsonConvert.SerializeObject(Games, Formatting.None);

            if (eventg != null)
                GenerationFinished += new GenerateJSONfinishEvent(eventg);

            if (json != null)
            {
                string md5 = utils.CalculateMD5(file);
                await File.WriteAllTextAsync(file, json);
                await File.WriteAllTextAsync(file.Replace(".json", ".md5"), md5);
                OnGenerationFinishes(json, md5);
            }
            return json;
        }

        private static async Task<bool> gameDBApiFetch()
        {
            if (AlreadyFetch)
                return true;

            var response = await client.ExecuteAsync(new RestRequest("/?rating_content=&language=&category=&region=us&rating=0", Method.Get));

            if (!response.IsSuccessful || response.StatusCode == System.Net.HttpStatusCode.NotFound || response.Content == null)
                return false;

            if (!Directory.Exists(imagesFolder()))
                Directory.CreateDirectory(imagesFolder());

            var json = JsonConvert.DeserializeObject<TinfoilJSON>(response.Content);
            NTotal = json.data.Length;
            List<Task> tasks = new List<Task>();
            foreach (var game in json.data)
                tasks.Add(AddToGames(game));

            while (tasks.Any())
            {
                var task = await Task.WhenAny(tasks);
                tasks.Remove(task);
            }

            return AlreadyFetch = true;
        }

        private static async Task AddToGames(TinfoilGame game)
        {
            var SwitchGame = new CheckpointSwitchGame()
            {
                id = game.id,
                title = StripHTML(game.name)
            };

            if (Games.Contains(SwitchGame))
                return;

            Games.Add(SwitchGame);
            string image = imagesFolder($"{game.id}.png");
            if (!File.Exists(image) && !String.IsNullOrEmpty(game.release_date))
                await utils.SaveImage($"https://tinfoil.media/ti/{game.id}/128/128", image, ImageFormat.Png);
            OnGenerationProgress();
        }

        protected static void OnGenerationProgress()
        {
            NActual++;
            System.Diagnostics.Trace.WriteLine($"Switch: {NActual}/{NTotal}");
        }

        public static string StripHTML(string input)
        {
            return Regex.Replace(input, "<.*?>", String.Empty).Replace("&#39;", "'");
        }
    }
}
