using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

using Newtonsoft.Json;
using RestSharp;
using F23.StringSimilarity;

using EmojiBot.Models;
using EmojiBot.Models.YouTube;
using EmojiBot.Managers;

using Ditch;
using Ditch.Operations.Get;
using Ditch.Errors;
using Ditch.JsonRpc;
using System.IO;

namespace EmojiBot.Services
{
    public class VideoAnalyserService
    {
        private readonly OperationManager _steemClient;
        private readonly ConfigurationManager _configurationManager;

        public VideoAnalyserService(OperationManager steemClient, ConfigurationManager configurationManager)
        {
            _steemClient = steemClient;
            _configurationManager = configurationManager;
        }

        public async Task<List<string>> AnalyzeFromDiscordUserMessage(string url)
        {
            var output = new List<string>();
            if(string.IsNullOrWhiteSpace(url))
                return output;

            if (url.StartsWith("https://d.tube/"))
                output.Add(await AnalyzeFromDtubeUrl(url));
            else if(url.StartsWith("https://steemit.com/"))
                output.Add(await AnalyzeFromSteemitUrl(url));

            return output;
        }

        public async Task<List<string>> AnalyzeFromDiscordBotMessage(string videoMessages)
        {
            var output = new List<string>();
            string[] messages = videoMessages.Split('\n', StringSplitOptions.RemoveEmptyEntries);
            // s'il y a au mieux 2 lignes et que le nombre de lignes est un nombre pair
            if (messages.Length < 2 || (messages.Length % 2 != 0))
            {
                output.Add("Erreur, le nombre de ligne dans le message bot doit être pair");
                return output;
            }

            for (int i = 0; i < messages.Length; i += 2)
            {
                //string title = messages[i]; //1ère ligne
                string url = messages[i + 1]; //2e ligne
                output.Add(await AnalyzeFromDtubeUrl(url));
            }
            return output;
        }

        public async Task<string> AnalyzeFromDtubeUrl(string dtubeUrl)
        {
            if (!dtubeUrl.StartsWith("https://d.tube/#!/v/"))
                return "Erreur le lien n'est pas un lien dtube";

            string[] parts = dtubeUrl.Split('/', StringSplitOptions.RemoveEmptyEntries);
            if (parts.Length != 6)
                return "Erreur récupération author et permLink dans lien url dtube";

            string author = parts[parts.Length - 2];
            string permLink = parts[parts.Length - 1];

            return await AnalyzeFromSteemAuthorAndPermLink(author, permLink);
        }

        public async Task<string> AnalyzeFromSteemitUrl(string steemitUrl)
        {
            if (!steemitUrl.StartsWith("https://steemit.com/") || !steemitUrl.Contains("@"))
                return "Erreur le lien n'est pas un lien steemit";

            string[] parts = steemitUrl.Split('/', StringSplitOptions.RemoveEmptyEntries);
            if (parts.Length != 5)
                return "Erreur récupération author et permLink dans lien url steemit";

            string author = parts[parts.Length - 2];
            author = author.Substring(1, author.Length - 1);
            string permLink = parts[parts.Length - 1];

            return await AnalyzeFromSteemAuthorAndPermLink(author, permLink);
        }

        public async Task<string> AnalyzeFromSteemAuthorAndPermLink(string author, string permLink)
        {
            JsonRpcResponse<Discussion> response = _steemClient.GetContent(author, permLink);
            if (response.IsError)
            {
                ErrorInfo error = response.Error;
                return $"Erreur récupération steem content avec {author}/{permLink}";
            }

            // recup info dans steem dans le champ metadata et champ video, montant estimé gagné, etc ...
            Discussion result = response.Result;
            object reputation = result.AuthorReputation;
            Money promoted = result.Promoted;
            DtubeRoot jsonMetaData = JsonConvert.DeserializeObject<DtubeRoot>(result.JsonMetadata);

            if(jsonMetaData == null)
                return "jsonMetaData est vide";
            if(jsonMetaData.video == null)
                return "jsonMetaData?.video est vide";

            string steemTitle = jsonMetaData.video.info.title;
            string steemDescription = jsonMetaData.video.content.description;
            double steemDuration = jsonMetaData.video.info.duration??-1;
            
            Money curatorPayoutValue = result.CuratorPayoutValue;
            object authorRewards = result.AuthorRewards;
            Money pendingPayoutValue = result.PendingPayoutValue;
            Money totalPayoutValue = result.TotalPayoutValue;

            string youTubeInfo = await AnalyzeFromYouTubeSearchAPI(steemTitle, steemDescription, steemDuration, author);
            return youTubeInfo + ";" + totalPayoutValue.Value;

            /*string steemShortDescription = steemDescription.Length > 500 ? steemDescription.Substring(0, 500) + "..." : steemDescription;
            string steemInfo = $@"Informations provenant de steem avec https://d.tube/#!/v/{author}/{permLink} :
Titre : {steemTitle}
Durée vidéo : {steemDuration}";

            return $@"=============================
>{steemInfo}
=============================
>{youTubeInfo}
=============================";*/
        }

        public async Task<string> AnalyzeFromYouTubeSearchAPI(string steemTitle, string steemDescription, double steemDuration, string steemAuthor)
        {
            var client = new RestClient("https://www.googleapis.com/youtube/v3/");
            
            var request = new RestRequest("search", Method.GET);
            request.AddQueryParameter("part", "snippet");
            request.AddQueryParameter("q", steemTitle);
            request.AddQueryParameter("type", "video");
            request.AddQueryParameter("maxResults", "1");
            request.AddQueryParameter("fields", "items(snippet(publishedAt,title,description,channelTitle,channelId),id(videoId))");
            request.AddQueryParameter("key", _configurationManager.YouTubeApiKey);

            IRestResponse response = await client.ExecuteTaskAsync(request);

            YouTubeRoot resp = JsonConvert.DeserializeObject<YouTubeRoot>(response.Content);
            if(resp == null || resp.Items.Length == 0)
                return "-1;-1;-1";

            YouTubeSnippet video = resp.Items[0].Snippet;
            YouTubeId id = resp.Items[0].Id;

            string videoUrl = "https://www.youtube.com/watch?v=" + id.VideoId;
            string channelUrl = "https://www.youtube.com/channel/" + video.ChannelId;

            // similitudes
            var jw = new JaroWinkler();
            string distanceTitle = FormatScore(jw.Similarity(steemTitle, video.Title));
            string distanceDescription = FormatScore(jw.Similarity(steemDescription, video.Description));
            string distanceAuthor = FormatScore(jw.Similarity(steemAuthor, video.ChannelTitle));

            return $"{distanceTitle};{distanceDescription};{distanceAuthor}";

            // todo vérification date publication du jour, nb de vue basse, duration, date création compte ... pseudo identique

            /*string youtubeShortDescription = video.Description.Length > 500 ? video.Description.Substring(0, 500) + "..." : video.Description;

            return $@"Information provenant de Youtube (1ère video youtube trouvée) avec le titre provenant de steem :
VideoUrl : {videoUrl}
Titre : {video.Title}
Publié le : {video.PublishedAt}
Auteur : {video.ChannelTitle}
=============================
Similitude Titre : {distanceTitle}
Similitude Description : {distanceDescription}
Similitude Auteur : {distanceAuthor}";*/
        }

        private string FormatScore(double score)
        {
            string percent = score.ToString();
            if(percent.Length > 6)
                percent = percent.Substring(0, 6);
            return percent;
        }
    }
}