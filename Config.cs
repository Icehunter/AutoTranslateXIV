// --------------------------------------------------------------------------------------------------------------------
// <copyright file="Config.cs" company="SyndicatedLife">
//   Copyright© 2007 - 2021 Ryan Wilson &amp;lt;syndicated.life@gmail.com&amp;gt; (https://syndicated.life/)
//   Licensed under the MIT license. See LICENSE.md in the solution root for full license information.
// </copyright>
// <summary>
//   Config.cs Implementation
// </summary>
// --------------------------------------------------------------------------------------------------------------------

namespace AutoTranslateXIV {
    using Dalamud.Configuration;

    using Translation;

    public class Config : IPluginConfiguration {
        public string AutomaticTranslateFrom { get; set; } = "Japanese";
        public string AutomaticTranslateTo { get; set; } = "English";
        public string CognitiveServiceKey { get; set; } = string.Empty;
        public string CognitiveServiceRegion { get; set; } = string.Empty;
        public bool Enabled { get; set; } = true;
        public string GoogleServiceKey { get; set; } = string.Empty;
        public string ManualTranslateFrom { get; set; } = "English";
        public string ManualTranslateTo { get; set; } = "Japanese";
        public TranslationProvider TranslationProvider { get; set; } = TranslationProvider.Google;
        public int Version { get; set; } = 0;
    }
}