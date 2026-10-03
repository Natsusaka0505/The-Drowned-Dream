using UnityEngine;
using UnityEngine.EventSystems;

namespace DrownedDream
{
    /// <summary>封面按鈕：滑鼠移上或被選取時放大（不受 timeScale 影響）。</summary>
    public class TitleButtonHover : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler, ISelectHandler, IDeselectHandler
    {
        /// <summary>放大倍率。</summary>
        private float _scale = 1.08f;
        /// <summary>滑鼠是否在按鈕上。</summary>
        private bool _hovered;
        /// <summary>是否被選取（鍵盤 / 手把導覽）。</summary>
        private bool _selected;

        /// <summary>設定放大倍率。</summary>
        public void Init(float scale) => _scale = scale;

        /// <summary>滑鼠移入。</summary>
        public void OnPointerEnter(PointerEventData eventData)
        {
            _hovered = true;
            if (EventSystem.current != null) EventSystem.current.SetSelectedGameObject(gameObject);
        }

        /// <summary>滑鼠移出。</summary>
        public void OnPointerExit(PointerEventData eventData) => _hovered = false;
        /// <summary>被選取。</summary>
        public void OnSelect(BaseEventData eventData) => _selected = true;
        /// <summary>取消選取。</summary>
        public void OnDeselect(BaseEventData eventData) => _selected = false;

        /// <summary>平滑縮放到目標大小。</summary>
        private void Update()
        {
            float target = _hovered || _selected ? _scale : 1f;
            transform.localScale = Vector3.one * Mathf.MoveTowards(transform.localScale.x, target, 1.5f * Time.unscaledDeltaTime);
        }
    }
}
