using Messenger.DAL.Interfaces;
using Messenger.Domain.Core.DTOs;
using Messenger.Domain.Core.Models;
using Messenger.Domain.Core.Responses;
using Messenger.Domain.Core;
using Messenger.Services.Implementations;
using Microsoft.Extensions.Logging;
using Moq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Messenger.Services.Tests
{
    [TestFixture]
    public class MessageServiceTests
    {

        private Mock<IMessageRepository> _mockMessageRepository;
        private Mock<IUserRepository> _mockUserRepository;
        private Mock<IContactRepository> _mockContactRepository;
        private Mock<ILogger<MessageService>> _mockLogger;
        private MessageService _messageService;

        [SetUp]
        public void SetUp()
        {
            _mockMessageRepository = new Mock<IMessageRepository>();
            _mockUserRepository = new Mock<IUserRepository>();
            _mockContactRepository = new Mock<IContactRepository>();
            _mockLogger = new Mock<ILogger<MessageService>>();

            _messageService = new MessageService(
                _mockMessageRepository.Object,
                _mockUserRepository.Object,
                _mockContactRepository.Object,
                _mockLogger.Object
            );
        }

        [Test]
        public async Task AddMessage_ContactDoesNotExist_ReturnsFail()
        {
            // Arrange
            var messageForm = new ClientMessageForm { SenderLogin = "sender", RecipientLogin = "recipient", Text = "Hello" };

            _mockContactRepository.Setup(repo => repo.GetAll()).Returns(new List<Contact>().AsQueryable());

            // Act
            var result = await _messageService.AddMessage(messageForm);

            // Assert
            Assert.AreEqual(StatusCode.Fail, result.Status);
            Assert.IsNull(result.Data);
            Assert.AreEqual("Such contact doesn't exist", result.Message);
        }

        [Test]
        public async Task AddMessage_SenderOrRecipientDoesNotExist_ReturnsFail()
        {
            // Arrange
            var messageForm = new ClientMessageForm { SenderLogin = "sender", RecipientLogin = "recipient", Text = "Hello" };
            var contact = new Contact { User = new User { Login = "sender" }, ContactUser = new User { Login = "recipient" } };

            _mockContactRepository.Setup(repo => repo.GetAll()).Returns(new List<Contact> { contact }.AsQueryable());
            _mockUserRepository.Setup(repo => repo.GetAll()).Returns(new List<User>().AsQueryable());

            // Act
            var result = await _messageService.AddMessage(messageForm);

            // Assert
            Assert.AreEqual(StatusCode.Fail, result.Status);
            Assert.IsNull(result.Data);
            Assert.AreEqual("Sender or Recipient account doesn't exist", result.Message);
        }

        [Test]
        public async Task AddMessage_MessageAddedSuccessfully_ReturnsSuccess()
        {
            // Arrange
            var messageForm = new ClientMessageForm { SenderLogin = "sender", RecipientLogin = "recipient", Text = "Hello" };
            var sender = new User { Id = 1, Login = "sender" };
            var recipient = new User { Id = 2, Login = "recipient" };
            var contact = new Contact { User = sender, ContactUser = recipient };
            var repositoryResponse = new RepositoryResponse { Status = StatusCode.Success, IsSuccessCompleted = true };
            var createdMessage = new Message { Id = 1, SenderId = sender.Id, RecipientId = recipient.Id, Text = "Hello", LastModificationDate = DateTime.UtcNow };

            _mockUserRepository.Setup(repo => repo.GetAll()).Returns(new List<User> { sender, recipient }.AsQueryable());
            _mockContactRepository.Setup(repo => repo.GetAll()).Returns(new List<Contact> { contact }.AsQueryable());
            _mockMessageRepository.Setup(repo => repo.Create(It.IsAny<Message>())).ReturnsAsync(repositoryResponse).Callback<Message>(m => m.Id = createdMessage.Id);

            // Act
            var result = await _messageService.AddMessage(messageForm);

            // Assert
            Assert.AreEqual(StatusCode.Success, result.Status);
            Assert.IsNotNull(result.Data);
            Assert.AreEqual(createdMessage.Id, result.Data.Id);
        }

        [Test]
        public async Task AddMessage_FailedToAddMessage_ReturnsFail()
        {
            // Arrange
            var messageForm = new ClientMessageForm { SenderLogin = "sender", RecipientLogin = "recipient", Text = "Hello" };
            var sender = new User { Id = 1, Login = "sender" };
            var recipient = new User { Id = 2, Login = "recipient" };
            var contact = new Contact { User = sender, ContactUser = recipient };
            var repositoryResponse = new RepositoryResponse { Status = StatusCode.Fail, IsSuccessCompleted = false, Message = "Error" };

            _mockUserRepository.Setup(repo => repo.GetAll()).Returns(new List<User> { sender, recipient }.AsQueryable());
            _mockContactRepository.Setup(repo => repo.GetAll()).Returns(new List<Contact> { contact }.AsQueryable());
            _mockMessageRepository.Setup(repo => repo.Create(It.IsAny<Message>())).ReturnsAsync(repositoryResponse);

            // Act
            var result = await _messageService.AddMessage(messageForm);

            // Assert
            Assert.AreEqual(StatusCode.Fail, result.Status);
            Assert.IsNull(result.Data);
            Assert.AreEqual("Error", result.Message);
        }

        [Test]
        public async Task EditMessage_ContactDoesNotExist_ReturnsFail()
        {
            // Arrange
            var messageForm = new ClientMessageForm { SenderLogin = "sender", RecipientLogin = "recipient", Text = "Edited text" };

            _mockContactRepository.Setup(repo => repo.GetAll()).Returns(new List<Contact>().AsQueryable());

            // Act
            var result = await _messageService.EditMessage(messageForm, 1);

            // Assert
            Assert.AreEqual(StatusCode.Fail, result.Status);
            Assert.IsNull(result.Data);
            Assert.AreEqual("Such contact doesn't exist", result.Message);
        }

        [Test]
        public async Task EditMessage_SenderOrRecipientDoesNotExist_ReturnsFail()
        {
            // Arrange
            var messageForm = new ClientMessageForm { SenderLogin = "sender", RecipientLogin = "recipient", Text = "Edited text" };
            var contact = new Contact { User = new User { Login = "sender" }, ContactUser = new User { Login = "recipient" } };

            _mockContactRepository.Setup(repo => repo.GetAll()).Returns(new List<Contact> { contact }.AsQueryable());
            _mockUserRepository.Setup(repo => repo.GetAll()).Returns(new List<User>().AsQueryable());

            // Act
            var result = await _messageService.EditMessage(messageForm, 1);

            // Assert
            Assert.AreEqual(StatusCode.Fail, result.Status);
            Assert.IsNull(result.Data);
            Assert.AreEqual("Sender or Recipient account doesn't exist", result.Message);
        }

        [Test]
        public void EditMessage_MessageDoesNotExist_ThrowsException()
        {
            // Arrange
            var messageForm = new ClientMessageForm { SenderLogin = "sender", RecipientLogin = "recipient", Text = "Edited text" };
            var sender = new User { Id = 1, Login = "sender" };
            var recipient = new User { Id = 2, Login = "recipient" };
            var contact = new Contact { User = sender, ContactUser = recipient };

            _mockContactRepository.Setup(repo => repo.GetAll()).Returns(new List<Contact> { contact }.AsQueryable());
            _mockUserRepository.Setup(repo => repo.GetAll()).Returns(new List<User> { sender, recipient }.AsQueryable());
            _mockMessageRepository.Setup(repo => repo.GetAll()).Returns(new List<Message>().AsQueryable());

            // Act & Assert
            var ex = Assert.ThrowsAsync<Exception>(() => _messageService.EditMessage(messageForm, 1));
            Assert.AreEqual("Message with this Id doesn't exist", ex.Message);
        }

        [Test]
        public async Task EditMessage_MessageEditedSuccessfully_ReturnsSuccess()
        {
            // Arrange
            var messageForm = new ClientMessageForm { SenderLogin = "sender", RecipientLogin = "recipient", Text = "Edited text" };
            var sender = new User { Id = 1, Login = "sender" };
            var recipient = new User { Id = 2, Login = "recipient" };
            var message = new Message { Id = 1, SenderId = sender.Id, RecipientId = recipient.Id, Text = "Original text" };
            var contact = new Contact { User = sender, ContactUser = recipient };
            var repositoryResponse = new RepositoryResponse { Status = StatusCode.Success, IsSuccessCompleted = true };

            _mockContactRepository.Setup(repo => repo.GetAll()).Returns(new List<Contact> { contact }.AsQueryable());
            _mockUserRepository.Setup(repo => repo.GetAll()).Returns(new List<User> { sender, recipient }.AsQueryable());
            _mockMessageRepository.Setup(repo => repo.GetAll()).Returns(new List<Message> { message }.AsQueryable());
            _mockMessageRepository.Setup(repo => repo.Update(It.IsAny<Message>())).ReturnsAsync(repositoryResponse);

            // Act
            var result = await _messageService.EditMessage(messageForm, 1);

            // Assert
            Assert.AreEqual(StatusCode.Success, result.Status);
            Assert.IsNotNull(result.Data);
            Assert.AreEqual(1, result.Data.Id);
            Assert.AreEqual("Edited text", message.Text);
        }

        [Test]
        public async Task GetBlockMessageInfo_LastMessageDoesNotExist_ReturnsFail()
        {
            // Arrange
            var userLogin = "user";
            var contact = new Contact { User = new User { Login = "contactUser1" }, ContactUser = new User { Login = "contactUser2" } };

            _mockMessageRepository.Setup(repo => repo.GetAll()).Returns(new List<Message>().AsQueryable());

            // Act
            var result = await _messageService.GetBlockMessageInfo(userLogin, contact);

            // Assert
            Assert.AreEqual(StatusCode.Fail, result.Status);
            Assert.IsNull(result.Data);
            Assert.AreEqual("Last message doesn't exist", result.Message);
        }

        [Test]
        public async Task GetBlockMessageInfo_LastMessageExists_ReturnsSuccess()
        {
            // Arrange
            var userLogin = "user";
            var contact = new Contact { User = new User { Login = "contactUser1" }, ContactUser = new User { Login = "contactUser2" } };
            var lastMessage = new Message
            {
                Id = 1,
                Sender = contact.User,
                Recipient = contact.ContactUser,
                Text = "Last message",
                LastModificationDate = DateTime.UtcNow
            };

            _mockMessageRepository.Setup(repo => repo.GetAll()).Returns(new List<Message> { lastMessage }.AsQueryable());

            // Act
            var result = await _messageService.GetBlockMessageInfo(userLogin, contact);

            // Assert
            Assert.AreEqual(StatusCode.Success, result.Status);
            Assert.IsNotNull(result.Data);
            Assert.AreEqual("Last message", result.Data.LastMessage);
            Assert.AreEqual(lastMessage.LastModificationDate, result.Data.LastMessageDateTime);
        }

        [Test]
        public async Task GetChatMessages_ContactDoesNotExist_ReturnsFail()
        {
            // Arrange
            var userLogin = "user";
            var contactLogin = "contact";

            _mockContactRepository.Setup(repo => repo.GetAll()).Returns(new List<Contact>().AsQueryable());

            // Act
            var result = await _messageService.GetChatMessages(userLogin, contactLogin);

            // Assert
            Assert.AreEqual(StatusCode.Fail, result.Status);
            Assert.IsNull(result.Data);
            Assert.AreEqual("Such contact doesn't exist", result.Message);
        }

        [Test]
        public async Task GetChatMessages_UserOrContactUserDoesNotExist_ReturnsFail()
        {
            // Arrange
            var userLogin = "user";
            var contactLogin = "contact";
            var contact = new Contact { User = new User { Login = "user" }, ContactUser = new User { Login = "contact" } };

            _mockContactRepository.Setup(repo => repo.GetAll()).Returns(new List<Contact> { contact }.AsQueryable());
            _mockUserRepository.Setup(repo => repo.GetAll()).Returns(new List<User>().AsQueryable());

            // Act
            var result = await _messageService.GetChatMessages(userLogin, contactLogin);

            // Assert
            Assert.AreEqual(StatusCode.Fail, result.Status);
            Assert.IsNull(result.Data);
            Assert.AreEqual("Sender or Recipient account doesn't exist", result.Message);
        }

        [Test]
        public async Task GetChatMessages_MessagesRetrievedSuccessfully_ReturnsSuccess()
        {
            // Arrange
            var userLogin = "user";
            var contactLogin = "contact";
            var user = new User { Id = 1, Login = "user" };
            var contactUser = new User { Id = 2, Login = "contact" };
            var contact = new Contact { User = user, ContactUser = contactUser };
            var messages = new List<Message>
            {
                new Message { Id = 1, Sender = user, Recipient = contactUser, Text = "Hello", LastModificationDate = DateTime.UtcNow },
                new Message { Id = 2, Sender = contactUser, Recipient = user, Text = "Hi", LastModificationDate = DateTime.UtcNow }
            };

            _mockContactRepository.Setup(repo => repo.GetAll()).Returns(new List<Contact> { contact }.AsQueryable());
            _mockUserRepository.Setup(repo => repo.GetAll()).Returns(new List<User> { user, contactUser }.AsQueryable());
            _mockMessageRepository.Setup(repo => repo.GetAll()).Returns(messages.AsQueryable());

            // Act
            var result = await _messageService.GetChatMessages(userLogin, contactLogin);

            // Assert
            Assert.AreEqual(StatusCode.Success, result.Status);
            Assert.IsNotNull(result.Data);
            Assert.AreEqual(2, result.Data.Count);
            Assert.AreEqual("Hello", result.Data[0].Text);
            Assert.AreEqual("Hi", result.Data[1].Text);
        }

        [Test]
        public async Task RemoveMessage_ContactDoesNotExist_ReturnsFail()
        {
            // Arrange
            var messageForm = new ClientMessageForm { SenderLogin = "sender", RecipientLogin = "recipient" };
            var messageId = 1;

            _mockContactRepository.Setup(repo => repo.GetAll()).Returns(new List<Contact>().AsQueryable());

            // Act
            var result = await _messageService.RemoveMessage(messageForm, messageId);

            // Assert
            Assert.AreEqual(StatusCode.Fail, result.Status);
            Assert.IsFalse(result.Data);
            Assert.AreEqual("Such contact doesn't exist", result.Message);
        }

        [Test]
        public async Task RemoveMessage_SenderOrRecipientDoesNotExist_ReturnsFail()
        {
            // Arrange
            var messageForm = new ClientMessageForm { SenderLogin = "sender", RecipientLogin = "recipient" };
            var messageId = 1;
            var contact = new Contact { User = new User { Login = "sender" }, ContactUser = new User { Login = "recipient" } };

            _mockContactRepository.Setup(repo => repo.GetAll()).Returns(new List<Contact> { contact }.AsQueryable());
            _mockUserRepository.Setup(repo => repo.GetAll()).Returns(new List<User>().AsQueryable());

            // Act
            var result = await _messageService.RemoveMessage(messageForm, messageId);

            // Assert
            Assert.AreEqual(StatusCode.Fail, result.Status);
            Assert.IsFalse(result.Data);
            Assert.AreEqual("Sender or Recipient account doesn't exist", result.Message);
        }

        [Test]
        public async Task RemoveMessage_MessageRemovedSuccessfully_ReturnsSuccess()
        {
            // Arrange
            var messageForm = new ClientMessageForm { SenderLogin = "sender", RecipientLogin = "recipient" };
            var messageId = 1;
            var sender = new User { Id = 1, Login = "sender" };
            var recipient = new User { Id = 2, Login = "recipient" };
            var message = new Message { Id = 1, SenderId = sender.Id, RecipientId = recipient.Id };
            var contact = new Contact { User = sender, ContactUser = recipient };
            var repositoryResponse = new RepositoryResponse { Status = StatusCode.Success, IsSuccessCompleted = true, Message = "Success" };

            _mockContactRepository.Setup(repo => repo.GetAll()).Returns(new List<Contact> { contact }.AsQueryable());
            _mockUserRepository.Setup(repo => repo.GetAll()).Returns(new List<User> { sender, recipient }.AsQueryable());
            _mockMessageRepository.Setup(repo => repo.GetAll()).Returns(new List<Message> { message }.AsQueryable());
            _mockMessageRepository.Setup(repo => repo.Delete(It.IsAny<Message>())).ReturnsAsync(repositoryResponse);

            // Act
            var result = await _messageService.RemoveMessage(messageForm, messageId);

            // Assert
            Assert.AreEqual(StatusCode.Success, result.Status);
            Assert.IsTrue(result.Data);
            Assert.AreEqual("Success", result.Message);
        }

        [Test]
        public async Task RemoveMessage_FailedToRemoveMessage_ReturnsFail()
        {
            // Arrange
            var messageForm = new ClientMessageForm { SenderLogin = "sender", RecipientLogin = "recipient" };
            var messageId = 1;
            var sender = new User { Id = 1, Login = "sender" };
            var recipient = new User { Id = 2, Login = "recipient" };
            var message = new Message { Id = 1, SenderId = sender.Id, RecipientId = recipient.Id };
            var contact = new Contact { User = sender, ContactUser = recipient };
            var repositoryResponse = new RepositoryResponse { Status = StatusCode.Fail, IsSuccessCompleted = false, Message = "Error" };

            _mockContactRepository.Setup(repo => repo.GetAll()).Returns(new List<Contact> { contact }.AsQueryable());
            _mockUserRepository.Setup(repo => repo.GetAll()).Returns(new List<User> { sender, recipient }.AsQueryable());
            _mockMessageRepository.Setup(repo => repo.GetAll()).Returns(new List<Message> { message }.AsQueryable());
            _mockMessageRepository.Setup(repo => repo.Delete(It.IsAny<Message>())).ReturnsAsync(repositoryResponse);

            // Act
            var result = await _messageService.RemoveMessage(messageForm, messageId);

            // Assert
            Assert.AreEqual(StatusCode.Fail, result.Status);
            Assert.IsFalse(result.Data);
            Assert.AreEqual("Error", result.Message);
        }
    }
}
