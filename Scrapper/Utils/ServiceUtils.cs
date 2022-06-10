using System.Text;

namespace Scrapper.Utils
{
    public static class ServiceUtils
    {
        public static string IncrementStringNumber(string str)
        {
            string strNew = "";
            if (!string.IsNullOrEmpty(str) && int.TryParse(str, out int num))
            {
                num++;
                strNew = num.ToString();
            }
            return strNew;
        }
        public static string GetUntilSpecial(string text)
        {
            //Get until a special character appear
            StringBuilder sb = new();
            for (int i = 0; i < text.Length; i++)
            {
                if (text[i] >= '0' && text[i] <= '9' || text[i] >= 'A' && text[i] <= 'Z' || text[i] >= 'a' && text[i] <= 'z' || text[i] == '.' || text[i] == '_')
                {
                    sb.Append(text[i]);
                }
                else
                {
                    break;
                }
            }
            return sb.ToString();
        }

    }
}
