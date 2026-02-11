using Google.Apis.Auth.OAuth2;

namespace FluentTasks.Infrastructure.Google
{
    public interface IGoogleAuthService
    {
        Task<UserCredential> GetCredentialAsync();
    }
}
