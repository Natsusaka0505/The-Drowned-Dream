using System;
using System.Collections.Generic;
using UnityEngine;

namespace DrownedDream
{
    /// <summary>Action：投擲魚叉攻擊（Space，朝面向左/右）、撿魚叉（F-WPN）。發射會結束憋氣（F-BRE-02c）。</summary>
    public class PlayerAttack : MonoBehaviour
    {
        /// <summary>魚叉 Prefab。</summary>
        [SerializeField] private Harpoon _harpoonPrefab;
        /// <summary>發射點相對玩家中心的偏移（x 會依面向翻轉）。</summary>
        [SerializeField] private Vector2 _muzzleOffset = new Vector2(0.6f, 0.2f);

        /// <summary>場上尚未撿回的魚叉。</summary>
        private readonly List<Harpoon> _active = new List<Harpoon>();
        /// <summary>玩家數值（魚叉數）。</summary>
        private PlayerStatus _status;
        /// <summary>精神錯亂（提供錯亂後的發射輸入：可能延遲或沒射出）。</summary>
        private PlayerConfusion _confusion;
        /// <summary>移動（取得面向）。</summary>
        private PlayerMove _move;
        /// <summary>憋氣（發射時結束）。</summary>
        private PlayerBreath _breath;
        /// <summary>發射間隔剩餘秒數。</summary>
        private float _fireTimer;

        /// <summary>射出一支魚叉（音效用）。</summary>
        public event Action Thrown;

        /// <summary>快取元件。</summary>
        private void Awake()
        {
            _status = GetComponent<PlayerStatus>();
            _confusion = GetComponent<PlayerConfusion>();
            _move = GetComponent<PlayerMove>();
            _breath = GetComponent<PlayerBreath>();
        }

        /// <summary>處理發射輸入。</summary>
        private void Update()
        {
            _fireTimer -= Time.deltaTime;
            if (_confusion.FirePressed) Throw();
        }

        /// <summary>投擲魚叉攻擊：有魚叉且不在冷卻時發射一支。</summary>
        public void Throw()
        {
            if (_status.HarpoonCount <= 0)
            {
                GameEvents.ShowMessage("沒有魚叉了——去把它們撿回來", 1.5f);
                return;
            }
            if (_fireTimer > 0f) return;

            _fireTimer = _status.Harpoon.FireInterval;
            _breath.ForceEnd();

            int dir = _move.Facing;
            var origin = (Vector2)transform.position + new Vector2(_muzzleOffset.x * dir, _muzzleOffset.y);
            var harpoon = Instantiate(_harpoonPrefab, origin, Quaternion.identity);
            harpoon.Launch(this, _status.Harpoon, dir);
            _active.Add(harpoon);
            Thrown?.Invoke();
            _status.AddHarpoons(-1);
        }

        /// <summary>撿魚叉（魚叉被碰到或自動歸還時呼叫）。</summary>
        public void PickUp(Harpoon harpoon)
        {
            if (!_active.Remove(harpoon)) return;
            _status.AddHarpoons(1);
            Destroy(harpoon.gameObject);
        }

        /// <summary>清除場上所有魚叉（復活用；數量由 Status.ApplyRespawn 處理）。</summary>
        public void ClearActive()
        {
            foreach (var h in _active)
            {
                if (h != null) Destroy(h.gameObject);
            }
            _active.Clear();
        }
    }
}
