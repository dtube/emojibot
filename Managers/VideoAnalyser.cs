using System;
using System.Collections.Generic;

using Ditch;
using Newtonsoft.Json;
using RestSharp;

using EmojiBot.Models;
using EmojiBot.Models.YouTube;

namespace EmojiBot.Managers
{
    public static class VideoAnalyser
    {
        private static readonly OperationManager _operationManager = new OperationManager();

        static VideoAnalyser()
        {
            var steem = new List<string>
            {
            "wss://steemd.steemit.com"
            }; //"https://api.steemit.com"
            string urlSteem = _operationManager.TryConnectTo(steem);
            Console.WriteLine($"Connected to {urlSteem}");
        }

        public static void AnalyzeDiscordContent(string discordContent)
        {
            string[] messages = discordContent.Split('\n', StringSplitOptions.RemoveEmptyEntries);
            // s'il y a au mieux 2 lignes et que le nombre de lignes est un nombre pair
            if (messages.Length < 2 || (messages.Length % 2 != 0))
                return;

            for (int i = 0; i < messages.Length; i += 2)
            {
                //string title = messages[i]; //1ère ligne
                string url = messages[i + 1]; //2e ligne

                AnalyzeDtubeUrl(url);
            }
        }

        public static void AnalyzeDtubeUrl(string url)
        {
            if (!url.StartsWith("https://d.tube/#!/v/"))
                return;

            string[] parts = url.Split('/', StringSplitOptions.RemoveEmptyEntries);
            if (parts.Length != 6)
                return;

            string author = parts[parts.Length - 2];
            string permLink = parts[parts.Length - 1];

            AnalyzeSteemContent(author, permLink);
        }

        public static void AnalyzeSteemContent(string author, string permLink)
        {
            var response = _operationManager.GetContent(author, permLink);
            if (response.IsError)
            {
                var error = response.Error;
                return;
            }

            // recup info dans steem dans le champ metadata et champ video, montant estimé gagné, etc ...
            var result = response.Result;
            var reputation = result.AuthorReputation;
            Money promoted = result.Promoted;
            DtubeRoot jsonMetaData = JsonConvert.DeserializeObject<DtubeRoot>(result.JsonMetadata);
            string title = jsonMetaData.video.info.title;
            string description = jsonMetaData.video.content.description;
            double duration = jsonMetaData.video.info.duration;

            double findInYouTube = ScoreFindInYouTube(title, description, duration);
        }

        public static double ScoreFindInYouTube(string title, string description, double duration)
        {
            var client = new RestClient("https://www.googleapis.com/youtube/v3/");
            
            var request = new RestRequest("search", Method.GET);
            request.AddQueryParameter("part", "snippet");
            request.AddQueryParameter("q", title);
            request.AddQueryParameter("type", "video");
            request.AddQueryParameter("maxResults", "1");
            request.AddQueryParameter("fields", "items(snippet(publishedAt,title,description))");
            request.AddQueryParameter("key", ConfigurationManager.YouTubeApiKey);

            IRestResponse response = client.Execute(request);

            YouTubeRoot resp = JsonConvert.DeserializeObject<YouTubeRoot>(response.Content);
            
            //todo match title, duration and description


            //todo vérification date publication du jour, nb de vue basse... pseudo identique
            

            return 1d;
        }
    }
}