using CinemasParser.Core;
using CinemasParser.Core.Abstract;
using CinemasParser.Core.WebClient;
using CinemasParser.Models;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;
using Parse = CinemasParser.Models;

namespace Muvicinemas.Parser.Service
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

            if(cinemas.IsSuccess)
            {
                List<Movie> c_movies = new List<Movie>();
                List<Movie> movies = new List<Movie>();

                foreach(var cinema in cinemas.Data)
                {
                    var c_movies_result = GetMoviesAsync(cinema.ExternalId);

                    if(c_movies_result.IsSuccess)
                    {
                        c_movies.AddRange(c_movies_result.Data);
                    }
                }

                var group_movies = c_movies.GroupBy(g => g.ExternalId);

                foreach(IGrouping<string, Movie> g in group_movies)
                {
                    Movie movie = new Movie() { ExternalId = g.Key };
                    List<Session> sessions = new List<Session>();

                    foreach(var t in g)
                    {
                        movie.Title = t.Title.Trim();
                        movie.Url = t.Url;

                        sessions.AddRange(t.Sessions);
                    }
                    movie.Sessions = sessions;
                    movies.Add(movie);
                }

                return new ParseResult<Data>(Result.Success)
                {
                    Data = new Data
                    {
                        Cinemas = cinemas.Data,
                        Movies = movies
                    }
                };
            }

            return new ParseResult<Data>(Result.Error);
        }

        private async Task<string> GetToken()
        {
            string token = String.Empty;

            ConcurrentDictionary<string, string> headers = new ConcurrentDictionary<string, string>();
            headers.TryAdd("Authorization", "Basic bXV2aS5pb3NAaW5qaW4uY29tOmRLOHdqX1ludU4kRSFLNEs=");

            var result = await _http.Execute(o =>
            {
                o.Host = new Uri("https://apiprod.muvicinemas.com/user/v1/token");
                o.Method = HttpMethod.Post;
                o.Headers = headers;
            });

            if(result.IsSuccess)
            {
                dynamic json = JObject.Parse(result.Response);
                token = json.accessToken.Value;
            }
            else
            {
                throw new Exception();
            }

            return token;
        }

        #region Cinemas
        // Get all cinemas
        private async Task<ParseResult<List<Parse.Cinema>>> GetCinemasAsync()
        {
            var result = await _http.Execute(o =>
            {
                o.Host = new Uri("https://apiprod.muvicinemas.com/cms/v1/cinemas");
                o.Method = HttpMethod.Get;
                o.Headers = Headers();
            });

            if(result.IsSuccess)
            {
                dynamic json = JObject.Parse(result.Response);

                List<Parse.Cinema> cinemas = new List<Parse.Cinema>();

                foreach(var i in json.data)
                {
                    cinemas.Add(new Parse.Cinema()
                    {
                        ExternalId = i.cinemaid,
                        Name = i.name,
                        Latitude = i.latitude,
                        Longitude = i.longitude
                    });
                }

                return new ParseResult<List<Parse.Cinema>>(Result.Success)
                {
                    Data = cinemas
                };
            }

            return new ParseResult<List<Parse.Cinema>>(Result.Error);
        }
        #endregion

        #region Movies
        // Get all movies
        private ParseResult<List<Movie>> GetMoviesAsync(string cinemaId)
        {
            List<Movie> movies = new List<Movie>();

            var headers = Headers();
            headers.TryAdd("dataversion", "en-US");

            List<string> dates = new List<string>();
            for(int i = 0; i < 10; i++)
            {
                dates.Add(DateTime.Now.AddDays(i).ToString("MM/dd/yyyy"));
            }

            var tasks = dates.Select(date => Task.Run(() =>
            {
                var result = _http.Execute(o =>
                {
                    o.Host = new Uri($@"https://apiprod.muvicinemas.com/cms/v1/cinemas/{cinemaId}/sessionsbyexperience?showdate={date}");
                    o.Method = HttpMethod.Get;
                    o.Headers = Headers();
                }).Result;

                if(result.IsSuccess)
                {
                    dynamic json = JObject.Parse(result.Response);

                    foreach(var item in json.data)
                    {
                        dynamic film = item.film;

                        Movie movie = new Movie()
                        {
                            ExternalId = film.id.Value,
                            Title = film.title.Value,
                            Url = film.websiteurl.Value,
                            Sessions = new List<Session>()
                        };

                        List<Session> sessions = new List<Session>();

                        dynamic json_sessions = item.sessionsbyexperience[0].experiences.sessions;

                        foreach(var s in json_sessions)
                        {
                            Session session = new Session()
                            {
                                ExternalId = s.id,
                                Format = s.formats[0].formats_attributes[0].shortname.Value,
                                ShowTime = s.showtime,
                                MovieId = movie.ExternalId,
                                CinemaId = cinemaId,
                                Url = movie.Url
                            };

                            movie.Sessions.Add(session);
                        }

                        movies.Add(movie);
                    }
                }
            }));

            Task.WaitAll(tasks.ToArray());

            return new ParseResult<List<Movie>>(Result.Success)
            {
                Data = movies
            };
        }
        #endregion

        #region helpers
        private ConcurrentDictionary<string, string> Headers()
        {
            var token = GetToken().Result;

            ConcurrentDictionary<string, string> headers = new ConcurrentDictionary<string, string>();
            headers.TryAdd("Authorization", $"Bearer {token}");
            headers.TryAdd("appplatform", "ios");
            headers.TryAdd("appversion", "2.0.1");

            return headers;
        }
        #endregion
    }
}

