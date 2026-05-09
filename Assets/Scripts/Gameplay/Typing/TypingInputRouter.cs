using System;
using UnityEngine;

namespace IdleTafang.Gameplay.Typing
{
    public sealed class TypingInputRouter : MonoBehaviour
    {
        public event Action<char> CharacterSubmitted;

        private void Update()
        {
            string input = Input.inputString;
            if (string.IsNullOrEmpty(input))
            {
                return;
            }

            for (int index = 0; index < input.Length; index++)
            {
                char currentChar = input[index];
                if (!char.IsControl(currentChar))
                {
                    CharacterSubmitted?.Invoke(currentChar);
                }
            }
        }
    }
}
