using UnityEngine;
using UnityEngine.InputSystem;

namespace DrownedDream
{
    /// <summary>
    /// 讀取玩家輸入。按鍵可在 Inspector 調整；未設定時自動套用預設綁定。
    /// 非 Playing 狀態或被鎖定時輸入一律回傳 0 / false。
    /// </summary>
    public class PlayerInputReader : MonoBehaviour
    {
        /// <summary>左右移動（A/D、←/→）。</summary>
        [SerializeField] private InputAction _move = new InputAction("Move", InputActionType.Value, expectedControlType: "Axis");
        /// <summary>跳躍（W、↑）。</summary>
        [SerializeField] private InputAction _jump = new InputAction("Jump", InputActionType.Button);
        /// <summary>發射魚叉（Space）。</summary>
        [SerializeField] private InputAction _fire = new InputAction("Fire", InputActionType.Button);
        /// <summary>憋氣 / 提早結束（K、Left Shift）。</summary>
        [SerializeField] private InputAction _breath = new InputAction("Breath", InputActionType.Button);
        /// <summary>互動（E）。</summary>
        [SerializeField] private InputAction _interact = new InputAction("Interact", InputActionType.Button);
        /// <summary>往下（S、↓）：站在單向平台上時穿過平台往下掉。</summary>
        [SerializeField] private InputAction _down = new InputAction("Down", InputActionType.Button);
        /// <summary>開關背包（Tab、I）。</summary>
        [SerializeField] private InputAction _inventory = new InputAction("Inventory", InputActionType.Button);

        /// <summary>是否被鎖定（死亡演出等）。</summary>
        private bool _locked;

        /// <summary>目前是否接受遊玩輸入。</summary>
        private bool Active => GameFlow.IsPlaying && !_locked;

        /// <summary>原始水平輸入（-1 ~ 1），未套用方向錯亂。</summary>
        public float MoveX => Active ? _move.ReadValue<float>() : 0f;
        /// <summary>本幀按下跳躍。</summary>
        public bool JumpPressed => Active && _jump.WasPressedThisFrame();
        /// <summary>跳躍鍵按住中（落地後自動連跳用）。</summary>
        public bool JumpHeld => Active && _jump.IsPressed();
        /// <summary>本幀按下發射。</summary>
        public bool FirePressed => Active && _fire.WasPressedThisFrame();
        /// <summary>本幀按下憋氣。</summary>
        public bool BreathPressed => Active && _breath.WasPressedThisFrame();
        /// <summary>往下鍵按住中（穿過單向平台用）。</summary>
        public bool DownHeld => Active && _down.IsPressed();
        /// <summary>本幀按下互動。</summary>
        public bool InteractPressed => Active && _interact.WasPressedThisFrame();
        /// <summary>本幀按下背包鍵（暫停中也要能關閉，因此不受 Playing 限制）。</summary>
        public bool InventoryPressed => !_locked && _inventory.WasPressedThisFrame();

        /// <summary>鎖定 / 解鎖輸入。</summary>
        public void SetLocked(bool locked) => _locked = locked;

        /// <summary>編輯器加入元件時套用預設綁定。</summary>
        private void Reset() => EnsureDefaultBindings();

        /// <summary>執行時確保有綁定。</summary>
        private void Awake() => EnsureDefaultBindings();

        /// <summary>啟用所有動作。</summary>
        private void OnEnable()
        {
            foreach (var action in AllActions()) action.Enable();
        }

        /// <summary>停用所有動作。</summary>
        private void OnDisable()
        {
            foreach (var action in AllActions()) action.Disable();
        }

        /// <summary>所有輸入動作清單。</summary>
        private InputAction[] AllActions() =>
            new[] { _move, _jump, _fire, _breath, _interact, _inventory, _down };

        /// <summary>沒有綁定的動作套用預設按鍵。</summary>
        private void EnsureDefaultBindings()
        {
            if (_move.bindings.Count == 0)
            {
                _move.AddCompositeBinding("1DAxis").With("Negative", "<Keyboard>/a").With("Positive", "<Keyboard>/d");
                _move.AddCompositeBinding("1DAxis").With("Negative", "<Keyboard>/leftArrow").With("Positive", "<Keyboard>/rightArrow");
                _move.AddBinding("<Gamepad>/leftStick/x");
                _move.AddCompositeBinding("1DAxis").With("Negative", "<Gamepad>/dpad/left").With("Positive", "<Gamepad>/dpad/right");
            }
            AddDefault(_jump, "<Keyboard>/w", "<Keyboard>/upArrow", "<Gamepad>/buttonSouth");
            AddDefault(_fire, "<Keyboard>/space", "<Gamepad>/buttonWest");
            AddDefault(_breath, "<Keyboard>/k", "<Keyboard>/leftShift", "<Gamepad>/rightShoulder");
            AddDefault(_interact, "<Keyboard>/e", "<Gamepad>/buttonNorth");
            AddDefault(_down, "<Keyboard>/s", "<Keyboard>/downArrow", "<Gamepad>/dpad/down");
            AddDefault(_inventory, "<Keyboard>/tab", "<Keyboard>/i", "<Gamepad>/select");
        }

        /// <summary>動作沒有綁定時加入指定路徑。</summary>
        private static void AddDefault(InputAction action, params string[] paths)
        {
            if (action.bindings.Count > 0) return;
            foreach (var path in paths) action.AddBinding(path);
        }
    }
}
