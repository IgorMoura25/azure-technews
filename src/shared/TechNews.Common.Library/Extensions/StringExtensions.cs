namespace TechNews.Common.Library.Extensions;

public static class StringExtensions
{
    public static string ToLowerKebabCase(this string text)
    {
        if (string.IsNullOrEmpty(text) || string.IsNullOrWhiteSpace(text))
            return text;

        if (!text.Any(char.IsUpper))
            return text;

        var normalizedText = text.Replace("-", string.Empty);

        for (var i = 0; i < normalizedText.Length; i++)
        {
            var currentChar = normalizedText[i];

            if (char.IsUpper(currentChar) && i > 0)
            {
                normalizedText = normalizedText.Insert(i, "-");
                i++;
            }
        }

        return normalizedText.ToLower();
    }
}