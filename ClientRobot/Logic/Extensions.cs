using System;
using Google.Protobuf;

namespace Game
{
    static class Extensions
    {
        public static string ToErrorMessage(this IMessage message, string desc)
        {
            if (message == null)
                return "连接丢失";

            var code = (int)message.GetType().GetProperty("Errcode").GetGetMethod().Invoke(message, null);
            return ToErrorMessage(code, desc);
        }

        public static string ToErrorMessage(this int code, string desc)
        {
            var codestr = code.ToString();
            foreach (var str in desc.Split(' '))
            {
                if (str.StartsWith(codestr))
                    return str.Substring(codestr.Length);
            }

            return $"未知错误_{codestr}";
        }
    }
}
