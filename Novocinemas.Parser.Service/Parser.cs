using CinemasParser.Core;
using CinemasParser.Core.Abstract;
using CinemasParser.Core.WebClient;
using CinemasParser.Models;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Net.Http;
using System.Threading.Tasks;
using Parse = CinemasParser.Models;

namespace Novocinemas.Parser.Service
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
            return await GetDataAsync();
        }

        #region Data
        private async Task<ParseResult<Data>> GetDataAsync()
        {
            ConcurrentDictionary<string, string> headers = new ConcurrentDictionary<string, string>();
            headers.TryAdd("ContentType", "application/x-www-form-urlencoded");
            headers.TryAdd("Accept-Encoding", "gzip, deflate, br");
            headers.TryAdd("Accept-Language", "ru-RU,ru;q=0.9,en-US;q=0.8,en;q=0.7,uk;q=0.6,pl;q=0.5");
            ConcurrentDictionary<string, string> parameters = new ConcurrentDictionary<string, string>();
            parameters.TryAdd("movieid", "");
            parameters.TryAdd("cinemaid", "");
            parameters.TryAdd("date", DateTime.Now.ToString("yyyy-MM-dd"));
            parameters.TryAdd("experience", "");

            var result = await _http.Execute(o =>
            {
                o.Host = new Uri("https://uae.novocinemas.com/showtimes/GetCinemaShowTimes");
                o.Method = HttpMethod.Post;
                o.Headers = headers;
                o.Parameters = parameters;
            });
            if(result.IsSuccess)
            {
                List<Parse.Movie> movies = new List<Movie>();
                List<Parse.Cinema> cinemas = new List<Parse.Cinema>();
                dynamic json = JArray.Parse(result.Response);
                foreach (var item in json)
                {
                    Parse.Cinema cinema = new Cinema()
                    {
                        ExternalId = item.CinemaId,
                        Name=item.CinemaName
                    };
                    cinemas.Add(cinema);
                    List<Parse.Movie> c_movies = new List<Movie>();
                    foreach (var c_movie in item.movieDetails)
                    {
                        Parse.Movie movie = new Movie();
                        movie.ExternalId = c_movie.VistaMovieId;
                        movie.Title = c_movie.MovieName;
                        movie.Url = @$"https://uae.novocinemas.com/movie/details/{c_movie.VistaMovieId}";
                        foreach (var c_languge in c_movie.LanguageList)
                        {
                            List<Parse.Session> sessions = new List<Session>();
                            foreach (var c_session in c_languge.showtimelist)
                            {
                                Parse.Session session = new Session();
                                session.Format = c_session.ExperienceName;
                                session.ShowTime = c_session.SessionShowTime;
                                session.CinemaId = cinema.ExternalId.ToString();
                                session.MovieId = movie.ExternalId.ToString();
                                sessions.Add(session);
                            }
                            if(sessions.Count!=0)
                            {
                                movie.Sessions.AddRange(sessions);
                            }
                        }
                        if(movie.Sessions.Count!=0)
                        {
                            c_movies.Add(movie);
                        }
                    }
                    movies.AddRange(c_movies);
                }
                return new ParseResult<Parse.Data>(Result.Success) 
                { 
                    Data = new Data()
                    {
                        Cinemas = cinemas,
                        Movies = movies 
                    } 
                };
            }
            return new ParseResult<Parse.Data>(Result.Error);
        }
        #endregion
    }

}
