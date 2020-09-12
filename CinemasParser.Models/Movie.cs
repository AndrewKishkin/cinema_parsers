using CinemasParser.Models.Base;
using MongoDB.Bson.Serialization.Attributes;
using System.Collections.Generic;

namespace CinemasParser.Models
{
    public class Movie : BaseModel
    {
        public Movie()
        {
            Sessions = new List<Session>();
        }

        public string Title { get; set; }
        public string Url { get; set; }

        [BsonIgnoreIfNull]
        public List<Session> Sessions { get; set; }
    }
}
