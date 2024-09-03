using Messenger.Domain.Core.DTOs.Authentication;
using Messenger.Services.Interfaces;
using Messenger.UserOnlineTracking;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;
using System.Text;

namespace Messenger.Hubs
{
    public class MessengerHub : Hub
    {
        private UserConnections _userConnections;
        private UserContacts _userContacts;
        private UserOnlineContacts _userOnlineContacts;
        private UserLoginsOnline _userLoginsOnline;
        private IContactService _contactService;
        private ILogger<MessengerHub> _logger;

        public MessengerHub(
            UserConnections userConnections,
            UserContacts userContacts,
            UserOnlineContacts userOnlineContacts,
            UserLoginsOnline userLoginsOnline,
            IContactService contactService,
            ILogger<MessengerHub> logger
        )
        {
            _userConnections = userConnections;
            _userContacts = userContacts;
            _userOnlineContacts = userOnlineContacts;
            _userLoginsOnline = userLoginsOnline;
            _contactService = contactService;
            _logger = logger;
        }

        public async Task Connect(string userLogin)
        {
            _userConnections.AddConnection(userLogin, Context.ConnectionId);

            if (_userConnections.GetConnectionsCount(userLogin) == 1)
            {
                List<string> contactLogins = (await _contactService.GetContactLogins(userLogin)).Data;
                _userContacts.SetContacts(userLogin, contactLogins);

                _userLoginsOnline.AddLogin(userLogin);

                var onlineContacts = contactLogins.Intersect(_userLoginsOnline.GetAllLogins()).ToList();

                _userOnlineContacts.SetOnlineContacts(userLogin, onlineContacts);

                foreach(var onlineContactLogin in onlineContacts)
                {
                    if (!_userOnlineContacts.IsContactOnline(onlineContactLogin, userLogin))
                    {
                        _userOnlineContacts.AddOnlineContact(onlineContactLogin, userLogin);
                    }

                    await Clients.Clients(_userConnections.GetConnections(onlineContactLogin)).SendAsync("ReceiveOnlineStatus", new
                    {
                        login = userLogin,
                        onlineStatus = true
                    });
                }
            }

            await LogInfo("Connection");
        }

        public override async Task OnDisconnectedAsync(Exception? exception)
        {
            string userConnectionId = Context.ConnectionId;
            string? login = _userConnections.GetLogin(userConnectionId);
            _userConnections.RemoveConnection(login, userConnectionId);

            if (!_userConnections.ContainsUser(login))
            {
                _userContacts.RemoveUser(login);
                _userLoginsOnline.RemoveLogin(login);

                _userOnlineContacts.GetOnlineContacts(login).ForEach(async onlineContactLogin =>
                {
                    if (onlineContactLogin != login)
                    {
                        _userOnlineContacts.RemoveOnlineContact(onlineContactLogin, login);
                        await Clients.Clients(_userConnections.GetConnections(onlineContactLogin)).SendAsync("ReceiveOnlineStatus",
                        new
                        {
                            login = login,
                            onlineStatus = false
                        });
                    }
                });

                _userOnlineContacts.RemoveUser(login);
            }

            await LogInfo("Disconnection");
            await base.OnDisconnectedAsync(exception);
        }

        public async Task CheckOnlineStatus(string contactLogin)
        {
            bool isContactOnline = _userLoginsOnline.Contains(contactLogin);
            await Clients.Caller.SendAsync("ReceiveOnlineStatus", new {
                login = contactLogin,
                onlineStatus = isContactOnline
            });
        }

        private async Task LogInfo(string title)
        {
            StringBuilder builder = new StringBuilder();
            builder.AppendLine($"====================[{title}]====================");

            _userConnections.LogConnections(_logger);
            _userLoginsOnline.LogLoginsOnline(_logger);
            _userOnlineContacts.LogOnlineContacts(_logger);
            _userContacts.LogContacts(_logger);

            builder.AppendLine("=======================================================");

            string info = builder.ToString();
            _logger.LogInformation(info);
        }
    }
}
