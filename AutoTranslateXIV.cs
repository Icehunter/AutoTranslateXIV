// --------------------------------------------------------------------------------------------------------------------
// <copyright file="AutoTranslateXIV.cs" company="SyndicatedLife">
//   Copyright© 2007 - 2021 Ryan Wilson &amp;lt;syndicated.life@gmail.com&amp;gt; (https://syndicated.life/)
//   Licensed under the MIT license. See LICENSE.md in the solution root for full license information.
// </copyright>
// <summary>
//   AutoTranslateXIV.cs Implementation
// </summary>
// --------------------------------------------------------------------------------------------------------------------

namespace AutoTranslateXIV {
    using System;
    using System.Collections.Concurrent;
    using System.Collections.Generic;
    using System.Linq;
    using System.Threading.Tasks;

    using Dalamud.Game;
    using Dalamud.Game.ClientState;
    using Dalamud.Game.Command;
    using Dalamud.Game.Gui;
    using Dalamud.Game.Text;
    using Dalamud.Game.Text.SeStringHandling;
    using Dalamud.IoC;
    using Dalamud.Logging;
    using Dalamud.Plugin;

    using ImGuiNET;

    using Translation;

    using Num = System.Numerics;

    public sealed class AutoTranslateXIV : IDalamudPlugin {
        private static CognitiveTranslateProvider _cognitiveTranslateProvider;

        private static GoogleTranslateProvider _googleTranslateProvider;

        private readonly ConcurrentQueue<ChatMessage> _chatEntries = new ConcurrentQueue<ChatMessage>();

        private readonly Dictionary<XivChatType, string> _translateTypes = new() {
            {
                XivChatType.Say, "S"
            }, {
                XivChatType.Shout, "SH"
            }, {
                XivChatType.TellIncoming, "T"
            }, {
                XivChatType.Party, "P"
            }, {
                XivChatType.Alliance, "A"
            }, {
                XivChatType.Ls1, "LS1"
            }, {
                XivChatType.Ls2, "LS2"
            }, {
                XivChatType.Ls3, "LS3"
            }, {
                XivChatType.Ls4, "LS4"
            }, {
                XivChatType.Ls5, "LS5"
            }, {
                XivChatType.Ls6, "LS6"
            }, {
                XivChatType.Ls7, "LS7"
            }, {
                XivChatType.Ls8, "LS8"
            }, {
                XivChatType.FreeCompany, "FC"
            }, {
                XivChatType.NoviceNetwork, "Novice"
            }, {
                XivChatType.CustomEmote, "CustomEmote"
            }, {
                XivChatType.StandardEmote, "Emote"
            }, {
                XivChatType.Yell, "Y"
            }, {
                XivChatType.CrossParty, "CP"
            }, {
                XivChatType.PvPTeam, "PVP"
            }, {
                XivChatType.CrossLinkShell1, "CLS1"
            }, {
                XivChatType.CrossLinkShell2, "CLS2"
            }, {
                XivChatType.CrossLinkShell3, "CLS3"
            }, {
                XivChatType.CrossLinkShell4, "CLS4"
            }, {
                XivChatType.CrossLinkShell5, "CLS5"
            }, {
                XivChatType.CrossLinkShell6, "CLS6"
            }, {
                XivChatType.CrossLinkShell7, "CLS7"
            }, {
                XivChatType.CrossLinkShell8, "CLS8"
            },
        };

        private string _automaticTranslateFrom;

        private string _automaticTranslateTo;

        private string _cognitiveServiceKey;

        private string _cognitiveServiceRegion;

        private Config _configuration;

        private bool _enabled;

        private string _googleServiceKey;

        private bool _isDisposed;

        private string _manualTranslateFrom;

        private string _manualTranslateTo;

        private bool _showConfig;

        private TranslationProvider _translationProvider;

        public AutoTranslateXIV() {
            Task.Run(
                () => {
                    FFXIVClientStructs.Resolver.Initialize();
                    if (this._isDisposed) {
                        return;
                    }

                    try {
                        this._configuration = PluginInterface.GetPluginConfig() as Config ?? new Config();

                        this._enabled = this._configuration.Enabled;
                        this._translationProvider = this._configuration.TranslationProvider;
                        this._googleServiceKey = this._configuration.GoogleServiceKey;
                        this._cognitiveServiceKey = this._configuration.CognitiveServiceKey;
                        this._cognitiveServiceRegion = this._configuration.CognitiveServiceRegion;
                        this._automaticTranslateFrom = this._configuration.AutomaticTranslateFrom;
                        this._automaticTranslateTo = this._configuration.AutomaticTranslateTo;
                        this._manualTranslateFrom = this._configuration.ManualTranslateFrom;
                        this._manualTranslateTo = this._configuration.ManualTranslateTo;

                        PluginInterface.UiBuilder.Draw += this.Draw;
                        PluginInterface.UiBuilder.OpenConfigUi += this.OnOpenConfigUi;

                        Chat.ChatMessage += this.OnChatMessage;
                        Framework.Update += this.FrameworkUpdate;

                        while (!this._isDisposed) {
                            if (!this._chatEntries.TryDequeue(out ChatMessage result)) {
                                continue;
                            }

                            TranslationResult translationResult = this.GetTranslationResult(result.Message, Constants.LanguageMap[this._automaticTranslateFrom], Constants.LanguageMap[this._automaticTranslateTo], true);

                            if (string.Equals(translationResult.Translated, translationResult.Original, StringComparison.OrdinalIgnoreCase)) {
                                continue;
                            }

                            Chat.PrintChat(
                                new XivChatEntry {
                                    Message = $"[{this._translateTypes[result.Type]}]  {result.Sender}: {translationResult.Translated}",
                                    Type = XivChatType.Debug,
                                });
                        }
                    }
                    catch (Exception ex) {
                        PluginLog.LogError(ex.ToString());
                    }
                });
        }

        [PluginService]
        public static ChatGui Chat { get; private set; } = null!;

        [PluginService]
        public static ClientState ClientState { get; private set; } = null!;

        [PluginService]
        public static CommandManager CommandManager { get; private set; } = null!;

        [PluginService]
        public static Framework Framework { get; private set; } = null!;

        [PluginService]
        public static DalamudPluginInterface PluginInterface { get; private set; } = null;

        public string Name => "Auto Translate";

        public void Dispose() {
            this._isDisposed = true;

            PluginInterface.UiBuilder.Draw -= this.Draw;
            PluginInterface.UiBuilder.OpenConfigUi -= this.OnOpenConfigUi;

            Chat.ChatMessage -= this.OnChatMessage;
            Framework.Update -= this.FrameworkUpdate;
        }

        public void FrameworkUpdate(Framework framework) {
            if (ClientState == null) {
                return;
            }

            if (!ClientState.IsLoggedIn) { }
        }

        public TranslationResult GetTranslationResult(string message, string fromLanguage, string toLanguage, bool detectFromLanguage) {
            try {
                TranslationResult result = this.GetTranslationProvider()?.TranslateText(message, fromLanguage, toLanguage, detectFromLanguage);

                if (result is null) {
                    return null;
                }

                if (result.Translated.Length <= 0 || string.Equals(message, result.Translated, StringComparison.OrdinalIgnoreCase)) {
                    return new TranslationResult {
                        Original = message,
                    };
                }

                return result;
            }
            catch (Exception ex) {
                PluginLog.LogError(ex.ToString());
            }

            return null;
        }

        private void Draw() {
            if (!this._showConfig) {
                return;
            }

            ImGui.Text("Main Settings");
            ImGui.Separator();

            ImGui.SetNextWindowSize(new Num.Vector2(600, 500), ImGuiCond.FirstUseEver);

            if (!ImGui.Begin("Auto Translate Config", ref this._showConfig)) {
                ImGui.End();
                return;
            }

            if (ImGui.Button("Save and Close Config")) {
                this.SaveConfig();
                this._showConfig = false;
            }

            ImGui.Checkbox("Enabled", ref this._enabled);

            ImGui.Text("Services Settings");
            ImGui.Separator();

            if (ImGui.BeginCombo("Service Provider", this._translationProvider.ToString())) {
                List<TranslationProvider> providers = Enum.GetValues(typeof(TranslationProvider)).Cast<TranslationProvider>().ToList();
                foreach (TranslationProvider chatType in providers.Where(provider => ImGui.Selectable(provider.ToString(), provider == this._translationProvider))) {
                    this._translationProvider = chatType;
                }

                ImGui.EndCombo();
            }

            ImGui.InputText("Google Service Key", ref this._googleServiceKey, 256);
            ImGui.InputText("Cognitive Service Key", ref this._cognitiveServiceKey, 256);
            ImGui.InputText("Cognitive Service Region", ref this._cognitiveServiceRegion, 256);

            ImGui.Text("Automatic Translation Settings");
            ImGui.Separator();

            if (ImGui.BeginCombo("Automatic From", this._automaticTranslateFrom)) {
                foreach (var language in Constants.LanguageMap.Keys.Where(language => ImGui.Selectable(language, language == this._automaticTranslateFrom))) {
                    this._automaticTranslateFrom = language;
                }

                ImGui.EndCombo();
            }

            if (ImGui.BeginCombo("Automatic To", this._automaticTranslateTo)) {
                foreach (var language in Constants.LanguageMap.Keys.Where(language => ImGui.Selectable(language, language == this._automaticTranslateTo))) {
                    this._automaticTranslateTo = language;
                }

                ImGui.EndCombo();
            }

            ImGui.Text("Manual Translation Settings");
            ImGui.Separator();

            if (ImGui.BeginCombo("Manual From", this._manualTranslateFrom)) {
                foreach (var language in Constants.LanguageMap.Keys.Where(language => ImGui.Selectable(language, language == this._manualTranslateFrom))) {
                    this._manualTranslateFrom = language;
                }

                ImGui.EndCombo();
            }

            if (ImGui.BeginCombo("Manual To", this._manualTranslateTo)) {
                foreach (var language in Constants.LanguageMap.Keys.Where(language => ImGui.Selectable(language, language == this._manualTranslateTo))) {
                    this._manualTranslateTo = language;
                }

                ImGui.EndCombo();
            }

            ImGui.End();
        }

        private ITranslationProvider? GetTranslationProvider() {
            switch (this._translationProvider) {
                case TranslationProvider.Google:
                    if (string.IsNullOrWhiteSpace(this._googleServiceKey)) {
                        return null;
                    }

                    return _googleTranslateProvider ??= new GoogleTranslateProvider(this._googleServiceKey);
                case TranslationProvider.Cognitive:
                    if (string.IsNullOrWhiteSpace(this._cognitiveServiceKey)) {
                        return null;
                    }

                    if (string.IsNullOrWhiteSpace(this._cognitiveServiceRegion)) {
                        return null;
                    }

                    return _cognitiveTranslateProvider ??= new CognitiveTranslateProvider(this._cognitiveServiceKey, this._cognitiveServiceRegion);
            }

            return null;
        }

        private bool IsInternational(string line)
        {
            //// 0x0020 -> 0x007E === everything Latin based
            //// 0x00A0 -> 0x00BF === supplement punctuation and symbols
            //var excludesLatin = line.Any(c => c > 0x007E && c < 0x00A0 && c > 0x00BF);
            //return (this._manualTranslateFrom == "en" || this._automaticTranslateFrom == "en") && !excludesLatin ? true : excludesLatin;
            // 0x3040 -> 0x309F === Hirigana
            // 0x30A0 -> 0x30FF === Katakana
            // 0x4E00 -> 0x9FBF === Kanji
            return line.Any(c => c >= 0x3040 && c <= 0x309F) || line.Any(c => c >= 0x30A0 && c <= 0x30FF) || line.Any(c => c >= 0x4E00 && c <= 0x9FBF);
        }

        private void OnChatMessage(XivChatType type, uint senderid, ref SeString sender, ref SeString message, ref bool ishandled) {
            var shouldTranslate = this._translateTypes.ContainsKey(type) && this.IsInternational(message.TextValue);

            if (!shouldTranslate) {
                return;
            }

            this._chatEntries.Enqueue(
                new ChatMessage {
                    Type = type,
                    Sender = sender.TextValue,
                    Message = message.TextValue,
                });
        }

        private void OnOpenConfigUi() {
            this._showConfig = true;
        }

        private void SaveConfig() {
            this._configuration.Enabled = this._enabled;
            this._configuration.TranslationProvider = this._translationProvider;
            this._configuration.GoogleServiceKey = this._googleServiceKey;
            this._configuration.CognitiveServiceKey = this._cognitiveServiceKey;
            this._configuration.CognitiveServiceRegion = this._cognitiveServiceRegion;
            this._configuration.AutomaticTranslateFrom = this._automaticTranslateFrom;
            this._configuration.AutomaticTranslateTo = this._automaticTranslateTo;
            this._configuration.ManualTranslateFrom = this._manualTranslateFrom;
            this._configuration.ManualTranslateTo = this._manualTranslateTo;

            PluginInterface.SavePluginConfig(this._configuration);
        }
    }
}