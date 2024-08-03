using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Security.Cryptography;

namespace Messenger.Domain.Core
{
    public static class Hasher
    {
        public static string GetSHA256Hash(string inputString)
        {
            using (SHA256 sha256 = SHA256.Create())
            {
                // Преобразуем строку в байты
                byte[] bytes = Encoding.UTF8.GetBytes(inputString);

                // Вычисляем хеш
                byte[] hashBytes = sha256.ComputeHash(bytes);

                // Преобразуем байты в строку HEX
                StringBuilder stringBuilder = new StringBuilder();
                for (int i = 0; i < hashBytes.Length; i++)
                {
                    stringBuilder.Append(hashBytes[i].ToString("X2"));
                }

                return stringBuilder.ToString();
            }
        }
    }
}
