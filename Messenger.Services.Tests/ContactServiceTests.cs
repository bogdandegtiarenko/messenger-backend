using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Messenger.Domain.Core;
using Messenger.Domain.Core.DTOs;
using Messenger.Domain.Core.DTOs.Authentication;
using Messenger.Domain.Core.Models;
using Messenger.Domain.Core.Responses;
using Messenger.Services.Implementations;
using Microsoft.Extensions.Logging;
using Moq;
using NUnit.Framework;
using Messenger.DAL.Repositories;
using Messenger.DAL;
using Microsoft.EntityFrameworkCore;
using Messenger.DAL.Interfaces;
using System.Text;
using Microsoft.AspNetCore.Authentication.Cookies;
using Messenger.Services.Interfaces;

namespace Messenger.Services.Tests
{
    [TestFixture]
    public class ContactServiceTests
    {
        private Mock<IContactRepository> _contactRepositoryMock;
        private Mock<IUserRepository> _userRepositoryMock;
        private Mock<IMessageService> _messageServiceMock;
        private Mock<IAccountService> _accountServiceMock;
        private Mock<ILogger<ContactService>> _loggerMock;
        private ContactService _contactService;

        [SetUp]
        public void Setup()
        {
            _contactRepositoryMock = new Mock<IContactRepository>();
            _userRepositoryMock = new Mock<IUserRepository>();
            _messageServiceMock = new Mock<IMessageService>();
            _accountServiceMock = new Mock<IAccountService>();
            _loggerMock = new Mock<ILogger<ContactService>>();
            _contactService = new ContactService(_contactRepositoryMock.Object, _userRepositoryMock.Object, _messageServiceMock.Object, _accountServiceMock.Object, _loggerMock.Object);
        }

        [Test]
        public async Task AddContact_ValidUsers_ShouldReturnSuccess()
        {
            // Arrange
            var userLogin = "user1";
            var contactLogin = "user2";
            var user = new User { Login = userLogin };
            var contactUser = new User { Login = contactLogin };
            _userRepositoryMock.Setup(r => r.GetAll()).Returns(new List<User> { user, contactUser }.AsQueryable());
            _contactRepositoryMock.Setup(r => r.GetAll()).Returns(new List<Contact>().AsQueryable());
            _contactRepositoryMock.Setup(r => r.Create(It.IsAny<Contact>())).ReturnsAsync(new RepositoryResponse { Status = StatusCode.Success, IsSuccessCompleted = true });

            // Act
            var result = await _contactService.AddContact(userLogin, contactLogin);

            // Assert
            Assert.AreEqual(StatusCode.Success, result.Status);
            Assert.IsTrue(result.Data);
        }

        [Test]
        public async Task AddContact_EmptyLogins_ShouldReturnFail()
        {
            // Arrange
            var userLogin = "";
            var contactLogin = "";
            // Act
            var result = await _contactService.AddContact(userLogin, contactLogin);

            // Assert
            Assert.AreEqual(StatusCode.Fail, result.Status);
            Assert.IsFalse(result.Data);
            Assert.AreEqual("UserLogin or ContactLogin is null or empty", result.Message);
        }

        [Test]
        public void AddContact_UserNotExists_ShouldThrowException()
        {
            // Arrange
            var userLogin = "nonexistent";
            var contactLogin = "user2";
            _userRepositoryMock.Setup(r => r.GetAll()).Returns(new List<User>().AsQueryable());

            // Act & Assert
            var ex = Assert.ThrowsAsync<Exception>(async () => await _contactService.AddContact(userLogin, contactLogin));
            Assert.AreEqual("A non-existent user makes a request", ex.Message);
        }

        [Test]
        public async Task AddContact_ContactUserNotExists_ShouldReturnFail()
        {
            // Arrange
            var userLogin = "user1";
            var contactLogin = "nonexistent";
            var user = new User { Login = userLogin };
            _userRepositoryMock.Setup(r => r.GetAll()).Returns(new List<User> { user }.AsQueryable());

            // Act
            var result = await _contactService.AddContact(userLogin, contactLogin);

            // Assert
            Assert.AreEqual(StatusCode.Fail, result.Status);
            Assert.IsFalse(result.Data);
            Assert.AreEqual("Contact user doesn't exist", result.Message);
        }

        [Test]
        public async Task AddContact_ContactAlreadyExists_ShouldReturnFail()
        {
            // Arrange
            var userLogin = "user1";
            var contactLogin = "user2";
            var user = new User { Login = userLogin };
            var contactUser = new User { Login = contactLogin };
            var existingContact = new Contact { User = user, ContactUser = contactUser };
            _userRepositoryMock.Setup(r => r.GetAll()).Returns(new List<User> { user, contactUser }.AsQueryable());
            _contactRepositoryMock.Setup(r => r.GetAll()).Returns(new List<Contact> { existingContact }.AsQueryable());

            // Act
            var result = await _contactService.AddContact(userLogin, contactLogin);

            // Assert
            Assert.AreEqual(StatusCode.Fail, result.Status);
            Assert.IsFalse(result.Data);
            Assert.AreEqual("Contact already exists", result.Message);
        }

        [Test]
        public async Task GetContacts_UserExists_ShouldReturnContacts()
        {
            // Arrange
            var login = "user1";
            var user = new User { Login = login };
            var contact1 = new Contact { User = user, ContactUser = new User { Login = "user2" } };
            var contact2 = new Contact { User = new User { Login = "user3" }, ContactUser = user };
            var contacts = new List<Contact> { contact1, contact2 };

            _userRepositoryMock.Setup(r => r.GetAll()).Returns(new List<User> { user }.AsQueryable());
            _contactRepositoryMock.Setup(r => r.GetAll()).Returns(contacts.AsQueryable());

            // Act
            var result = await _contactService.GetContacts(login);

            // Assert
            Assert.AreEqual(StatusCode.Success, result.Status);
            Assert.IsNotNull(result.Data);
            Assert.AreEqual(2, result.Data.Count);
            Assert.AreEqual(contact1, result.Data[0]);
            Assert.AreEqual(contact2, result.Data[1]);
        }

        [Test]
        public async Task GetContacts_UserNotExists_ShouldReturnFail()
        {
            // Arrange
            var login = "nonexistent";
            _userRepositoryMock.Setup(r => r.GetAll()).Returns(new List<User>().AsQueryable());

            // Act
            var result = await _contactService.GetContacts(login);

            // Assert
            Assert.AreEqual(StatusCode.Fail, result.Status);
            Assert.IsNull(result.Data);
            Assert.AreEqual("User doesn't exist", result.Message);
        }

        [Test]
        public async Task GetContacts_UserExistsButNoContacts_ShouldReturnSuccessWithEmptyList()
        {
            // Arrange
            var login = "user1";
            var user = new User { Login = login };
            _userRepositoryMock.Setup(r => r.GetAll()).Returns(new List<User> { user }.AsQueryable());
            _contactRepositoryMock.Setup(r => r.GetAll()).Returns(new List<Contact>().AsQueryable());

            // Act
            var result = await _contactService.GetContacts(login);

            // Assert
            Assert.AreEqual(StatusCode.Success, result.Status);
            Assert.IsNotNull(result.Data);
            Assert.AreEqual(0, result.Data.Count);
        }

        [Test]
        public async Task GetContactBlockInfos_ValidUserWithContacts_ShouldReturnContactBlockInfos()
        {
            // Arrange
            var userLogin = "user1";
            var user = new User { Login = userLogin };
            var contact = new Contact { User = user, ContactUser = new User { Login = "user2" } };
            var contacts = new List<Contact> { contact };

            _userRepositoryMock.Setup(r => r.GetAll()).Returns(new List<User> { user }.AsQueryable());
            _contactRepositoryMock.Setup(r => r.GetAll()).Returns(contacts.AsQueryable());
            _accountServiceMock.Setup(s => s.GetContactProfile(userLogin, contact)).ReturnsAsync(new ServiceResponse<ProfileDTO> { Data = new ProfileDTO() });
            _messageServiceMock.Setup(s => s.GetBlockMessageInfo(userLogin, contact)).ReturnsAsync(new ServiceResponse<ContactMessageInfo> { Data = new ContactMessageInfo() });

            // Act
            var result = await _contactService.GetContactBlockInfos(userLogin);

            // Assert
            Assert.AreEqual(StatusCode.Success, result.Status);
            Assert.IsNotNull(result.Data);
            Assert.IsNotEmpty(result.Data);
        }

        [Test]
        public async Task GetContactLogins_UserExistsWithContacts_ShouldReturnContactLogins()
        {
            // Arrange
            var userLogin = "user1";
            var user = new User { Login = userLogin };
            var contact1 = new Contact { User = user, ContactUser = new User { Login = "user2" } };
            var contact2 = new Contact { User = new User { Login = "user3" }, ContactUser = user };
            var contacts = new List<Contact> { contact1, contact2 };

            _userRepositoryMock.Setup(r => r.GetAll()).Returns(new List<User> { user }.AsQueryable());
            _contactRepositoryMock.Setup(r => r.GetAll()).Returns(contacts.AsQueryable());

            // Act
            var result = await _contactService.GetContactLogins(userLogin);

            // Assert
            Assert.AreEqual(StatusCode.Success, result.Status);
            Assert.IsNotNull(result.Data);
            Assert.AreEqual(2, result.Data.Count);
            Assert.Contains("user2", result.Data);
            Assert.Contains("user3", result.Data);
        }

        [Test]
        public async Task GetContactLogins_UserExistsNoContacts_ShouldReturnEmptyList()
        {
            // Arrange
            var userLogin = "user1";
            var user = new User { Login = userLogin };

            _userRepositoryMock.Setup(r => r.GetAll()).Returns(new List<User> { user }.AsQueryable());
            _contactRepositoryMock.Setup(r => r.GetAll()).Returns(new List<Contact>().AsQueryable());

            // Act
            var result = await _contactService.GetContactLogins(userLogin);

            // Assert
            Assert.AreEqual(StatusCode.Success, result.Status);
            Assert.IsNotNull(result.Data);
            Assert.IsEmpty(result.Data);
        }

        [Test]
        public async Task GetContactLogins_UserNotExists_ShouldReturnFail()
        {
            // Arrange
            var userLogin = "nonexistent";
            _userRepositoryMock.Setup(r => r.GetAll()).Returns(new List<User>().AsQueryable());

            // Act
            var result = await _contactService.GetContactLogins(userLogin);

            // Assert
            Assert.AreEqual(StatusCode.Fail, result.Status);
            Assert.IsNull(result.Data);
            Assert.AreEqual("User doesn't exist", result.Message);
        }

        [Test]
        public async Task RemoveContact_ValidContact_ShouldReturnSuccess()
        {
            // Arrange
            var userLogin = "user1";
            var contactLogin = "user2";
            var user = new User { Login = userLogin };
            var contactUser = new User { Login = contactLogin };
            var contact = new Contact { User = user, ContactUser = contactUser };

            _contactRepositoryMock.Setup(r => r.GetAll()).Returns(new List<Contact> { contact }.AsQueryable());
            _contactRepositoryMock.Setup(r => r.Delete(contact)).ReturnsAsync(new RepositoryResponse { Status = StatusCode.Success, IsSuccessCompleted = true });

            // Act
            var result = await _contactService.RemoveContact(userLogin, contactLogin);

            // Assert
            Assert.AreEqual(StatusCode.Success, result.Status);
            Assert.IsTrue(result.Data);
            Assert.AreEqual("Contact is deleted", result.Message);
        }

        [Test]
        public async Task RemoveContact_NullOrEmptyLogins_ShouldReturnFail()
        {
            // Arrange
            var userLogin = "";
            var contactLogin = "";

            // Act
            var result = await _contactService.RemoveContact(userLogin, contactLogin);

            // Assert
            Assert.AreEqual(StatusCode.Fail, result.Status);
            Assert.IsFalse(result.Data);
            Assert.AreEqual("ContactLogin is null or empty", result.Message);
        }

        [Test]
        public async Task RemoveContact_ContactNotExists_ShouldReturnFail()
        {
            // Arrange
            var userLogin = "user1";
            var contactLogin = "user2";

            _contactRepositoryMock.Setup(r => r.GetAll()).Returns(new List<Contact>().AsQueryable());

            // Act
            var result = await _contactService.RemoveContact(userLogin, contactLogin);

            // Assert
            Assert.AreEqual(StatusCode.Fail, result.Status);
            Assert.IsFalse(result.Data);
            Assert.AreEqual("Contact doesn't exist", result.Message);
        }




    }
}