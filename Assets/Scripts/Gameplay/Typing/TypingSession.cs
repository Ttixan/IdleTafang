namespace IdleTafang.Gameplay.Typing
{
    public sealed class TypingSession
    {
        public TypingStats Stats { get; } = new TypingStats();

        public void SubmitCharacter(char typedChar)
        {
            if (char.IsWhiteSpace(typedChar))
            {
                return;
            }

            if (char.IsLetterOrDigit(typedChar) || char.IsPunctuation(typedChar) || char.IsSymbol(typedChar))
            {
                Stats.RegisterCorrect();
            }
        }

        public void Reset()
        {
            Stats.Reset();
        }
    }
}
