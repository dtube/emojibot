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

        [DeserializeAs(Name = "channelTitle")]
        public string ChannelTitle { get; set; }

        [DeserializeAs(Name = "channelId")]
        public string ChannelId { get; set; }

        [DeserializeAs(Name = "publishedAt")]
        public DateTime PublishedAt { get; set; }
    }
}