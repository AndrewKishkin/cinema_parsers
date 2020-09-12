using AngleSharp.Dom;
using AngleSharp.Html.Parser;
using CinemasParser.Core;
using CinemasParser.Core.Abstract;
using CinemasParser.Core.Extensions;
using CinemasParser.Core.WebClient;
using CinemasParser.Models;
using Newtonsoft.Json;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;
using Parse = CinemasParser.Models;

namespace Voxcinemas.Parser.Service
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
            var taskCinemas = Task.Run(() => GetCinemasAsync());
            var taskMovies = Task.Run(() => GetMoviesAsync());

            var (cinemas, movies) = await TaskEx.WhenAll(taskCinemas, taskMovies);

            if(cinemas.IsSuccess && movies.IsSuccess)
            {
                return new ParseResult<Data>(Result.Success)
                {
                    Data = new Data
                    {
                        Cinemas = cinemas.Data,
                        Movies = movies.Data
                    }
                };
            }

            return new ParseResult<Data>(Result.Error);
        }

        #region Cinemas
        // Get all cinemas
        private async Task<ParseResult<List<Parse.Cinema>>> GetCinemasAsync()
        {
            var result = await _http.Execute(o =>
            {
                o.Host = new Uri("https://ksa.voxcinemas.com/cinemas");
                o.Method = HttpMethod.Get;
            });

            if(result.IsSuccess)
            {
                var document = new HtmlParser().ParseDocument(result.Response);

                var elements = document.QuerySelectorAll("a").Where(x => x.HasAttribute("href"))
                                                             .Where(x => x.GetAttribute("href").Contains(@"/showtimes/"));

                var tasks = elements.Select(i => Task.Run(() => ParseCinema(i))).ToArray();
                Task.WaitAll(tasks);

                List<Parse.Cinema> cinemas = new List<Parse.Cinema>();

                foreach(var action in tasks)
                {
                    cinemas.Add(await action);
                }

                return new ParseResult<List<Parse.Cinema>>(Result.Success)
                {
                    Data = cinemas
                };
            }

            return new ParseResult<List<Parse.Cinema>>(Result.Error);
        }

        // Parse single cinema
        private async Task<Parse.Cinema> ParseCinema(IElement element)
        {
            var result = await _http.Execute(o =>
            {
                o.Host = new Uri("https://ksa.voxcinemas.com/" + element.GetAttribute("href"));
                o.Method = HttpMethod.Get;
            });

            var document = new HtmlParser().ParseDocument(result.Response);

            return new Parse.Cinema()
            {
                ExternalId = "00" + GetUntilOrEmpty(document.QuerySelector(@"li[data-id*='00']").GetAttribute("data-id")),
                Name = document.QuerySelector("h3.highlight").TextContent
            };
        }
        #endregion

        #region Movies
        // Get all movies
        private async Task<ParseResult<List<Movie>>> GetMoviesAsync()
        {
            var result = await _http.Execute(o =>
            {
                o.Host = new Uri("https://ksa.voxcinemas.com/movies/whatson");
                o.Method = HttpMethod.Get;
            });

            if(result.IsSuccess)
            {
                var document = new HtmlParser().ParseDocument(result.Response);
                var elements = document.QuerySelectorAll("article").Where(x => x.HasAttribute("class")).Where(x => x.GetAttribute("class").Contains(@"movie-summary"));

                List<Movie> movies = new List<Movie>();

                foreach(var item in elements)
                {
                    Movie movie = new Movie()
                    {
                        ExternalId = "00" + BetweenStrings(item.FirstElementChild.FirstElementChild.GetAttribute("data-src"), "P_", "."),
                        Url = "https://ksa.voxcinemas.com/movies/" + item.GetAttribute("data-slug")
                    };

                    var movieResult = MovieDetailsAsync(movie.ExternalId).Result;

                    if(movieResult.IsSuccess)
                    {
                        movie.Title = movieResult.Data.Title;
                        movie.Sessions = movieResult.Data.Sessions;

                        movies.Add(movie);
                    }
                }

                return new ParseResult<List<Movie>>(Result.Success)
                {
                    Data = movies
                };
            }

            return new ParseResult<List<Movie>>(Result.Error);
        }

        // Get movie details
        private async Task<ParseResult<Movie>> MovieDetailsAsync(string movieId)
        {
            ConcurrentDictionary<string, string> headers = new ConcurrentDictionary<string, string>();
            headers.TryAdd("application-version", "2.9.4");
            headers.TryAdd("application-name", "VOX iOS Application");
            headers.TryAdd("device-identifier", "48D0B00B-1F30-4306-BDE1-4A7D0265E892");

            var result = await _http.Execute(o =>
            {
                o.Host = new Uri($"https://api.voxcinemas.com/api/sessions?language=en&version=2&region=SA&movie={movieId}");
                o.Method = HttpMethod.Get;
                o.Headers = headers;
            });

            if(result.IsSuccess)
            {
                List<dynamic> obj = JsonConvert.DeserializeObject<List<dynamic>>(result.Response);
                var firstItem = obj.FirstOrDefault();

                if(firstItem != null)
                {
                    List<Session> sessions = new List<Session>();

                    foreach(var i in obj)
                    {
                        DateTimeOffset showtime = DateTimeOffset.Parse(i.showtime.Value);
                        string attributes = i.attributes?.Value.Replace(";", "");

                        sessions.Add(new Session
                        {
                            ExternalId = i.id,
                            Format = attributes?.ToUpper(),
                            ShowTime = showtime.UtcDateTime,
                            Url = i.bookingUrl.Value,
                            MovieId = movieId,
                            CinemaId = i.cinemaId.Value
                        });
                    }

                    return new ParseResult<Movie>(Result.Success)
                    {
                        Data = new Movie()
                        {
                            Title = firstItem.movieTitle,
                            Sessions = sessions
                        }
                    };
                }
            }

            return new ParseResult<Movie>(Result.Error);
        }
        #endregion

        #region helpers
        private string GetUntilOrEmpty(string text, string stopAt = "-")
        {
            if(!string.IsNullOrWhiteSpace(text))
            {
                int charLocation = text.IndexOf(stopAt, StringComparison.Ordinal);

                if(charLocation > 0)
                {
                    return text.Substring(0, charLocation);
                }
            }

            return string.Empty;
        }

        private static string BetweenStrings(string text, string start, string end)
        {
            int p1 = text.IndexOf(start) + start.Length;
            int p2 = text.IndexOf(end, p1);

            if(end == String.Empty) return (text.Substring(p1));
            else return text[p1..p2];
        }
        #endregion
    }
}
