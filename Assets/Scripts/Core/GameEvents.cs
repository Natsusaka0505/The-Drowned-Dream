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
        /// <summary>遊戲流程狀態切換（開場 / 遊玩 / 暫停 / 結局）。</summary>
        public static event Action<GameState> GameStateChanged;
        /// <summary>敵人被魚叉命中（參數：這一下是否擊殺）。</summary>
        public static event Action<bool> EnemyHit;
        /// <summary>使用了回復道具（藥丸 / 海草繃帶）。</summary>
        public static event Action RecoveryUsed;
        /// <summary>第一次進 Boss 房、鏡頭特寫 Boss。</summary>
        public static event Action BossRevealed;
        /// <summary>Boss 咆哮（攻擊）。</summary>
        public static event Action BossRoared;
        /// <summary>Boss 啟動（開始攻擊）。</summary>
        public static event Action BossActivated;
        /// <summary>全畫面閃白（參數：淡出秒數）。</summary>
        public static event Action<float> ScreenFlashRequested;

        /// <summary>顯示畫面提示文字。</summary>
        public static void ShowMessage(string text, float duration = 2.5f) => MessageRequested?.Invoke(text, duration);
        /// <summary>發出玩家死亡事件。</summary>
        public static void RaisePlayerDied() => PlayerDied?.Invoke();
        /// <summary>發出玩家復活事件。</summary>
        public static void RaisePlayerRespawned() => PlayerRespawned?.Invoke();
        /// <summary>發出 Boss 封印成功事件。</summary>
        public static void RaiseBossSealed() => BossSealed?.Invoke();
        /// <summary>發出流程狀態切換事件。</summary>
        public static void RaiseGameStateChanged(GameState state) => GameStateChanged?.Invoke(state);
        /// <summary>發出敵人被命中事件。</summary>
        public static void RaiseEnemyHit(bool killed) => EnemyHit?.Invoke(killed);
        /// <summary>發出使用回復道具事件。</summary>
        public static void RaiseRecoveryUsed() => RecoveryUsed?.Invoke();
        /// <summary>發出 Boss 現身（第一次特寫）事件。</summary>
        public static void RaiseBossRevealed() => BossRevealed?.Invoke();
        /// <summary>發出 Boss 咆哮事件。</summary>
        public static void RaiseBossRoared() => BossRoared?.Invoke();
        /// <summary>發出 Boss 啟動事件。</summary>
        public static void RaiseBossActivated() => BossActivated?.Invoke();
        /// <summary>要求全畫面閃白。</summary>
        public static void RaiseScreenFlash(float fadeSeconds) => ScreenFlashRequested?.Invoke(fadeSeconds);
    }
}
