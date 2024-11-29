using System.Reflection;
using System.Runtime.Serialization;
using System.Security.Cryptography;
using System.Text;

namespace DefenseApiExample.Utils;

public static class DefenseUtils
{
    public static string GetEnumMemberValue<T>(T enumValue) where T : Enum
    {
        var type = typeof(T);
        var memberInfo = type.GetMember(enumValue.ToString());

        if (memberInfo.Length > 0)
        {
            var attribute = memberInfo[0].GetCustomAttribute<EnumMemberAttribute>();
            if (attribute != null)
            {
                return attribute.Value;
            }
        }

        throw new ArgumentException($"Enum member '{enumValue}' does not have an EnumMember attribute.");
    }
    
    public static string EncryptWithMD5(string text)
    {
        using (var md5 = MD5.Create())
        {
            byte[] inputBytes = Encoding.UTF8.GetBytes(text);
            byte[] hashBytes = md5.ComputeHash(inputBytes);

            StringBuilder stringBuilder = new StringBuilder();
            foreach (byte b in hashBytes)
            {
                stringBuilder.Append(b.ToString("x2"));
            }

            return stringBuilder.ToString();
        }
    }
}