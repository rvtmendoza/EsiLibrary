using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Http;
using System.Threading.Tasks;
using Newtonsoft.Json;

namespace EsiLibrary
{
    public static class HttpHelper
    {
        private static readonly HttpListener _httpListener = new HttpListener();

        public static async Task<string> GetResponseFromHttpServer(string uri)
        {
            try
            {
                _httpListener.Prefixes.Add(uri);
                _httpListener.Start();

                var context = await _httpListener.GetContextAsync();

                var responseUri = context.Request.Url.ToString();

                context.Response.KeepAlive = false;
                context.Response.Close();

                return responseUri;
            }
            catch (Exception e)
            {
                Console.WriteLine(e);
                return null;
            }
        }

        public static async Task<T> PostData<T>(string uri, HttpClient httpClient, IEnumerable<KeyValuePair<string, string>> postData)
        {
            using (var content = new FormUrlEncodedContent(postData))
            {
                var response = await httpClient.PostAsync(uri, content);

                return JsonConvert.DeserializeObject<T>((await response.Content.ReadAsStringAsync()));
            }
        }
    }
}