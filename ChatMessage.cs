// --------------------------------------------------------------------------------------------------------------------
// <copyright file="ChatMessage.cs" company="SyndicatedLife">
//   Copyright© 2007 - 2021 Ryan Wilson &amp;lt;syndicated.life@gmail.com&amp;gt; (https://syndicated.life/)
//   Licensed under the MIT license. See LICENSE.md in the solution root for full license information.
// </copyright>
// <summary>
//   ChatMessage.cs Implementation
// </summary>
// --------------------------------------------------------------------------------------------------------------------

namespace AutoTranslateXIV {
    using Dalamud.Game.Text;
    using Dalamud.Game.Text.SeStringHandling;

    public class ChatMessage {
        public XivChatType Type { get; set; }
        public string Message { get; set; }
        public string Sender { get; set; }
    }
}