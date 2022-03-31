using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using RestSharp;
using System;
using System.Collections.Generic;
using System.Drawing.Imaging;
using System.IO;
using System.Linq;
using System.Net;
using System.Threading.Tasks;

namespace Checkpoint_SaMa_JSONs.JSONBuilders
{
    struct CheckpointWiiU_3DSGame
    {
        public string id { get; set; }
        public string title { get; set; }
    }

    internal class N_WiiU
    {
        public static event GenerateJSONfinishEvent? GenerationFinished;
        public delegate void GenerateJSONfinishEvent(object? sender, GeneratedJSONEventArgs args);
        protected static void OnGenerationFinishes(string json, string md5)
        {
            GenerationFinished?.Invoke(null, new GeneratedJSONEventArgs(json, md5));
        }

        static readonly RestClient client = new RestClient("https://dantheman827.github.io/nus-info");
        internal static JObject APIresponse;
        private static bool AlreadyFetch = false;
        private static int NTotal = 0;
        private static int NActual = 0;
        private static List<CheckpointWiiU_3DSGame> Games = new List<CheckpointWiiU_3DSGame>();
        private static string WiiUCode = "00050000";
        internal static string _3DSCode = "00040000";
        private static string imgfolder = "";
        private static string imagesFolder => $"{imgfolder}\\img\\WiiU\\";

        public static async Task<string?> GenerateCheckpointJSON(string file, GenerateJSONfinishEvent? eventg = null)
        {
            imgfolder = Path.GetDirectoryName(file);
            if (await gameDBApiFetch() == false)
                return null;
            await ConvertWiiU();

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

        internal static async Task<bool> gameDBApiFetch()
        {
            if (AlreadyFetch)
                return true;

            var response = await client.ExecuteAsync(new RestRequest("/title-names.json", Method.Get));

            if (!response.IsSuccessful || response.StatusCode == HttpStatusCode.NotFound || response.Content == null)
                return AlreadyFetch = false;

            APIresponse = JObject.Parse(response.Content);

            return AlreadyFetch = true;
        }

        private static async Task ConvertWiiU()
        {
            if (!Directory.Exists(imagesFolder))
                Directory.CreateDirectory(imagesFolder);

            foreach (var item in N_WiiU.APIresponse)
                if (item.Key.StartsWith(WiiUCode))
                    NTotal++;

            foreach (var gameid in N_WiiU.APIresponse)
                if (gameid.Key.StartsWith(N_WiiU.WiiUCode))
                    await AddToGames(gameid);

            /*List<Task> tasks = new List<Task>();
            foreach (var gameid in APIresponse)
                if (gameid.Key.StartsWith(WiiUCode))
                    tasks.Add(AddToGames(gameid));

            while (tasks.Any())
            {
                var task = await Task.WhenAny(tasks);
                tasks.Remove(task);
            }*/
        }

        private static async Task AddToGames(KeyValuePair<string, JToken?> gameid)
        {
            Games.Add(await wiiu_3dsJSONconvertToCheckpoint(gameid, WiiUCode, imagesFolder));
            OnGenerationProgress();
        }

        protected static void OnGenerationProgress()
        {
            NActual++;
            System.Diagnostics.Trace.WriteLine($"WiiU: {NActual}/{NTotal}");
        }

        internal static async Task<CheckpointWiiU_3DSGame> wiiu_3dsJSONconvertToCheckpoint(KeyValuePair<string, JToken?> gameid, string Concode, string imagesFolder)
        {
            string code = WiiUCode;
            if (Concode == _3DSCode)
                code = _3DSCode;

            string? finalTitle = null;
            string? tempRegion = null;
            string gameidkey = gameid.Key.Replace(code, "").ToLower();
            string[] RegionPriority = { "US", "MX", "ES", "BE", "AU", "AT", "JP", "RU", "GB", "BG", "DE", "DK" };

            JToken titles = JsonConvert.DeserializeObject<JToken>(gameid.Value.ToString());
            foreach (var region in RegionPriority)
                try
                {
                    if (!String.IsNullOrWhiteSpace(titles[region].ToString()))
                    {
                        tempRegion = region;
                        finalTitle = titles[region].ToString();
                        break;
                    }
                }
                catch (Exception) { }
            /*JObject titlesJObject = JObject.Parse(gameid.Value.ToString());
            foreach (var title in titlesJObject)
            {
                if (RegionPriority.Contains(title.Key))
                {
                    if (tempRegion != null && Array.IndexOf(RegionPriority, title.Key) > Array.IndexOf(RegionPriority, tempRegion))
                        continue;
                    finalTitle = title.Value.ToString();
                    tempRegion = title.Key;
                }
            }*/

            string image = imagesFolder + $"{gameidkey}.png";
            if (!File.Exists(image) && tempRegion != null)
            {
                var imageresponse = await client.ExecuteAsync(new RestRequest($"/titles/{gameid.Key.ToLower()}-{tempRegion.ToLower()}.json"), Method.Get);

                if (imageresponse.IsSuccessful && imageresponse.StatusCode != HttpStatusCode.NotFound && imageresponse.Content != null)
                {
                    var titlejson = JsonConvert.DeserializeObject<JToken>(imageresponse.Content);
                    if (!String.IsNullOrWhiteSpace(titlejson["icon_url"].ToString()))
                        await utils.SaveImage(titlejson["icon_url"].ToString(), image, ImageFormat.Png);
                }
            }

            return new CheckpointWiiU_3DSGame()
            {
                id = gameidkey,
                title = finalTitle,
            };
        }
    }
}
