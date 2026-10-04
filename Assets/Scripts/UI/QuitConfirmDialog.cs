using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace DrownedDream
{
    /// <summary>「是否離開遊戲」確認視窗。自帶最上層 Canvas，開啟時停止時間並擋下遊玩輸入。</summary>
    public class QuitConfirmDialog : MonoBehaviour
    {
        /// <summary>確認視窗是否開啟中。</summary>
        public static bool IsOpen { get; private set; }
        /// <summary>是否應擋下其他輸入（開啟中，或剛關閉的那一兩幀，避免確認鍵穿透到劇情面板）。</summary>
        public static bool BlocksInput => IsOpen || Time.frameCount - s_closedFrame <= 1;

        /// <summary>詢問文字。</summary>
        [SerializeField] private string _message = "確定要離開遊戲嗎？";
        /// <summary>確認按鈕文字。</summary>
        [SerializeField] private string _confirmLabel = "離開";
        /// <summary>取消按鈕文字。</summary>
        [SerializeField] private string _cancelLabel = "取消";
        /// <summary>Canvas 排序（需高於主 Canvas）。</summary>
        [SerializeField] private int _sortingOrder = 100;

        /// <summary>最後一次關閉的幀數。</summary>
        private static int s_closedFrame = -10;

        /// <summary>視窗根物件。</summary>
        private GameObject _root;
        /// <summary>取消按鈕（開啟時預設選取，防止誤按確認）。</summary>
        private Button _cancelButton;
        /// <summary>開啟前選取的 UI（關閉時還原，例如封面的 Start）。</summary>
        private GameObject _prevSelected;

        /// <summary>建立視窗（預設隱藏）。</summary>
        private void Awake()
        {
            var canvasGo = new GameObject("QuitConfirmCanvas");
            canvasGo.transform.SetParent(transform, false);
            var canvas = canvasGo.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = _sortingOrder;
            var scaler = canvasGo.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920f, 1080f);
            scaler.matchWidthOrHeight = 0.5f;
            canvasGo.AddComponent<GraphicRaycaster>();
            _root = canvasGo;

            // 全螢幕半透明遮罩，擋住下層 UI 的點擊
            var dim = UIFactory.Stretch("Dim", canvasGo.transform);
            UIFactory.Image(dim, new Color(0f, 0f, 0f, 0.6f)).raycastTarget = true;

            var center = new Vector2(0.5f, 0.5f);
            var box = UIFactory.Rect("Box", dim, center, center, center, Vector2.zero, new Vector2(640f, 300f));
            UIFactory.Image(box, new Color(0.04f, 0.08f, 0.14f, 0.95f));

            var msg = UIFactory.Rect("Message", box, new Vector2(0f, 1f), new Vector2(1f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, -40f), new Vector2(-40f, 100f));
            UIFactory.Text(msg, _message, 40, TextAnchor.MiddleCenter, Color.white);

            MakeButton("Confirm", box, _confirmLabel, -130f, GameFlow.QuitGame);
            _cancelButton = MakeButton("Cancel", box, _cancelLabel, 130f, Close);

            _root.SetActive(false);
        }

        /// <summary>銷毀時清除開啟狀態（換場景時避免殘留）。</summary>
        private void OnDestroy()
        {
            if (_root != null && _root.activeSelf) IsOpen = false;
        }

        /// <summary>開啟中就關閉，否則開啟。</summary>
        public void Toggle()
        {
            if (IsOpen) Close();
            else Open();
        }

        /// <summary>開啟視窗並停止時間，預設選取「取消」。</summary>
        public void Open()
        {
            if (IsOpen) return;
            IsOpen = true;
            _root.SetActive(true);
            Time.timeScale = 0f;
            var es = EventSystem.current;
            if (es == null) return;
            _prevSelected = es.currentSelectedGameObject;
            es.SetSelectedGameObject(_cancelButton.gameObject);
        }

        /// <summary>關閉視窗，依目前流程狀態還原時間並還原 UI 選取。</summary>
        public void Close()
        {
            if (!IsOpen) return;
            IsOpen = false;
            s_closedFrame = Time.frameCount;
            _root.SetActive(false);
            Time.timeScale = GameFlow.State == GameState.Playing ? 1f : 0f; // 與 GameFlow.SetState 規則一致
            var es = EventSystem.current;
            if (es != null) es.SetSelectedGameObject(_prevSelected);
            _prevSelected = null;
        }

        /// <summary>建立文字按鈕（x = 相對視窗中心的水平位移）。</summary>
        private Button MakeButton(string name, RectTransform parent, string label, float x, UnityEngine.Events.UnityAction onClick)
        {
            var anchor = new Vector2(0.5f, 0f);
            var rt = UIFactory.Rect(name, parent, anchor, anchor, new Vector2(0.5f, 0f), new Vector2(x, 40f), new Vector2(200f, 72f));
            var image = rt.gameObject.AddComponent<Image>();
            image.color = Color.white; // 顏色由 ColorBlock 決定

            var button = rt.gameObject.AddComponent<Button>();
            button.targetGraphic = image;
            var colors = button.colors;
            colors.normalColor = new Color(0.15f, 0.28f, 0.4f);
            colors.highlightedColor = new Color(0.3f, 0.5f, 0.7f);
            colors.selectedColor = new Color(0.3f, 0.5f, 0.7f);
            colors.pressedColor = new Color(0.1f, 0.18f, 0.26f);
            button.colors = colors;
            button.onClick.AddListener(onClick);

            var textRt = UIFactory.Stretch("Label", rt);
            UIFactory.Text(textRt, label, 34, TextAnchor.MiddleCenter, Color.white);
            rt.gameObject.AddComponent<TitleButtonHover>().Init(1.08f);
            return button;
        }
    }
}
