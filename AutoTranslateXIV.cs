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

        private static CognitiveTranslateProvider _cognitiveTranslateProvider;

        private static GoogleTranslateProvider _googleTranslateProvider;

        private readonly List<XivChatType> _translateTypes = new() {
            XivChatType.Say,
            XivChatType.Shout,
            XivChatType.TellIncoming,
            XivChatType.Party,
            XivChatType.Alliance,
            XivChatType.Ls1,
            XivChatType.Ls2,
            XivChatType.Ls3,
            XivChatType.Ls4,
            XivChatType.Ls5,
            XivChatType.Ls6,
            XivChatType.Ls7,
            XivChatType.Ls8,
            XivChatType.FreeCompany,
            XivChatType.NoviceNetwork,
            XivChatType.CustomEmote,
            XivChatType.StandardEmote,
            XivChatType.Yell,
            XivChatType.CrossParty,
            XivChatType.PvPTeam,
            XivChatType.CrossLinkShell1,
            XivChatType.CrossLinkShell2,
            XivChatType.CrossLinkShell3,
            XivChatType.CrossLinkShell4,
            XivChatType.CrossLinkShell5,
            XivChatType.CrossLinkShell6,
            XivChatType.CrossLinkShell7,
            XivChatType.CrossLinkShell8,
        };

        private string _cognitiveServiceKey;

        private string _cognitiveServiceRegion;

        private Config _configuration;

        private bool _enabled;

        private string _googleServiceKey;

        private bool _showConfig;

        private TranslationProvider _translationProvider;

        private bool isDisposed;

        public AutoTranslateXIV() {
            Task.Run(
                () => {
                    FFXIVClientStructs.Resolver.Initialize();
                    if (this.isDisposed) {
                        return;
                    }

                    try {
                        this._configuration = PluginInterface.GetPluginConfig() as Config ?? new Config();

                        this._enabled = this._configuration.Enabled;
                        this._translationProvider = this._configuration.TranslationProvider;
                        this._googleServiceKey = this._configuration.GoogleServiceKey;
                        this._cognitiveServiceKey = this._configuration.CognitiveServiceKey;
                        this._cognitiveServiceRegion = this._configuration.CognitiveServiceRegion;

                        CommandManager.AddHandler(
                            "/xt", new CommandInfo(this.OnRunTranslateCommand) {
                                HelpMessage = "Translate From A to B. - /xt en ja Hello!",
                                ShowInHelp = true,
                            });

                        PluginInterface.UiBuilder.Draw += this.Draw;
                        PluginInterface.UiBuilder.OpenConfigUi += this.OnOpenConfigUi;

                        Chat.ChatMessage += this.OnChatMessage;
                        Framework.Update += this.FrameworkUpdate;
                    }
                    catch (Exception ex) {
                        PluginLog.LogError(ex.ToString());
                    }
                });
        }

        public string Name => "Auto Translate";

        public void Dispose() {
            this.isDisposed = true;

            CommandManager.RemoveHandler("/xt");

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

        public TranslationResult GetAutomaticResult(string message, bool isInternational = false) {
            try {
                TranslationResult result = this.TranslateText(message, isInternational);

                if (result is null) {
                    return null;
                }

                if (result.Translated.Length <= 0 || string.Equals(message, result.Translated, StringComparison.InvariantCultureIgnoreCase)) {
                    return new TranslationResult {
                        Original = message,
                    };
                }

                return result;
            }
            catch (Exception ex) { }

            return null;
        }

        public void OnRunTranslateCommand(string command, string args) {
            try { }
            catch (Exception ex) {
                PluginLog.LogError(ex.ToString());
            }
        }

        private void Draw() {
            if (!this._showConfig) {
                return;
            }

            ImGui.SetNextWindowSize(new Num.Vector2(600, 500), ImGuiCond.FirstUseEver);
            if (!ImGui.Begin("Auto Translate Config", ref this._showConfig)) {
                ImGui.End();
                return;
            }

            ImGui.Checkbox("Enabled", ref this._enabled);

            if (ImGui.BeginCombo("Service Provider", this._translationProvider.ToString())) {
                List<TranslationProvider> providers = Enum.GetValues(typeof(TranslationProvider)).Cast<TranslationProvider>().ToList();
                foreach (TranslationProvider chatType in providers.Where(provider => ImGui.Selectable(provider.ToString(), provider == this._translationProvider))) {
                    this._translationProvider = chatType;
                }

                ImGui.EndCombo();
            }

            if (ImGui.InputText("Google Service Key", ref this._googleServiceKey, 256)) {
                this.SaveConfig();
            }

            if (ImGui.InputText("Cognitive Service Key", ref this._cognitiveServiceKey, 256)) {
                this.SaveConfig();
            }

            if (ImGui.InputText("Cognitive Service Region", ref this._cognitiveServiceRegion, 256)) {
                this.SaveConfig();
            }

            if (ImGui.Button("Save and Close Config")) {
                this.SaveConfig();
                this._showConfig = false;
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

        private bool IsInternational(string line) {
            // 0x3040 -> 0x309F === Hirigana
            // 0x30A0 -> 0x30FF === Katakana
            // 0x4E00 -> 0x9FBF === Kanji
            return line.Any(c => c >= 0x3040 && c <= 0x309F) || line.Any(c => c >= 0x30A0 && c <= 0x30FF) || line.Any(c => c >= 0x4E00 && c <= 0x9FBF);
        }

        private void OnChatMessage(XivChatType type, uint senderid, ref SeString sender, ref SeString message, ref bool ishandled) {
            var senderName = sender.TextValue;
            var messageText = message.TextValue;

            var shouldTranslate = this._translateTypes.Contains(type) && this.IsInternational(message.TextValue);

            if (!shouldTranslate) {
                return;
            }

            Task.Run(
                () => {
                    TranslationResult translationResult = this.GetAutomaticResult(messageText);
                    if (string.Equals(translationResult.Translated, translationResult.Original, StringComparison.OrdinalIgnoreCase)) {
                        return;
                    }

                    Chat.PrintChat(
                        new XivChatEntry {
                            Message = $"  {senderName}: {translationResult.Translated}",
                            Type = XivChatType.Debug,
                        });
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

            PluginInterface.SavePluginConfig(this._configuration);
        }

        private TranslationResult TranslateText(string message, bool isInternational = false) {
            TranslationResult result = null;

            var fromLanguage = "ja";
            var toLanguage = "en";

            result = this.GetTranslationProvider()?.TranslateText(message, fromLanguage, toLanguage, this.IsInternational(message));

            return result;
        }
    }
}