namespace DrownedDream
{
    /// <summary>可被魚叉命中的對象（以「被攻擊次數」計算）。</summary>
    public interface IDamageable
    {
        /// <summary>是否還活著（死亡後魚叉不再命中）。</summary>
        bool IsAlive { get; }
        /// <summary>被命中一次，回傳是否有效命中。</summary>
        bool TakeHit();
    }
}
