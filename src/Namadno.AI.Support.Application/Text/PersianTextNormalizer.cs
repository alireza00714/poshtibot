using System.Text;
using System.Text.RegularExpressions;
using Namadno.AI.Support.Application.Abstractions.Text;

namespace Namadno.AI.Support.Application.Text;

public sealed class PersianTextNormalizer : IPersianTextNormalizer
{
    private static readonly Regex Diacritics = new(@"[\u064B-\u065F\u0670\u06D6-\u06ED]", RegexOptions.Compiled);
    private static readonly Regex ExtraWhitespace = new(@"\s+", RegexOptions.Compiled);

    public string Normalize(string? input)
    {
        if (string.IsNullOrWhiteSpace(input))
        {
            return string.Empty;
        }

        var builder = new StringBuilder(input.Length);
        foreach (var ch in input.Normalize(NormalizationForm.FormKC))
        {
            builder.Append(Map(ch));
        }

        var text = builder.ToString();
        text = text.Replace("\u200c", " ", StringComparison.Ordinal);
        text = text.Replace("\u200d", string.Empty, StringComparison.Ordinal);
        text = text.Replace("\u200b", string.Empty, StringComparison.Ordinal);
        text = Diacritics.Replace(text, string.Empty);
        text = ExtraWhitespace.Replace(text, " ").Trim();
        return text;
    }

    private static char Map(char ch) => ch switch
    {
        'ي' or 'ى' or 'ئ' or 'ۍ' => 'ی',
        'ك' => 'ک',
        'ة' => 'ه',
        'أ' or 'إ' or 'آ' or 'ٱ' => 'ا',
        'ؤ' => 'و',
        '٠' or '۰' => '0',
        '١' or '۱' => '1',
        '٢' or '۲' => '2',
        '٣' or '۳' => '3',
        '٤' or '۴' => '4',
        '٥' or '۵' => '5',
        '٦' or '۶' => '6',
        '٧' or '۷' => '7',
        '٨' or '۸' => '8',
        '٩' or '۹' => '9',
        '؟' or '?' => ' ',
        '،' or ',' or ';' or '؛' or '!' or '！' => ' ',
        '‌' => ' ',
        _ => ch
    };
}
