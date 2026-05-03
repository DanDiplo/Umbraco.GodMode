namespace Diplo.GodMode.Models
{
    /// <summary>
    /// Represents an individual diagnostic item.
    /// </summary>
    public class Diagnostic
    {
        public Diagnostic(string key, string value)
        {
            Key = key;
            Value = value;
        }

        public Diagnostic(string key, object value)
        {
            Key = key;
            Value = value?.ToString() ?? string.Empty;
        }

        public Diagnostic(string key, IEnumerable<string> items)
        {
            Key = key;
            Value = string.Join(", ", items ?? []);
        }

        public Diagnostic(string key, IEnumerable<object> items)
        {
            Key = key;
            Value = string.Join(", ", items ?? []);
        }

        public string Key { get; }

        public string Value { get; set; }

        public override string ToString()
        {
            return $"{Key}: {Value}{Environment.NewLine}";
        }
    }
}