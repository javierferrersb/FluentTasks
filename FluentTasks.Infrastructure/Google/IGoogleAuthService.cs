using Google.Apis.Auth.OAuth2;

namespace FluentTasks.Infrastructure.Google
{
    public interface IGoogleAuthService
    {
        /// <summary>
        /// Gets the user credential for Google API access, prompting for authentication if needed.
        /// </summary>
        Task<UserCredential> GetCredentialAsync();

        /// <summary>
        /// Logs out the current user by revoking tokens and clearing stored credentials.
        /// </summary>
        Task LogOutAsync();

        /// <summary>
        /// Clears stored credentials and performs a fresh interactive authentication.
        /// Used when the existing token has expired and cannot be refreshed.
        /// </summary>
        Task<UserCredential> ReAuthenticateAsync();
    }
}
