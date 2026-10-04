using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;

namespace DrownedDream
{
    /// <summary>遊戲流程狀態。</summary>
    public enum GameState
    {
        /// <summary>封面（等待按 Start）。</summary>
        Title,
        /// <summary>開場演出中。</summary>
        Intro,
        /// <summary>遊玩中。</summary>
        Playing,
        /// <summary>暫停（背包開啟等）。</summary>
        Paused,
        /// <summary>結局演出中。</summary>
        Ending,
    }

    /// <summary>遊戲流程：封面 → 開場 → 遊玩 → 結局 → 回到封面（F-STORY）。</summary>
    public class GameFlow : MonoBehaviour
    {
        /// <summary>場景中唯一的流程控制器。</summary>
        public static GameFlow Instance { get; private set; }
        /// <summary>目前流程狀態（沒有 GameFlow 時視為遊玩中，方便單獨測試）。</summary>
        public static GameState State => Instance != null ? Instance._state : GameState.Playing;
        /// <summary>是否在遊玩中（輸入、計時只在此狀態運作）。</summary>
        public static bool IsPlaying => State == GameState.Playing && !QuitConfirmDialog.IsOpen;

        /// <summary>封面（Start / Quit）。</summary>
        [SerializeField] private TitleScreen _titleScreen;
        /// <summary>開場 / 結局文字面板。</summary>
        [SerializeField] private StoryPanel _storyPanel;
        /// <summary>測試用：跳過封面與開場。</summary>
        [SerializeField] private bool _skipIntro;
        /// <summary>任何時候按 ESC 開啟「是否離開遊戲」確認視窗（再按一次 ESC 關閉）。</summary>
        [SerializeField] private bool _escToQuit = true;

        [Header("[待確認] 開場 / 結局文字")]
        /// <summary>開場逐句文字。</summary>
        [TextArea(2, 4)]
        [SerializeField]
        private string[] _introLines =
        {
            "又是那個夢……",
            "漆黑的海底洞窟深處，沉睡著古老邪神的寶藏。",
            "祂在呼喚我。",
            "（A/D 移動　W 跳躍　Space 發射魚叉　Q 憋氣）",
        };

        /// <summary>結局：每頁一張插圖 + 逐句文字（插圖依序為 Assets/Art/Ending/ending_0~4.png，由更新工具綁定；最後一頁黑底）。</summary>
        [SerializeField]
        private StoryPage[] _endingPages =
        {
            new StoryPage("封印完成的瞬間，海水灌進了我的肺——", "四周只剩下黑暗……和遠方某個呼喚我的聲音。"),
            new StoryPage("冰冷的海水，慢慢變成了柔軟的枕頭。", "那個聲音還在耳邊，輕輕地……"),
            new StoryPage("——！", "我猛然睜開了眼睛。"),
            new StoryPage("熟悉的房間，熟悉的床。", "……原來，全都是夢。", "洞窟、邪神、那些雕像……大概只是太累了吧。"),
            new StoryPage("直到我看見——床底下，有什麼正望著我。", "「謝謝你……帶我回家。」"),
            new StoryPage("聽海窟得聲音\n\n— 感謝遊玩 —"),
        };

        /// <summary>目前狀態。</summary>
        private GameState _state = GameState.Title;
        /// <summary>離開確認視窗（執行時自動建立）。</summary>
        private QuitConfirmDialog _quitDialog;

        /// <summary>註冊單例。</summary>
        private void Awake()
        {
            Instance = this;
            _quitDialog = GetComponent<QuitConfirmDialog>();
            if (_quitDialog == null) _quitDialog = gameObject.AddComponent<QuitConfirmDialog>();
        }

        /// <summary>訂閱封印事件。</summary>
        private void OnEnable()
        {
            GameEvents.BossSealed += OnBossSealed;
        }

        /// <summary>取消訂閱封印事件。</summary>
        private void OnDisable()
        {
            GameEvents.BossSealed -= OnBossSealed;
        }

        /// <summary>顯示封面（沒有封面時直接播開場）。</summary>
        private void Start()
        {
            if (_skipIntro || _titleScreen == null)
            {
                PlayIntro();
                return;
            }

            SetState(GameState.Title);
            _titleScreen.Show(PlayIntro);
        }

        /// <summary>播放開場文字，播完進入遊玩（跳過開場或沒有面板時直接遊玩）。</summary>
        private void PlayIntro()
        {
            if (_skipIntro || _storyPanel == null)
            {
                SetState(GameState.Playing);
                return;
            }

            SetState(GameState.Intro);
            _storyPanel.Play(_introLines, () => SetState(GameState.Playing));
        }

        /// <summary>偵測 ESC 開關離開確認視窗（不受 timeScale 影響）。</summary>
        private void Update()
        {
            if (_escToQuit && Keyboard.current != null && Keyboard.current.escapeKey.wasPressedThisFrame)
                _quitDialog.Toggle();
        }

        /// <summary>清除單例並恢復時間流速。</summary>
        private void OnDestroy()
        {
            if (Instance == this)
            {
                Instance = null;
                Time.timeScale = 1f;
            }
        }

        /// <summary>切換暫停（封面 / 開場 / 結局中不可切換）。</summary>
        public void SetPaused(bool paused)
        {
            if (_state != GameState.Playing && _state != GameState.Paused) return;
            SetState(paused ? GameState.Paused : GameState.Playing);
        }

        /// <summary>封印成功 → 播放結局。</summary>
        private void OnBossSealed()
        {
            SetState(GameState.Ending);
            if (_storyPanel == null)
            {
                Restart();
                return;
            }
            _storyPanel.Play(_endingPages, Restart, true);
        }

        /// <summary>結束遊戲（Editor 中停止 Play）。封面與結局的 Quit 按鈕共用。</summary>
        public static void QuitGame()
        {
#if UNITY_EDITOR
            UnityEditor.EditorApplication.isPlaying = false;
#else
            Application.Quit();
#endif
        }

        /// <summary>重新載入目前場景（回到封面）。</summary>
        private void Restart()
        {
            Time.timeScale = 1f;
            SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
        }

        /// <summary>切換狀態，非遊玩狀態時停止時間。</summary>
        private void SetState(GameState state)
        {
            _state = state;
            Time.timeScale = state == GameState.Playing ? 1f : 0f;
            GameEvents.RaiseGameStateChanged(state);
        }
    }
}
