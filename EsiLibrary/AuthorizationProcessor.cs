using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;
using System.Web;

namespace EsiLibrary
{
    public class AuthorizationProcessor
    {
        private readonly string _baseAuthorizationUri = "https://login.eveonline.com/v2/oauth/authorize/";
        private readonly string _baseTokenUri = "https://login.eveonline.com/v2/oauth/token";

        private string _originalEncodedString;

        public async Task<Token> Authorize(string clientId, string redirectUri, IEnumerable<string> scopes, string state)
        {
            var authorizationCode = await GetAuthorizationCode(clientId, redirectUri, scopes, state);

            var token = await GetToken(clientId, authorizationCode);

            //TODO: Add support for token validation
            //https://docs.esi.evetech.net/docs/sso/validating_eve_jwt.html

            return token;
        }

        public async Task RefreshToken()
        {
            //TODO: Add implementation for refreshing tokens
            //https://docs.esi.evetech.net/docs/sso/refreshing_access_tokens.html
            throw new NotImplementedException();
        }

        public async Task RevokeToken()
        {
            //TODO: Add implementation for revoking tokens
            //https://docs.esi.evetech.net/docs/sso/revoking_refresh_tokens.html
            throw new NotImplementedException();
        }

        private async Task<string> GetAuthorizationCode(string clientId, string redirectUri, IEnumerable<string> scopes, string state)
        {
            var authorizationUri = GetAuthorizationUri(clientId, redirectUri, scopes, state);

            Process.Start(authorizationUri);

            var responseCode = await HttpHelper.GetResponseFromHttpServer(authorizationUri);

            return ExtractAuthorizationCode(responseCode, redirectUri, state);
        }

        private async Task<Token> GetToken(string clientId, string authorizationCode)
        {
            var httpClient = new HttpClient();
            httpClient.DefaultRequestHeaders.Host = "login.eveonline.com";
            
            var postData = new List<KeyValuePair<string, string>>
            {
                new KeyValuePair<string, string>("grant_type", "authorization_code"),
                new KeyValuePair<string, string>("code", authorizationCode),
                new KeyValuePair<string, string>("client_id", clientId),
                new KeyValuePair<string, string>("code_verifier", _originalEncodedString)
            };

            return await HttpHelper.PostData<Token>(_baseTokenUri, httpClient, postData);
        }

        private async Task ValidateToken()
        {
            throw new NotImplementedException();
        }

        private string GetAuthorizationUri(string clientId, string redirectUri, IEnumerable<string> scopes, string state)
        {
            _originalEncodedString = EncodeString(GenerateRandomString());

            var scopeDelimitedString = GetScopeDelimitedString(scopes);
            var codeChallenge = EncodeString(HashString(_originalEncodedString));

            return
                $"{_baseAuthorizationUri}?" +
                $"response_type=code&" +
                $"redirect_uri={EncodeUri(redirectUri)}&" +
                $"client_id={clientId}&" +
                $"scope={scopeDelimitedString}&" +
                $"code_challenge={codeChallenge}&" +
                $"code_challenge_method=S256&" +
                $"state={state}";
        }

        private string EncodeUri(string redirectUri)
        {
            return HttpUtility.UrlEncode(redirectUri);
        }

        private string GetScopeDelimitedString(IEnumerable<string> scopes)
        {
            return string.Join("%20", scopes);
        }

        private string GenerateRandomString(int length = 32)
        {
            const string chars = "ABCDEFGHIJKLMNOPQRSTUVWXYZabcdefghijklmnopqrstuvwxyz";
            var random = new Random();

            return new string(Enumerable.Repeat(chars, length).Select(x => x[random.Next(x.Length)]).ToArray());
        }

        private string EncodeString(string unencodedString)
        {
            var bytes = Encoding.UTF8.GetBytes(unencodedString);
            
            return HttpServerUtility.UrlTokenEncode(bytes);
        }

        private string HashString(string unhashedString)
        {
            var stringBuilder = new StringBuilder();

            using (var hash = SHA256.Create())
            {
                var bytes = hash.ComputeHash(Encoding.UTF8.GetBytes(unhashedString));

                foreach (byte b in bytes)
                {
                    stringBuilder.Append(b.ToString("x2"));
                }
            }

            return stringBuilder.ToString();
        }

        private string ExtractAuthorizationCode(string responseCode, string redirectUri, string state)
        {
            return responseCode.Replace($"{redirectUri}?code=", "").Replace($"&state={state}", "");
        }
    }
}