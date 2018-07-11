using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

using Newtonsoft.Json;
using RestSharp;
using F23.StringSimilarity;

using EmojiBot.Models;
using EmojiBot.Models.YouTube;
using EmojiBot.Managers;

using Ditch.Core.JsonRpc;
using Ditch.Steem;
using Ditch.Steem.Helpers;
using Ditch.Steem.Models.Other;
using Ditch.Core.Errors;

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

            public List<string> Curators { get; set; }

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
                        System.Diagnostics.Process.Start("node",
                            "upvote.js "
                            +steemVideoDTO.Url.Split('/')[5]
                            +" "+steemVideoDTO.Url.Split('/')[6]
                            +" "+String.Join(',', steemVideoDTO.Curators));
                        DiscordMessageService.SendMessageAsync($"**Voted {steemVideoDTO.Url}**").Wait(5000);
                    }
                    else if(steemVideoDTO.NbDownVote > 0 && steemVideoDTO.NbUpVote == 0)
                    {
                        System.Diagnostics.Process.Start("node",
                            "downvote.js "
                            +steemVideoDTO.Url.Split('/')[5]
                            +" "+steemVideoDTO.Url.Split('/')[6]
                            +" "+String.Join(',', steemVideoDTO.Curators));
                        DiscordMessageService.SendMessageAsync($"**Downvoted {steemVideoDTO.Url}**").Wait(5000);
                    }

                    _dicoVote.Remove(kvp.Key);
                }
            }
        }

        public async Task<string> AnalyzeFromDiscordUserMessage(string curator, string videoMessage)
        {
            // if(string.IsNullOrWhiteSpace(videoMessage))
            //     return curator + ": Error: Message is empty";

            string url = null;
            string warnings = "";
            // bool isVote = false;
            // if(videoMessage.StartsWith("!vote "))
            // {
            //     url = videoMessage.Replace("!vote ", string.Empty);
            //     isVote = true;
            //     await Console.Out.WriteLineAsync("Discord receive vote for : " + url);
            // }
            // else if(videoMessage.StartsWith("!downvote "))
            // {
            //     url = videoMessage.Replace("!downvote ", string.Empty);
            //     isVote = false;
            //     await Console.Out.WriteLineAsync("Discord receive downvote for : " + url);
            // }
            // else if(videoMessage.StartsWith("!cancel "))
            // {
            //     url = videoMessage.Replace("!cancel ", string.Empty);
            //     await Console.Out.WriteLineAsync("Discord receive cancel for : " + url);
            //     if (!_dicoVote.ContainsKey(url))
            //         return curator + ": Could not find any vote to cancel.";

            //     if (_dicoVote[url].Curators.Count() > 1) {
            //         _dicoVote[url].Curators.Remove(curator);
            //         return "Ok, " + curator + ". " + _dicoVote[url].Curators.Aggregate((a, b) => a+", "+b) + " are still voting on this video.";
            //     }
            //     _dicoVote.Remove(url);
            //     return "No more curator voting on "+url+" , there will be no vote.";
                
            // }
            // else
            //     return curator + ": Error: Unknown Command";

            // if(string.IsNullOrWhiteSpace(url))
            //     return curator + ": Error: No url found";

            string[] words = videoMessage.Split(' ');
            for (int i = 0; i < words.Length; i++)
            {
                if (words[i].StartsWith("https://d.tube/")) {
                    url = words[i];
                    break;
                }
            }

            if (url == null) {
                return null;
            }

            Tuple<string, string> tuple = null;
            if (url.StartsWith("https://d.tube/"))
                tuple = ExtractAuthorAndPermLinkFromDtubeUrl(url);
            
            SteemDTO steemInfo = GetInfoFromSteem(tuple.Item1, tuple.Item2);
            if(!steemInfo.Success)
                return url + "\nError: Could not fetch STEEM content";
            // if (steemInfo.DTubeVoted)
            //     return url + "\nError: We already voted on this video";
            YoutubeDTO youtubeInfo = await GetInfoFromYouTubeSearchAPI(steemInfo.Title, steemInfo.Description, steemInfo.Duration, steemInfo.Author);
            if(!youtubeInfo.Success) {
                await Console.Out.WriteLineAsync("Error fetching YT for: " + url);
                warnings += "\nWarning! YT error. Please check manually";
                //return "Error: Could not fetch YT Content";
                //je prefere que ca ne bloque pas dans ce cas, je met un systeme de warning
            }
                

            // todo normalisation des scores
            // pour eviter le spaghetti temporaire suivant

            double scorePlagiat = 0;
            if (steemInfo.Description.Length < 10)
                scorePlagiat = youtubeInfo.DistanceTitle;
            else
                scorePlagiat = (youtubeInfo.DistanceTitle + youtubeInfo.DistanceDescription)/2;
            double days = (DateTime.Now - youtubeInfo.PublishedAt).TotalDays;
            double scoreTime = Math.Pow(0.5, days/3.5);

            if (scorePlagiat > 0.75 && days > 7) {
                return "**PLAGIARISM OR REPOST DETECTED ("+String.Format("{0:P0}", scorePlagiat)+"):** <" + youtubeInfo.VideoUrl + ">";
            }

            // si suspection de plaggiat
            if (scorePlagiat > 0.75 && youtubeInfo.DistanceAuthor < 0.5)
            {
                warnings += "Plagiarism detected ("+String.Format("{0:P0}", scorePlagiat)+")"
                    +", but the original is only "+Math.Round(days)+" days old. Check if it's the same author. ";
                warnings += "<"+youtubeInfo.VideoUrl+">";
            } else if (scorePlagiat > 0.75)
            {
                warnings += "Looks like the same author reposting... it was reposted "+Math.Round(days)+" days later. ";
                warnings += "<"+youtubeInfo.VideoUrl+">";
            } else if (scorePlagiat > 0.50)
            {
                warnings += "Possible plagiarism ("+String.Format("{0:P0}", scorePlagiat)+") ";
                warnings += "<"+youtubeInfo.VideoUrl+">";
            }

            return warnings;

            // DateTime creationDate = DateTime.SpecifyKind(steemInfo.Created, DateTimeKind.Utc);

            // if(!_dicoVote.ContainsKey(url))
            // {
            //     DateTime afterVote = DateTime.UtcNow.AddMinutes(15);
            //     DateTime afterCreationVideo = creationDate.AddMinutes(30);
            //     DateTime voteDateTime = afterCreationVideo > afterVote && isVote ? afterCreationVideo : afterVote; //on prend le max des 2
            //     _dicoVote.Add(url, new DtubeVideoDTO
            //     { 
            //         Url = url, 
            //         CreationDateTime = creationDate,
            //         VoteDateTime = voteDateTime,
            //         Curators = new List<string>{curator}
            //     });
            // } else if (!_dicoVote[url].Curators.Contains(curator)) {
            //     _dicoVote[url].Curators.Add(curator);
            // } else {
            //     return curator+": You already voted on this video.";
            // }

            // DtubeVideoDTO dtubeVideoDTO = _dicoVote[url];
            // TimeSpan voteTimeSpan = (dtubeVideoDTO.VoteDateTime - DateTime.Now.ToUniversalTime());
            // if(isVote)
            //     dtubeVideoDTO.NbUpVote++;
            // else
            //     dtubeVideoDTO.NbDownVote++;

            // if (_dicoVote[url].Curators.Count() > 1) {
            //     warnings += "\nCurators: " + _dicoVote[url].Curators.Aggregate((a, b) => a+", "+b);
            // }

            // if(dtubeVideoDTO.NbDownVote > 0 && dtubeVideoDTO.NbUpVote == 0)
            //     return dtubeVideoDTO.Url + " ```diff\n- Downvote in " 
            //         + Math.Round(voteTimeSpan.TotalMinutes) + "mins```"
            //         + warnings;
            // if(dtubeVideoDTO.NbUpVote > 0 && dtubeVideoDTO.NbDownVote == 0)
            //     return dtubeVideoDTO.Url + " ```diff\n+ Vote in " 
            //         + Math.Round(voteTimeSpan.TotalMinutes) + "mins```"
            //         + warnings;

            // _dicoVote.Remove(url);
            // return dtubeVideoDTO.Url + "\n**Curator disagreament**. There will be no vote";
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
            JsonRpcResponse<Discussion> response = _steemClient.GetContent(author, permLink, CancellationToken.None);
            if (response.IsError)
            {
                ErrorInfo error = response.Error;
                return new SteemDTO{ ErrorMessage = $"Erreur récupération steem content avec {author}/{permLink} : {error}" };
            }

            // recup info dans steem dans le champ metadata et champ video, montant estimé gagné, etc ...
            Discussion result = response.Result;
            object reputation = result.AuthorReputation;
            bool hasDTubeVoted = false;
            foreach (VoteState vote in result.ActiveVotes)
                if (vote.Voter == "dtube")
                    hasDTubeVoted = true;

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
            var totalPayoutValue = result.TotalPayoutValue;

            return new SteemDTO
            {
                Success = true,
                Created = result.Created,
                Title = steemTitle,
                Description = steemDescription,
                Duration = steemDuration,
                Author = author,
                PermLink = permLink,
                TotalPayout = totalPayoutValue.Value,
                DTubeVoted = hasDTubeVoted
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

            public bool DTubeVoted { get; set; }

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