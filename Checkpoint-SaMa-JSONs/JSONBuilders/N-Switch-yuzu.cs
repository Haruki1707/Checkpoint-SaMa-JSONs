using Newtonsoft.Json;
using RestSharp;
using System.Collections.Generic;
using System.Drawing.Imaging;
using System.IO;
using System.Threading.Tasks;

namespace Checkpoint_SaMa_JSONs.JSONBuilders
{
    public class GeneratedJSONEventArgs
    {
        public GeneratedJSONEventArgs(string Json, string md5) { json = Json; MD5 = md5; }
        public string json { get; set; }
        public string MD5 { get; set; }
    }

    struct YuzuGame
    {
        public string id { get; set; }
        public string directory { get; set; }
        public string title { get; set; }
        public int compatibilty { get; set; }

        public YuzuGameRelease[] releases { get; set; }
    }

    struct YuzuGameRelease
    {
        public string id { get; set; }
        public string region { get; set; }
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

        static readonly RestClient client = new RestClient("https://api.yuzu-emu.org");
        private static bool AlreadyFetch = false;
        private static List<YuzuGame>? Games = new List<YuzuGame>();
        private static string imgfolder = "";
        private static string imagesFolder(string img = "") => $"{imgfolder}\\img\\Switch\\{img}";

        public static async Task<string?> GenerateCheckpointJSON(string file, GenerateJSONfinishEvent? eventg = null)
        {
            imgfolder = Path.GetDirectoryName(file);
            if (await gameDBApiFetch() == false)
                return null;

            string json = JsonConvert.SerializeObject(await switchJSONconvertToCheckpoint(), Formatting.None);

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

            var response = await client.ExecuteAsync(new RestRequest("/gamedb", Method.Get));

            if (!response.IsSuccessful || response.StatusCode == System.Net.HttpStatusCode.NotFound || response.Content == null)
                return false;

            Games = JsonConvert.DeserializeObject<List<YuzuGame>>(response.Content);
            return true;
        }

        private static async Task<List<CheckpointSwitchGame>> switchJSONconvertToCheckpoint()
        {
            List<CheckpointSwitchGame> list = new List<CheckpointSwitchGame>();

            if (await gameDBApiFetch())
            {
                if (!Directory.Exists(imagesFolder()))
                    Directory.CreateDirectory(imagesFolder());

                if (Games != null)
                    foreach (var game in Games)
                        foreach (var gameRelease in game.releases)
                        {
                            list.Add(new CheckpointSwitchGame()
                            {
                                id = gameRelease.id,
                                title = game.title
                            });
                            string image = imagesFolder($"{gameRelease.id}.png");
                            if (!File.Exists(image))
                                utils.SaveImage($"https://tinfoil.media/ti/{gameRelease.id}/128/128", image, ImageFormat.Png);
                        }
            }

            return list;
        }
    }
}
