using Google.Apis.Auth.OAuth2;
using Google.Apis.Util.Store;

namespace FluentTasks.Infrastructure.Google
{
    public class GoogleAuthService : IGoogleAuthService
    {
        private UserCredential? _credential;
        private readonly string _appDataPath;

        private readonly ClientSecrets _clientSecrets = new()
        {
            ClientId = "649786439194-47tih5e6b7kvetmk4dhgntdaiar4u4oo.apps.googleusercontent.com",
            ClientSecret = "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"
        };

        public GoogleAuthService()
        {
            _appDataPath = Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
                "FluentTasks");
        }

        public async Task<UserCredential> GetCredentialAsync()
        {
            if (_credential != null)
                return _credential;

            _credential = await GoogleWebAuthorizationBroker.AuthorizeAsync(
                _clientSecrets,
                ["https://www.googleapis.com/auth/tasks"],
                "user",
                CancellationToken.None,
                new FileDataStore(_appDataPath, true)
            );

            return _credential;
        }

        public async Task LogOutAsync()
        {
            // Revoke the token if we have a valid credential
            if (_credential != null)
            {
                try
                {
                    await _credential.RevokeTokenAsync(CancellationToken.None);
                }
                catch
                {
                    // Ignore revocation errors - token may already be invalid
                }
            }

            // Clear in-memory credential
            _credential = null;

            // Delete stored token files
            await DeleteStoredTokensAsync();
        }

        private Task DeleteStoredTokensAsync()
        {
            try
            {
                if (Directory.Exists(_appDataPath))
                {
                    // Delete Google.Apis.Auth token files
                    var tokenFiles = Directory.GetFiles(_appDataPath, "Google.Apis.Auth.*");
                    foreach (var file in tokenFiles)
                    {
                        try
                        {
                            File.Delete(file);
                        }
                        catch
                        {
                            // Continue on error
                        }
                    }
                }
            }
            catch
            {
                // Silently fail if deletion fails
            }

            return Task.CompletedTask;
        }
    }
}
