using System.Collections.Generic;
using System.Threading.Tasks;

namespace EsiLibrary
{
    public interface IAuthorizationProcessor
    {
        Task<Token> Authorize(string clientId, string redirectUri, IEnumerable<string> scopes, string state);
        Task RefreshToken();
        Task RevokeToken();
    }
}