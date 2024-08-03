using Messenger.Services.Interfaces;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Messenger.DAL.Repositories;
using Messenger.DAL.Interfaces;
using Messenger.Domain.Core.Responses;
using Messenger.Domain.Core;
using System.Security.Claims;
using Microsoft.EntityFrameworkCore.Update;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Newtonsoft.Json.Linq;
using Messenger.Domain.Core.DTOs.Authentication;
using Messenger.Domain.Core.DTOs;
using Messenger.Domain.Core.Models;
using Serilog;
using Microsoft.Extensions.Logging;

namespace Messenger.Services.Implementations
{
    public class AccountService : IAccountService
    {
        private IUserRepository _userRepository;
        private IProfileRepository _profileRepository;
        private ILogger<AccountService> _logger;

        public AccountService(
            IUserRepository userRepository,
            IProfileRepository profileRepository,
            ILogger<AccountService> logger)
        {
            _userRepository = userRepository;
            _profileRepository = profileRepository;
            _logger = logger;
        }

        public async Task<ServiceResponse<bool>> ChangeEmail(string userLogin, string newEmail)
        {
            _logger.LogInformation("ChangeEmail called with userLogin: {userLogin}, newEmail: {newEmail}", userLogin, newEmail);
            ServiceResponse<bool> serviceResponse = new ServiceResponse<bool>();
            var user = _userRepository.GetAll().FirstOrDefault(u => u.Login == userLogin);
            if (user == null)
            {
                _logger.LogWarning("ChangeEmail: User not found with login: {userLogin}", userLogin);
                serviceResponse.Data = false;
                serviceResponse.Status = StatusCode.Fail;
                serviceResponse.Message = "The user with this email doesn't exist";
            }
            else
            {
                user.Email = newEmail;
                RepositoryResponse repositoryResponse = await _userRepository.Update(user);
                serviceResponse.Status = repositoryResponse.Status;
                serviceResponse.Data = repositoryResponse.IsSuccessCompleted;
                serviceResponse.Message = repositoryResponse.Message;
                _logger.LogInformation("ChangeEmail: Email changed for user {userLogin} to {newEmail}, Status: {status}, Message: {message}", userLogin, newEmail, repositoryResponse.Status, repositoryResponse.Message);
            }
            return serviceResponse;
        }

        public async Task<ServiceResponse<bool>> ChangeNickname(string userLogin, string newNickname)
        {
            _logger.LogInformation("ChangeNickname called with userLogin: {userLogin}, newNickname: {newNickname}", userLogin, newNickname);
            ServiceResponse<bool> serviceResponse = new ServiceResponse<bool>();
            var user = _userRepository.GetAll().Include(u => u.Profile).FirstOrDefault(u => u.Login == userLogin);
            if (user == null)
            {
                _logger.LogWarning("ChangeNickname: User not found with login: {userLogin}", userLogin);
                serviceResponse.Data = false;
                serviceResponse.Status = StatusCode.Fail;
                serviceResponse.Message = "The user with this login doesn't exist";
            }
            else
            {
                user.Profile.Nickname = newNickname;
                RepositoryResponse repositoryResponse = await _userRepository.Update(user);
                serviceResponse.Status = repositoryResponse.Status;
                serviceResponse.Data = repositoryResponse.IsSuccessCompleted;
                serviceResponse.Message = repositoryResponse.Message;
                _logger.LogInformation("ChangeNickname: Nickname changed for user {userLogin} to {newNickname}, Status: {status}, Message: {message}", userLogin, newNickname, repositoryResponse.Status, repositoryResponse.Message);
            }
            return serviceResponse;
        }

        public async Task<ServiceResponse<bool>> ChangePassword(string userLogin, ChangePasswordDTO changePasswordData)
        {
            _logger.LogInformation("ChangePassword called with userLogin: {userLogin}", userLogin);
            ServiceResponse<bool> serviceResponse = new ServiceResponse<bool>();
            var user = _userRepository.GetAll().FirstOrDefault(u => u.Login == userLogin);

            if (user == null)
            {
                _logger.LogWarning("ChangePassword: User not found with login: {userLogin}", userLogin);
                serviceResponse.Data = false;
                serviceResponse.Status = StatusCode.Fail;
                serviceResponse.Message = "The user with this login doesn't exist";
            }
            else
            {
                string currentPasswordHash = Hasher.GetSHA256Hash(changePasswordData.CurrentPassword);
                if (currentPasswordHash == user.Password)
                {
                    user.Password = Hasher.GetSHA256Hash(changePasswordData.NewPassword);
                    RepositoryResponse repositoryResponse = await _userRepository.Update(user);

                    serviceResponse.Status = repositoryResponse.Status;
                    serviceResponse.Data = repositoryResponse.IsSuccessCompleted;
                    serviceResponse.Message = "Password changed successfully";
                    _logger.LogInformation("ChangePassword: Password changed for user {userLogin}, Status: {status}, Message: {message}", userLogin, repositoryResponse.Status, repositoryResponse.Message);
                }
                else
                {
                    _logger.LogWarning("ChangePassword: Current password does not match for user {userLogin}", userLogin);
                    serviceResponse.Status = StatusCode.Fail;
                    serviceResponse.Data = false;
                    serviceResponse.Message = "CurrentPassword doesn't match current user password";
                }
            }
            return serviceResponse;
        }

        public async Task<ServiceResponse<ProfileDTO>> GetContactProfile(string userLogin, Contact contact)
        {
            _logger.LogInformation("GetContactProfile called with userLogin: {userLogin}, contact: {contact}", userLogin, contact);
            ServiceResponse<ProfileDTO> serviceResponse = new ServiceResponse<ProfileDTO>();

            var _userLogin = contact.User.Login;
            var _contactLogin = contact.ContactUser.Login;
            if (_userLogin != userLogin && _contactLogin != userLogin)
            {
                _logger.LogError("GetContactProfile: Incorrect contact for userLogin: {userLogin}", userLogin);
                throw new Exception("Uncorrect contact");
            }

            string contactLogin = contact.User.Login == userLogin
                ? contact.ContactUser.Login : contact.User.Login;

            User? contactUser = _userRepository.GetAll()
                .Include(u => u.Profile)
                .FirstOrDefault(u => u.Login == contactLogin);

            if (contactUser == null)
            {
                _logger.LogWarning("GetContactProfile: Contact user not found for login: {contactLogin}", contactLogin);
                serviceResponse.Data = null;
                serviceResponse.Status = StatusCode.Fail;
                serviceResponse.Message = "Contact user doesn't exist";
            }
            else
            {
                string? avatarData = (contactUser.Profile.Avatar == null)
                    ? null
                    : Encoding.UTF8.GetString(contactUser.Profile.Avatar);

                serviceResponse.Data = new ProfileDTO
                {
                    Avatar = avatarData,
                    Login = contactUser.Login,
                    Nickname = contactUser.Profile.Nickname
                };
                serviceResponse.Status = StatusCode.Success;
                serviceResponse.Message = "Success";
                _logger.LogInformation("GetContactProfile. UserLogin: {userLogin} Profile retrieved for contactLogin: {contactLogin}, Status: {status}, Message: {message}", userLogin, contactLogin, serviceResponse.Status, serviceResponse.Message);
            }
            return serviceResponse;
        }

        public async Task<ServiceResponse<ProfileDTO>> GetProfileByLogin(string login)
        {
            _logger.LogInformation("GetProfileByLogin called with login: {login}", login);
            ServiceResponse<ProfileDTO> serviceResponse = new ServiceResponse<ProfileDTO>();
            User? user = _userRepository.GetAll()
                .Include(u => u.Profile)
                .FirstOrDefault(u => u.Login == login);

            if (user == null)
            {
                _logger.LogWarning("GetProfileByLogin: User not found with login: {login}", login);
                serviceResponse.Status = StatusCode.Fail;
                serviceResponse.Data = null;
                serviceResponse.Message = "User with this login doesn't exist";
            }
            else
            {
                string? avatarData = (user.Profile.Avatar == null)
                    ? null
                    : Encoding.UTF8.GetString(user.Profile.Avatar);

                ProfileDTO profile = new ProfileDTO
                {
                    Avatar = avatarData,
                    Login = user.Login,
                    Nickname = user.Profile.Nickname
                };

                serviceResponse.Status = StatusCode.Success;
                serviceResponse.Data = profile;
                serviceResponse.Message = "Success";
                _logger.LogInformation("GetProfileByLogin: Profile retrieved for login: {login}, Status: {status}, Message: {message}", login, serviceResponse.Status, serviceResponse.Message);
            }
            return serviceResponse;
        }

        public async Task<ServiceResponse<ClaimsIdentity>> Login(LoginDTO loginData)
        {
            _logger.LogInformation("Login called with login: {login}", loginData.Login);
            ServiceResponse<ClaimsIdentity> response = new ServiceResponse<ClaimsIdentity>();
            User? user = _userRepository.GetAll().FirstOrDefault(u => u.Login == loginData.Login);
            if (user == null)
            {
                _logger.LogWarning("Login: User not found with login: {login}", loginData.Login);
                response.Data = null;
                response.Status = StatusCode.Fail;
                response.Message = "There is not user with this login";
            }
            else
            {
                string userPasswordHash = Hasher.GetSHA256Hash(loginData.Password);
                if (userPasswordHash == user.Password)
                {
                    var claims = new List<Claim>
                {
                    new Claim(ClaimTypes.Name, user.Login)
                };

                    response.Data = new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme);
                    response.Status = StatusCode.Success;
                    response.Message = "Successful authorization";
                    _logger.LogInformation("Login: Successful authorization for login: {login}", loginData.Login);
                }
                else
                {
                    _logger.LogWarning("Login: Incorrect password for login: {login}", loginData.Login);
                    response.Data = null;
                    response.Status = StatusCode.Fail;
                    response.Message = "Incorrect password";
                }
            }
            return response;
        }

        public async Task<ServiceResponse<ClaimsIdentity>> Register(RegistrationDTO registrationData)
        {
            _logger.LogInformation("Register called with login: {login}, email: {email}", registrationData.Login, registrationData.Email);
            ServiceResponse<ClaimsIdentity> response = new ServiceResponse<ClaimsIdentity>();

            var loginUser = _userRepository.GetAll().FirstOrDefault(u => u.Login == registrationData.Login);
            if (loginUser != null)
            {
                _logger.LogWarning("Register: User already exists with login: {login}", registrationData.Login);
                response.Status = StatusCode.Fail;
                response.Data = null;
                response.Message = "The user with the current login already exists";
                return response;
            }

            var emailUser = _userRepository.GetAll().FirstOrDefault(u => u.Email == registrationData.Email);
            if (emailUser != null)
            {
                _logger.LogWarning("Register: User already exists with email: {email}", registrationData.Email);
                response.Status = StatusCode.Fail;
                response.Data = null;
                response.Message = "The user with the current email already exists";
                return response;
            }

            byte[]? avatarBytes = Encoding.UTF8.GetBytes(registrationData.Avatar);

            var profile = new Profile
            {
                Avatar = avatarBytes,
                Nickname = registrationData.Nickname
            };

            string passwordHash = Hasher.GetSHA256Hash(registrationData.Password);

            var user = new User
            {
                Email = registrationData.Email,
                Login = registrationData.Login,
                Password = passwordHash,
                Profile = profile
            };

            await _profileRepository.Create(profile);

            var repositoryResponse = await _userRepository.Create(user);

            if (repositoryResponse.IsSuccessCompleted)
            {
                var identity = new ClaimsIdentity(new List<Claim>
            {
                new Claim(ClaimTypes.Name, registrationData.Login)
            }, CookieAuthenticationDefaults.AuthenticationScheme);
                response.Status = StatusCode.Success;
                response.Message = "Success register";
                response.Data = identity;
                _logger.LogInformation("Register: Successful registration for login: {login}", registrationData.Login);
            }
            else
            {
                _logger.LogError("Register: Failed to register user with login: {login}, Message: {message}", registrationData.Login, repositoryResponse.Message);
                response.Status = StatusCode.Fail;
                response.Message = repositoryResponse.Message;
                response.Data = null;
            }
            return response;
        }

        public async Task<ServiceResponse<bool>> RemoveAccount(string userLogin, string password)
        {
            _logger.LogInformation("RemoveAccount called with userLogin: {userLogin}", userLogin);
            ServiceResponse<bool> serviceResponse = new ServiceResponse<bool>();

            User? user = _userRepository
                .GetAll()
                .Include(u => u.Profile)
                .FirstOrDefault(u => u.Login == userLogin);

            if (user == null)
            {
                _logger.LogWarning("RemoveAccount: User not found with login: {userLogin}", userLogin);
                serviceResponse.Status = StatusCode.Fail;
                serviceResponse.Data = false;
                serviceResponse.Message = "Account doesn't exist";
            }
            else
            {
                if (Hasher.GetSHA256Hash(password) == user.Password)
                {
                    Profile profile = user.Profile;
                    RepositoryResponse repositoryResponse = await _userRepository.Delete(user);
                    RepositoryResponse profileRepositoryResponse = await _profileRepository.Delete(profile);
                    bool isUserDeleted = !_userRepository.GetAll().Contains(user);

                    if (isUserDeleted == false)
                    {
                        _logger.LogError("RemoveAccount: User was not deleted for userLogin: {userLogin}", userLogin);
                        throw new Exception("User wasn't deleted when profile was");
                    }

                    serviceResponse.Message = repositoryResponse.Message;
                    serviceResponse.Data = repositoryResponse.IsSuccessCompleted;

                    if (repositoryResponse.IsSuccessCompleted)
                    {
                        serviceResponse.Status = StatusCode.Success;
                        _logger.LogInformation("RemoveAccount: Account successfully removed for userLogin: {userLogin}", userLogin);
                    }
                    else
                    {
                        serviceResponse.Status = StatusCode.Fail;
                        _logger.LogError("RemoveAccount: Failed to remove account for userLogin: {userLogin}, Message: {message}", userLogin, repositoryResponse.Message);
                    }
                }
                else
                {
                    _logger.LogWarning("RemoveAccount: Incorrect password for userLogin: {userLogin}", userLogin);
                    serviceResponse.Status = StatusCode.Fail;
                    serviceResponse.Data = false;
                    serviceResponse.Message = "Password doesn't match";
                }
            }

            return serviceResponse;
        }
    }
}
