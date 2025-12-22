using System;

namespace ProjectSingularity.Dialogues
{
    [Serializable]
    public class DialogueTurn
    {
        public string playerText;
        public string npcText;
        public DateTime utcTime;

        public DialogueTurn(string playerText, string npcText)
        {
            this.playerText = playerText;
            this.npcText = npcText;
            this.utcTime = DateTime.UtcNow;
        }
    }
}
