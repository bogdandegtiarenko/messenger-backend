using Messenger.DAL.Interfaces;
using Messenger.DAL.Repositories;
using Messenger.Domain.Core;
using Messenger.Domain.Core.DTOs;
using Messenger.Domain.Core.Models;
using Messenger.Domain.Core.Responses;
using Messenger.Services.Interfaces;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Messenger.Services.Implementations
{
    public class MessageService : IMessageService
    {
        private readonly IMessageRepository _messageRepository;
        private readonly IUserRepository _userRepository;
        private readonly IContactRepository _contactRepository;
        private readonly ILogger<MessageService> _logger;

        public MessageService(
            IMessageRepository messageRepository,
            IUserRepository userRepository,
            IContactRepository contactRepository,
            ILogger<MessageService> logger
            )
        {
            _messageRepository = messageRepository;
            _userRepository = userRepository;
            _contactRepository = contactRepository;
            _logger = logger;
        }

        public async Task<ServiceResponse<MessageActionDTO>> AddMessage(ClientMessageForm messageForm)
        {
            _logger.LogInformation("AddMessage called with messageForm: {@messageForm}", messageForm);
            ServiceResponse<MessageActionDTO> serviceResponse = new ServiceResponse<MessageActionDTO>();

            Contact? contact = _contactRepository.GetAll().FirstOrDefault(c =>
                    c.User.Login == messageForm.SenderLogin && c.ContactUser.Login == messageForm.RecipientLogin ||
                    c.User.Login == messageForm.RecipientLogin && c.ContactUser.Login == messageForm.SenderLogin
                );

            if (contact == null)
            {
                _logger.LogWarning("AddMessage: Contact doesn't exist for senderLogin: {senderLogin}, recipientLogin: {recipientLogin}", messageForm.SenderLogin, messageForm.RecipientLogin);
                serviceResponse.Status = StatusCode.Fail;
                serviceResponse.Data = null;
                serviceResponse.Message = "Such contact doesn't exist";
            }
            else
            {
                User? sender = _userRepository.GetAll().FirstOrDefault(u => u.Login == messageForm.SenderLogin);
                User? recipient = _userRepository.GetAll().FirstOrDefault(u => u.Login == messageForm.RecipientLogin);

                if (sender == null || recipient == null)
                {
                    _logger.LogWarning("AddMessage: Sender or recipient doesn't exist. SenderLogin: {senderLogin}, RecipientLogin: {recipientLogin}", messageForm.SenderLogin, messageForm.RecipientLogin);
                    serviceResponse.Status = StatusCode.Fail;
                    serviceResponse.Data = null;
                    serviceResponse.Message = "Sender or Recipient account doesn't exist";
                }
                else
                {
                    Message message = new Message
                    {
                        IsEdited = false,
                        LastModificationDate = DateTime.UtcNow,
                        SenderId = sender.Id,
                        RecipientId = recipient.Id,
                        Text = messageForm.Text
                    };

                    RepositoryResponse repositoryResponse = await _messageRepository.Create(message);
                    serviceResponse.Status = repositoryResponse.Status;
                    serviceResponse.Message = repositoryResponse.Message;

                    if (repositoryResponse.IsSuccessCompleted)
                    {
                        MessageActionDTO messageAction = new MessageActionDTO()
                        {
                            Id = message.Id,
                            OperationDate = message.LastModificationDate
                        };
                        serviceResponse.Data = messageAction;
                        _logger.LogInformation("AddMessage: Message added successfully with Id: {messageId}", message.Id);
                    }
                    else
                    {
                        serviceResponse.Data = null;
                        _logger.LogError("AddMessage: Failed to add message. Status: {status}, Message: {message}, SenderLogin: {senderLogin}, RecipientLogin: {recipientLogin}", repositoryResponse.Status, repositoryResponse.Message, sender.Login, recipient.Login);
                    }
                }
            }

            return serviceResponse;
        }

        public async Task<ServiceResponse<MessageActionDTO>> EditMessage(ClientMessageForm messageForm, int id)
        {
            _logger.LogInformation("EditMessage called with messageForm: {@messageForm}, messageId: {messageId}", messageForm, id);
            ServiceResponse<MessageActionDTO> serviceResponse = new ServiceResponse<MessageActionDTO>();

            Contact? contact = _contactRepository.GetAll().FirstOrDefault(c =>
                    c.User.Login == messageForm.SenderLogin && c.ContactUser.Login == messageForm.RecipientLogin ||
                    c.User.Login == messageForm.RecipientLogin && c.ContactUser.Login == messageForm.SenderLogin
                );

            if (contact == null)
            {
                _logger.LogWarning("EditMessage: Contact doesn't exist for senderLogin: {senderLogin}, recipientLogin: {recipientLogin}", messageForm.SenderLogin, messageForm.RecipientLogin);
                serviceResponse.Status = StatusCode.Fail;
                serviceResponse.Data = null;
                serviceResponse.Message = "Such contact doesn't exist";
            }
            else
            {
                User? sender = _userRepository.GetAll().FirstOrDefault(u => u.Login == messageForm.SenderLogin);
                User? recipient = _userRepository.GetAll().FirstOrDefault(u => u.Login == messageForm.RecipientLogin);

                if (sender == null || recipient == null)
                {
                    _logger.LogWarning("EditMessage: Sender or recipient doesn't exist. SenderLogin: {senderLogin}, RecipientLogin: {recipientLogin}", messageForm.SenderLogin, messageForm.RecipientLogin);
                    serviceResponse.Status = StatusCode.Fail;
                    serviceResponse.Data = null;
                    serviceResponse.Message = "Sender or Recipient account doesn't exist";
                }
                else
                {
                    Message? _message = _messageRepository.GetAll().FirstOrDefault(m => m.Id == id);
                    if (_message == null)
                    {
                        _logger.LogError("EditMessage: Message with Id: {messageId} doesn't exist, MessageForm: {@messageForm}", id, messageForm);
                        throw new Exception("Message with this Id doesn't exist");
                    }

                    _message.Text = messageForm.Text;
                    _message.LastModificationDate = DateTime.UtcNow;
                    _message.IsEdited = true;

                    RepositoryResponse repositoryResponse = await _messageRepository.Update(_message);

                    serviceResponse.Status = repositoryResponse.Status;
                    serviceResponse.Message = repositoryResponse.Message;
                    serviceResponse.Data = new MessageActionDTO
                    {
                        Id = _message.Id,
                        OperationDate = _message.LastModificationDate
                    };
                    _logger.LogInformation("EditMessage: Message edited successfully with Id: {messageId}", _message.Id);
                }
            }

            return serviceResponse;
        }

        public async Task<ServiceResponse<ContactMessageInfo>> GetBlockMessageInfo(string userLogin, Contact contact)
        {
            _logger.LogInformation("GetBlockMessageInfo called with userLogin: {userLogin}, contactId: {contactId}", userLogin, contact.Id);
            ServiceResponse<ContactMessageInfo> serviceResponse = new ServiceResponse<ContactMessageInfo>();

            Message? lastMessage = _messageRepository.GetAll()
                .Where(m =>
                        m.Sender.Login == contact.User.Login && m.Recipient.Login == contact.ContactUser.Login ||
                        m.Sender.Login == contact.ContactUser.Login && m.Recipient.Login == contact.User.Login)
                .OrderBy(m => m.Id)
                .ToList()
                .LastOrDefault();

            if (lastMessage == null)
            {
                _logger.LogWarning("GetBlockMessageInfo: Last message doesn't exist for userLogin: {userLogin}", userLogin);
                serviceResponse.Data = null;
                serviceResponse.Message = "Last message doesn't exist";
                serviceResponse.Status = StatusCode.Fail;

            }
            else
            {
                serviceResponse.Status = StatusCode.Success;
                serviceResponse.Message = "Success";

                DateTime lastMessageDateTime = new DateTime(lastMessage.LastModificationDate.Ticks, DateTimeKind.Utc);
                serviceResponse.Data = new ContactMessageInfo
                {
                    LastMessage = lastMessage.Text,
                    LastMessageDateTime = lastMessageDateTime,
                    UnreadMessageCount = null
                };
                _logger.LogInformation("GetBlockMessageInfo: Last message retrieved successfully for userLogin: {userLogin}, contact: {contactId}", userLogin, contact.Id);
            }

            return serviceResponse;
        }

        public async Task<ServiceResponse<List<MessageDTO>>> GetChatMessages(string userLogin, string contactLogin)
        {
            _logger.LogInformation("GetChatMessages called with userLogin: {userLogin}, contactLogin: {contactLogin}", userLogin, contactLogin);
            ServiceResponse<List<MessageDTO>> serviceResponse = new ServiceResponse<List<MessageDTO>>();

            Contact? contact = _contactRepository.GetAll().FirstOrDefault(c =>
                    c.User.Login == userLogin && c.ContactUser.Login == contactLogin ||
                    c.User.Login == contactLogin && c.ContactUser.Login == userLogin
                );

            if (contact == null)
            {
                _logger.LogWarning("GetChatMessages: Contact doesn't exist for userLogin: {userLogin}, contactLogin: {contactLogin}", userLogin, contactLogin);
                serviceResponse.Status = StatusCode.Fail;
                serviceResponse.Data = null;
                serviceResponse.Message = "Such contact doesn't exist";
            }
            else
            {
                User? user = _userRepository.GetAll().FirstOrDefault(u => u.Login == userLogin);
                User? contactUser = _userRepository.GetAll().FirstOrDefault(u => u.Login == contactLogin);

                if (user == null || contactUser == null)
                {
                    _logger.LogWarning("GetChatMessages: User or contact user doesn't exist. UserLogin: {userLogin}, ContactLogin: {contactLogin}", userLogin, contactLogin);
                    serviceResponse.Status = StatusCode.Fail;
                    serviceResponse.Data = null;
                    serviceResponse.Message = "Sender or Recipient account doesn't exist";
                }
                else
                {
                    List<Message> _messages = _messageRepository.GetAll().Where(m =>
                        m.Sender.Login == userLogin && m.Recipient.Login == contactLogin ||
                        m.Sender.Login == contactLogin && m.Recipient.Login == userLogin).ToList();

                    List<MessageDTO> messages = _messages.OrderBy(m => m.Id).ToList().Select(m =>
                    {
                        DateTime dateTime = new DateTime(m.LastModificationDate.Ticks, DateTimeKind.Utc);
                        return new MessageDTO
                        {
                            Id = m.Id,
                            LastModify = dateTime,
                            RecipientLogin = m.Recipient.Login,
                            SenderLogin = m.Sender.Login,
                            Text = m.Text
                        };
                    }).ToList();

                    serviceResponse.Data = messages;
                    serviceResponse.Status = StatusCode.Success;
                    serviceResponse.Message = "Success";
                    _logger.LogInformation("GetChatMessages: Messages retrieved successfully for userLogin: {userLogin}, contactLogin: {contactLogin}, Messages count: {count}", userLogin, contactLogin, messages.Count);
                }
            }

            return serviceResponse;
        }

        public async Task<ServiceResponse<bool>> RemoveMessage(ClientMessageForm messageForm, int id)
        {
            _logger.LogInformation("RemoveMessage called with messageForm: {@messageForm}, messageId: {messageId}", messageForm, id);
            ServiceResponse<bool> serviceResponse = new ServiceResponse<bool>();

            Contact? contact = _contactRepository.GetAll().FirstOrDefault(c =>
                    c.User.Login == messageForm.SenderLogin && c.ContactUser.Login == messageForm.RecipientLogin ||
                    c.User.Login == messageForm.RecipientLogin && c.ContactUser.Login == messageForm.SenderLogin
                );

            if (contact == null)
            {
                _logger.LogWarning("RemoveMessage: Contact doesn't exist for senderLogin: {senderLogin}, recipientLogin: {recipientLogin}", messageForm.SenderLogin, messageForm.RecipientLogin);
                serviceResponse.Status = StatusCode.Fail;
                serviceResponse.Data = false;
                serviceResponse.Message = "Such contact doesn't exist";
            }
            else
            {
                User? sender = _userRepository.GetAll().FirstOrDefault(u => u.Login == messageForm.SenderLogin);
                User? recipient = _userRepository.GetAll().FirstOrDefault(u => u.Login == messageForm.RecipientLogin);

                if (sender == null || recipient == null)
                {
                    _logger.LogWarning("RemoveMessage: Sender or recipient doesn't exist. SenderLogin: {senderLogin}, RecipientLogin: {recipientLogin}", messageForm.SenderLogin, messageForm.RecipientLogin);
                    serviceResponse.Status = StatusCode.Fail;
                    serviceResponse.Data = false;
                    serviceResponse.Message = "Sender or Recipient account doesn't exist";
                }
                else
                {
                    Message _message = _messageRepository.GetAll().FirstOrDefault(m => m.Id == id);
                    RepositoryResponse repositoryResponse = await _messageRepository.Delete(_message);

                    serviceResponse.Status = repositoryResponse.Status;
                    serviceResponse.Data = repositoryResponse.IsSuccessCompleted;
                    serviceResponse.Message = repositoryResponse.Message;
                    _logger.LogInformation("RemoveMessage: Message removed successfully with Id: {messageId}", id);
                }
            }

            return serviceResponse;
        }


    }
}