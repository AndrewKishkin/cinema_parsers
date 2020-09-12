using CinemasParser.Models.Base;

namespace CinemasParser.Models
{
    public class Cinema : BaseModel
    {
        public string Name { get; set; }
        public string Description { get; set; }
        public string Address { get; set; }
        public decimal? Latitude { get; set; }
        public decimal? Longitude { get; set; }
    }
}
