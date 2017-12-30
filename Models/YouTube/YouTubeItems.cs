using RestSharp.Deserializers;

namespace EmojiBot.Models.YouTube
{
    public class YouTubeItems
    {
        [DeserializeAs(Name = "snippet")]
        public YouTubeSnippet Snippet { get; set; }
    }
}