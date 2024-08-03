using Messenger.DAL.Repositories;
using Messenger.Domain.Core;
using Messenger.Domain.Core.DTOs;
using Messenger.Domain.Core.Models;
using Messenger.Domain.Core.Responses;
using Messenger.Services.Interfaces;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Messenger.DAL.Interfaces;

namespace Messenger.Services.Implementations
{
    public class ContactService : IContactService
    {
        private readonly IContactRepository _contactRepository;
        private readonly IUserRepository _userRepository;
        private readonly IAccountService _accountService;
        private readonly IMessageService _messageService;
        private readonly ILogger<ContactService> _logger;

        public ContactService(
            IContactRepository contactRepository,
            IUserRepository userRepository,
            IMessageService messageService,
            IAccountService accountService,
            ILogger<ContactService> logger
            )
        {
            _contactRepository = contactRepository;
            _userRepository = userRepository;
            _messageService = messageService;
            _accountService = accountService;
            _logger = logger;
        }

        public async Task<ServiceResponse<bool>> AddContact(string userLogin, string contactLogin)
        {
            _logger.LogInformation("AddContact called with userLogin: {userLogin}, contactLogin: {contactLogin}", userLogin, contactLogin);
            ServiceResponse<bool> serviceResponse = new ServiceResponse<bool>();

            if (userLogin.IsNullOrEmpty() || contactLogin.IsNullOrEmpty())
            {
                _logger.LogWarning("AddContact: userLogin or contactLogin is null or empty");
                serviceResponse.Status = StatusCode.Fail;
                serviceResponse.Data = false;
                serviceResponse.Message = "UserLogin or ContactLogin is null or empty";
                return serviceResponse;
            }

            User? user = _userRepository.GetAll().FirstOrDefault(u => u.Login == userLogin);
            User? contactUser = _userRepository.GetAll().FirstOrDefault(u => u.Login == contactLogin);

            if (user == null)
            {
                _logger.LogError("AddContact: A non-existent user makes a request with userLogin: {userLogin}", userLogin);
                throw new Exception("A non-existent user makes a request");
            }

            if (contactUser == null)
            {
                _logger.LogWarning("AddContact: Contact user doesn't exist with contactLogin: {contactLogin}", contactLogin);
                serviceResponse.Status = StatusCode.Fail;
                serviceResponse.Data = false;
                serviceResponse.Message = "Contact user doesn't exist";
            }
            else
            {
                Contact? existingContact = _contactRepository.GetAll().FirstOrDefault(c =>
                    c.User.Login == userLogin && c.ContactUser.Login == contactLogin ||
                    c.User.Login == contactLogin && c.ContactUser.Login == userLogin);
                if (existingContact is null)
                {
                    Contact contact = new Contact()
                    {
                        User = user,
                        ContactUser = contactUser,
                    };
                    RepositoryResponse repositoryResponse = await _contactRepository.Create(contact);
                    serviceResponse.Status = repositoryResponse.Status;
                    serviceResponse.Data = repositoryResponse.IsSuccessCompleted;
                    serviceResponse.Message = repositoryResponse.Message;
                    _logger.LogInformation("AddContact: Contact added for userLogin: {userLogin} and contactLogin: {contactLogin}, Status: {status}, Message: {message}", userLogin, contactLogin, repositoryResponse.Status, repositoryResponse.Message);
                }
                else
                {
                    _logger.LogWarning("AddContact: Contact already exists between userLogin: {userLogin} and contactLogin: {contactLogin}", userLogin, contactLogin);
                    serviceResponse.Status = StatusCode.Fail;
                    serviceResponse.Data = false;
                    serviceResponse.Message = "Contact already exists";
                }
            }
            return serviceResponse;
        }

        public async Task<ServiceResponse<List<Contact>>> GetContacts(string login)
        {
            _logger.LogInformation("GetContacts called with login: {login}", login);
            ServiceResponse<List<Contact>> serviceResponse = new ServiceResponse<List<Contact>>();
            User? user = _userRepository.GetAll().FirstOrDefault(u => u.Login == login);
            if (user is null)
            {
                _logger.LogWarning("GetContacts: User doesn't exist with login: {login}", login);
                serviceResponse.Status = StatusCode.Fail;
                serviceResponse.Data = null;
                serviceResponse.Message = "User doesn't exist";
            }
            else
            {
                List<Contact> contacts = _contactRepository.GetAll()
                    .Where(c =>
                        c.User.Login == login || c.ContactUser.Login == login)
                    .Include(c => c.User)
                    .Include(c => c.ContactUser)
                    .ToList();

                serviceResponse.Status = StatusCode.Success;
                serviceResponse.Data = contacts;
                serviceResponse.Message = "Success";
                _logger.LogInformation("GetContacts: Contacts retrieved for login: {login}, Contacts count: {count}", login, contacts.Count);
            }
            return serviceResponse;
        }

        public async Task<ServiceResponse<List<ContactBlockDTO>>> GetContactBlockInfos(string userLogin)
        {
            _logger.LogInformation("GetContactBlockInfos called with userLogin: {userLogin}", userLogin);
            ServiceResponse<List<ContactBlockDTO>> serviceResponse = new ServiceResponse<List<ContactBlockDTO>>();

            List<Contact>? contacts = (await GetContacts(userLogin)).Data;

            if (contacts == null)
            {
                _logger.LogWarning("GetContactBlockInfos: Contacts is null for userLogin: {userLogin}", userLogin);
                serviceResponse.Status = StatusCode.Fail;
                serviceResponse.Data = null;
                serviceResponse.Message = "Contacts is null";
            }
            else
            {
                var contactBlockDtoTasks = contacts.Select(async c =>
                {
                    ProfileDTO? profile = (await _accountService.GetContactProfile(userLogin, c)).Data;
                    ContactMessageInfo? contactMessageInfo = (await _messageService.GetBlockMessageInfo(userLogin, c)).Data;

                    return new ContactBlockDTO
                    {
                        Profile = profile,
                        MessageInfo = contactMessageInfo
                    };

                }).ToList();

                var middleware = (await Task.WhenAll(contactBlockDtoTasks));
                var contactBlockDTOs = middleware.ToList();

                if (contactBlockDTOs.IsNullOrEmpty())
                {
                    _logger.LogWarning("GetContactBlockInfos: No contact block info found for userLogin: {userLogin}", userLogin);
                    serviceResponse.Data = null;
                    serviceResponse.Message = "Fail";
                    serviceResponse.Status = StatusCode.Fail;
                }
                else
                {
                    serviceResponse.Data = contactBlockDTOs;
                    serviceResponse.Message = "Success";
                    serviceResponse.Status = StatusCode.Success;
                    _logger.LogInformation("GetContactBlockInfos: Contact block info retrieved for userLogin: {userLogin}, Info count: {count}", userLogin, contactBlockDTOs.Count);
                }
            }
            return serviceResponse;
        }

        public async Task<ServiceResponse<List<string>>> GetContactLogins(string userLogin)
        {
            _logger.LogInformation("GetContactLogins called with userLogin: {userLogin}", userLogin);
            List<string> contactLogins = new List<string>();
            ServiceResponse<List<Contact>> response = await GetContacts(userLogin);
            ServiceResponse<List<string>> serviceResponse = new ServiceResponse<List<string>>();

            if (response.Status == StatusCode.Success)
            {
                var contacts = response.Data;
                contacts.ForEach(c =>
                {
                    string contactLogin = (c.User.Login == userLogin) ? c.ContactUser.Login : c.User.Login;
                    contactLogins.Add(contactLogin);
                });

                serviceResponse.Status = StatusCode.Success;
                serviceResponse.Message = response.Message;
                serviceResponse.Data = contactLogins;
                _logger.LogInformation("GetContactLogins: Contact logins retrieved for userLogin: {userLogin}, Logins count: {count}", userLogin, contactLogins.Count);
            }
            else
            {
                _logger.LogWarning("GetContactLogins: Failed to retrieve contacts for userLogin: {userLogin}", userLogin);
                serviceResponse.Status = StatusCode.Fail;
                serviceResponse.Message = response.Message;
                serviceResponse.Data = null;
            }
            return serviceResponse;
        }

        public async Task<ServiceResponse<bool>> RemoveContact(string userLogin, string contactLogin)
        {
            _logger.LogInformation("RemoveContact called with userLogin: {userLogin}, contactLogin: {contactLogin}", userLogin, contactLogin);
            ServiceResponse<bool> serviceResponse = new ServiceResponse<bool>();
            if (contactLogin.IsNullOrEmpty() || userLogin.IsNullOrEmpty())
            {
                _logger.LogWarning("RemoveContact: userLogin or contactLogin is null or empty");
                serviceResponse.Status = StatusCode.Fail;
                serviceResponse.Data = false;
                serviceResponse.Message = "ContactLogin is null or empty";
                return serviceResponse;
            }

            Contact? contact = _contactRepository.GetAll().FirstOrDefault(c =>
                c.User.Login == userLogin && c.ContactUser.Login == contactLogin ||
                c.User.Login == contactLogin && c.ContactUser.Login == userLogin);

            if (contact is null)
            {
                _logger.LogWarning("RemoveContact: Contact doesn't exist between userLogin: {userLogin} and contactLogin: {contactLogin}", userLogin, contactLogin);
                serviceResponse.Status = StatusCode.Fail;
                serviceResponse.Data = false;
                serviceResponse.Message = "Contact doesn't exist";
            }
            else
            {
                RepositoryResponse repositoryResponse = await _contactRepository.Delete(contact);
                serviceResponse.Status = repositoryResponse.Status;
                serviceResponse.Data = repositoryResponse.IsSuccessCompleted;
                serviceResponse.Message = "Contact is deleted";
                _logger.LogInformation("RemoveContact: Contact deleted for userLogin: {userLogin} and contactLogin: {contactLogin}, Status: {status}", userLogin, contactLogin, repositoryResponse.Status);
            }
            return serviceResponse;
        }
    }
}