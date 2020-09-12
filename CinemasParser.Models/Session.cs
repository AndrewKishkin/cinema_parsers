using CinemasParser.Models.Base;
using System;

namespace CinemasParser.Models
{
    public class Session : BaseModel
    {
        public DateTime ShowTime { get; set; }
        public string Format { get; set; }
        public string Url { get; set; }
        public string MovieId { get; set; }
        public string CinemaId { get; set; }
    }
}
