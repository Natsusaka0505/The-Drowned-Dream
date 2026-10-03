using UnityEngine;

namespace DrownedDream
{
    /// <summary>
    /// 回復道具（docs/core）：撿到立即回復玩家 SAN / HP。
    /// [待確認] 依 script 分類推定為「拾取即生效」（玩家 status 只記魚叉數與封印道具數）。
    /// </summary>
    public class RecoveryItem : PickupItem
    {
        #region Status

        /// <summary>SAN 值回復數值。</summary>
        [SerializeField] private double _sanityRestore;
        /// <summary>HP 回復數值。</summary>
        [SerializeField] private double _hpRestore;

        #endregion

        /// <summary>SAN 值回復數值。</summary>
        public double SanityRestore => _sanityRestore;
        /// <summary>HP 回復數值。</summary>
        public double HpRestore => _hpRestore;

        #region Action

        /// <summary>玩家數值回復。</summary>
        public override bool Apply(Player player)
        {
            var status = player.Status;
            if (_sanityRestore > 0d) status.RestoreSanity(_sanityRestore);
            if (_hpRestore > 0d) status.RestoreHp(_hpRestore);
            GameEvents.RaiseRecoveryUsed();
            GameEvents.ShowMessage(BuildMessage(), 1.5f);
            return true;
        }

        #endregion

        /// <summary>組出拾取提示文字。</summary>
        private string BuildMessage()
        {
            string text = $"取得 {DisplayName}";
            if (_sanityRestore > 0d) text += $"　SAN +{_sanityRestore:0}";
            if (_hpRestore > 0d) text += $"　HP +{_hpRestore:0}";
            return text;
        }
    }
}
