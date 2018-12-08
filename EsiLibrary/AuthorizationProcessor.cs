using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Net.Http;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;
using System.Web;

namespace EsiLibrary
{
    public class AuthorizationProcessor : IAuthorizationProcessor
    {
        private readonly string _baseAuthorizationUri = "https://login.eveonline.com/v2/oauth/authorize/";
        private readonly string _baseTokenUri = "https://login.eveonline.com/v2/oauth/token";
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
            httpClient.DefaultRequestHeaders.Host = "login.eveonline.com";
            httpClient.DefaultRequestHeaders.TryAddWithoutValidation("Content-Type", "application/x-www-form-urlencoded");
            
            var postData = new Dictionary<string, string>
            {
                {"grant_type", "authorization_code"},
                {"code", authorizationCode},
                {"client_id", clientId},
                {"code_verifier", _originalEncodedString}
            };

            return await HttpHelper.PostData<Token>(_baseTokenUri, httpClient, new FormUrlEncodedContent(postData));
        }

        private async Task ValidateToken()
        {
            throw new NotImplementedException();
        }

        private string GetAuthorizationUri(string clientId, string redirectUri, IEnumerable<string> scopes, string state)
        {
            var random = GenerateRandomString();

            var firstEncodedString = EncodeString(random);

            var scopeDelimitedString = GetScopeDelimitedString(scopes);
            var codeChallenge = EncodeString(HashString(firstEncodedString));

            _originalEncodedString = firstEncodedString;

            var authorizationUri =
                $"{_baseAuthorizationUri}?" +
                $"response_type=code&" +
                $"redirect_uri={EncodeUri(redirectUri)}&" +
                $"client_id={clientId}&" +
                $"scope={scopeDelimitedString}&" +
                $"code_challenge={codeChallenge}&" +
                $"code_challenge_method=S256&" +
                $"state={state}/";
            
            return authorizationUri;
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
            var authorizationCode = responseCode.Replace($"{redirectUri}?code=", "").Replace($"&state={state}%2F", "");

            return authorizationCode;
        }
    }
}