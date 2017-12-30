using RestSharp.Deserializers;

namespace EmojiBot.Models.YouTube
{
    public class YouTubeRoot
    {
        [DeserializeAs(Name = "items")]
        public YouTubeItems[] Items { get; set; }
    }
}