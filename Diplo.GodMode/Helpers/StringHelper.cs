namespace Diplo.GodMode.Helpers
{
    public static class StringHelper
    {
        /// <summary>
        /// Redacts the given input string with the given character
        /// </summary>
        /// <param name="input">The input to be redacted</param>
        /// <param name="redactChar">The redaction character</param>
        /// <returns></returns>
        public static string RedactString(string input, char redactChar = 'x')
        {
            // Check if the input string is null or its length is 4 or less
            if (string.IsNullOrEmpty(input))
            {
                return string.Empty;
            }

            return new string(redactChar, input.Length);
        }
    }
}
