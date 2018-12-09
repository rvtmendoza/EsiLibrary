using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Net.Http;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;
using System.Web;
using Newtonsoft.Json;

namespace EsiLibrary
{
    public class PkceAuthorizationProcessor : IAuthorizationProcessor
    {
        private readonly string _baseAuthorizationUri = "https://login.eveonline.com/v2/oauth/authorize/";
        private readonly Uri _baseTokenUri = new Uri("https://login.eveonline.com/v2/oauth/token/");
        private readonly string _baseTokenValidationUri = "https://login.eveonline.com/oauth/jwks/";

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

            var responseCode = await HttpHelper.GetResponseFromHttpServer(redirectUri);

            return ExtractAuthorizationCode(responseCode, redirectUri, state);
        }

        private async Task<Token> GetToken(string clientId, string authorizationCode)
        {
            var httpClient = new HttpClient();
            httpClient.DefaultRequestHeaders.Clear();
            httpClient.DefaultRequestHeaders.TryAddWithoutValidation("Host", "login.eveonline.com");
            httpClient.DefaultRequestHeaders.TryAddWithoutValidation("Content-Type", "application/x-www-form-urlencoded");

            var postParams = new Dictionary<string, string>
            {
                {"grant_type", "authorization_code"},
                {"code", authorizationCode},
                {"client_id", clientId},
                {"code_verifier", _originalEncodedString}
            };

            var response = await HttpHelper.PostAsync(httpClient, _baseTokenUri, postParams);

            return JsonConvert.DeserializeObject<Token>(await response.Content.ReadAsStringAsync());
        }

        private async Task ValidateToken()
        {
            throw new NotImplementedException();
        }

        private string GetAuthorizationUri(string clientId, string redirectUri, IEnumerable<string> scopes, string state)
        {
            var randomByteData = GenerateRandomString();

            var originalEncodedString = EncodeString(randomByteData);
            
            var codeChallenge = EncodeString(HashString(originalEncodedString));

            _originalEncodedString = originalEncodedString;

            var authorizationUri =
                $"{_baseAuthorizationUri}?" +
                $"response_type=code&" +
                $"redirect_uri={HttpUtility.UrlEncode(redirectUri)}&" +
                $"client_id={clientId}&" +
                $"scope={string.Join("%20", scopes)}&" +
                $"code_challenge={codeChallenge}&" +
                $"code_challenge_method=S256&" +
                $"state={state}/";

            return authorizationUri;
        }
        
        private byte[] GenerateRandomString(int length = 32)
        {
            var random = new Random();

            byte[] cc = new byte[length];

            random.NextBytes(cc);

            return cc;
        }

        private string EncodeString(byte[] data)
        {
            return Convert.ToBase64String(data).Replace('+', '-').Replace('/', '_').Replace("=","");
        }

        private byte[] HashString(string unhashedString)
        {
            byte[] hash;

            using (var sha = new SHA256Managed())
            {
                hash = sha.ComputeHash(Encoding.ASCII.GetBytes(unhashedString));
            }
        
            return hash;
        }

        private string ExtractAuthorizationCode(string responseCode, string redirectUri, string state)
        {
            var authorizationCode = responseCode.Replace($"{redirectUri}?code=", "").Replace($"&state={state}%2F", "");

            return authorizationCode;
        }
    }
}