using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace launch.Views.Other
{
    class WordDeclensionHelper
    {
        public static string GetLikesText(int count)
        {
            return GetWordForm(count, "лайк", "лайка", "лайков");
        }

        public static string GetDislikesText(int count)
        {
            return GetWordForm(count, "дизлайк", "дизлайка", "дизлайков");
        }

        public static string GetSubscriptionsText(int count)
        {
            return GetWordForm(count, "активная подписка", "активные подписки", "активных подписок");
        }

        private static string GetWordForm(int count, string form1, string form2, string form5)
        {
            if (count < 0) return $"Ошибка: отрицательное число";

            if (count == 0) return "Нет активных подписок";

            int lastDigit = count % 10;
            int lastTwoDigits = count % 100;

            if (count == 0) return $"0 {form5}";
            if (lastTwoDigits >= 11 && lastTwoDigits <= 19) return $"{count} {form5}";

            return lastDigit switch
            {
                1 => $"{count} {form1}",
                2 or 3 or 4 => $"{count} {form2}",
                _ => $"{count} {form5}",
            };
        }
    }
}
