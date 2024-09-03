using System.Diagnostics.Eventing.Reader;
using Microsoft.Extensions.Logging;
using System.Text;

namespace Messenger.UserOnlineTracking
{
    public class UserOnlineContacts
    {
        private readonly Dictionary<string, List<string>> _onlineContacts = new Dictionary<string, List<string>>();

        public void AddOnlineContact(string userLogin, string contactLogin)
        {
            if (!_onlineContacts.ContainsKey(userLogin))
            {
                _onlineContacts[userLogin] = new List<string>();
            }
            _onlineContacts[userLogin].Add(contactLogin);
        }

        public void RemoveOnlineContact(string userLogin, string contactLogin)
        {
            if (_onlineContacts.ContainsKey(userLogin))
            {
                _onlineContacts[userLogin].Remove(contactLogin);
                if (_onlineContacts[userLogin].Count == 0)
                {
                    _onlineContacts.Remove(userLogin);
                }
            }
        }

        public List<string> GetOnlineContacts(string userLogin)
        {
            return _onlineContacts.ContainsKey(userLogin) ? _onlineContacts[userLogin] : new List<string>();
        }

        public bool ContainsUser(string userLogin)
        {
            return _onlineContacts.ContainsKey(userLogin);
        }

        public void SetOnlineContacts(string userLogin, List<string> contacts)
        {
            _onlineContacts[userLogin] = contacts;
        }

        public void RemoveUser(string userLogin)
        {
            _onlineContacts.Remove(userLogin);
        }
        public bool IsContactOnline(string userLogin, string contactLogin)
        {
            return _onlineContacts.ContainsKey(userLogin) && _onlineContacts[userLogin].Contains(contactLogin);
        }

        public void LogOnlineContacts(ILogger logger)
        {
            StringBuilder builder = new StringBuilder();
            builder.AppendLine("--------------------[UserOnlineContacts]--------------------");

            foreach (var loginToOnlineContacts in _onlineContacts)
            {
                builder.AppendLine($"UserLogin: {loginToOnlineContacts.Key}");
                foreach (var login in loginToOnlineContacts.Value)
                {
                    builder.AppendLine($" ContactLogin: {login}");
                }
            }

            logger.LogInformation(builder.ToString());
        }
    }
}
