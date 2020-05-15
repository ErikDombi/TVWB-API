using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Threading.Tasks;

namespace TVWBAPI
{
    public static class WebFunctions
    {
        public static async Task<string> GetLocation(string url)
        {
            HttpClientHandler httpClientHandler = new HttpClientHandler();
            httpClientHandler.AllowAutoRedirect = false;
            using (HttpClient client = new HttpClient(httpClientHandler))
            {
                var response = await client.GetAsync(url);
                return response.Headers.GetValues("Location").FirstOrDefault();
            }
        }

        public static async Task<List<Cookie>> GetCookies(string url)
        {
            var cookieContainer = new CookieContainer();
            var uri = new Uri(url);
            using (var httpClientHandler = new HttpClientHandler
            {
                CookieContainer = cookieContainer
            })
            {
                using (var httpClient = new HttpClient(httpClientHandler))
                {
                    await httpClient.GetAsync(uri);
                    return cookieContainer.GetCookies(uri).Cast<Cookie>().ToList();
                }
            }
        }
    }

    public static class ExtensionMethods
    {
        public static bool TryAddCookie(this WebRequest webRequest, Cookie cookie)
        {
            HttpWebRequest httpRequest = webRequest as HttpWebRequest;
            if (httpRequest == null)
            {
                return false;
            }

            if (httpRequest.CookieContainer == null)
            {
                httpRequest.CookieContainer = new CookieContainer();
            }

            httpRequest.CookieContainer.Add(cookie);
            return true;
        }
    }
 }
