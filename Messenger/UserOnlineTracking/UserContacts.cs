using Microsoft.Extensions.Logging;
using System.Text;

namespace Messenger.UserOnlineTracking
{
    public class UserContacts
    {
        private readonly Dictionary<string, List<string>> _contacts = new Dictionary<string, List<string>>();

        public void AddContact(string userLogin, string contactLogin)
        {
            if (!_contacts.ContainsKey(userLogin))
            {
                _contacts[userLogin] = new List<string>();
            }
            _contacts[userLogin].Add(contactLogin);
        }

        public void RemoveContact(string userLogin, string contactLogin)
        {
            if (_contacts.ContainsKey(userLogin))
            {
                _contacts[userLogin].Remove(contactLogin);
                if (_contacts[userLogin].Count == 0)
                {
                    _contacts.Remove(userLogin);
                }
            }
        }

        public List<string> GetContacts(string userLogin)
        {
            return _contacts.ContainsKey(userLogin) ? _contacts[userLogin] : new List<string>();
        }

        public bool ContainsUser(string userLogin)
        {
            return _contacts.ContainsKey(userLogin);
        }

        public void SetContacts(string userLogin, List<string> contacts)
        {
            _contacts[userLogin] = contacts;
        }

        public void RemoveUser(string userLogin)
        {
            _contacts.Remove(userLogin);
        }

        public void LogContacts(ILogger logger)
        {
            StringBuilder builder = new StringBuilder();
            builder.AppendLine("--------------------[UserContacts]--------------------");

            foreach (var userContact in _contacts)
            {
                builder.AppendLine($"UserLogin: {userContact.Key}");
                foreach (var contactLogin in userContact.Value)
                {
                    builder.AppendLine($" ContactLogin: {contactLogin}");
                }
            }

            logger.LogInformation(builder.ToString());
        }
    }
}
