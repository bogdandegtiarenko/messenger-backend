using Messenger.Domain.Core.DTOs;
using Messenger.Domain.Core.DTOs.Authentication;
using Messenger.Domain.Core.Models;
using Messenger.Domain.Core.Responses;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;
using System.Text;
using System.Threading.Tasks;

namespace Messenger.Services.Interfaces
{
    public interface IAccountService
    {
        Task<ServiceResponse<ClaimsIdentity>> Register(RegistrationDTO registrationData);
        Task<ServiceResponse<ClaimsIdentity>> Login(LoginDTO loginData);

        Task<ServiceResponse<ProfileDTO>> GetProfileByLogin(string login);

        Task<ServiceResponse<ProfileDTO>> GetContactProfile(string userLogin, Contact contact);
        Task<ServiceResponse<bool>> ChangePassword(string userLogin, ChangePasswordDTO changePasswordData);
        Task<ServiceResponse<bool>> ChangeEmail(string userLogin, string newEmail);
        Task<ServiceResponse<bool>> ChangeNickname(string userLogin, string newNickname);
        Task<ServiceResponse<bool>> RemoveAccount(string userLogin, string password);

    }
}
