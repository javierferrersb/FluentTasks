using Google.Apis.Auth.OAuth2;
using Google.Apis.Util.Store;

namespace FluentTasks.Infrastructure.Google
{
    public class GoogleAuthService : IGoogleAuthService
    {
        private UserCredential? _credential;

        private readonly ClientSecrets _clientSecrets = new()
        {
            ClientId = "649786439194-47tih5e6b7kvetmk4dhgntdaiar4u4oo.apps.googleusercontent.com",
            ClientSecret = "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"
        };

        public async Task<UserCredential> GetCredentialAsync()
        {
            if (_credential != null)
                return _credential;

            _credential = await GoogleWebAuthorizationBroker.AuthorizeAsync(
                _clientSecrets,
                ["https://www.googleapis.com/auth/tasks"],
                "user",
                CancellationToken.None,
                new FileDataStore("FluentTasks", true)
            );

            return _credential;
        }
    }
}
