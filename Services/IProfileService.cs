using Khoon_e_Hayat.ViewModels;
using Microsoft.AspNetCore.Http;

namespace Khoon_e_Hayat.Services
{
    public interface IProfileService
    {
        Task<ProfileViewModel> GetProfileAsync(int userId);
        Task<bool> UpdateProfileAsync(int userId, ProfileViewModel model);
        Task<(bool Success, string Message)> ChangePasswordAsync(int userId, string currentPassword, string newPassword);
        Task<(bool Success, string FilePath, string Message)> UploadProfilePictureAsync(int userId, IFormFile file);
        Task<bool> RemoveProfilePictureAsync(int userId);
    }
}