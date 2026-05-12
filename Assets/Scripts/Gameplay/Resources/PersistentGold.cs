using UnityEngine;

namespace IdleTafang.Gameplay.Resources
{
    /// <summary>
    /// 局外金币持久化（最小实现：PlayerPrefs）。后续可替换为存档系统。
    /// </summary>
    public static class PersistentGold
    {
        private const string Key = "IdleTafang.MetaGold";

        public static int Load()
        {
            return Mathf.Max(0, PlayerPrefs.GetInt(Key, 0));
        }

        public static void Save(int totalGold)
        {
            PlayerPrefs.SetInt(Key, Mathf.Max(0, totalGold));
            PlayerPrefs.Save();
        }
    }
}
