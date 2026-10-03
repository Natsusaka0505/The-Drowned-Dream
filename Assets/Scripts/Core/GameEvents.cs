using System;

namespace DrownedDream
{
    /// <summary>跨系統的全域事件。系統間以此解耦，不直接互相引用。</summary>
    public static class GameEvents
    {
        /// <summary>畫面提示文字（text, 顯示秒數）。</summary>
        public static event Action<string, float> MessageRequested;
        /// <summary>玩家死亡。</summary>
        public static event Action PlayerDied;
        /// <summary>玩家復活完成。</summary>
        public static event Action PlayerRespawned;
        /// <summary>Boss 封印成功 → 結局。</summary>
        public static event Action BossSealed;

        /// <summary>顯示畫面提示文字。</summary>
        public static void ShowMessage(string text, float duration = 2.5f) => MessageRequested?.Invoke(text, duration);
        /// <summary>發出玩家死亡事件。</summary>
        public static void RaisePlayerDied() => PlayerDied?.Invoke();
        /// <summary>發出玩家復活事件。</summary>
        public static void RaisePlayerRespawned() => PlayerRespawned?.Invoke();
        /// <summary>發出 Boss 封印成功事件。</summary>
        public static void RaiseBossSealed() => BossSealed?.Invoke();
    }
}
