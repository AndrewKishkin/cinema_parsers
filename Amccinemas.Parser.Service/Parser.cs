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
using AngleSharp.Html.Parser;
using AngleSharp.Dom;
using AngleSharp;
using Parse = CinemasParser.Models;
using System.Linq;
using System.Globalization;
using AngleSharp.XPath;

namespace Amccinemas.Parser.Service
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
            var movies = await GetMoviesAsync(cinemas.Data.Cinemas);
            int sum = 0;
            foreach (var item in movies.Data.Movies)
            {
                sum += item.Sessions.Count;
            }
            return new ParseResult<Parse.Data>(Result.Success)
            {
                Data = new Data
                {
                    Cinemas = cinemas.Data.Cinemas,
                    Movies = movies.Data.Movies
                }
            };
        }
        #region Movies
        public async Task<ParseResult<Parse.Data>> GetMoviesByDateAsync(DateTime date, List<Cinema> cinemas, List<Movie> movies_proto)
        {
            List<Movie> movies = new List<Movie>();
            movies.AddRange(movies_proto);
            string param = "";
            param = $"{date.Year}-{date.Month}-{date.Day}";
            ConcurrentDictionary<string, string> headers = new ConcurrentDictionary<string, string>();
            headers.TryAdd("ContentType", "application/json");
            headers.TryAdd("Cookie", "cookieLanguageCode=en-US; _gcl_au=1.1.1238722976.1602705939; _fbp=fb.1.1602705939926.1048953155; _gid=GA1.2.445262349.1603531359; moe_uuid=56694985-c80f-499b-b8ee-353d2ba58144; movieDetails=qeoEzVnzBc-6cYMbYWwS4scp_uWZetsLEfEwEONYBHu6Ug3EcLkPOkkIJRxNIl21LoXAHMoiLl-K2GFvWFxLoLetERKNJ9mg2n5nJuGQnCTm4cLcKFW7kZ4wKrzk7QcgU5mbqjSflnb4_PeFk656nOlvLWaS7p8iFkS-zpiG8sRM94UJOeX_RAJBHeSg-2ca0iolX65I08aDzQXISNe834N45rFvnlXKnczOqHQxCJvkiScCXPEMudUt0mZ50_zT4mA6cD08aIHg-Hc-kaOI0Sg3RjPFVqt3tsn14JDZoCeu0PFyc7VLVr79jwvPn6r--qKRLudWoNJO5aJNQv_2smOTfz0_ZUF7RxCpwQ8geT_vx7XKrH_0c9o23bsv5jiokSdBTwrkHbiwYk4ITrwCkmhHtkHeJVYs3sKRoroNevKirCwY585fkH59_SHcrTA8RUc6JMj2ixXsezJeeszlngooxw8GHDdH5o1ASeVsvKFbOcHeWPCNjA5XgQZvyIFKoHDQjZXS5N-Tu0-G_wNFmL4aQBnOdvIbFw0_DfbDHLaFTfeJY9NuTSyhoZJP8mOaebaIZMokCCFINDhBcgerB1CcAhP5SUnKdXkeLbBADZskqh_sU-f8QzGsesP1dOa1tNQmRDwenGOKb3MDof1EkQCYenShYRMmJC7VY1SBqmlM8n0aOMycbc1kcoLME98Z7F4BGJUKMMxa4PB1XU_e3OIEZYobFZP6czI1juhh9yYHWum4Yyd_JVGXqjh622CGWUhIXWAjkwDX1n6n5C-nb5ar5UVbclfatS1YKyuUXAU_zODz2OFT8Wxo5Lto1OFtqEGYkKiIu-m9A7w4l8yQIUpb1tLpOvJnEbXe_1CWiqM6hEaixfrc7jJHQ-FncGApH7i0WNF4iALHGV0aeIwjHiL5jbE5rMrHi1K_suW7TM2xrzVg93DQQBlgvhnd7g5UJ4jTCR4XyWMKKMcPBhw3R-aNQEnlbLyhWznB3ljwjYwOV4EGb8iBSqBw0I2V0uTfk7tPhv8DRZi-GkAZznbyGxcNPw32wxy2hU33iWPTbk09TfVWsyySmAhSvdSZ3240g2jYn36zA11QnAIT-UlJynV5Hi2wQA2bJKof7FPn_EMxrHrD9XTmtbTUJkQ8HpxjAwoeVLSoTKPQ4y2TWLCzR9Ei5wWaWcn3MjZnddtzlBGOiECIDhKg5cvAi5xIRSpTWuDwdV1P3tziBGWKGxWT-nMyNY7oYfcmB1rpuGMnfyVRl6o4ettghllISF1gI5MA19Z-p-Qvp2-Wq-VFW3JX2lovt8CIT7PaD5XH9yjF98wF5oI0EPF53Lc738yxFmYEfw1Kbiphe4_NiXlcBxA2rIj5MI_W0zvmFZchj8C0eES6TXP_fFrixhgjHoboPhh0jSDlwXIV-kd1zBprw6hS4YPF4MlAm57mDdEnjV8a-GNx2JO20V7SeT9Jz852f9ooX6uZGVYXcaF7tpJ-xlK_lQJOV3XVyz_KJZRfILUk-QCoND7wXTraBj3qRM7h7rfdJ3SbyxHW4kpaM6i2DODUh5ZT8_yOKDzi7Ueba1g_6F3Uf97Rk_w4uNL8C58OyDdurk9Q_OweYQuFfmV4aoKNnF5r5Vq_Kc69zTw79tbFWEL0GrFW9QTj8WTr7r6dyup5NhKbmuzd6OL1mdCdxQGASSVesopwH44N0UIx-_UCNAn3zhfAlxD53CiOdBMGW5Clq-CQyaBo9TFZ2UH-CqdlTqDK_2z0XZKxvnSlBp--b9VRWoHoa9fnPMkB2lBmtrQHR1BO9A7Et6IPlpwqbOK2572nGc3FLffigcWEYxLiTv3N0QzKnxka6oldA7f0-Apqe40I5hu9OzFn-8kMwEnWxf1hWkUABpZeHdgdZkIRi5Ahz2mpBnqJwyDCX5pRE2tgjje_ST3iOzn7PbcXQhQzzt6nR0jz-ADG7-JkLXZ68Waud65ZrZ5oBkZDRk-JwMR0kM4LD9ARW-EVXsPJ9S5Y_fcAgUHIrU7AL_M_c5Eplx-OcA-zua4UhO5r5h69qLewm-E7V0KvfmJf77HDDjXxK-fFXsKQnjVzV18WLXLB-A35MgPPwaJ1R4ewcF8SNup8DeAykd6FzgS21uYAO4jMus-qf6rg4_RwUJ80eWBXSAoXHQV-EL5ULP74mXTdCQ5fsaaDv-NVkFreM1J8ci1geDlrqrH8QC0W5X-CJzqE-EzQ5RwkWYIdOx7v_LPhb5hi6bzodBbRzg_vCxKbz2dKDJs2K4vSvJPkXW50W1kUGWW6TXP_fFrixmV7HljSa-BEThFF1iSQwnPyGqH5QvX3kcOKuAqadrSKht423Ge2agI_uwUw9WL3I0zfewFuTT6RMFfaIWOAzRJtHzT-cK8X4e3OKggm-cCfFIA4KTo8DPGm48OQSnQHnDTzQPjeoSV0M-31M8cNvd6He6j6RS4QZmiaD4VShb8zkZu7h-UO-hsgt7Hlw8ufys580_sSRY0fzLEtSnB2As02oU262OM-_1ioC6hC_shFKq4XSUgj-lmi3iJsFVLL4ismpqxRrlaPrBdamlWixdh7sspXVzz_8r2dVcQxP57nmgmfg0kQ7e_lN68fsvocEbFsGrVmFvhBDBZR4C5ieSJZ2Mjx4jek_-c9epWdVKjG-7aYgR7b5Wy3TmnUpnzPLFSRzKMyQzF07yi9BRSlt0JNqJgxZut9aezSGoMxJOXEvmR28hbWK9fOHaqH_YgXWLnWwmPE_oE2w9v24I9mRqdz-Ev2Qrlb2itS5La3cXrApIqHcZdHKdQeXydAX9ZwzaRAcq1I8wLDToCntq1opN2sqkEYwOfK1iAz3kV16I7A-oXjWvBiYX848t7iua4KaYACrIc96WArIxMxW1zflgXJ1AAD0nLzLIZkMPd5lo7K257dGMCKo5TtlzVJNkrHMEXMV2FHgFqsS_UBHv9OLxynpA0ixs1Ixho-D4WpMgOiX2JLt3vyRG3CWNKrjoF5mY7AN_yiVifETdNtFtcWF6SJjku5ZJLHmD-pAW0Yx1gqFgbboUu1T8YhdHu58Km5Ri77GCZnj0wVcVFeePYSCMw=; _gat_UA-117333109-1=1; AccessTokenInfo=u2x96MRfae-glKK-nne0UV_A-GUIp1Pp36QiNk9wKOOkCGuHUNUsNAMdZQBNFno3kZ9VqVTPBMIxMGUOdzfyb1rGxtEq6lS8-iP8KeTvybVFqjc-5HctD_wkIJEF7-7kx8meVjzWxySwVgwkEZmqv5fGeVqv0EsLhQa05blbJJoF2HSdEMiOBwgyeTQ_lR6gOo_4s-N2npeI4xHYtG4p_TFRfLXvVa8llp8mIxBrHwUKfAJH_osh7K6v1RWm7ZvNIo4XwnuyUTmDeaeNmWtotuLjKE3MvCYYycGrak3AnBMuEX1EpR4synaZBqmGFgJK_RYyfuRoiqL-aFm9GVSfmKVGi2kiLBuAwTp0nuDlMoqjG6r53iG9x8D2RIRgv5DsgOgN8SS7KhNrQFoaL-rxlKmtrTSYPzWci8igX0lEBF7RrJV8qYnN1ER6V6KOQkwLQ1MhpROy3Lk3fu12Tz-_0UYHIzp9QNgFpL4y8b1ZsUmVxcyGkAqFg6TOPBi32mvnUdCre-QPTbA_dLwmF0yNETtVxAVZo4aeDOdihITWOZMCj6H5OLSglKMfWj615P9bWMf_Pihog7uRy-sDA5TjxLYhU4ZvH97xyQKEUWk6rnT_A6mH8DnfipmRBGFO1kkKUyrJvJBvADCt4nL567uFBvbGdFadUcq-JzBjzmFIe9CoU_VsEYtBWhAGV0sbajk_k_9Ls190ziS3d9lOSi5YRxAMLCipEKM3yGi2IduB_ERn1d5exZTCRolUrSdD4d3yFNlu1FQS7QtJD75FxN5BYCB03KgMBRUnyNejPU0RiqHx-liJQMQiPrRkDBpyxo-de5MBKoNSgt-nW44dmEWMrPK5h2jNpOwjtfbwxGxFFSp6txscWxaMDbo9mGSJeE6e27tb2WvX5YSNi68fccyo3PZLTF-H8ZixtfoTn4g1nL1JGA55-oXDCjq95ivHCJfGSdXmV99vayz6I_wp5O_JtUWqNz7kdy0Pf_SsPVbZS7bHyZ5WPNbHJLBWDCQRmaq_l8Z5Wq_QSwv4obJ14EiI6gO4QyDgrNISZkkg3fwMSDiTDKMoMVLi0xsXF7xJnGUoaH2Dvehd22ZFaozIpZB94wHIE0elFyUtXCLVqeNo-gyAUcIOdORSk7XqfCElKheoGzI4Op1kfF3rouycJtpjGcztaWblB-nY52VmuwOpsfRatMz60tA06ZSAQK3FzQmbJNPi_LyWagUDokf3pvAxfz1LJgnxe7cn55cPBW8IWOuHoxAPIZ7HePAxFtttra0C2F9_CB6SI11M9QSweb-rSgZwU_xc5wFsEb2CIWj3SgE4gbO3WxnbCJcXTd8RK_7yG9OzGhvLk-uzz7W6g1KXVFjb8mCWoxgGMK97SL_CjmkTQGcgwup24JzVo_IGkxVNJQyUDhfndbHoxLZstyyfKlrGxtEq6lS8JMPOGXaUEQK3yn_wkykYX0yrtmgM99sTXzHcpK1916aI_ev7q5X2KUjdDofJfybNKEVcSE8n-hSP8b652vCeFdkY7P4cXN29; _ga_EJRWLKGJNW=GS1.1.1603541129.5.1.1603542988.47; _ga=GA1.2.87188470.1602703684");
            var result = await _http.Execute(o =>
            {
                o.Host = new Uri("https://www.amccinemas.com/showtime/getallmovies");
                o.Method = HttpMethod.Post;
                o.Headers = headers;
                o.Body = @"{""url"":""showdate="+ param + @"""}";
            });
            if (result.IsSuccess)
            {
                var config = Configuration.Default;

                //Create a new context for evaluating webpages with the given config
                var context = BrowsingContext.New(config);

                //Just get the DOM representation
                var document = await context.OpenAsync(req => req.Content(result.Response));

                //Select all sessions of films
                var listofgroups = document.All.Where(m=>m.ClassName=="amc-time-list");

                foreach (var item in listofgroups)
                {
                    string cinemaName = item.GetAttribute("data-cinemaname");
                    string movieName = item.GetAttribute("data-moviename");
                    var childdoc= await context.OpenAsync(req => req.Content(item.InnerHtml));
                    var listofsessions = childdoc.QuerySelectorAll("span").Where(m => m.ClassName=="amc-time");
                    List<Session> sessions = new List<Session>();
                    foreach (var c_session in listofsessions)
                    {
                        Session session = new Session();
                        DateTime sessionDate = new DateTime(date.Year,date.Month, date.Day);
                        string time= c_session.InnerHtml;
                        time = time.Remove(0, 1);
                        time = time.Remove(time.Length - 1, 1);
                        time ="10/10/2000 " +time;
                        DateTime parsedTime = DateTime.ParseExact(time, "M/d/yyyy h:mm tt", CultureInfo.InvariantCulture);
                        sessionDate = sessionDate.AddHours(parsedTime.Hour);
                        sessionDate = sessionDate.AddMinutes(parsedTime.Minute);
                        session.ShowTime = sessionDate;
                        session.CinemaId = cinemas.First(x => x.Name == cinemaName).ExternalId;
                        sessions.Add(session);
                    }
                    
                    movies.First(x => x.Title == movieName).Sessions.AddRange(sessions);
                }
                return new ParseResult<Data>(Result.Success)
                {
                    Data = new Data()
                    {
                        Movies = movies
                    }
                };
            }
            
            return new ParseResult<Parse.Data>(Result.Error);
        }
        public async Task<ParseResult<Parse.Data>> GetMoviesAsync(List<Cinema> cinemas)
        {
            ConcurrentDictionary<string, string> headers = new ConcurrentDictionary<string, string>();
            headers.TryAdd("ContentType", "application/x-www-form-urlencoded");
            headers.TryAdd("Cookie", "cookieLanguageCode=en-US; _gcl_au=1.1.1238722976.1602705939; _fbp=fb.1.1602705939926.1048953155; _gid=GA1.2.445262349.1603531359; moe_uuid=56694985-c80f-499b-b8ee-353d2ba58144; movieDetails=qeoEzVnzBc-6cYMbYWwS4scp_uWZetsLEfEwEONYBHu6Ug3EcLkPOkkIJRxNIl21LoXAHMoiLl-K2GFvWFxLoLetERKNJ9mg2n5nJuGQnCTm4cLcKFW7kZ4wKrzk7QcgU5mbqjSflnb4_PeFk656nOlvLWaS7p8iFkS-zpiG8sRM94UJOeX_RAJBHeSg-2ca0iolX65I08aDzQXISNe834N45rFvnlXKnczOqHQxCJvkiScCXPEMudUt0mZ50_zT4mA6cD08aIHg-Hc-kaOI0Sg3RjPFVqt3tsn14JDZoCeu0PFyc7VLVr79jwvPn6r--qKRLudWoNJO5aJNQv_2smOTfz0_ZUF7RxCpwQ8geT_vx7XKrH_0c9o23bsv5jiokSdBTwrkHbiwYk4ITrwCkmhHtkHeJVYs3sKRoroNevKirCwY585fkH59_SHcrTA8RUc6JMj2ixXsezJeeszlngooxw8GHDdH5o1ASeVsvKFbOcHeWPCNjA5XgQZvyIFKoHDQjZXS5N-Tu0-G_wNFmL4aQBnOdvIbFw0_DfbDHLaFTfeJY9NuTSyhoZJP8mOaebaIZMokCCFINDhBcgerB1CcAhP5SUnKdXkeLbBADZskqh_sU-f8QzGsesP1dOa1tNQmRDwenGOKb3MDof1EkQCYenShYRMmJC7VY1SBqmlM8n0aOMycbc1kcoLME98Z7F4BGJUKMMxa4PB1XU_e3OIEZYobFZP6czI1juhh9yYHWum4Yyd_JVGXqjh622CGWUhIXWAjkwDX1n6n5C-nb5ar5UVbclfatS1YKyuUXAU_zODz2OFT8Wxo5Lto1OFtqEGYkKiIu-m9A7w4l8yQIUpb1tLpOvJnEbXe_1CWiqM6hEaixfrc7jJHQ-FncGApH7i0WNF4iALHGV0aeIwjHiL5jbE5rMrHi1K_suW7TM2xrzVg93DQQBlgvhnd7g5UJ4jTCR4XyWMKKMcPBhw3R-aNQEnlbLyhWznB3ljwjYwOV4EGb8iBSqBw0I2V0uTfk7tPhv8DRZi-GkAZznbyGxcNPw32wxy2hU33iWPTbk09TfVWsyySmAhSvdSZ3240g2jYn36zA11QnAIT-UlJynV5Hi2wQA2bJKof7FPn_EMxrHrD9XTmtbTUJkQ8HpxjAwoeVLSoTKPQ4y2TWLCzR9Ei5wWaWcn3MjZnddtzlBGOiECIDhKg5cvAi5xIRSpTWuDwdV1P3tziBGWKGxWT-nMyNY7oYfcmB1rpuGMnfyVRl6o4ettghllISF1gI5MA19Z-p-Qvp2-Wq-VFW3JX2lovt8CIT7PaD5XH9yjF98wF5oI0EPF53Lc738yxFmYEfw1Kbiphe4_NiXlcBxA2rIj5MI_W0zvmFZchj8C0eES6TXP_fFrixhgjHoboPhh0jSDlwXIV-kd1zBprw6hS4YPF4MlAm57mDdEnjV8a-GNx2JO20V7SeT9Jz852f9ooX6uZGVYXcaF7tpJ-xlK_lQJOV3XVyz_KJZRfILUk-QCoND7wXTraBj3qRM7h7rfdJ3SbyxHW4kpaM6i2DODUh5ZT8_yOKDzi7Ueba1g_6F3Uf97Rk_w4uNL8C58OyDdurk9Q_OweYQuFfmV4aoKNnF5r5Vq_Kc69zTw79tbFWEL0GrFW9QTj8WTr7r6dyup5NhKbmuzd6OL1mdCdxQGASSVesopwH44N0UIx-_UCNAn3zhfAlxD53CiOdBMGW5Clq-CQyaBo9TFZ2UH-CqdlTqDK_2z0XZKxvnSlBp--b9VRWoHoa9fnPMkB2lBmtrQHR1BO9A7Et6IPlpwqbOK2572nGc3FLffigcWEYxLiTv3N0QzKnxka6oldA7f0-Apqe40I5hu9OzFn-8kMwEnWxf1hWkUABpZeHdgdZkIRi5Ahz2mpBnqJwyDCX5pRE2tgjje_ST3iOzn7PbcXQhQzzt6nR0jz-ADG7-JkLXZ68Waud65ZrZ5oBkZDRk-JwMR0kM4LD9ARW-EVXsPJ9S5Y_fcAgUHIrU7AL_M_c5Eplx-OcA-zua4UhO5r5h69qLewm-E7V0KvfmJf77HDDjXxK-fFXsKQnjVzV18WLXLB-A35MgPPwaJ1R4ewcF8SNup8DeAykd6FzgS21uYAO4jMus-qf6rg4_RwUJ80eWBXSAoXHQV-EL5ULP74mXTdCQ5fsaaDv-NVkFreM1J8ci1geDlrqrH8QC0W5X-CJzqE-EzQ5RwkWYIdOx7v_LPhb5hi6bzodBbRzg_vCxKbz2dKDJs2K4vSvJPkXW50W1kUGWW6TXP_fFrixmV7HljSa-BEThFF1iSQwnPyGqH5QvX3kcOKuAqadrSKht423Ge2agI_uwUw9WL3I0zfewFuTT6RMFfaIWOAzRJtHzT-cK8X4e3OKggm-cCfFIA4KTo8DPGm48OQSnQHnDTzQPjeoSV0M-31M8cNvd6He6j6RS4QZmiaD4VShb8zkZu7h-UO-hsgt7Hlw8ufys580_sSRY0fzLEtSnB2As02oU262OM-_1ioC6hC_shFKq4XSUgj-lmi3iJsFVLL4ismpqxRrlaPrBdamlWixdh7sspXVzz_8r2dVcQxP57nmgmfg0kQ7e_lN68fsvocEbFsGrVmFvhBDBZR4C5ieSJZ2Mjx4jek_-c9epWdVKjG-7aYgR7b5Wy3TmnUpnzPLFSRzKMyQzF07yi9BRSlt0JNqJgxZut9aezSGoMxJOXEvmR28hbWK9fOHaqH_YgXWLnWwmPE_oE2w9v24I9mRqdz-Ev2Qrlb2itS5La3cXrApIqHcZdHKdQeXydAX9ZwzaRAcq1I8wLDToCntq1opN2sqkEYwOfK1iAz3kV16I7A-oXjWvBiYX848t7iua4KaYACrIc96WArIxMxW1zflgXJ1AAD0nLzLIZkMPd5lo7K257dGMCKo5TtlzVJNkrHMEXMV2FHgFqsS_UBHv9OLxynpA0ixs1Ixho-D4WpMgOiX2JLt3vyRG3CWNKrjoF5mY7AN_yiVifETdNtFtcWF6SJjku5ZJLHmD-pAW0Yx1gqFgbboUu1T8YhdHu58Km5Ri77GCZnj0wVcVFeePYSCMw=; _gat_UA-117333109-1=1; AccessTokenInfo=u2x96MRfae-glKK-nne0UV_A-GUIp1Pp36QiNk9wKOOkCGuHUNUsNAMdZQBNFno3kZ9VqVTPBMIxMGUOdzfyb1rGxtEq6lS8-iP8KeTvybVFqjc-5HctD_wkIJEF7-7kx8meVjzWxySwVgwkEZmqv5fGeVqv0EsLhQa05blbJJoF2HSdEMiOBwgyeTQ_lR6gOo_4s-N2npeI4xHYtG4p_TFRfLXvVa8llp8mIxBrHwUKfAJH_osh7K6v1RWm7ZvNIo4XwnuyUTmDeaeNmWtotuLjKE3MvCYYycGrak3AnBMuEX1EpR4synaZBqmGFgJK_RYyfuRoiqL-aFm9GVSfmKVGi2kiLBuAwTp0nuDlMoqjG6r53iG9x8D2RIRgv5DsgOgN8SS7KhNrQFoaL-rxlKmtrTSYPzWci8igX0lEBF7RrJV8qYnN1ER6V6KOQkwLQ1MhpROy3Lk3fu12Tz-_0UYHIzp9QNgFpL4y8b1ZsUmVxcyGkAqFg6TOPBi32mvnUdCre-QPTbA_dLwmF0yNETtVxAVZo4aeDOdihITWOZMCj6H5OLSglKMfWj615P9bWMf_Pihog7uRy-sDA5TjxLYhU4ZvH97xyQKEUWk6rnT_A6mH8DnfipmRBGFO1kkKUyrJvJBvADCt4nL567uFBvbGdFadUcq-JzBjzmFIe9CoU_VsEYtBWhAGV0sbajk_k_9Ls190ziS3d9lOSi5YRxAMLCipEKM3yGi2IduB_ERn1d5exZTCRolUrSdD4d3yFNlu1FQS7QtJD75FxN5BYCB03KgMBRUnyNejPU0RiqHx-liJQMQiPrRkDBpyxo-de5MBKoNSgt-nW44dmEWMrPK5h2jNpOwjtfbwxGxFFSp6txscWxaMDbo9mGSJeE6e27tb2WvX5YSNi68fccyo3PZLTF-H8ZixtfoTn4g1nL1JGA55-oXDCjq95ivHCJfGSdXmV99vayz6I_wp5O_JtUWqNz7kdy0Pf_SsPVbZS7bHyZ5WPNbHJLBWDCQRmaq_l8Z5Wq_QSwv4obJ14EiI6gO4QyDgrNISZkkg3fwMSDiTDKMoMVLi0xsXF7xJnGUoaH2Dvehd22ZFaozIpZB94wHIE0elFyUtXCLVqeNo-gyAUcIOdORSk7XqfCElKheoGzI4Op1kfF3rouycJtpjGcztaWblB-nY52VmuwOpsfRatMz60tA06ZSAQK3FzQmbJNPi_LyWagUDokf3pvAxfz1LJgnxe7cn55cPBW8IWOuHoxAPIZ7HePAxFtttra0C2F9_CB6SI11M9QSweb-rSgZwU_xc5wFsEb2CIWj3SgE4gbO3WxnbCJcXTd8RK_7yG9OzGhvLk-uzz7W6g1KXVFjb8mCWoxgGMK97SL_CjmkTQGcgwup24JzVo_IGkxVNJQyUDhfndbHoxLZstyyfKlrGxtEq6lS8JMPOGXaUEQK3yn_wkykYX0yrtmgM99sTXzHcpK1916aI_ev7q5X2KUjdDofJfybNKEVcSE8n-hSP8b652vCeFdkY7P4cXN29; _ga_EJRWLKGJNW=GS1.1.1603541129.5.1.1603542988.47; _ga=GA1.2.87188470.1602703684");
            headers.TryAdd("Accept-Encoding", "gzip, deflate, br");
            headers.TryAdd("Accept-Language", "ru-RU,ru;q=0.9,en-US;q=0.8,en;q=0.7,uk;q=0.6,pl;q=0.5");
            var result = await _http.Execute(o =>
            {
                o.Host = new Uri("https://www.amccinemas.com/MovieDetails/getallnowshowingfilms");
                o.Method = HttpMethod.Post;
                o.Headers = headers;
            });
            if (result.IsSuccess)
            {
                List<Movie> movies_proto = new List<Movie>();
                List<Movie> c_movies = new List<Movie>();
                List<Movie> movies = new List<Movie>();
                dynamic json = JArray.Parse(result.Response);
                foreach (var item in json)
                {
                    Movie movie = new Movie();
                    movie.Title = item.title;
                    movie.ExternalId = item.id;
                    movie.Url = "https://www.amccinemas.com/movies/" + item.slugName;
                    movies_proto.Add(movie);
                }
                
                for (int i = 0; i < 5; i++)
                {
                    DateTime date = DateTime.Now.AddDays(i);
                    var data = await GetMoviesByDateAsync(date, cinemas, movies_proto);
                    if (data.Result == Result.Success)
                    {
                        c_movies.Clear();
                        c_movies.AddRange(data.Data.Movies);
                    }
                }
                var group_movies = c_movies.GroupBy(g => g.ExternalId);

                foreach (IGrouping<string, Movie> g in group_movies)
                {
                    Movie movie = new Movie() { ExternalId = g.Key };
                    List<Session> sessions = new List<Session>();

                    foreach (var t in g)
                    {
                        movie.Title = t.Title.Trim();
                        movie.Url = t.Url;
                        sessions.AddRange(t.Sessions);
                    }
                    movie.Sessions = sessions;
                    movies.Add(movie);
                }
                return new ParseResult<Parse.Data>(Result.Success)
                {
                    Data = new Data
                    {
                        Movies = movies
                    }
                };
            }
            return new ParseResult<Parse.Data>(Result.Error);
        }
        #endregion
        #region Cinemas
        public async Task<ParseResult<Parse.Data>> GetCinemasAsync()
        {
            ConcurrentDictionary<string, string> headers = new ConcurrentDictionary<string, string>();
            headers.TryAdd("ContentType", "application/x-www-form-urlencoded");
            headers.TryAdd("Accept-Encoding", "gzip, deflate, br");
            headers.TryAdd("Cookie", "cookieLanguageCode=en-US; _gcl_au=1.1.1238722976.1602705939; _fbp=fb.1.1602705939926.1048953155; _gid=GA1.2.445262349.1603531359; moe_uuid=56694985-c80f-499b-b8ee-353d2ba58144; movieDetails=qeoEzVnzBc-6cYMbYWwS4scp_uWZetsLEfEwEONYBHu6Ug3EcLkPOkkIJRxNIl21LoXAHMoiLl-K2GFvWFxLoLetERKNJ9mg2n5nJuGQnCTm4cLcKFW7kZ4wKrzk7QcgU5mbqjSflnb4_PeFk656nOlvLWaS7p8iFkS-zpiG8sRM94UJOeX_RAJBHeSg-2ca0iolX65I08aDzQXISNe834N45rFvnlXKnczOqHQxCJvkiScCXPEMudUt0mZ50_zT4mA6cD08aIHg-Hc-kaOI0Sg3RjPFVqt3tsn14JDZoCeu0PFyc7VLVr79jwvPn6r--qKRLudWoNJO5aJNQv_2smOTfz0_ZUF7RxCpwQ8geT_vx7XKrH_0c9o23bsv5jiokSdBTwrkHbiwYk4ITrwCkmhHtkHeJVYs3sKRoroNevKirCwY585fkH59_SHcrTA8RUc6JMj2ixXsezJeeszlngooxw8GHDdH5o1ASeVsvKFbOcHeWPCNjA5XgQZvyIFKoHDQjZXS5N-Tu0-G_wNFmL4aQBnOdvIbFw0_DfbDHLaFTfeJY9NuTSyhoZJP8mOaebaIZMokCCFINDhBcgerB1CcAhP5SUnKdXkeLbBADZskqh_sU-f8QzGsesP1dOa1tNQmRDwenGOKb3MDof1EkQCYenShYRMmJC7VY1SBqmlM8n0aOMycbc1kcoLME98Z7F4BGJUKMMxa4PB1XU_e3OIEZYobFZP6czI1juhh9yYHWum4Yyd_JVGXqjh622CGWUhIXWAjkwDX1n6n5C-nb5ar5UVbclfatS1YKyuUXAU_zODz2OFT8Wxo5Lto1OFtqEGYkKiIu-m9A7w4l8yQIUpb1tLpOvJnEbXe_1CWiqM6hEaixfrc7jJHQ-FncGApH7i0WNF4iALHGV0aeIwjHiL5jbE5rMrHi1K_suW7TM2xrzVg93DQQBlgvhnd7g5UJ4jTCR4XyWMKKMcPBhw3R-aNQEnlbLyhWznB3ljwjYwOV4EGb8iBSqBw0I2V0uTfk7tPhv8DRZi-GkAZznbyGxcNPw32wxy2hU33iWPTbk09TfVWsyySmAhSvdSZ3240g2jYn36zA11QnAIT-UlJynV5Hi2wQA2bJKof7FPn_EMxrHrD9XTmtbTUJkQ8HpxjAwoeVLSoTKPQ4y2TWLCzR9Ei5wWaWcn3MjZnddtzlBGOiECIDhKg5cvAi5xIRSpTWuDwdV1P3tziBGWKGxWT-nMyNY7oYfcmB1rpuGMnfyVRl6o4ettghllISF1gI5MA19Z-p-Qvp2-Wq-VFW3JX2lovt8CIT7PaD5XH9yjF98wF5oI0EPF53Lc738yxFmYEfw1Kbiphe4_NiXlcBxA2rIj5MI_W0zvmFZchj8C0eES6TXP_fFrixhgjHoboPhh0jSDlwXIV-kd1zBprw6hS4YPF4MlAm57mDdEnjV8a-GNx2JO20V7SeT9Jz852f9ooX6uZGVYXcaF7tpJ-xlK_lQJOV3XVyz_KJZRfILUk-QCoND7wXTraBj3qRM7h7rfdJ3SbyxHW4kpaM6i2DODUh5ZT8_yOKDzi7Ueba1g_6F3Uf97Rk_w4uNL8C58OyDdurk9Q_OweYQuFfmV4aoKNnF5r5Vq_Kc69zTw79tbFWEL0GrFW9QTj8WTr7r6dyup5NhKbmuzd6OL1mdCdxQGASSVesopwH44N0UIx-_UCNAn3zhfAlxD53CiOdBMGW5Clq-CQyaBo9TFZ2UH-CqdlTqDK_2z0XZKxvnSlBp--b9VRWoHoa9fnPMkB2lBmtrQHR1BO9A7Et6IPlpwqbOK2572nGc3FLffigcWEYxLiTv3N0QzKnxka6oldA7f0-Apqe40I5hu9OzFn-8kMwEnWxf1hWkUABpZeHdgdZkIRi5Ahz2mpBnqJwyDCX5pRE2tgjje_ST3iOzn7PbcXQhQzzt6nR0jz-ADG7-JkLXZ68Waud65ZrZ5oBkZDRk-JwMR0kM4LD9ARW-EVXsPJ9S5Y_fcAgUHIrU7AL_M_c5Eplx-OcA-zua4UhO5r5h69qLewm-E7V0KvfmJf77HDDjXxK-fFXsKQnjVzV18WLXLB-A35MgPPwaJ1R4ewcF8SNup8DeAykd6FzgS21uYAO4jMus-qf6rg4_RwUJ80eWBXSAoXHQV-EL5ULP74mXTdCQ5fsaaDv-NVkFreM1J8ci1geDlrqrH8QC0W5X-CJzqE-EzQ5RwkWYIdOx7v_LPhb5hi6bzodBbRzg_vCxKbz2dKDJs2K4vSvJPkXW50W1kUGWW6TXP_fFrixmV7HljSa-BEThFF1iSQwnPyGqH5QvX3kcOKuAqadrSKht423Ge2agI_uwUw9WL3I0zfewFuTT6RMFfaIWOAzRJtHzT-cK8X4e3OKggm-cCfFIA4KTo8DPGm48OQSnQHnDTzQPjeoSV0M-31M8cNvd6He6j6RS4QZmiaD4VShb8zkZu7h-UO-hsgt7Hlw8ufys580_sSRY0fzLEtSnB2As02oU262OM-_1ioC6hC_shFKq4XSUgj-lmi3iJsFVLL4ismpqxRrlaPrBdamlWixdh7sspXVzz_8r2dVcQxP57nmgmfg0kQ7e_lN68fsvocEbFsGrVmFvhBDBZR4C5ieSJZ2Mjx4jek_-c9epWdVKjG-7aYgR7b5Wy3TmnUpnzPLFSRzKMyQzF07yi9BRSlt0JNqJgxZut9aezSGoMxJOXEvmR28hbWK9fOHaqH_YgXWLnWwmPE_oE2w9v24I9mRqdz-Ev2Qrlb2itS5La3cXrApIqHcZdHKdQeXydAX9ZwzaRAcq1I8wLDToCntq1opN2sqkEYwOfK1iAz3kV16I7A-oXjWvBiYX848t7iua4KaYACrIc96WArIxMxW1zflgXJ1AAD0nLzLIZkMPd5lo7K257dGMCKo5TtlzVJNkrHMEXMV2FHgFqsS_UBHv9OLxynpA0ixs1Ixho-D4WpMgOiX2JLt3vyRG3CWNKrjoF5mY7AN_yiVifETdNtFtcWF6SJjku5ZJLHmD-pAW0Yx1gqFgbboUu1T8YhdHu58Km5Ri77GCZnj0wVcVFeePYSCMw=; _gat_UA-117333109-1=1; AccessTokenInfo=u2x96MRfae-glKK-nne0UV_A-GUIp1Pp36QiNk9wKOOkCGuHUNUsNAMdZQBNFno3kZ9VqVTPBMIxMGUOdzfyb1rGxtEq6lS8-iP8KeTvybVFqjc-5HctD_wkIJEF7-7kx8meVjzWxySwVgwkEZmqv5fGeVqv0EsLhQa05blbJJoF2HSdEMiOBwgyeTQ_lR6gOo_4s-N2npeI4xHYtG4p_TFRfLXvVa8llp8mIxBrHwUKfAJH_osh7K6v1RWm7ZvNIo4XwnuyUTmDeaeNmWtotuLjKE3MvCYYycGrak3AnBMuEX1EpR4synaZBqmGFgJK_RYyfuRoiqL-aFm9GVSfmKVGi2kiLBuAwTp0nuDlMoqjG6r53iG9x8D2RIRgv5DsgOgN8SS7KhNrQFoaL-rxlKmtrTSYPzWci8igX0lEBF7RrJV8qYnN1ER6V6KOQkwLQ1MhpROy3Lk3fu12Tz-_0UYHIzp9QNgFpL4y8b1ZsUmVxcyGkAqFg6TOPBi32mvnUdCre-QPTbA_dLwmF0yNETtVxAVZo4aeDOdihITWOZMCj6H5OLSglKMfWj615P9bWMf_Pihog7uRy-sDA5TjxLYhU4ZvH97xyQKEUWk6rnT_A6mH8DnfipmRBGFO1kkKUyrJvJBvADCt4nL567uFBvbGdFadUcq-JzBjzmFIe9CoU_VsEYtBWhAGV0sbajk_k_9Ls190ziS3d9lOSi5YRxAMLCipEKM3yGi2IduB_ERn1d5exZTCRolUrSdD4d3yFNlu1FQS7QtJD75FxN5BYCB03KgMBRUnyNejPU0RiqHx-liJQMQiPrRkDBpyxo-de5MBKoNSgt-nW44dmEWMrPK5h2jNpOwjtfbwxGxFFSp6txscWxaMDbo9mGSJeE6e27tb2WvX5YSNi68fccyo3PZLTF-H8ZixtfoTn4g1nL1JGA55-oXDCjq95ivHCJfGSdXmV99vayz6I_wp5O_JtUWqNz7kdy0Pf_SsPVbZS7bHyZ5WPNbHJLBWDCQRmaq_l8Z5Wq_QSwv4obJ14EiI6gO4QyDgrNISZkkg3fwMSDiTDKMoMVLi0xsXF7xJnGUoaH2Dvehd22ZFaozIpZB94wHIE0elFyUtXCLVqeNo-gyAUcIOdORSk7XqfCElKheoGzI4Op1kfF3rouycJtpjGcztaWblB-nY52VmuwOpsfRatMz60tA06ZSAQK3FzQmbJNPi_LyWagUDokf3pvAxfz1LJgnxe7cn55cPBW8IWOuHoxAPIZ7HePAxFtttra0C2F9_CB6SI11M9QSweb-rSgZwU_xc5wFsEb2CIWj3SgE4gbO3WxnbCJcXTd8RK_7yG9OzGhvLk-uzz7W6g1KXVFjb8mCWoxgGMK97SL_CjmkTQGcgwup24JzVo_IGkxVNJQyUDhfndbHoxLZstyyfKlrGxtEq6lS8JMPOGXaUEQK3yn_wkykYX0yrtmgM99sTXzHcpK1916aI_ev7q5X2KUjdDofJfybNKEVcSE8n-hSP8b652vCeFdkY7P4cXN29; _ga_EJRWLKGJNW=GS1.1.1603541129.5.1.1603542988.47; _ga=GA1.2.87188470.1602703684");
            headers.TryAdd("Accept-Language", "ru-RU,ru;q=0.9,en-US;q=0.8,en;q=0.7,uk;q=0.6,pl;q=0.5");
            var result = await _http.Execute(o =>
            {
                o.Host = new Uri("https://www.amccinemas.com/MovieDetails/getcinema");
                o.Method = HttpMethod.Post;
                o.Headers = headers;
            });
            if (result.IsSuccess)
            {
                List<Cinema> cinemas = new List<Cinema>();
                dynamic json = JObject.Parse(result.Response);
                foreach (var item in json.sourceData.CinemaList)
                {
                    Cinema cinema = new Cinema();
                    cinema.ExternalId = item.id;
                    cinema.Name = item.name;
                    cinemas.Add(cinema);
                }
                return new ParseResult<Data>(Result.Success)
                {
                    Data = new Data()
                    {
                        Cinemas = cinemas
                    }
                };
            }
            return new ParseResult<Parse.Data>(Result.Error);
        }
        #endregion
    }
}
