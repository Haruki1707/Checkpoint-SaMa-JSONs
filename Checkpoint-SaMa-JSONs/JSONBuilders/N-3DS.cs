using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

namespace Checkpoint_SaMa_JSONs.JSONBuilders
{
    internal class N_3DS
    {
        public static event GenerateJSONfinishEvent? GenerationFinished;
        public delegate void GenerateJSONfinishEvent(object? sender, GeneratedJSONEventArgs args);
        protected static void OnGenerationFinishes(string json, string md5)
        {
            GenerationFinished?.Invoke(null, new GeneratedJSONEventArgs(json, md5));
        }

        private static int NTotal = 0;
        private static int NActual = 0;
        private static List<CheckpointWiiU_3DSGame> Games = new List<CheckpointWiiU_3DSGame>();
        private static string imgfolder = "";
        private static string imagesFolder => $"{imgfolder}\\img\\3DS\\";

        public static async Task<string?> GenerateCheckpointJSON(string file, GenerateJSONfinishEvent? eventg = null)
        {
            imgfolder = Path.GetDirectoryName(file);
            if (await N_WiiU.gameDBApiFetch() == false)
                return null;
            await Convert3DS();
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

        private static async Task Convert3DS()
        {
            if (!Directory.Exists(imagesFolder))
                Directory.CreateDirectory(imagesFolder);

            foreach (var item in N_WiiU.APIresponse)
                if (item.Key.StartsWith(N_WiiU._3DSCode))
                    NTotal++;

            /*foreach (var gameid in N_WiiU.APIresponse)
                if (gameid.Key.StartsWith(N_WiiU._3DSCode))
                {
                    await AddToGames(gameid);
                }*/

            List<Task> tasks = new List<Task>();
            foreach (var gameid in N_WiiU.APIresponse)
                if (gameid.Key.StartsWith(N_WiiU._3DSCode))
                    tasks.Add(AddToGames(gameid));

            while (tasks.Any())
            {
                var task = await Task.WhenAny(tasks);
                tasks.Remove(task);
            }
        }

        private static async Task AddToGames(KeyValuePair<string, JToken?> gameid)
        {
            Games.Add(await N_WiiU.wiiu_3dsJSONconvertToCheckpoint(gameid, N_WiiU._3DSCode, imagesFolder));
            OnGenerationProgress();
        }

        protected static void OnGenerationProgress()
        {
            NActual++;
            System.Diagnostics.Trace.WriteLine($"3DS: {NActual}/{NTotal}");
        }
    }
}
