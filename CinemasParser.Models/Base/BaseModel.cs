using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;
using System;

namespace CinemasParser.Models.Base
{
    public abstract class BaseModel
    {
        [BsonId]
        [BsonIgnoreIfDefault]
        public ObjectId Id { get; set; }

        public string ExternalId { get; set; }

        [BsonIgnoreIfDefault]
        public Nullable<DateTime> DateCreated { get; set; }

        [BsonIgnoreIfDefault]
        public Nullable<DateTime> DateModified { get; set; }
    }
}
