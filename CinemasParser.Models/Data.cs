using System.Collections.Generic;

namespace CinemasParser.Models
{
    public class Data
    {
        public List<Cinema> Cinemas { get; set; }
        public List<Movie> Movies { get; set; }
        public List<Session> Sessions { get; set; }
    }
}
