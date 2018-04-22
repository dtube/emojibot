using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
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

namespace EmojiBot.Services
{
    public class VideoAnalyserService
    {
        private readonly OperationManager _steemClient;
        private readonly ConfigurationManager _configurationManager;

        private readonly Dictionary<string, DtubeVideoDTO> _dicoVote = new Dictionary<string, DtubeVideoDTO>();

        private class DtubeVideoDTO
        {
            public string Url { get; set; }

            public DateTime CreationDateTime { get; set; }

            public DateTime VoteDateTime { get; set; }

            public int NbUpVote { get; set; }

            public int NbDownVote { get; set; }

            public string ErrorMessage { get; set; }
        }

        public VideoAnalyserService(OperationManager steemClient, ConfigurationManager configurationManager)
        {
            _steemClient = steemClient;
            _configurationManager = configurationManager;

            Task.Run(() => SteemVote());
        }

        private void SteemVote()
        {
            while(true)
            {
                // 10 secondes
                System.Threading.Thread.Sleep(10 * 1000);

                DateTime now = DateTime.UtcNow;
                foreach (var kvp in _dicoVote.Where(d => (now - d.Value.VoteDateTime).TotalSeconds >= 0).ToList())
                {
                    DtubeVideoDTO steemVideoDTO = kvp.Value;

                    if(steemVideoDTO.NbUpVote > 0 && steemVideoDTO.NbDownVote == 0)
                    {
                        // TODO voter à 100%
                        DiscordMessageService.SendMessageAsync($"[VOTE 100% {steemVideoDTO.Url}]").Wait(5000);

                        //_configurationManager.SteemCuratorId;
                    }
                    else if(steemVideoDTO.NbDownVote > 0 && steemVideoDTO.NbUpVote == 0)
                    {
                        // TODO
                        // calculer le % de vote à effectuer pour faire arriver à 0$ la video
                        // par rapport à la valeur en $ de la vidéo actuelle et le % de powervote de dtube
                        DiscordMessageService.SendMessageAsync($"[DOWNVOTE ?% {steemVideoDTO.Url}]").Wait(5000);
                    }

                    _dicoVote.Remove(kvp.Key);
                }
            }
        }

        public async Task<string> AnalyzeFromDiscordUserMessage(string videoMessage)
        {
            if(string.IsNullOrWhiteSpace(videoMessage))
                return "Error: Message is empty";

            string url = null;
            string warnings = "";
            bool isVote = false;
            if(videoMessage.StartsWith("!vote "))
            {
                url = videoMessage.Replace("!vote ", string.Empty);
                isVote = true;
                await Console.Out.WriteLineAsync("Discord receive vote for : " + url);
            }
            else if(videoMessage.StartsWith("!downvote "))
            {
                url = videoMessage.Replace("!downvote ", string.Empty);
                isVote = false;
                await Console.Out.WriteLineAsync("Discord receive downvote for : " + url);
            }
            else
                return "Error: Unknown Command";

            if(string.IsNullOrWhiteSpace(url))
                return "Error: No url found";

            Tuple<string, string> tuple = null;
            if (url.StartsWith("https://d.tube/"))
                tuple = ExtractAuthorAndPermLinkFromDtubeUrl(url);
            else if(url.StartsWith("https://steemit.com/"))
                tuple = ExtractAuthorAndPermLinkFromSteemitUrl(url);
            
            if(tuple == null)
                return "Error: URL needs to start with either d.tube or steemit.com";
            
            SteemDTO steemInfo = GetInfoFromSteem(tuple.Item1, tuple.Item2);
            if(!steemInfo.Success)
                return "Error: Could not fetch STEEM content";
            YoutubeDTO youtubeInfo = await GetInfoFromYouTubeSearchAPI(steemInfo.Title, steemInfo.Description, steemInfo.Duration, steemInfo.Author);
            if(!youtubeInfo.Success) {
                await Console.Out.WriteLineAsync("Error fetching YT for: " + url);
                warnings += "\nWarning! YT error";
                //return "Error: Could not fetch YT Content";
                //je prefere que ca ne bloque pas dans ce cas, je met un systeme de warning
            }
                

            // si plaggiat
            if((youtubeInfo.DistanceTitle + youtubeInfo.DistanceDescription) > 1.2 && (DateTime.Now - youtubeInfo.PublishedAt).TotalDays > 8)
            {
                return "**PLAGIARISM DETECTED**, there will be no vote. " + youtubeInfo.VideoUrl;
            }

            // si suspection de plaggiat
            if((youtubeInfo.DistanceTitle + youtubeInfo.DistanceDescription) > 0.6 && (DateTime.Now - youtubeInfo.PublishedAt).TotalDays > 1)
            {
                warnings += "\nWarning! Possible plagiarism: ";
                warnings += youtubeInfo.VideoUrl;
            }

            DateTime creationDate = DateTime.SpecifyKind(steemInfo.Created, DateTimeKind.Utc);

            if(!_dicoVote.ContainsKey(url))
            {
                DateTime afterVote = DateTime.UtcNow.AddMinutes(5);
                DateTime afterCreationVideo = creationDate.AddMinutes(30);
                DateTime voteDateTime = afterCreationVideo > afterVote && isVote ? afterCreationVideo : afterVote; //on prend le max des 2
                _dicoVote.Add(url, new DtubeVideoDTO
                { 
                    Url = url, 
                    CreationDateTime = creationDate,
                    VoteDateTime = voteDateTime
                });
            }

            DtubeVideoDTO dtubeVideoDTO = _dicoVote[url];
            TimeSpan voteTimeSpan = (dtubeVideoDTO.VoteDateTime - DateTime.Now.ToUniversalTime());
            if(isVote)
                dtubeVideoDTO.NbUpVote++;
            else
                dtubeVideoDTO.NbDownVote++;

            if(dtubeVideoDTO.NbDownVote > 0 && dtubeVideoDTO.NbUpVote == 0)
                return $"Downvote in " 
                    + Math.Round(voteTimeSpan.TotalMinutes) + "mins"
                    + warnings;
            if(dtubeVideoDTO.NbUpVote > 0 && dtubeVideoDTO.NbDownVote == 0)
                return $"Vote in " 
                    + Math.Round(voteTimeSpan.TotalMinutes) + "mins"
                    + warnings;

            _dicoVote.Remove(url);
            return "**Curator disagreament**. There will be no vote";
        }

        private Tuple<string, string> ExtractAuthorAndPermLinkFromDtubeUrl(string dtubeUrl)
        {
            if (!dtubeUrl.StartsWith("https://d.tube/#!/v/"))
                return null;

            string[] parts = dtubeUrl.Split('/', StringSplitOptions.RemoveEmptyEntries);
            if (parts.Length != 6)
                return null;

            string author = parts[parts.Length - 2];
            string permLink = parts[parts.Length - 1];

            return new Tuple<string, string>(author, permLink);
        }

        private Tuple<string, string> ExtractAuthorAndPermLinkFromSteemitUrl(string steemitUrl)
        {
            if (!steemitUrl.StartsWith("https://steemit.com/") || !steemitUrl.Contains("@"))
                return null;

            string[] parts = steemitUrl.Split('/', StringSplitOptions.RemoveEmptyEntries);
            if (parts.Length != 5)
                return null;

            string author = parts[parts.Length - 2];
            author = author.Substring(1, author.Length - 1);
            string permLink = parts[parts.Length - 1];

            return new Tuple<string, string>(author, permLink);
        }

        private SteemDTO GetInfoFromSteem(string author, string permLink)
        {
            JsonRpcResponse<Discussion> response = _steemClient.GetContent(author, permLink);
            if (response.IsError)
            {
                ErrorInfo error = response.Error;
                return new SteemDTO{ ErrorMessage = $"Erreur récupération steem content avec {author}/{permLink} : {error}" };
            }

            // recup info dans steem dans le champ metadata et champ video, montant estimé gagné, etc ...
            Discussion result = response.Result;
            object reputation = result.AuthorReputation;
            Money promoted = result.Promoted;
            DtubeRoot jsonMetaData = JsonConvert.DeserializeObject<DtubeRoot>(result.JsonMetadata);

            if(jsonMetaData == null)
                return new SteemDTO{ ErrorMessage = $"jsonMetaData est vide" };
            if(jsonMetaData.video == null)
                return new SteemDTO{ ErrorMessage = "jsonMetaData.video est vide" };

            string steemTitle = jsonMetaData.video.info.title;
            string steemDescription = jsonMetaData.video.content.description;
            double steemDuration = jsonMetaData.video.info.duration??-1;
            
            //Money curatorPayoutValue = result.CuratorPayoutValue;
            //object authorRewards = result.AuthorRewards;
            //Money pendingPayoutValue = result.PendingPayoutValue;
            Money totalPayoutValue = result.TotalPayoutValue;

            return new SteemDTO
            {
                Success = true,
                Created = result.Created,
                Title = steemTitle,
                Description = steemDescription,
                Duration = steemDuration,
                Author = author,
                PermLink = permLink,
                TotalPayout = totalPayoutValue.Value
            };
        }

        private class SteemDTO
        {
            public bool Success { get; set; }

            public DateTime Created { get; set; }

            public string Title { get; set; }

            public string Description { get; set; }

            public string ShortDescription => Description.Length > 500 ? Description.Substring(0, 500) + "..." : Description;

            public double Duration { get; set; }

            public string Author { get; set; }

            public string PermLink { get; set; }

            public string DtubeUrl => $"https://d.tube/#!/v/{Author}/{PermLink}";

            public long TotalPayout { get; set; }

            public string ErrorMessage { get; set; }

            public string GetScoreInfo()
            {
                return TotalPayout.ToString();
            }

            public string GetFullInfo()
            {
                return $@"Informations provenant de steem avec {DtubeUrl} :
Titre : {Title}
Durée vidéo : {Duration}";
            }
        }

        private async Task<YoutubeDTO> GetInfoFromYouTubeSearchAPI(string steemTitle, string steemDescription, double steemDuration, string steemAuthor)
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
                return new YoutubeDTO { ErrorMessage = "pas de réponse de YouTube" };

            YouTubeSnippet video = resp.Items[0].Snippet;
            YouTubeId id = resp.Items[0].Id;

            // similitudes
            var jw = new JaroWinkler();
            double distanceTitle = FormatScore(jw.Similarity(steemTitle, video.Title));
            double distanceDescription = FormatScore(jw.Similarity(steemDescription, video.Description));
            double distanceAuthor = FormatScore(jw.Similarity(steemAuthor, video.ChannelTitle));

            var dto = new YoutubeDTO
            {
                Success = true,
                VideoId = id.VideoId,
                VideoTitle = video.Title,
                ChannelId = video.ChannelId,
                ChannelTitle = video.ChannelTitle,
                PublishedAt = video.PublishedAt,
                DistanceTitle = distanceTitle,
                DistanceDescription = distanceDescription,
                DistanceAuthor = distanceAuthor
            };

            return dto;
        }

        private class YoutubeDTO
        {
            public bool Success { get; set; }

            public string VideoId { get; set; }

            public string VideoUrl => "https://www.youtube.com/watch?v=" + VideoId;

            public string VideoTitle { get; set; }

            public string ChannelId { get; set; }

            public string ChannelUrl => "https://www.youtube.com/channel/" + ChannelId;

            public string ChannelTitle { get; set; }

            public DateTime PublishedAt { get; set; }

            public double DistanceTitle { get; set; }

            public double DistanceDescription { get; set; }

            public double DistanceAuthor { get; set; }

            public string ErrorMessage { get; set; }

            public string GetScoreInfo()
            {
                return $"{DistanceTitle};{DistanceDescription};{DistanceAuthor}";
            }

            public string GetFullInfo()
            {
                return $@"Information provenant de Youtube (1ère video youtube trouvée) avec le titre provenant de steem :
VideoUrl : {VideoUrl}
Titre : {VideoTitle}
Publié le : {PublishedAt}
Auteur : {ChannelTitle}
AuteurUrl : {ChannelUrl}
=============================
Similitude Titre : {DistanceTitle}
Similitude Description : {DistanceDescription}
Similitude Auteur : {DistanceAuthor}";
            }
        }

        private double FormatScore(double score)
        {
            string percent = score.ToString();
            if(percent.Length > 6)
                percent = percent.Substring(0, 6);
            return Convert.ToDouble(percent);
        }
    }
}