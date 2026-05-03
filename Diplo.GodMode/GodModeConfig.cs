namespace Diplo.GodMode
{
    /// <summary>
    /// Configuration for GodMode
    /// </summary>
    public class GodModeConfig
    {
        /// <summary>
        /// Gets the configuration section name in appSettings
        /// </summary>
        public const string ConfigSectionName = "GodMode";

        public string[] FeaturesToHide { get; set; } = [];

        public string[] AliasesToIgnore { get; set; } = ["umbracoFile"];

        public DiagnosticsConfig Diagnostics { get; set; } = new DiagnosticsConfig();

        /// <summary>
        /// Config for the diagnostics section
        /// </summary>
        public class DiagnosticsConfig
        {
            /// <summary>
            /// Get or set the groups that should be hidden in diganostics
            /// </summary>
            public string[] GroupsToHide { get; set; } = [];

            /// <summary>
            /// Get or set the sub-sections that should be hidden in diganostics
            /// </summary>
            public string[] SectionsToHide { get; set; } = [];

            /// <summary>
            /// Get or set the keys to redact the values from
            /// </summary>
            public string[] KeysToRedact { get; set; } = ["ConnectionStrings:umbracoDbDSN", "godmode_password", "ConnectionString"];

            /// <summary>
            /// Get or set keywords found in a key that should have their values redacted.
            /// </summary>
            public string[] KeyMatchesToRedact { get; set; } = ["password", "pwd", "secret", "key"];

            /// <summary>
            /// Get or set the environment variable that reveals the redacted value
            /// </summary>
            public string RedactRevealPasswordEnv { get; set; } = "godmode_password";
        }
    }
}
