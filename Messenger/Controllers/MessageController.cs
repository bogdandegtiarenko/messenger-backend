using Messenger.DAL.Repositories;
using Messenger.Domain.Core.DTOs;
using Messenger.Domain.Core.Responses;
using Messenger.Hubs;
using Messenger.Services.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.SignalR;
using System.Text.Json;

namespace Messenger.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class MessageController : ControllerBase
    {
        private IMessageService _messageService;
        private IHubContext<MessengerHub> _hubContext;
        private UserConnections _userConnections;
        private UserOnlineContacts _userOnlineContacts;
        private UserLoginsOnline _userLoginsOnline;

        public MessageController(
            IMessageService messageService,
            IHubContext<MessengerHub> hubContext,
            UserConnections userConnections,
            UserOnlineContacts userOnlineContacts,
            UserLoginsOnline userLoginsOnline
        )
        {
            _messageService = messageService;
            _hubContext = hubContext;
            _userConnections = userConnections;
            _userOnlineContacts = userOnlineContacts;
            _userLoginsOnline = userLoginsOnline;
        }


        [Authorize]
        [HttpPost("AddMessage")]
        public async Task<IActionResult> AddMessage(ClientMessageForm messageForm)
        {
            string? userLogin = User?.Identity?.Name;
            if (userLogin == null)
                return BadRequest("Login is null");

            try
            {
                ServiceResponse<MessageActionDTO> serviceResponse = (await _messageService.AddMessage(messageForm));

                if (serviceResponse.Status == Domain.Core.StatusCode.Success)
                {
                    if (_userLoginsOnline.Contains(messageForm.RecipientLogin) &&
                        messageForm.RecipientLogin != userLogin)
                    {
                        string onlineContactLogin = messageForm.RecipientLogin;

                        await _hubContext.Clients.Clients(_userConnections[onlineContactLogin])
                            .SendAsync("ReceiveMessage", new
                            {
                                senderLogin = userLogin,
                                recipientLogin = messageForm.RecipientLogin,
                                text = messageForm.Text,
                                id = serviceResponse?.Data?.Id,
                                dateTime = serviceResponse?.Data?.OperationDate
                            });
                    }

                    if (_userConnections.ContainsKey(messageForm.SenderLogin))
                    {
                        List<string> senderConnections = new List<string>(_userConnections[messageForm.SenderLogin]);
                        senderConnections.Remove(messageForm.SenderConnectionId);

                        if (senderConnections.Count >= 1)
                        {
                            await _hubContext.Clients.Clients(senderConnections)
                                .SendAsync("ReceiveMessage", new
                                {
                                    senderLogin = userLogin,
                                    recipientLogin = messageForm.RecipientLogin,
                                    text = messageForm.Text,
                                    id = serviceResponse?.Data?.Id,
                                    dateTime = serviceResponse?.Data?.OperationDate
                                });
                        }
                    }

                    return Ok(serviceResponse?.Data);
                }
                else
                    return BadRequest(serviceResponse.Message);
            }
            catch (Exception ex)
            {
                return BadRequest(ex.Message);
            }
        }

        [Authorize]
        [HttpPut("EditMessage")]
        public async Task<IActionResult> EditMessage(ClientMessageForm messageForm, int id)
        {
            string? userLogin = User?.Identity?.Name;
            if (userLogin == null)
                return BadRequest("Login is null");

            try
            {
                ServiceResponse<MessageActionDTO> serviceResponse = (await _messageService.EditMessage(messageForm, id));

                if (serviceResponse.Status == Domain.Core.StatusCode.Success)
                    return Ok(serviceResponse.Data);
                else
                    return BadRequest(serviceResponse.Message);
            }
            catch(Exception ex)
            {
                return BadRequest(ex.Message);
            }
        }


        [Authorize]
        [HttpDelete("RemoveMessage")]
        public async Task<IActionResult> RemoveMessage(ClientMessageForm messageForm, int id)
        {
            string? userLogin = User?.Identity?.Name;
            if (userLogin == null)
                return BadRequest("Login is null");

            try
            {
                ServiceResponse<bool> serviceResponse = (await _messageService.RemoveMessage(messageForm, id));
                bool isMessageRemoved = serviceResponse.Data;
                if (isMessageRemoved)
                    return Ok("Message removed");
                else
                    return BadRequest(serviceResponse.Message);
            }
            catch(Exception ex)
            {
                return BadRequest(ex.Message);
            }
        }

        [Authorize]
        [HttpGet("GetChatMessages")]
        public async Task<IActionResult> GetChatMessagese(string contactLogin)
        {
            string? userLogin = User?.Identity?.Name;
            if (userLogin == null)
                return BadRequest("Login is null");

            try
            {
                var serviceResponse = (await _messageService.GetChatMessages(userLogin, contactLogin));
                if(serviceResponse.Status == Domain.Core.StatusCode.Success)
                {
                    List<MessageDTO>? messages = serviceResponse.Data;
                    return Ok(messages);
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
