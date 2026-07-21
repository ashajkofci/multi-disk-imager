using System.Globalization;
using System.Xml.Linq;

namespace MultiDiskImager.Platform;

internal sealed class MacPlist
{
    private readonly Dictionary<string, XElement> _values;

    private MacPlist(Dictionary<string, XElement> values) => _values = values;

    public static MacPlist Parse(string xml)
    {
        var document = XDocument.Parse(xml);
        var dictionary = document.Root?.Element("dict") ?? throw new FormatException("The plist has no root dictionary.");
        var children = dictionary.Elements().ToArray();
        var values = new Dictionary<string, XElement>(StringComparer.Ordinal);
        for (var index = 0; index + 1 < children.Length; index += 2)
        {
            if (children[index].Name.LocalName == "key")
            {
                values[children[index].Value] = children[index + 1];
            }
        }

        return new MacPlist(values);
    }

    public string? String(string key) => _values.TryGetValue(key, out var value) ? value.Value : null;

    public long Integer(string key, long fallback = 0) =>
        _values.TryGetValue(key, out var value) && long.TryParse(value.Value, NumberStyles.Integer, CultureInfo.InvariantCulture, out var result)
            ? result
            : fallback;

    public bool Boolean(string key, bool fallback = false) =>
        _values.TryGetValue(key, out var value)
            ? value.Name.LocalName == "true" || (value.Name.LocalName != "false" && fallback)
            : fallback;

    public IReadOnlyList<string> StringArray(string key) =>
        _values.TryGetValue(key, out var array) && array.Name.LocalName == "array"
            ? array.Elements("string").Select(element => element.Value).ToArray()
            : [];

    public IReadOnlyList<string> DescendantStrings(string key) => _values.Values
        .SelectMany(value => DescendantStrings(value, key))
        .Distinct(StringComparer.Ordinal)
        .ToArray();

    public IReadOnlyList<MacPlist> DictionaryArray(string key)
    {
        if (!_values.TryGetValue(key, out var array) || array.Name.LocalName != "array")
        {
            return [];
        }

        return array.Elements("dict").Select(dictionary =>
        {
            var document = new XDocument(new XElement("plist", new XElement(dictionary)));
            return Parse(document.ToString(SaveOptions.DisableFormatting));
        }).ToArray();
    }

    private static IEnumerable<string> DescendantStrings(XElement element, string key)
    {
        if (element.Name.LocalName == "dict")
        {
            var children = element.Elements().ToArray();
            for (var index = 0; index + 1 < children.Length; index += 2)
            {
                if (children[index].Name.LocalName == "key" &&
                    children[index].Value.Equals(key, StringComparison.Ordinal) &&
                    children[index + 1].Name.LocalName == "string" &&
                    !string.IsNullOrWhiteSpace(children[index + 1].Value))
                {
                    yield return children[index + 1].Value;
                }

                foreach (var value in DescendantStrings(children[index + 1], key))
                {
                    yield return value;
                }
            }

            yield break;
        }

        foreach (var child in element.Elements())
        {
            foreach (var value in DescendantStrings(child, key))
            {
                yield return value;
            }
        }
    }
}
