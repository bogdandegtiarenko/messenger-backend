using Messenger.Domain.Core.DTOs;
using Messenger.Domain.Core.Models;
using Messenger.Domain.Core.Responses;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Messenger.Services.Interfaces
{
    public interface IMessageService
    {
        Task<ServiceResponse<MessageActionDTO>> AddMessage(ClientMessageForm messageForm);
        Task<ServiceResponse<bool>> RemoveMessage(ClientMessageForm messageForm, int id);
        Task<ServiceResponse<MessageActionDTO>> EditMessage(ClientMessageForm messageForm, int id);
        Task<ServiceResponse<List<MessageDTO>>> GetChatMessages(string userLogin ,string contactLogin);
        Task<ServiceResponse<ContactMessageInfo>> GetBlockMessageInfo(string userLogin, Contact contact);
    }
}
