using UnityEngine;

namespace IdleTafang.Config
{
    [CreateAssetMenu(menuName = "IdleTafang/Game Config", fileName = "GameConfig")]
    public sealed class GameConfig : ScriptableObject
    {
        [SerializeField] private string gameName = "Idle Tafang";
        [SerializeField] private float targetFrameRate = 60f;

        public string GameName => gameName;
        public float TargetFrameRate => targetFrameRate;
    }
}
