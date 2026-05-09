namespace IdleTafang.Gameplay.Typing
{
    public sealed class TypingStats
    {
        public int TotalTyped { get; private set; }
        public int PromptTyped { get; private set; }
        public int CorrectTyped { get; private set; }
        public int Combo { get; private set; }
        public int BestCombo { get; private set; }

        public float Accuracy => PromptTyped == 0 ? 0f : (float)CorrectTyped / PromptTyped;

        public void RegisterCorrect()
        {
            TotalTyped += 1;
            PromptTyped += 1;
            CorrectTyped += 1;
            Combo += 1;

            if (Combo > BestCombo)
            {
                BestCombo = Combo;
            }
        }

        public void RegisterMiss()
        {
            TotalTyped += 1;
            PromptTyped += 1;
            Combo = 0;
        }

        public void ClearPromptProgress()
        {
            PromptTyped = 0;
            CorrectTyped = 0;
            Combo = 0;
        }

        public void Reset()
        {
            TotalTyped = 0;
            PromptTyped = 0;
            CorrectTyped = 0;
            Combo = 0;
            BestCombo = 0;
        }
    }
}
