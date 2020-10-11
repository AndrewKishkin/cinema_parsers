using CinemasParser.Core;
using CinemasParser.Core.Abstract;
using CinemasParser.Core.WebClient;
using CinemasParser.Models;
using MongoDB.Bson;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;
using Parse = CinemasParser.Models;

namespace Empirecinemas.Parser.Service
{
    internal class Parser : ICinemasParse
    {
        private readonly IHttpService _http;

        public Parser(IHttpService http)
        {
            _http = http;
        }

        public async Task<ParseResult<Data>> ExecuteAsync()
        {
            var cinemas = await GetCinemasAsync();
            var movies = await GetMoviesAsync();
            return new ParseResult<Parse.Data>(Result.Success)
            {
                Data = new Data()
                {
                    Cinemas = cinemas.Data,
                    Movies = movies.Data
                }
            };
        }
            
        #region Movies
        private async Task<ParseResult<List<Movie>>> GetMoviesAsync()
        {
            ConcurrentDictionary<string, string> headers = new ConcurrentDictionary<string, string>();
            headers.TryAdd("ContentType", "application/x-www-form-urlencoded");
            headers.TryAdd("Accept-Encoding", "gzip, deflate, br");
            headers.TryAdd("Accept-Language", "ru-RU,ru;q=0.9,en-US;q=0.8,en;q=0.7,uk;q=0.6,pl;q=0.5");
            var result = await _http.Execute(o =>
            {
                o.Host = new Uri("https://apiksa.empirecinemas.com.sa/GetAllCinemasSch");
                o.Method = HttpMethod.Post;
                o.Headers = headers;
                o.Body = @"{""Latitude"":10,""Longitude"":10,""CinemaType"":""normal"",""LanguageID"":2,""CountryID"":230}";
            });
            if (result.IsSuccess)
            {
                dynamic json = JObject.Parse(result.Response);
                List<Parse.Movie> movies = new List<Parse.Movie>();
                foreach (var c_movie in json.Data)
                {
                    Movie temp_movie = movies.FirstOrDefault(x => x.ExternalId == c_movie.MovieID);
                    if(temp_movie==null)
                    {
                        Movie movie = new Movie();
                        movie.Title = c_movie.Title;
                        movie.Url = "";
                        movie.ExternalId = c_movie.MovieID;
                        Session session = new Session();
                        session.Format = c_movie.Format;
                        session.Url = $"https://empirecinemas.com.sa/checkout/{movie.ExternalId}/{c_movie.SchID}/1";
                        string date = c_movie.ShowDate+" "+ c_movie.ShowTime;
                        session.ShowTime= GetParsedDate(date);
                        movie.Sessions.Add(session);
                        movies.Add(movie);
                    }
                    else
                    {
                        Session session = new Session();
                        session.Format = c_movie.Format;
                        session.Url = $"https://empirecinemas.com.sa/checkout/{c_movie.MovieID}/{c_movie.SchID}/1";
                        string date = c_movie.ShowDate + " " + c_movie.ShowTime;
                        session.ShowTime = GetParsedDate(date);
                        movies.First(x=>x.ExternalId==c_movie.MovieID).Sessions.Add(session);
                    }
                    return new ParseResult<List<Movie>>(Result.Success) { Data = movies };
                }
            }
            return new ParseResult<List<Movie>>(Result.Error);
        }
        #endregion
        #region Cinemas
        private async Task<ParseResult<List<Cinema>>> GetCinemasAsync()
        {
            ConcurrentDictionary<string, string> headers = new ConcurrentDictionary<string, string>();
            headers.TryAdd("ContentType", "application/x-www-form-urlencoded");
            headers.TryAdd("Accept-Encoding", "gzip, deflate, br");
            headers.TryAdd("Accept-Language", "ru-RU,ru;q=0.9,en-US;q=0.8,en;q=0.7,uk;q=0.6,pl;q=0.5");
            var result = await _http.Execute(o =>
            {
                o.Host = new Uri("https://apiksa.empirecinemas.com.sa/GetAllCinemas");
                o.Method = HttpMethod.Post;
                o.Headers = headers;
                o.Body= @"{""Latitude"":10,""Longitude"":10,""CinemaType"":""normal"",""LanguageID"":2,""CountryID"":230}";
            });
            if (result.IsSuccess)
            {
                dynamic json = JObject.Parse(result.Response);
                List<Parse.Cinema> cinemas = new List<Parse.Cinema>();
                foreach (var c_cinema in json.Data)
                {
                    Cinema cinema = new Cinema();
                    cinema.Address = c_cinema.Address;
                    cinema.Name = c_cinema.CinemaName;
                    cinema.Latitude = c_cinema.Latitude;
                    cinema.Longitude = c_cinema.Longitude;
                    cinema.ExternalId = c_cinema.CinemaID;
                    cinemas.Add(cinema);
                }
                return new ParseResult<List<Cinema>>(Result.Success) { Data = cinemas };
            }
            return new ParseResult<List<Cinema>>(Result.Error);
        }
        #endregion
        #region Helpers
        private DateTime GetParsedDate(string indate)
        {
            string pattern = "dd-MM-yyyy h:mm tt";
            DateTime date = DateTime.ParseExact(indate, pattern, CultureInfo.InvariantCulture);
            return date;
        }
        #endregion
    }
}
