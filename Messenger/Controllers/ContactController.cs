using Messenger.Services.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using System.Text.RegularExpressions;
using Messenger.Domain.Core.Responses;
using Messenger.Hubs;
using Microsoft.AspNetCore.SignalR;
using Messenger.Domain.Core.DTOs;

namespace Messenger.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class ContactController : ControllerBase
    {
        private IContactService _contactService;
        private IAccountService _accountService;
        private IHubContext<MessengerHub> _hubContext;
        private UserConnections _userConnections;
        private UserContacts _userContacts;
        private UserOnlineContacts _userOnlineContacts;
        private UserLoginsOnline _userLoginsOnline;

        public ContactController(
                IContactService contactService,
                IAccountService accountService,
                IHubContext<MessengerHub> hubContext,
                UserConnections userConnections,
                UserContacts userContacts,
                UserOnlineContacts userOnlineContacts,
                UserLoginsOnline userLoginsOnline
                )
        {
            _contactService = contactService;
            _accountService = accountService;
            _hubContext = hubContext;
            _userConnections = userConnections;
            _userContacts = userContacts;
            _userOnlineContacts = userOnlineContacts;
            _userLoginsOnline = userLoginsOnline;
        } 


        [Authorize]
        [HttpPost("AddContact")]
        public async Task<IActionResult> AddContact(string? contactLogin) 
        {
            string? userLogin = User?.Identity?.Name;
            if (userLogin == null)
                return BadRequest("Login is null");

            try
            {
                ServiceResponse<bool> serviceResponse = (await _contactService.AddContact(userLogin, contactLogin));
                bool isContactAdded = serviceResponse.Data;
                if (isContactAdded)
                {
                    _userContacts[userLogin].Add(contactLogin);

                    ProfileDTO? contactProfile = (await _accountService.GetProfileByLogin(contactLogin)).Data;
                    ProfileDTO? userProfile = (await _accountService.GetProfileByLogin(userLogin)).Data;
                    
                    if (_userLoginsOnline.Contains(contactLogin))
                    {
                        if (_userOnlineContacts.ContainsKey(userLogin))
                        {
                            _userContacts[contactLogin].Add(userLogin);

                            _userOnlineContacts[userLogin].Add(contactLogin);
                            _userOnlineContacts[contactLogin].Add(userLogin);
                        }

                        if(contactLogin != userLogin)
                        {
                            await _hubContext.Clients.Clients(_userConnections[contactLogin])
                            .SendAsync("ReceiveNewContact", userProfile);
                        }
                    }
                    
                    return Ok(contactProfile);
                }
                else
                {
                    return BadRequest(serviceResponse.Message);
                }
            }
            catch(Exception ex)
            {
                return BadRequest(ex.Message);
            }
        }


        [Authorize]
        [HttpDelete("RemoveContact")]
        public async Task<IActionResult> RemoveContact(string? contactLogin)
        {
            string? userLogin = User?.Identity?.Name;
            if (userLogin == null)
                return BadRequest("Login is null");

            try
            {
                ServiceResponse<bool> serviceResponse = (await _contactService.RemoveContact(userLogin, contactLogin));
                bool isContactRemoved = serviceResponse.Data;
                if (isContactRemoved)
                {
                    _userContacts[userLogin].Remove(contactLogin);

                    if(_userLoginsOnline.Contains(contactLogin))
                    {

                        if (_userOnlineContacts.ContainsKey(userLogin))
                        {
                            _userContacts[contactLogin].Remove(userLogin);

                            _userOnlineContacts[userLogin].Remove(contactLogin);
                            _userOnlineContacts[contactLogin].Remove(userLogin);
                        }


                        if(contactLogin != userLogin)
                        {
                            await _hubContext.Clients.Clients(_userConnections[contactLogin])
                            .SendAsync("ReceiveRemovedContact", new
                            {
                                login = userLogin
                            });
                        }
                    }

                    return Ok("Contact removed");
                }
                else
                {
                    return BadRequest("Contact not removed");
                }
            }
            catch(Exception ex)
            {
                return BadRequest(ex.Message);
            }
        }


        [Authorize]
        [HttpGet("GetContactBlockInfos")]
        public async Task<IActionResult> GetContactBlockInfos()
        {
            string? userLogin = User?.Identity?.Name;

            if (userLogin == null)
                return BadRequest("Login is null");

            try
            {
                var serviceResponse = await _contactService.GetContactBlockInfos(userLogin);
                if(serviceResponse.Status == Domain.Core.StatusCode.Success)
                {
                    var contactBlockInfos = serviceResponse.Data;
                    return Ok(contactBlockInfos);
                }
                else
                {
                    return BadRequest(serviceResponse.Message);
                }
            }
            catch(Exception ex)
            {
                return BadRequest(ex.Message);
            }
        }
    }
}
