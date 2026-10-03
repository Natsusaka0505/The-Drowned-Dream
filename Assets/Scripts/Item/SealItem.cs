namespace DrownedDream
{
    /// <summary>封印道具（關鍵道具）：撿到後玩家封印道具數 +1。</summary>
    public class SealItem : PickupItem
    {
        /// <summary>封印道具數 +1 並提示進度。</summary>
        public override bool Apply(Player player)
        {
            var status = player.Status;
            status.AddSeal();
            GameEvents.ShowMessage($"取得 {DisplayName}（{status.SealCount}/{status.RequiredSeals}）", 2f);
            return true;
        }
    }
}
