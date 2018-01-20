using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

using Newtonsoft.Json;
using RestSharp;

using EmojiBot.Models;
using EmojiBot.Models.YouTube;
using EmojiBot.Managers;

using Ditch;
using Ditch.Operations.Get;
using Ditch.Errors;
using Ditch.JsonRpc;

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

        public async Task<string> AnalyzeDiscordContent(string discordContent)
        {
            string[] messages = discordContent.Split('\n', StringSplitOptions.RemoveEmptyEntries);
            // s'il y a au mieux 2 lignes et que le nombre de lignes est un nombre pair
            if (messages.Length < 2 || (messages.Length % 2 != 0))
                return "Erreur, le nombre de ligne doit être pair";

            var output = new StringBuilder();
            for (int i = 0; i < messages.Length; i += 2)
            {
                //string title = messages[i]; //1ère ligne
                string url = messages[i + 1]; //2e ligne

                string result = await AnalyzeDtubeUrl(url);
                output.Append(result);
                output.AppendLine();                
            }
            return output.ToString();
        }

        public async Task<string> AnalyzeDtubeUrl(string url)
        {
            if (!url.StartsWith("https://d.tube/#!/v/"))
                return "Erreur le lien n'est pas un lien dtube";

            string[] parts = url.Split('/', StringSplitOptions.RemoveEmptyEntries);
            if (parts.Length != 6)
                return "Erreur récupération author et permLink dans lien url dtube";

            string author = parts[parts.Length - 2];
            string permLink = parts[parts.Length - 1];

            return await AnalyzeSteemContent(author, permLink);
        }

        public async Task<string> AnalyzeSteemContent(string author, string permLink)
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
            string title = jsonMetaData.video.info.title;
            string description = jsonMetaData.video.content.description;
            double duration = jsonMetaData.video.info.duration;

            string steemInfo = $@"Informations provenant de steem avec https://d.tube/#!/v/{author}/{permLink} :
{title}
{description}
{duration}";

            string youTubeInfo = await GetInfoFromYouTube(title, description, duration);

            return $@"{steemInfo}

{youTubeInfo}";
        }

        public async Task<string> GetInfoFromYouTube(string title, string description, double duration)
        {
            var client = new RestClient("https://www.googleapis.com/youtube/v3/");
            
            var request = new RestRequest("search", Method.GET);
            request.AddQueryParameter("part", "snippet");
            request.AddQueryParameter("q", title);
            request.AddQueryParameter("type", "video");
            request.AddQueryParameter("maxResults", "1");
            request.AddQueryParameter("fields", "items(snippet(publishedAt,title,description))");
            request.AddQueryParameter("key", _configurationManager.YouTubeApiKey);

            IRestResponse response = await client.ExecuteTaskAsync(request);

            YouTubeRoot resp = JsonConvert.DeserializeObject<YouTubeRoot>(response.Content);
            
            //todo match title, duration and description


            //todo vérification date publication du jour, nb de vue basse... pseudo identique
            var distanceTitle = "";

            return $@"Information provenant de Youtube (1ère video youtube trouvée) avec le titre provenant de steem) :
Titre : {resp.Items[0].Snippet.Title}
Similitude Title avec Titre steem : {distanceTitle}
Publié le {resp.Items[0].Snippet.PublishedAt}
Url : TODO
Description {resp.Items[0].Snippet.Description}";
        }
    }
}