using Microsoft.Extensions.Logging;
using System.Text;
namespace Messenger.UserOnlineTracking
{
    public class UserLoginsOnline
    {
        private readonly HashSet<string> _loginsOnline = new HashSet<string>();

        public void AddLogin(string userLogin)
        {
            _loginsOnline.Add(userLogin);
        }

        public void RemoveLogin(string userLogin)
        {
            _loginsOnline.Remove(userLogin);
        }

        public bool Contains(string userLogin)
        {
            return _loginsOnline.Contains(userLogin);
        }

        public List<string> GetAllLogins()
        {
            return _loginsOnline.ToList();
        }

        public void LogLoginsOnline(ILogger logger)
        {
            StringBuilder builder = new StringBuilder();
            builder.AppendLine("--------------------[UserLoginsOnline]--------------------");

            foreach (var login in _loginsOnline)
            {
                builder.AppendLine($"- {login}");
            }

            logger.LogInformation(builder.ToString());
        }
    }
}
