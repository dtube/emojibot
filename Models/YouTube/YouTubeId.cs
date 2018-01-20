using RestSharp.Deserializers;

namespace EmojiBot.Models.YouTube
{
    public class YouTubeId
    {
        [DeserializeAs(Name = "videoId")]
        public string VideoId { get; set; }
    }
}