using System;

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

        public string[] FeaturesToHide { get; set; } = Array.Empty<string>();

        public DiagnosticsConfig Diagnostics { get; set; } = new DiagnosticsConfig();

        /// <summary>
        /// Config for the diagnostics section
        /// </summary>
        public class DiagnosticsConfig
        {
            /// <summary>
            /// Get or set the groups that should be hidden in diganostics
            /// </summary>
            public string[] GroupsToHide { get; set; } = Array.Empty<string>();

            /// <summary>
            /// Get or set the sub-sections that should be hidden in diganostics
            /// </summary>
            public string[] SectionsToHide { get; set; } = Array.Empty<string>();

            /// <summary>
            /// Get or set the keys to redact the values from
            /// </summary>
            public string[] KeysToRedact { get; set; } = Array.Empty<string>();
        }
    }
}
