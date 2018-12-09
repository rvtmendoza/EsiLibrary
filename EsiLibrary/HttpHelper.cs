using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Net;
using System.Net.Http;
using System.Threading.Tasks;

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
                Debug.Print(e.Message);
                return null;
            }
        }

        public static async Task<HttpResponseMessage> PostAsync(HttpClient httpClient, Uri requestUri, IEnumerable<KeyValuePair<string, string>> postParams)
        {
            try
            {
                var response = await httpClient.PostAsync(requestUri, new FormUrlEncodedContent(postParams));

                return response;
            }
            catch (Exception e)
            {
                Console.WriteLine(e);
                throw;
            }
        }
    }
}