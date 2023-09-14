using UnityEngine;

namespace GameEvent
{
    public static class LogColorUtil
    {
        public static string ToColor(this string content, Color color)
        {
            var hex = ColorUtility.ToHtmlStringRGB(color);
            return $"<color=#{hex}>{content}</color>";
        }
    }
}