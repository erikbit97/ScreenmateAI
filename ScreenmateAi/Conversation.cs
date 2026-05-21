using ChatGPTWPF;
using System;
using System.Collections.Generic;
using System.Text;

namespace ScreenmateAi
{
    public class Conversation
    {
        public string Title { get; set; } = "Neuer Chat";
        public bool IsRenaming { get; set; }
        public List<ChatMessage> Messages { get; set; } = new List<ChatMessage>();

        public override string ToString()
        {
            return Title;
        }
    }
}
