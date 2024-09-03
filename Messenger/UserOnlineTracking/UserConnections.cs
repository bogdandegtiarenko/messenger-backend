using System.Collections;
using Microsoft.Extensions.Logging;
using System.Text;

namespace Messenger.UserOnlineTracking
{
    public class UserConnections
    {
        private readonly Dictionary<string, List<string>> _connections = new Dictionary<string, List<string>>();

        public void AddConnection(string userLogin, string connectionId)
        {
            if (!_connections.ContainsKey(userLogin))
            {
                _connections[userLogin] = new List<string>();
            }
            _connections[userLogin].Add(connectionId);
        }

        public void RemoveConnection(string userLogin, string connectionId)
        {
            if (_connections.ContainsKey(userLogin))
            {
                _connections[userLogin].Remove(connectionId);
                if (_connections[userLogin].Count == 0)
                {
                    _connections.Remove(userLogin);
                }
            }
        }

        public string? GetLogin(string userConnectionId)
        {
            foreach (var userConnection in _connections)
            {
                string login = userConnection.Key;

                foreach (var connectionId in userConnection.Value)
                {
                    if (userConnectionId == connectionId)
                    {
                        return login;
                    }
                }
            }
            return null;
        }

        public List<string> GetConnections(string userLogin)
        {
            return _connections.ContainsKey(userLogin) ? _connections[userLogin] : new List<string>();
        }

        public bool ContainsUser(string userLogin)
        {
            return _connections.ContainsKey(userLogin);
        }

        public int GetConnectionsCount(string userLogin)
        {
            return _connections.ContainsKey(userLogin) ? _connections[userLogin].Count : 0;
        }

        public void LogConnections(ILogger logger)
        {
            StringBuilder builder = new StringBuilder();
            builder.AppendLine("--------------------[UserConnections]--------------------");

            foreach (var userConnection in _connections)
            {
                builder.AppendLine($"Login: {userConnection.Key}");
                foreach (var connectionId in userConnection.Value)
                {
                    builder.AppendLine($" id: {connectionId}");
                }
            }

            logger.LogInformation(builder.ToString());
        }
    }
}
