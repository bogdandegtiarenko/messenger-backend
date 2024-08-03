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

namespace Messenger.Services.Tests
{
    [TestFixture]
    public class AccountServiceTests
    {
        private Mock<IUserRepository> _mockUserRepository;
        private Mock<IProfileRepository> _mockProfileRepository;
        private Mock<ILogger<AccountService>> _mockLogger;
        private AccountService _accountService;

        [SetUp]
        public void Setup()
        {
            _mockUserRepository = new Mock<IUserRepository>();
            _mockProfileRepository = new Mock<IProfileRepository>();
            _mockLogger = new Mock<ILogger<AccountService>>();
            _accountService = new AccountService(_mockUserRepository.Object, _mockProfileRepository.Object, _mockLogger.Object);
        }

        [Test]
        public async Task ChangeEmail_UserExists_ReturnsSuccess()
        {
            // Arrange
            var user = new User { Login = "user1", Email = "old@example.com" };
            _mockUserRepository.Setup(repo => repo.GetAll()).Returns(new List<User> { user }.AsQueryable());
            _mockUserRepository.Setup(repo => repo.Update(It.IsAny<User>())).ReturnsAsync(new RepositoryResponse { IsSuccessCompleted = true, Status = StatusCode.Success, Message = "Email updated" });

            // Act
            var result = await _accountService.ChangeEmail("user1", "new@example.com");

            // Assert
            Assert.AreEqual(StatusCode.Success, result.Status);
            Assert.IsTrue(result.Data);
            Assert.AreEqual("Email updated", result.Message);
        }

        [Test]
        public async Task ChangeEmail_UserDoesNotExist_ReturnsFail()
        {
            // Arrange
            _mockUserRepository.Setup(repo => repo.GetAll()).Returns(new List<User>()
            {
                new User { Login = "firstUser" },
                new User { Login = "secondUser" }
            }.AsQueryable());

            // Act
            var result = await _accountService.ChangeEmail("nonexistent", "new@example.com");

            // Assert
            Assert.AreEqual(StatusCode.Fail, result.Status);
            Assert.IsFalse(result.Data);
            Assert.AreEqual("The user with this email doesn't exist", result.Message);
        }

        [Test]
        public async Task ChangeNickname_UserExists_ReturnsSuccess()
        {
            // Arrange
            var userLogin = "user1";
            var newNickname = "NewNickname";
            var user = new User { Login = userLogin, Profile = new Profile { Nickname = "OldNickname" } };
            _mockUserRepository.Setup(repo => repo.GetAll()).Returns(new List<User> { user }.AsQueryable());
            _mockUserRepository.Setup(repo => repo.Update(It.IsAny<User>())).ReturnsAsync(new RepositoryResponse { Status = StatusCode.Success, IsSuccessCompleted = true });

            // Act
            var result = await _accountService.ChangeNickname(userLogin, newNickname);

            // Assert
            Assert.IsTrue(result.Data);
            Assert.AreEqual(StatusCode.Success, result.Status);
            Assert.AreEqual(newNickname, user.Profile.Nickname);
        }

        [Test]
        public async Task ChangeNickname_UserNotFound_ReturnsFail()
        {
            // Arrange
            var userLogin = "nonexistentUser";
            var newNickname = "NewNickname";
            _mockUserRepository.Setup(repo => repo.GetAll()).Returns(new List<User>().AsQueryable());

            // Act
            var result = await _accountService.ChangeNickname(userLogin, newNickname);

            // Assert
            Assert.IsFalse(result.Data);
            Assert.AreEqual(StatusCode.Fail, result.Status);
            Assert.AreEqual("The user with this login doesn't exist", result.Message);
        }

        [Test]
        public async Task ChangePassword_UserNotFound_ReturnsFail()
        {
            // Arrange
            var userLogin = "nonexistentUser";
            var changePasswordData = new ChangePasswordDTO { CurrentPassword = "currentPassword", NewPassword = "newPassword" };
            _mockUserRepository.Setup(repo => repo.GetAll()).Returns(new List<User>().AsQueryable());

            // Act
            var result = await _accountService.ChangePassword(userLogin, changePasswordData);

            // Assert
            Assert.IsFalse(result.Data);
            Assert.AreEqual(StatusCode.Fail, result.Status);
            Assert.AreEqual("The user with this login doesn't exist", result.Message);
        }

        [Test]
        public async Task ChangePassword_CurrentPasswordDoesNotMatch_ReturnsFail()
        {
            // Arrange
            var userLogin = "user1";
            var currentPassword = "currentPassword";
            var newPassword = "newPassword";
            var user = new User { Login = userLogin, Password = Hasher.GetSHA256Hash("differentPassword") };
            var changePasswordData = new ChangePasswordDTO { CurrentPassword = currentPassword, NewPassword = newPassword };
            _mockUserRepository.Setup(repo => repo.GetAll()).Returns(new List<User> { user }.AsQueryable());

            // Act
            var result = await _accountService.ChangePassword(userLogin, changePasswordData);

            // Assert
            Assert.IsFalse(result.Data);
            Assert.AreEqual(StatusCode.Fail, result.Status);
            Assert.AreEqual("CurrentPassword doesn't match current user password", result.Message);
        }

        [Test]
        public async Task ChangePassword_Success_ReturnsSuccess()
        {
            // Arrange
            var userLogin = "user1";
            var currentPassword = "currentPassword";
            var newPassword = "newPassword";
            var user = new User { Login = userLogin, Password = Hasher.GetSHA256Hash(currentPassword) };
            var changePasswordData = new ChangePasswordDTO { CurrentPassword = currentPassword, NewPassword = newPassword };
            _mockUserRepository.Setup(repo => repo.GetAll()).Returns(new List<User> { user }.AsQueryable());
            _mockUserRepository.Setup(repo => repo.Update(It.IsAny<User>())).ReturnsAsync(new RepositoryResponse { Status = StatusCode.Success, IsSuccessCompleted = true });

            // Act
            var result = await _accountService.ChangePassword(userLogin, changePasswordData);

            // Assert
            Assert.IsTrue(result.Data);
            Assert.AreEqual(StatusCode.Success, result.Status);
            Assert.AreEqual("Password changed successfully", result.Message);
            Assert.AreEqual(Hasher.GetSHA256Hash(newPassword), user.Password);
        }

        [Test]
        public async Task GetContactProfile_ValidContact_ReturnsProfile()
        {
            // Arrange
            var userLogin = "user1";
            var contactLogin = "contactUser";
            var contact = new Contact
            {
                User = new User { Login = userLogin },
                ContactUser = new User { Login = contactLogin }
            };
            var contactUser = new User
            {
                Login = contactLogin,
                Profile = new Profile { Nickname = "ContactNickname", Avatar = Encoding.UTF8.GetBytes("avatarData") }
            };
            _mockUserRepository.Setup(repo => repo.GetAll()).Returns(new List<User> { contactUser }.AsQueryable());

            // Act
            var result = await _accountService.GetContactProfile(userLogin, contact);

            // Assert
            Assert.IsNotNull(result.Data);
            Assert.AreEqual(StatusCode.Success, result.Status);
            Assert.AreEqual("ContactNickname", result.Data.Nickname);
            Assert.AreEqual("Success", result.Message);
        }

        [Test]
        public void GetContactProfile_IncorrectContact_ThrowsException()
        {
            // Arrange
            var userLogin = "user1";
            var contact = new Contact
            {
                User = new User { Login = "anotherUser" },
                ContactUser = new User { Login = "differentUser" }
            };

            // Act & Assert
            var ex = Assert.ThrowsAsync<Exception>(() => _accountService.GetContactProfile(userLogin, contact));
            Assert.AreEqual("Uncorrect contact", ex.Message);
        }

        [Test]
        public async Task GetContactProfile_ContactUserNotFound_ReturnsFail()
        {
            // Arrange
            var userLogin = "user1";
            var contactLogin = "contactUser";
            var contact = new Contact
            {
                User = new User { Login = userLogin },
                ContactUser = new User { Login = contactLogin }
            };
            _mockUserRepository.Setup(repo => repo.GetAll()).Returns(new List<User>().AsQueryable());

            // Act
            var result = await _accountService.GetContactProfile(userLogin, contact);

            // Assert
            Assert.IsNull(result.Data);
            Assert.AreEqual(StatusCode.Fail, result.Status);
            Assert.AreEqual("Contact user doesn't exist", result.Message);
        }

        [Test]
        public async Task GetProfileByLogin_UserExists_ReturnsProfile()
        {
            // Arrange
            var login = "existingUser";
            var user = new User
            {
                Login = login,
                Profile = new Profile
                {
                    Nickname = "ExistingNickname",
                    Avatar = Encoding.UTF8.GetBytes("avatarData")
                }
            };
            _mockUserRepository.Setup(repo => repo.GetAll())
                .Returns(new List<User> { user }.AsQueryable());

            // Act
            var result = await _accountService.GetProfileByLogin(login);

            // Assert
            Assert.IsNotNull(result.Data);
            Assert.AreEqual(StatusCode.Success, result.Status);
            Assert.AreEqual("ExistingNickname", result.Data.Nickname);
            Assert.AreEqual("Success", result.Message);
        }

        [Test]
        public async Task GetProfileByLogin_UserNotFound_ReturnsFail()
        {
            // Arrange
            var login = "nonexistentUser";
            _mockUserRepository.Setup(repo => repo.GetAll())
                .Returns(new List<User>().AsQueryable());

            // Act
            var result = await _accountService.GetProfileByLogin(login);

            // Assert
            Assert.IsNull(result.Data);
            Assert.AreEqual(StatusCode.Fail, result.Status);
            Assert.AreEqual("User with this login doesn't exist", result.Message);
        }

        [Test]
        public async Task Login_ValidCredentials_ReturnsSuccess()
        {
            // Arrange
            var loginData = new LoginDTO { Login = "user1", Password = "password" };
            var user = new User
            {
                Login = loginData.Login,
                Password = Hasher.GetSHA256Hash(loginData.Password)
            };
            _mockUserRepository.Setup(repo => repo.GetAll()).Returns(new List<User> { user }.AsQueryable());

            // Act
            var result = await _accountService.Login(loginData);

            // Assert
            Assert.IsNotNull(result.Data);
            Assert.AreEqual(StatusCode.Success, result.Status);
            Assert.AreEqual("Successful authorization", result.Message);
            Assert.IsTrue(result.Data.IsAuthenticated);
            Assert.AreEqual(result.Data.AuthenticationType, CookieAuthenticationDefaults.AuthenticationScheme);
        }

        [Test]
        public async Task Login_InvalidPassword_ReturnsFail()
        {
            // Arrange
            var loginData = new LoginDTO { Login = "user1", Password = "wrongPassword" };
            var user = new User
            {
                Login = loginData.Login,
                Password = Hasher.GetSHA256Hash("correctPassword")
            };
            _mockUserRepository.Setup(repo => repo.GetAll()).Returns(new List<User> { user }.AsQueryable());

            // Act
            var result = await _accountService.Login(loginData);

            // Assert
            Assert.IsNull(result.Data);
            Assert.AreEqual(StatusCode.Fail, result.Status);
            Assert.AreEqual("Incorrect password", result.Message);
        }

        [Test]
        public async Task Login_UserNotFound_ReturnsFail()
        {
            // Arrange
            var loginData = new LoginDTO { Login = "nonexistentUser", Password = "password" };
            _mockUserRepository.Setup(repo => repo.GetAll()).Returns(new List<User>().AsQueryable());

            // Act
            var result = await _accountService.Login(loginData);

            // Assert
            Assert.IsNull(result.Data);
            Assert.AreEqual(StatusCode.Fail, result.Status);
            Assert.AreEqual("There is not user with this login", result.Message);
        }

        [Test]
        public async Task Register_SuccessfulRegistration_ReturnsSuccess()
        {
            // Arrange
            var registrationData = new RegistrationDTO
            {
                Login = "newUser",
                Email = "newUser@example.com",
                Password = "password",
                Avatar = "avatarData",
                Nickname = "NewNickname"
            };
            _mockUserRepository.Setup(repo => repo.GetAll()).Returns(new List<User>().AsQueryable());
            _mockUserRepository.Setup(repo => repo.Create(It.IsAny<User>())).ReturnsAsync(new RepositoryResponse { IsSuccessCompleted = true });
            _mockProfileRepository.Setup(repo => repo.Create(It.IsAny<Profile>())).ReturnsAsync(new RepositoryResponse { IsSuccessCompleted = true });

            // Act
            var result = await _accountService.Register(registrationData);

            // Assert
            Assert.IsNotNull(result.Data);
            Assert.AreEqual(StatusCode.Success, result.Status);
            Assert.AreEqual("Success register", result.Message);
        }

        [Test]
        public async Task Register_UserWithSameLoginExists_ReturnsFail()
        {
            // Arrange
            var registrationData = new RegistrationDTO
            {
                Login = "existingUser",
                Email = "newUser@example.com",
                Password = "password",
                Avatar = "avatarData",
                Nickname = "NewNickname"
            };
            var existingUser = new User { Login = "existingUser" };
            _mockUserRepository.Setup(repo => repo.GetAll()).Returns(new List<User> { existingUser }.AsQueryable());

            // Act
            var result = await _accountService.Register(registrationData);

            // Assert
            Assert.IsNull(result.Data);
            Assert.AreEqual(StatusCode.Fail, result.Status);
            Assert.AreEqual("The user with the current login already exists", result.Message);
        }

        [Test]
        public async Task Register_UserWithSameEmailExists_ReturnsFail()
        {
            // Arrange
            var registrationData = new RegistrationDTO
            {
                Login = "newUser",
                Email = "existingUser@example.com",
                Password = "password",
                Avatar = "avatarData",
                Nickname = "NewNickname"
            };
            var existingUser = new User { Email = "existingUser@example.com" };
            _mockUserRepository.Setup(repo => repo.GetAll()).Returns(new List<User> { existingUser }.AsQueryable());

            // Act
            var result = await _accountService.Register(registrationData);

            // Assert
            Assert.IsNull(result.Data);
            Assert.AreEqual(StatusCode.Fail, result.Status);
            Assert.AreEqual("The user with the current email already exists", result.Message);
        }

        [Test]
        public async Task Register_FailedToCreateUser_ReturnsFail()
        {
            // Arrange
            var registrationData = new RegistrationDTO
            {
                Login = "newUser",
                Email = "newUser@example.com",
                Password = "password",
                Avatar = "avatarData",
                Nickname = "NewNickname"
            };
            _mockUserRepository.Setup(repo => repo.GetAll()).Returns(new List<User>().AsQueryable());
            _mockUserRepository.Setup(repo => repo.Create(It.IsAny<User>())).ReturnsAsync(new RepositoryResponse { IsSuccessCompleted = false, Message = "Failed to create user" });
            _mockProfileRepository.Setup(repo => repo.Create(It.IsAny<Profile>())).ReturnsAsync(new RepositoryResponse { IsSuccessCompleted = false, Message = "Failed to create user" });

            // Act
            var result = await _accountService.Register(registrationData);

            // Assert
            Assert.IsNull(result.Data);
            Assert.AreEqual(StatusCode.Fail, result.Status);
            Assert.AreEqual("Failed to create user", result.Message);
        }


    }
}