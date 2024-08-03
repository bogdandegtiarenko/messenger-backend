using Messenger.Domain.Core.DTOs.Authentication;
using Messenger.Services.Interfaces;
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
            if (!_userConnections.ContainsKey(userLogin))
            {
                _userConnections[userLogin] = new List<string>();
            }

            _userConnections[userLogin].Add(Context.ConnectionId);

            if (_userConnections[userLogin].Count == 1)
            {
                List<string> contactLogins = (await _contactService.GetContactLogins(userLogin)).Data;
                _userContacts[userLogin] = contactLogins;
                _userLoginsOnline.Add(userLogin);

                var onlineContacts = _userContacts[userLogin].Intersect(_userLoginsOnline).ToList();
                _userOnlineContacts[userLogin] = onlineContacts;

                foreach(var onlineContactLogin in onlineContacts)
                {
                    if (!_userOnlineContacts[onlineContactLogin].Contains(userLogin))
                    {
                        _userOnlineContacts[onlineContactLogin].Add(userLogin);
                    }

                    await Clients.Clients(_userConnections[onlineContactLogin]).SendAsync("ReceiveOnlineStatus", new
                    {
                        login = userLogin,
                        onlineStatus = true
                    });
                }
            }

            await PrintInfo("Connection");
        }

        public override async Task OnDisconnectedAsync(Exception? exception)
        {
            string userConnectionId = Context.ConnectionId;

            foreach(var userConnection in _userConnections)
            {
                string login = userConnection.Key;
                bool isRightUser = false;
                
                foreach(var connectionId in userConnection.Value)
                {
                    if(userConnectionId == connectionId)
                    {
                        userConnection.Value.Remove(connectionId);
                        isRightUser = true;
                        break;
                    }
                }

                if (isRightUser)
                {
                    if(userConnection.Value.Count == 0)
                    {
                        _userContacts.Remove(login);
                        _userLoginsOnline.Remove(login);

                        _userOnlineContacts[login].ForEach(async onlineContactLogin =>
                        {
                            if(onlineContactLogin != login)
                            {
                                _userOnlineContacts[onlineContactLogin].Remove(login);

                                await Clients.Clients(_userConnections[onlineContactLogin]).SendAsync("ReceiveOnlineStatus", 
                                new
                                {
                                    login = login,
                                    onlineStatus = false
                                });
                            }
                        });

                        _userConnections.Remove(login);
                        _userOnlineContacts.Remove(login);
                    }
                    break;
                }

            }

            await PrintInfo("Disconnection");
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

        private async Task PrintInfo(string title)
        {
            StringBuilder builder = new StringBuilder();

            builder.AppendLine($"====================[{title}]====================");
            builder.AppendLine("--------------------[_userConnection]--------------------");

            foreach (var userConnection in _userConnections)
            {
                builder.AppendLine($"Login: {userConnection.Key}");
                foreach (var connectionId in _userConnections[userConnection.Key])
                {
                    builder.AppendLine($" id: {connectionId}");
                }
            }

            builder.AppendLine("--------------------[_userLoginsOnline]--------------------");

            foreach (var login in _userLoginsOnline)
            {
                builder.AppendLine($"- {login}");
            }

            builder.AppendLine("--------------------[_userOnlineContacts]--------------------");

            foreach (var loginToOnlineContacts in _userOnlineContacts)
            {
                builder.AppendLine($"UserLogin: {loginToOnlineContacts.Key}");
                foreach (var login in loginToOnlineContacts.Value)
                {
                    builder.AppendLine($" ContactLogin: {login}");
                }
            }

            builder.AppendLine("=======================================================");

            string info = builder.ToString();
            _logger.LogInformation(info);
        }
    }
}
