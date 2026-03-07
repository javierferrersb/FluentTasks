using System.Reflection;
using Google.Apis.Auth.OAuth2;
using Google.Apis.Util.Store;

namespace FluentTasks.Infrastructure.Google
{
    public class GoogleAuthService : IGoogleAuthService
    {
        private const string ClientSecretsFileName = "client_secrets.json";

        private UserCredential? _credential;
        private readonly string _appDataPath;
        private readonly Lazy<ClientSecrets> _clientSecrets;

        public GoogleAuthService()
        {
            _appDataPath = Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
                "FluentTasks");

            _clientSecrets = new Lazy<ClientSecrets>(LoadClientSecrets);
        }

        private static ClientSecrets LoadClientSecrets()
        {
            // Look for client_secrets.json next to the executable
            var exeDir = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location)
                ?? throw new InvalidOperationException("Could not determine executable directory.");

            var secretsPath = Path.Combine(exeDir, ClientSecretsFileName);

            if (!File.Exists(secretsPath))
            {
                throw new FileNotFoundException(
                    $"Google API credentials not found. Please create '{ClientSecretsFileName}' in the application directory. " +
                    $"See 'client_secrets.json.template' for the expected format.",
                    secretsPath);
            }

            using var stream = File.OpenRead(secretsPath);
            var secrets = GoogleClientSecrets.FromStream(stream);
            return secrets.Secrets;
        }

        public async Task<UserCredential> GetCredentialAsync()
        {
            if (_credential != null)
                return _credential;

            _credential = await GoogleWebAuthorizationBroker.AuthorizeAsync(
                _clientSecrets.Value,
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

        public async Task<UserCredential> ReAuthenticateAsync()
        {
            // Clear any existing credential state
            _credential = null;

            // Delete stored token files so the broker performs a fresh login
            await DeleteStoredTokensAsync();

            // Perform a fresh interactive authentication
            _credential = await GoogleWebAuthorizationBroker.AuthorizeAsync(
                _clientSecrets.Value,
                ["https://www.googleapis.com/auth/tasks"],
                "user",
                CancellationToken.None,
                new FileDataStore(_appDataPath, true)
            );

            return _credential;
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
