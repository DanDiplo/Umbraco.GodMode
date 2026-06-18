namespace Diplo.GodMode.Models
{
    /// <summary>
    /// Represents how many dictionary items are untranslated for a single language.
    /// </summary>
    public class DictionaryTranslationStatus
    {
        /// <summary>
        /// The language Id (umbracoLanguage.id).
        /// </summary>
        public int LanguageId { get; set; }

        /// <summary>
        /// The language culture name, e.g. "French".
        /// </summary>
        public string Name { get; set; }

        /// <summary>
        /// The language ISO code, e.g. "fr".
        /// </summary>
        public string Culture { get; set; }

        /// <summary>
        /// The total number of dictionary items that exist.
        /// </summary>
        public int TotalItems { get; set; }

        /// <summary>
        /// The number of dictionary items with no value, or a blank value, for this language.
        /// </summary>
        public int UntranslatedItems { get; set; }
    }
}
