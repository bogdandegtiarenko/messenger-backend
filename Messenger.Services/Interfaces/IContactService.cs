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
    public interface IContactService
    {
        Task<ServiceResponse<List<Contact>>> GetContacts(string login);
        Task<ServiceResponse<bool>> AddContact(string userLogin, string contactLogin);
        Task<ServiceResponse<bool>> RemoveContact(string userLogin, string contactLogin);
        Task<ServiceResponse<List<ContactBlockDTO>>> GetContactBlockInfos(string login);
        Task<ServiceResponse<List<string>>> GetContactLogins(string userLogin);
    }
}
