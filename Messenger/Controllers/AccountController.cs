using Messenger.DAL.Repositories;
using Messenger.Services.Interfaces;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Azure;
using System.Text.Json;
using Messenger.Domain.Core.Responses;
using Microsoft.AspNetCore.Cors;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.SignalR;
using Messenger.Hubs;
using Messenger.Domain.Core.DTOs.Authentication;
using Messenger.Domain.Core.DTOs;

namespace Messenger.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class AccountController : ControllerBase
    {
        private IAccountService _accountService;
        private IContactService _contactService;
        private IHubContext<MessengerHub> _hubContext;
        private UserConnections _userConnections;
        private UserOnlineContacts _userOnlineContacts;

        public AccountController(
            IAccountService accountService,
            IContactService contactService,
            IHubContext<MessengerHub> hubContext,
            UserConnections userConnections,
            UserOnlineContacts userOnlineContacts)
        {
            _accountService = accountService;
            _contactService = contactService;
            _hubContext = hubContext;
            _userConnections = userConnections;
            _userOnlineContacts = userOnlineContacts;
        }

        [HttpPost("Register")]
        public async Task<IActionResult> Register(RegistrationDTO registrationData)
        {
            try
            {
                var response = await _accountService.Register(registrationData);
                if (response.Status == Domain.Core.StatusCode.Success)
                {
                    var identity = response.Data;
                    await HttpContext.SignInAsync(CookieAuthenticationDefaults.AuthenticationScheme,
                        new ClaimsPrincipal(identity));

                    return Ok("Successful registration");
                }
                else
                {
                    return BadRequest(response.Message);
                }
            }
            catch(Exception ex)
            {
                return BadRequest(ex.Message);
            }
        }

        [HttpPut("Login")]
        public async Task<IActionResult> Login(LoginDTO loginData)
        {
            try
            {
                var response = await _accountService.Login(loginData);
                if (response.Status == Domain.Core.StatusCode.Success)
                {
                    var identity = response.Data;
                    await HttpContext.SignInAsync(CookieAuthenticationDefaults.AuthenticationScheme,
                        new ClaimsPrincipal(identity));
                    return Ok("Successful authorization");
                }
                else
                {
                    return BadRequest(response.Message);
                }
            }
            catch (Exception ex)
            {
                return BadRequest(ex.Message);
            }
        }

        [Authorize]
        [HttpGet("Logout")]
        public async Task<IActionResult> Logout()
        {
            try
            {
                await HttpContext.SignOutAsync();
                return Ok("Success logout");
            }
            catch (Exception ex)
            {
                return BadRequest(ex.Message);
            }
        }

        [Authorize]
        [HttpGet("GetOwnProfile")]
        public async Task<IActionResult> GetOwnProfile()
        {
            string? userLogin = User?.Identity?.Name;
            if (userLogin == null)
                return BadRequest("Login is null");

            try
            {
                ProfileDTO? userProfile = (await _accountService.GetProfileByLogin(userLogin)).Data;
                return Ok(userProfile);
            }
            catch(Exception ex)
            {
                return BadRequest(ex.Message);
            }
        }

        [Authorize]
        [HttpPut("ChangeNickname")]
        public async Task<IActionResult> ChangeNickname(string newNickname)
        {
            string? userLogin = User?.Identity?.Name;
            if (userLogin == null)
                return BadRequest("Login is null");

            try
            {
                ServiceResponse<bool> response = (await _accountService.ChangeNickname(userLogin, newNickname));
                bool isNicknameChanged = response.Data;
                if (isNicknameChanged)
                {
                    if (_userOnlineContacts.ContainsKey(userLogin))
                    {
                        foreach (var onlineContact in _userOnlineContacts[userLogin])
                        {
                            await _hubContext.Clients.Clients(_userConnections[onlineContact])
                                .SendAsync("ReceiveContactNickChange", new
                                {
                                    contactLogin = userLogin,
                                    newNickname = newNickname,
                                });
                        }
                    }
                    return Ok(response.Message);
                }
                else
                    return BadRequest(response.Message);
            }
            catch(Exception ex)
            {
                return BadRequest(ex.Message);
            }
        }

        [Authorize]
        [HttpPut("ChangeEmail")]
        public async Task<IActionResult> ChangeEmail(string newEmail)
        {
            string? userLogin = User?.Identity?.Name;
            if (userLogin == null)
                return BadRequest("Login is null");

            try
            {
                ServiceResponse<bool> response = (await _accountService.ChangeEmail(userLogin, newEmail));
                bool isEmailChanged = response.Data;
                if (isEmailChanged)
                    return Ok(response.Message);
                else
                    return BadRequest(response.Message);
            }
            catch (Exception ex)
            {
                return BadRequest(ex.Message);
            }
        }


        [Authorize]
        [HttpPut("ChangePassword")]
        public async Task<IActionResult> ChangePassword(ChangePasswordDTO changePasswordData)
        {
            string? userLogin = User?.Identity?.Name;
            if (userLogin == null)
                return BadRequest("Login is null");

            try
            {
                bool isPasswordChanged = (await _accountService.ChangePassword(userLogin, changePasswordData)).Data;
                if (isPasswordChanged)
                    return Ok("Password changed");
                else
                    return BadRequest("Password unchanged");
            }
            catch(Exception ex)
            {
                return BadRequest(ex.Message);
            }
        }


        [Authorize]
        [HttpDelete("RemoveAccount")]
        public async Task<IActionResult> RemoveAccount(string password)
        {
            string? userLogin = User?.Identity?.Name;
            if (userLogin == null)
                return BadRequest("Login is null");

            try
            {
                ServiceResponse<bool> serviceResponse = (await _accountService.RemoveAccount(userLogin, password));
                bool isAccountRemoved = serviceResponse.Data;
                if (isAccountRemoved)
                {
                    await HttpContext.SignOutAsync();
                    return Ok("Account removed");
                }
                else
                    return BadRequest(serviceResponse.Message);
            }
            catch(Exception ex)
            {
                return BadRequest(ex.Message);
            }
        }
    }
}
