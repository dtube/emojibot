using System;

using RestSharp.Deserializers;

namespace EmojiBot.Models.YouTube
{
    public class YouTubeSnippet
    {
        [DeserializeAs(Name = "title")]
        public string Title { get; set; }

        [DeserializeAs(Name = "description")]
        public string Description { get; set; }

        [DeserializeAs(Name = "publishedAt")]
        public DateTime PublishedAt { get; set; }
    }
}