using UnityEngine;
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
        public static bool IsPlaying => State == GameState.Playing;

        /// <summary>封面（Start / Quit）。</summary>
        [SerializeField] private TitleScreen _titleScreen;
        /// <summary>開場 / 結局文字面板。</summary>
        [SerializeField] private StoryPanel _storyPanel;
        /// <summary>測試用：跳過封面與開場。</summary>
        [SerializeField] private bool _skipIntro;

        [Header("[待確認] 開場 / 結局文字")]
        /// <summary>開場逐句文字。</summary>
        [TextArea(2, 4)]
        [SerializeField] private string[] _introLines =
        {
            "又是那個夢……",
            "漆黑的海底洞窟深處，沉睡著古老邪神的寶藏。",
            "祂在呼喚我。",
            "（A/D 移動　W 跳躍　Space 發射魚叉　Q 憋氣）",
        };

        /// <summary>結局逐句文字。</summary>
        [TextArea(2, 4)]
        [SerializeField] private string[] _endingLines =
        {
            "封印完成的瞬間，海水灌入肺中——",
            "……我猛然驚醒。",
            "原來，全都是夢。",
            "但那洞窟的位置，我記得一清二楚。",
            "收拾行囊吧。該出發了。",
            "The Drowned Dream\n\n— 感謝遊玩 —",
        };

        /// <summary>目前狀態。</summary>
        private GameState _state = GameState.Title;

        /// <summary>註冊單例。</summary>
        private void Awake()
        {
            Instance = this;
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
            _storyPanel.Play(_endingLines, Restart);
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
