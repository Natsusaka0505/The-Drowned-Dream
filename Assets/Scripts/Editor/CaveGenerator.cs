using System;
using System.Collections.Generic;

namespace DrownedDream.EditorTools
{
    /// <summary>洞窟格座標（x 向右、y 向上，0 = 最下排）。</summary>
    internal struct CaveCell
    {
        /// <summary>X 格座標。</summary>
        public int X;
        /// <summary>Y 格座標。</summary>
        public int Y;

        /// <summary>建立格座標。</summary>
        public CaveCell(int x, int y) { X = x; Y = y; }

        /// <summary>與另一格的距離平方。</summary>
        public int DistSq(CaveCell o) => (X - o.X) * (X - o.X) + (Y - o.Y) * (Y - o.Y);
    }

    /// <summary>洞窟格矩形（左下角 + 寬高，單位：格）。</summary>
    internal struct CaveRect
    {
        /// <summary>左下角 X。</summary>
        public int X;
        /// <summary>左下角 Y。</summary>
        public int Y;
        /// <summary>寬（格）。</summary>
        public int W;
        /// <summary>高（格）。</summary>
        public int H;

        /// <summary>建立矩形。</summary>
        public CaveRect(int x, int y, int w, int h) { X = x; Y = y; W = w; H = h; }

        /// <summary>是否包含某格。</summary>
        public bool Contains(int x, int y) => x >= X && x < X + W && y >= Y && y < Y + H;

        /// <summary>往外擴張指定格數。</summary>
        public CaveRect Expand(int n) => new CaveRect(X - n, Y - n, W + n * 2, H + n * 2);
    }

    /// <summary>自動擺放的物件種類。</summary>
    internal enum CaveThing
    {
        /// <summary>存檔點。</summary>
        Checkpoint,
        /// <summary>封印碎片。</summary>
        Seal,
        /// <summary>鎮靜藥丸。</summary>
        Pill,
        /// <summary>海草繃帶。</summary>
        Medkit,
        /// <summary>巡游魚怪。</summary>
        Fish,
        /// <summary>觸手。</summary>
        Tentacle,
        /// <summary>深淵之眼。</summary>
        Eye,
        /// <summary>幻覺物件。</summary>
        Hallucination,
    }

    /// <summary>洞窟生成參數（格為單位；由 CaveGenConfig 換算而來）。</summary>
    internal sealed class CaveSettings
    {
        /// <summary>寬（格）。</summary>
        public int Width = 256;
        /// <summary>高（格）。</summary>
        public int Height = 192;
        /// <summary>亂數種子。</summary>
        public int Seed = 1;
        /// <summary>初始填牆比例。</summary>
        public float FillPercent = 0.5f;
        /// <summary>粗格平滑次數。</summary>
        public int SmoothIterations = 5;
        /// <summary>粗格邊長（格）。</summary>
        public int CoarseSize = 4;
        /// <summary>外框厚度（格）。</summary>
        public int Border = 3;
        /// <summary>起點洞廳寬（格）。</summary>
        public int StartW = 24;
        /// <summary>起點洞廳高（格）。</summary>
        public int StartH = 12;
        /// <summary>Boss 廳寬（格）。</summary>
        public int BossW = 44;
        /// <summary>Boss 廳高（格）。</summary>
        public int BossH = 22;
        /// <summary>入口隧道高（格）。</summary>
        public int TunnelH = 7;
        /// <summary>入口隧道水平段長（格）。</summary>
        public int TunnelLength = 16;
        /// <summary>屏障離 Boss 廳的距離（格）。</summary>
        public int GateOffset = 6;
        /// <summary>屏障寬（格）。</summary>
        public int GateW = 4;
        /// <summary>浮台長度範圍（格）。</summary>
        public int LedgeMin = 3, LedgeMax = 6;
        /// <summary>同一排浮台間隔範圍（格）。</summary>
        public int GapMin = 5, GapMax = 11;
        /// <summary>品質門檻：可來回站立點至少幾個（太少代表大片區域上不去，換種子）。</summary>
        public int MinGoodStands = 1200;
        /// <summary>品質門檻：敵人 / 道具至少要放滿幾成。</summary>
        public float MinFillRatio = 0.8f;
        /// <summary>上不去的洞穴至少要有幾個站立點才補石台（太小的就放著）。</summary>
        public int MinUnreachedStands = 12;
        /// <summary>小於此格數的獨立空洞直接填實。</summary>
        public int MinRegion = 250;
        /// <summary>每次嘗試最多補幾塊石台。</summary>
        public int MaxRepairSteps = 300;
        /// <summary>最多嘗試幾個種子。</summary>
        public int MaxAttempts = 20;
        /// <summary>各類物件數量。</summary>
        public Dictionary<CaveThing, int> Counts = new Dictionary<CaveThing, int>();
        /// <summary>道具間最小距離（格）。</summary>
        public int ItemSpacing = 16;
        /// <summary>敵人間最小距離（格）。</summary>
        public int EnemySpacing = 18;
        /// <summary>敵人離起點最小距離（格）。</summary>
        public int EnemySafeRadius = 28;
        /// <summary>魚怪單邊巡邏距離（格）。</summary>
        public int FishPatrol = 8;
    }

    /// <summary>生成結果。</summary>
    internal sealed class CaveLayout
    {
        /// <summary>實心格（[x, y]）。</summary>
        public bool[,] Solid;
        /// <summary>實際使用的種子（失敗重試會往後加）。</summary>
        public int Seed;
        /// <summary>起點站立格（角色左腳那格，腳底 = 此格底邊）。</summary>
        public CaveCell Start;
        /// <summary>Boss 廳內部範圍。</summary>
        public CaveRect BossRoom;
        /// <summary>Boss 站立格（腳底 = 此格底邊）。</summary>
        public CaveCell Boss;
        /// <summary>祭壇站立格。</summary>
        public CaveCell Altar;
        /// <summary>憋氣屏障範圍。</summary>
        public CaveRect Gate;
        /// <summary>補的石台數。</summary>
        public int Ledges;
        /// <summary>可來回站立點數。</summary>
        public int GoodStands;
        /// <summary>自動擺放的物件（種類 + 站立格）。</summary>
        public List<(CaveThing thing, CaveCell cell)> Things = new List<(CaveThing, CaveCell)>();
    }

    /// <summary>
    /// 泰拉瑞亞式連通洞窟生成器（SD-03）：細胞自動機 → 起點 / Boss 廳 → 連通 → 清縫 → 可達性修補 → 擺物件。
    /// 純 C#（不依賴 UnityEngine），可在 Unity 外測試。
    /// </summary>
    internal sealed class CaveGenerator
    {
        /// <summary>角色寬（格）。</summary>
        private const int BodyW = 2;
        /// <summary>角色高（格）。</summary>
        private const int BodyH = 3;
        /// <summary>可跳上的最大高度（格，保守值）。</summary>
        private const int JumpUp = 4;
        /// <summary>挖隧道筆刷半徑（格）。</summary>
        private const int Brush = 4;

        /// <summary>參數。</summary>
        private readonly CaveSettings _s;
        /// <summary>亂數。</summary>
        private Random _rng;
        /// <summary>實心格。</summary>
        private bool[,] _solid;
        /// <summary>不可被挖開 / 加石台的保護區（Boss 廳外殼）。</summary>
        private bool[,] _protected;
        /// <summary>不可加石台 / 墊高的區域（起點洞廳）。</summary>
        private bool[,] _keepClear;
        /// <summary>寬（格）。</summary>
        private int W => _s.Width;
        /// <summary>高（格）。</summary>
        private int H => _s.Height;

        /// <summary>每次嘗試失敗的原因（除錯用）。</summary>
        public List<string> Failures { get; } = new List<string>();

        /// <summary>建立生成器。</summary>
        public CaveGenerator(CaveSettings settings) { _s = settings; }

        /// <summary>產生洞窟；種子失敗就換下一個，全部失敗丟例外。</summary>
        public CaveLayout Generate()
        {
            for (int attempt = 0; attempt < _s.MaxAttempts; attempt++)
            {
                var layout = TryGenerate(_s.Seed + attempt);
                if (layout != null) return layout;
            }
            throw new InvalidOperationException($"洞窟生成失敗（種子 {_s.Seed} 起試了 {_s.MaxAttempts} 次）");
        }

        /// <summary>用指定種子嘗試一次，不合格回傳 null。</summary>
        private CaveLayout TryGenerate(int seed)
        {
            _rng = new Random(seed);
            _solid = new bool[W, H];
            _protected = new bool[W, H];
            _keepClear = new bool[W, H];
            var layout = new CaveLayout { Seed = seed };

            CellularCave();
            var startRoom = CarveStartRoom();
            layout.Start = new CaveCell(startRoom.X + startRoom.W / 2 - 1, startRoom.Y);
            CarveBossRoom(layout, startRoom);

            for (int pass = 0; pass < 3; pass++)
            {
                Connect(layout.Start);
                FillNarrowGaps();
                EnforceBossShell(layout);
            }
            Connect(layout.Start);
            FillUnreachable(layout.Start);
            Scaffold();

            if (!Repair(layout)) { Failures.Add($"{seed}: 可達性修補失敗"); return null; }
            if (layout.GoodStands < _s.MinGoodStands) { Failures.Add($"{seed}: 可來回區域太小（{layout.GoodStands}）"); return null; }
            if (!PlaceThings(layout)) { Failures.Add($"{seed}: 可擺放位置不足"); return null; }
            int wanted = 0;
            foreach (var n in _s.Counts.Values) wanted += n;
            if (layout.Things.Count < wanted * _s.MinFillRatio) { Failures.Add($"{seed}: 物件放不滿（{layout.Things.Count}/{wanted}）"); return null; }
            layout.Solid = _solid;
            return layout;
        }

        // ───────────────────────── 地形 ─────────────────────────

        /// <summary>粗格細胞自動機產生洞穴形狀，放大後再細格平滑一次。</summary>
        private void CellularCave()
        {
            int cs = _s.CoarseSize;
            int cw = W / cs, ch = H / cs;
            var coarse = new bool[cw, ch];
            for (int x = 0; x < cw; x++)
                for (int y = 0; y < ch; y++)
                    coarse[x, y] = x == 0 || y == 0 || x == cw - 1 || y == ch - 1 || _rng.NextDouble() < _s.FillPercent;
            for (int i = 0; i < _s.SmoothIterations; i++) coarse = Smooth(coarse, cw, ch);

            for (int x = 0; x < W; x++)
                for (int y = 0; y < H; y++)
                    _solid[x, y] = coarse[Math.Min(x / cs, cw - 1), Math.Min(y / cs, ch - 1)];
            _solid = Smooth(_solid, W, H);
            _solid = Smooth(_solid, W, H);
            ApplyBorder();
        }

        /// <summary>細胞自動機一步：八鄰中牆 &gt; 4 變牆、&lt; 4 變空。</summary>
        private static bool[,] Smooth(bool[,] g, int w, int h)
        {
            var o = new bool[w, h];
            for (int x = 0; x < w; x++)
            {
                for (int y = 0; y < h; y++)
                {
                    int n = 0;
                    for (int dx = -1; dx <= 1; dx++)
                        for (int dy = -1; dy <= 1; dy++)
                        {
                            if (dx == 0 && dy == 0) continue;
                            int nx = x + dx, ny = y + dy;
                            if (nx < 0 || ny < 0 || nx >= w || ny >= h || g[nx, ny]) n++;
                        }
                    o[x, y] = n > 4 || (n == 4 && g[x, y]);
                }
            }
            return o;
        }

        /// <summary>地圖外框設為實心。</summary>
        private void ApplyBorder()
        {
            for (int x = 0; x < W; x++)
                for (int y = 0; y < H; y++)
                    if (x < _s.Border || y < _s.Border || x >= W - _s.Border || y >= H - _s.Border) _solid[x, y] = true;
        }

        /// <summary>地圖中央挖起點洞廳（平底）。</summary>
        private CaveRect CarveStartRoom()
        {
            var r = new CaveRect(W / 2 - _s.StartW / 2, H / 2 - _s.StartH / 2, _s.StartW, _s.StartH);
            SetRect(r, false);
            SetRect(new CaveRect(r.X, r.Y - 2, r.W, 2), true); // 地板
            for (int x = r.X; x < r.X + r.W; x++)
                for (int y = r.Y; y < r.Y + r.H; y++) _keepClear[x, y] = true;
            return r;
        }

        /// <summary>隨機在左 / 右 / 下邊緣挖 Boss 廳 + 水平入口隧道 + 往中央的通道。</summary>
        private void CarveBossRoom(CaveLayout layout, CaveRect startRoom)
        {
            int b = _s.Border + 2;
            int side = _rng.Next(3); // 0 左、1 右、2 下
            int x0, y0;
            if (side == 2)
            {
                y0 = b + 2;
                x0 = _rng.Next(b, W - b - _s.BossW);
            }
            else
            {
                x0 = side == 0 ? b : W - b - _s.BossW;
                y0 = _rng.Next(b + 2, H - b - _s.BossH);
            }
            var room = new CaveRect(x0, y0, _s.BossW, _s.BossH);
            // 入口朝地圖中央那一側
            int dir = side == 0 ? 1 : side == 1 ? -1 : (x0 + _s.BossW / 2 < W / 2 ? 1 : -1);
            int tunnelX = dir > 0 ? room.X + room.W : room.X - _s.TunnelLength;
            var tunnel = new CaveRect(tunnelX, room.Y, _s.TunnelLength, _s.TunnelH);
            int gateX = dir > 0 ? room.X + room.W + _s.GateOffset : room.X - _s.GateOffset - _s.GateW;

            layout.BossRoom = room;
            layout.Gate = new CaveRect(gateX, room.Y, _s.GateW, _s.TunnelH);
            // Boss 站在遠離入口一側的地板上；祭壇在地板中央
            layout.Boss = new CaveCell(dir > 0 ? room.X + 8 : room.X + room.W - 8, room.Y);
            layout.Altar = new CaveCell(room.X + room.W / 2 - 1, room.Y);

            // 保護區：Boss 廳外殼 + 廳到屏障這段隧道的外殼
            int shell = 3;
            MarkProtected(room.Expand(shell));
            int px0 = dir > 0 ? room.X + room.W : gateX;
            int px1 = dir > 0 ? gateX + _s.GateW - 1 : room.X - 1;
            MarkProtected(new CaveRect(px0, room.Y - shell, px1 - px0 + 1, _s.TunnelH + shell * 2));

            SetRect(room, false);
            SetRect(tunnel, false);
            EnforceBossShell(layout);

            // 從隧道口往起點洞廳隨機漫步挖通道
            var from = new CaveCell(dir > 0 ? tunnel.X + tunnel.W : tunnel.X - 1, tunnel.Y + _s.TunnelH / 2);
            var to = new CaveCell(startRoom.X + startRoom.W / 2, startRoom.Y + startRoom.H / 2);
            DrunkTunnel(from, to, stopOnOpen: false);
        }

        /// <summary>重新套用 Boss 保護區：外殼全實心，只留廳內與廳到屏障的隧道。</summary>
        private void EnforceBossShell(CaveLayout layout)
        {
            var room = layout.BossRoom;
            var gate = layout.Gate;
            bool right = gate.X > room.X;
            int tx0 = right ? room.X + room.W : gate.X;
            int tx1 = right ? gate.X + gate.W - 1 : room.X - 1;
            for (int x = 0; x < W; x++)
            {
                for (int y = 0; y < H; y++)
                {
                    if (!_protected[x, y]) continue;
                    bool open = room.Contains(x, y) || (x >= tx0 && x <= tx1 && y >= room.Y && y < room.Y + _s.TunnelH);
                    _solid[x, y] = !open;
                }
            }
        }

        /// <summary>從 from 往 to 隨機漫步挖通道（偏向目標）；stopOnOpen 時碰到既有空洞就停。</summary>
        private void DrunkTunnel(CaveCell from, CaveCell to, bool stopOnOpen)
        {
            int x = from.X, y = from.Y;
            int steps = 0, maxSteps = (W + H) * 8;
            while (steps++ < maxSteps)
            {
                if (stopOnOpen && steps > 4 && !_solid[x, y] && !_protected[x, y]) break;
                CarveCircle(x, y, Brush);
                if (Math.Abs(x - to.X) <= 2 && Math.Abs(y - to.Y) <= 2) break;

                if (_rng.NextDouble() < 0.65)
                {
                    // 往目標走（水平優先，讓通道比較好走）
                    if (Math.Abs(x - to.X) * 2 >= Math.Abs(y - to.Y) * (_rng.NextDouble() < 0.5 ? 1 : 3)) x += Math.Sign(to.X - x);
                    else y += Math.Sign(to.Y - y);
                }
                else
                {
                    switch (_rng.Next(4))
                    {
                        case 0: x++; break;
                        case 1: x--; break;
                        case 2: y++; break;
                        default: y--; break;
                    }
                }
                x = Clamp(x, _s.Border + Brush, W - _s.Border - Brush - 1);
                y = Clamp(y, _s.Border + Brush, H - _s.Border - Brush - 1);
            }
        }

        /// <summary>挖一個圓（保護區與外框不挖）。</summary>
        private void CarveCircle(int cx, int cy, int r)
        {
            for (int x = cx - r; x <= cx + r; x++)
                for (int y = cy - r; y <= cy + r; y++)
                {
                    if (!Inner(x, y) || _protected[x, y]) continue;
                    if ((x - cx) * (x - cx) + (y - cy) * (y - cy) <= r * r + 1) _solid[x, y] = false;
                }
        }

        /// <summary>把和起點不連通的空洞：小的填實、大的挖通道接回。</summary>
        private void Connect(CaveCell start)
        {
            for (int guard = 0; guard < 64; guard++)
            {
                var regions = Regions();
                int mainId = RegionAt(regions.map, start.X, start.Y);
                if (mainId < 0) { CarveCircle(start.X, start.Y + 2, Brush); continue; }

                bool changed = false;
                for (int id = 0; id < regions.cells.Count; id++)
                {
                    if (id == mainId) continue;
                    var cells = regions.cells[id];
                    if (cells.Count < _s.MinRegion)
                    {
                        foreach (var c in cells) _solid[c.X, c.Y] = true;
                        continue;
                    }
                    // 挖到主洞最近的一格
                    var from = cells[cells.Count / 2];
                    var target = Nearest(regions.cells[mainId], from);
                    DrunkTunnel(from, target, stopOnOpen: false);
                    changed = true;
                    break;
                }
                if (!changed) return;
            }
        }

        /// <summary>和起點不連通的空洞全部填實。</summary>
        private void FillUnreachable(CaveCell start)
        {
            var regions = Regions();
            int mainId = RegionAt(regions.map, start.X, start.Y);
            for (int id = 0; id < regions.cells.Count; id++)
            {
                if (id == mainId) continue;
                foreach (var c in regions.cells[id]) _solid[c.X, c.Y] = true;
            }
        }

        /// <summary>填掉角色進不去的窄縫（寬 &lt; 3 或高 &lt; 4）。</summary>
        private void FillNarrowGaps()
        {
            for (int pass = 0; pass < 2; pass++)
            {
                for (int y = 0; y < H; y++)
                {
                    int x = 0;
                    while (x < W)
                    {
                        if (_solid[x, y]) { x++; continue; }
                        int x1 = x;
                        while (x1 < W && !_solid[x1, y]) x1++;
                        if (x1 - x < BodyW + 1) for (int i = x; i < x1; i++) if (!_protected[i, y]) _solid[i, y] = true;
                        x = x1;
                    }
                }
                for (int x = 0; x < W; x++)
                {
                    int y = 0;
                    while (y < H)
                    {
                        if (_solid[x, y]) { y++; continue; }
                        int y1 = y;
                        while (y1 < H && !_solid[x, y1]) y1++;
                        if (y1 - y < BodyH + 1) for (int i = y; i < y1; i++) if (!_protected[x, i]) _solid[x, i] = true;
                        y = y1;
                    }
                }
            }
        }

        /// <summary>
        /// 在高的空間鋪稀疏浮台（類似泰拉瑞亞的平台）：每 4 排一層，長度與間隔隨機。
        /// 只放在上下各 3 排都空的地方（底下仍可走過，不會塞住隧道）；不夠的由 Repair 補。
        /// </summary>
        private void Scaffold()
        {
            for (int ly = _s.Border + JumpUp; ly < H - _s.Border - JumpUp; ly += JumpUp)
            {
                int x0 = _s.Border + _rng.Next(_s.GapMax);
                while (x0 < W - _s.Border)
                {
                    int len = _rng.Next(_s.LedgeMin, _s.LedgeMax + 1);
                    // 不疊在下一層浮台正上方（否則往上跳會撞頭）
                    if (Clear(x0 - 1, ly - BodyH, x0 + len, ly + BodyH) && Clear(x0, ly - JumpUp, x0 + len - 1, ly - JumpUp)
                        && !Touches(_protected, x0 - 1, ly - BodyH, x0 + len, ly + BodyH) && !Touches(_keepClear, x0 - 1, ly, x0 + len, ly))
                    {
                        for (int i = 0; i < len; i++) _solid[x0 + i, ly] = true;
                        x0 += len + _rng.Next(_s.GapMin, _s.GapMax + 1);
                    }
                    else x0++;
                }
            }
        }

        /// <summary>矩形內是否有任一格為 true。</summary>
        private bool Touches(bool[,] grid, int x0, int y0, int x1, int y1)
        {
            for (int x = Math.Max(0, x0); x <= Math.Min(W - 1, x1); x++)
                for (int y = Math.Max(0, y0); y <= Math.Min(H - 1, y1); y++)
                    if (grid[x, y]) return true;
            return false;
        }

        // ───────────────────────── 可達性 ─────────────────────────

        /// <summary>站立點圖：每個站立點的編號與鄰接。</summary>
        private sealed class StandGraph
        {
            /// <summary>格 → 站立點編號（-1 = 不是站立點）。</summary>
            public int[,] Index;
            /// <summary>站立點格座標。</summary>
            public List<CaveCell> Cells = new List<CaveCell>();
            /// <summary>正向鄰接。</summary>
            public List<List<int>> Out = new List<List<int>>();
            /// <summary>反向鄰接。</summary>
            public List<List<int>> In = new List<List<int>>();
        }

        /// <summary>身體（2×3 格）放在 (x, y) 是否完全在空格內。</summary>
        private bool BodyFits(int x, int y)
        {
            for (int i = 0; i < BodyW; i++)
                for (int j = 0; j < BodyH; j++)
                    if (!Inner(x + i, y + j) || _solid[x + i, y + j]) return false;
            return true;
        }

        /// <summary>(x, y) 是否為站立點：身體放得下且腳下有地。</summary>
        private bool IsStand(int x, int y) =>
            BodyFits(x, y) && y > 0 && (_solid[x, y - 1] || _solid[x + 1, y - 1]);

        /// <summary>矩形區域是否全是空格。</summary>
        private bool Clear(int x0, int y0, int x1, int y1)
        {
            if (x0 > x1) (x0, x1) = (x1, x0);
            if (y0 > y1) (y0, y1) = (y1, y0);
            for (int x = x0; x <= x1; x++)
                for (int y = y0; y <= y1; y++)
                    if (!Inner(x, y) || _solid[x, y]) return false;
            return true;
        }

        /// <summary>建立站立點圖（走、掉落、跳躍）。</summary>
        private StandGraph BuildGraph()
        {
            var g = new StandGraph { Index = new int[W, H] };
            for (int x = 0; x < W; x++)
                for (int y = 0; y < H; y++)
                {
                    g.Index[x, y] = -1;
                    if (x + 1 < W && IsStand(x, y))
                    {
                        g.Index[x, y] = g.Cells.Count;
                        g.Cells.Add(new CaveCell(x, y));
                    }
                }
            for (int i = 0; i < g.Cells.Count; i++) { g.Out.Add(new List<int>()); g.In.Add(new List<int>()); }

            void Link(int a, int bx, int by)
            {
                if (bx < 0 || by < 0 || bx >= W || by >= H) return;
                int b = g.Index[bx, by];
                if (b < 0 || b == a || g.Out[a].Contains(b)) return;
                g.Out[a].Add(b);
                g.In[b].Add(a);
            }

            for (int a = 0; a < g.Cells.Count; a++)
            {
                var c = g.Cells[a];
                // 走 / 走出邊緣掉落
                foreach (int d in new[] { -1, 1 })
                {
                    int nx = c.X + d;
                    if (!BodyFits(nx, c.Y)) continue;
                    int ny = c.Y;
                    while (ny > 0 && !IsStand(nx, ny) && BodyFits(nx, ny - 1)) ny--;
                    if (IsStand(nx, ny)) Link(a, nx, ny);
                }
                // 往上跳：頭頂淨空 + 頂點高度水平淨空
                for (int dy = 1; dy <= JumpUp; dy++)
                {
                    if (!Clear(c.X, c.Y, c.X + BodyW - 1, c.Y + dy + BodyH - 1)) break;
                    int maxDx = dy <= 2 ? 6 : dy == 3 ? 5 : 4; // 最大速度 5、頂點約 0.57 秒 → 約 5.7 格，取保守值
                    foreach (int d in new[] { -1, 1 })
                        for (int dx = 1; dx <= maxDx; dx++)
                        {
                            int tx = c.X + d * dx;
                            if (!Clear(c.X + (d > 0 ? BodyW : -1), c.Y + dy, tx + (d > 0 ? BodyW - 1 : 0), c.Y + dy + BodyH - 1)) break;
                            Link(a, tx, c.Y + dy);
                        }
                    Link(a, c.X, c.Y + dy);
                }
                // 跳過坑 / 往下跳：先小跳 1 格，再水平移動，最後落到目標
                if (Clear(c.X, c.Y, c.X + BodyW - 1, c.Y + BodyH))
                {
                    foreach (int d in new[] { -1, 1 })
                        for (int dx = 2; dx <= 6; dx++)
                        {
                            int tx = c.X + d * dx;
                            if (!Clear(c.X + (d > 0 ? BodyW : -1), c.Y + 1, tx + (d > 0 ? BodyW - 1 : 0), c.Y + BodyH)) break;
                            int ty = c.Y + 1;
                            while (ty > 0 && !IsStand(tx, ty) && BodyFits(tx, ty - 1) && c.Y - ty < 40) ty--;
                            if (IsStand(tx, ty)) Link(a, tx, ty);
                        }
                }
            }
            return g;
        }

        /// <summary>BFS：從 source 沿正向（或反向）鄰接能到的點；回傳步數（-1 = 到不了）。</summary>
        private static int[] Bfs(StandGraph g, int source, bool reverse)
        {
            var dist = new int[g.Cells.Count];
            for (int i = 0; i < dist.Length; i++) dist[i] = -1;
            var q = new Queue<int>();
            dist[source] = 0;
            q.Enqueue(source);
            while (q.Count > 0)
            {
                int a = q.Dequeue();
                foreach (int b in reverse ? g.In[a] : g.Out[a])
                {
                    if (dist[b] >= 0) continue;
                    dist[b] = dist[a] + 1;
                    q.Enqueue(b);
                }
            }
            return dist;
        }

        /// <summary>
        /// 可達性修補（只在需要的地方補石台，看起來比較自然）：
        /// ・能到但回不來的坑 → 在坑內最高點上方補石台；放不下就把坑底墊高一格。
        /// ・上不去的洞穴 → 在它最低點下方補石台，一層層往下接到可到達的地方。
        /// 重算直到沒有回不來的坑、也沒有可修的上不去洞穴。
        /// </summary>
        private bool Repair(CaveLayout layout)
        {
            var givenUp = new HashSet<long>();
            for (int step = 0; step < _s.MaxRepairSteps; step++)
            {
                var g = BuildGraph();
                int start = g.Index[layout.Start.X, layout.Start.Y];
                if (start < 0) { Failures.Add("起點不是站立點"); return false; }
                var fwd = Bfs(g, start, false);
                var back = Bfs(g, start, true);

                bool acted = false;
                // 回不來的坑
                foreach (var comp in Components(g, i => fwd[i] >= 0 && back[i] < 0))
                {
                    int top = comp[0];
                    foreach (int i in comp) if (g.Cells[i].Y > g.Cells[top].Y) top = i;
                    var c = g.Cells[top];
                    if (AddLedge(c, above: true)) layout.Ledges++;
                    else RaiseFloor(c);
                    acted = true;
                }
                // 上不去的洞穴
                foreach (var comp in Components(g, i => fwd[i] < 0))
                {
                    if (comp.Count < _s.MinUnreachedStands) continue;
                    // 由低到高找第一個下方放得下石台的點
                    comp.Sort((a, b) => g.Cells[a].Y.CompareTo(g.Cells[b].Y));
                    foreach (int i in comp)
                    {
                        var c = g.Cells[i];
                        if (givenUp.Contains(Key(c))) continue;
                        if (AddLedge(c, above: false)) { layout.Ledges++; acted = true; break; }
                        givenUp.Add(Key(c));
                    }
                }
                if (acted) continue;

                int good = 0;
                for (int i = 0; i < g.Cells.Count; i++) if (fwd[i] >= 0 && back[i] >= 0) good++;
                int altar = g.Index[layout.Altar.X, layout.Altar.Y];
                layout.GoodStands = good;
                if (altar < 0 || fwd[altar] < 0 || back[altar] < 0) { Failures.Add($"Boss 廳到不了（altar {altar}）"); return false; }
                return true;
            }
            Failures.Add("修補次數用完");
            return false;
        }

        /// <summary>依鄰接（不分方向）把符合條件的站立點分組。</summary>
        private static List<List<int>> Components(StandGraph g, Func<int, bool> include)
        {
            var result = new List<List<int>>();
            var seen = new bool[g.Cells.Count];
            var q = new Queue<int>();
            for (int i = 0; i < g.Cells.Count; i++)
            {
                if (seen[i] || !include(i)) continue;
                var comp = new List<int>();
                seen[i] = true;
                q.Enqueue(i);
                while (q.Count > 0)
                {
                    int a = q.Dequeue();
                    comp.Add(a);
                    foreach (var list in new[] { g.Out[a], g.In[a] })
                        foreach (int b in list)
                            if (!seen[b] && include(b)) { seen[b] = true; q.Enqueue(b); }
                }
                result.Add(comp);
            }
            return result;
        }

        /// <summary>把站立點所在的整排坑底填實一格（坑底墊高）。</summary>
        private void RaiseFloor(CaveCell s)
        {
            int x0 = s.X, x1 = s.X + BodyW - 1;
            while (x0 - 1 >= 0 && !_solid[x0 - 1, s.Y] && _solid[x0 - 1, s.Y - 1]) x0--;
            while (x1 + 1 < W && !_solid[x1 + 1, s.Y] && _solid[x1 + 1, s.Y - 1]) x1++;
            for (int x = x0; x <= x1; x++)
                if (!_protected[x, s.Y] && !_keepClear[x, s.Y]) _solid[x, s.Y] = true;
        }

        /// <summary>在站立點 s 旁邊加一塊 4 格寬石台：above = 在上方（往上爬出坑）、否則在下方（讓下面的人跳上來）。</summary>
        private bool AddLedge(CaveCell s, bool above)
        {
            const int len = 4;
            for (int up = JumpUp; up >= 2; up--)
            {
                int ly = above ? s.Y + up - 1 : s.Y - up - 1; // 石台頂排；站上去的高度 = ly + 1
                foreach (int off in new[] { 3, -len - 1, 2, -len, 4, -len - 2 })
                {
                    int lx = s.X + off;
                    if (!Clear(lx - 1, ly - 1, lx + len, ly + BodyH)) continue; // 石台本身、下方一排、上方站立空間都要空
                    if (Touches(_protected, lx, ly, lx + len - 1, ly) || Touches(_keepClear, lx, ly, lx + len - 1, ly)) continue;
                    for (int i = 0; i < len; i++) _solid[lx + i, ly] = true;
                    return true;
                }
            }
            return false;
        }

        // ───────────────────────── 擺物件 ─────────────────────────

        /// <summary>在可來回的站立點自動擺放存檔點、道具、敵人、幻覺。</summary>
        private bool PlaceThings(CaveLayout layout)
        {
            var g = BuildGraph();
            int start = g.Index[layout.Start.X, layout.Start.Y];
            var fwd = Bfs(g, start, false);
            var back = Bfs(g, start, true);
            var bossZone = layout.BossRoom.Expand(6);
            int gx = layout.Gate.X;
            var candidates = new List<CaveCell>();
            for (int i = 0; i < g.Cells.Count; i++)
            {
                var c = g.Cells[i];
                if (fwd[i] < 0 || back[i] < 0 || bossZone.Contains(c.X, c.Y) || _protected[c.X, c.Y]) continue;
                candidates.Add(c);
            }
            if (candidates.Count < 200) return false;
            Shuffle(candidates);

            var items = new List<CaveCell> { layout.Start };
            var enemies = new List<CaveCell>();
            int Count(CaveThing t) => _s.Counts.TryGetValue(t, out int n) ? n : 0;
            void Add(CaveThing t, CaveCell c, List<CaveCell> group) { layout.Things.Add((t, c)); group.Add(c); }

            // 存檔點：起點、Boss 入口外、其餘最遠點取樣
            Add(CaveThing.Checkpoint, layout.Start, new List<CaveCell>());
            int outDir = layout.Gate.X > layout.BossRoom.X ? 1 : -1;
            var outside = new CaveCell(gx + outDir * 10, layout.BossRoom.Y);
            var gateCp = Nearest(candidates, outside);
            Add(CaveThing.Checkpoint, gateCp, items);
            var checkpoints = new List<CaveCell> { layout.Start, gateCp };
            for (int i = 2; i < Count(CaveThing.Checkpoint); i++)
            {
                var c = Farthest(candidates, checkpoints);
                checkpoints.Add(c);
                Add(CaveThing.Checkpoint, c, items);
            }

            // 封印碎片：離起點與彼此最遠
            var seals = new List<CaveCell> { layout.Start, gateCp };
            for (int i = 0; i < Count(CaveThing.Seal); i++)
            {
                var c = Farthest(candidates, seals);
                seals.Add(c);
                Add(CaveThing.Seal, c, items);
            }

            int itemSq = _s.ItemSpacing * _s.ItemSpacing;
            int enemySq = _s.EnemySpacing * _s.EnemySpacing;
            int safeSq = _s.EnemySafeRadius * _s.EnemySafeRadius;

            // 回復道具：隨機、保持間距
            foreach (var t in new[] { CaveThing.Pill, CaveThing.Medkit })
            {
                int placed = 0;
                foreach (var c in candidates)
                {
                    if (placed >= Count(t)) break;
                    if (!FarFrom(c, items, itemSq)) continue;
                    Add(t, c, items);
                    placed++;
                }
            }

            // 敵人
            bool EnemyOk(CaveCell c) => c.DistSq(layout.Start) >= safeSq && FarFrom(c, enemies, enemySq) && FarFrom(c, checkpoints, safeSq / 4);
            // 敵人一律站在地面上（與玩家同一高度帶）
            PlaceEnemies(CaveThing.Fish, Count(CaveThing.Fish), candidates, enemies, layout, c =>
                EnemyOk(c) && FlatFloor(c.X, c.X + 1, c.Y) && Clear(c.X - _s.FishPatrol, c.Y, c.X + 1 + _s.FishPatrol, c.Y + BodyH - 1));
            PlaceEnemies(CaveThing.Tentacle, Count(CaveThing.Tentacle), candidates, enemies, layout, c =>
                EnemyOk(c) && FlatFloor(c.X, c.X + 1, c.Y) && Clear(c.X, c.Y, c.X + 1, c.Y + 4));
            PlaceEnemies(CaveThing.Eye, Count(CaveThing.Eye), candidates, enemies, layout, c =>
                EnemyOk(c) && FlatFloor(c.X - 1, c.X + 2, c.Y) && Clear(c.X - 1, c.Y, c.X + 2, c.Y + BodyH - 1));

            // 幻覺：上方空間大的位置
            var halls = new List<CaveCell>();
            int placedHall = 0;
            foreach (var c in candidates)
            {
                if (placedHall >= Count(CaveThing.Hallucination)) break;
                if (!Clear(c.X - 3, c.Y, c.X + 4, c.Y + BodyH) || !FarFrom(c, halls, 40 * 40) || !FarFrom(c, items, itemSq)) continue;
                layout.Things.Add((CaveThing.Hallucination, c));
                halls.Add(c);
                placedHall++;
            }
            return true;
        }

        /// <summary>x0~x1 在 y 那排的腳下是否全是地面（平地）。</summary>
        private bool FlatFloor(int x0, int x1, int y)
        {
            for (int x = x0; x <= x1; x++)
                if (x < 0 || x >= W || y < 1 || !_solid[x, y - 1]) return false;
            return true;
        }

        /// <summary>依條件放指定數量的敵人。</summary>
        private static void PlaceEnemies(CaveThing t, int count, List<CaveCell> candidates, List<CaveCell> enemies, CaveLayout layout, Func<CaveCell, bool> ok)
        {
            int placed = 0;
            foreach (var c in candidates)
            {
                if (placed >= count) break;
                if (!ok(c)) continue;
                layout.Things.Add((t, c));
                enemies.Add(c);
                placed++;
            }
        }

        // ───────────────────────── 工具 ─────────────────────────

        /// <summary>4 方向連通的空洞分區。</summary>
        private (int[,] map, List<List<CaveCell>> cells) Regions()
        {
            var map = new int[W, H];
            for (int x = 0; x < W; x++) for (int y = 0; y < H; y++) map[x, y] = -1;
            var list = new List<List<CaveCell>>();
            var q = new Queue<CaveCell>();
            for (int x = 0; x < W; x++)
                for (int y = 0; y < H; y++)
                {
                    if (_solid[x, y] || map[x, y] >= 0) continue;
                    var cells = new List<CaveCell>();
                    int id = list.Count;
                    map[x, y] = id;
                    q.Enqueue(new CaveCell(x, y));
                    while (q.Count > 0)
                    {
                        var c = q.Dequeue();
                        cells.Add(c);
                        foreach (var (dx, dy) in new[] { (1, 0), (-1, 0), (0, 1), (0, -1) })
                        {
                            int nx = c.X + dx, ny = c.Y + dy;
                            if (!Inner(nx, ny) || _solid[nx, ny] || map[nx, ny] >= 0) continue;
                            map[nx, ny] = id;
                            q.Enqueue(new CaveCell(nx, ny));
                        }
                    }
                    list.Add(cells);
                }
            return (map, list);
        }

        /// <summary>某格所在分區（附近 3 格內找空格）。</summary>
        private int RegionAt(int[,] map, int x, int y)
        {
            for (int r = 0; r <= 3; r++)
                for (int dx = -r; dx <= r; dx++)
                    for (int dy = -r; dy <= r; dy++)
                        if (Inner(x + dx, y + dy) && map[x + dx, y + dy] >= 0) return map[x + dx, y + dy];
            return -1;
        }

        /// <summary>清單中離 p 最近的一格。</summary>
        private static CaveCell Nearest(List<CaveCell> list, CaveCell p)
        {
            var best = list[0];
            int bd = int.MaxValue;
            foreach (var c in list)
            {
                int d = c.DistSq(p);
                if (d < bd) { bd = d; best = c; }
            }
            return best;
        }

        /// <summary>候選中離已選集合最遠（最小距離最大）的一格。</summary>
        private static CaveCell Farthest(List<CaveCell> candidates, List<CaveCell> chosen)
        {
            var best = candidates[0];
            int bd = -1;
            foreach (var c in candidates)
            {
                int d = int.MaxValue;
                foreach (var o in chosen) d = Math.Min(d, c.DistSq(o));
                if (d > bd) { bd = d; best = c; }
            }
            return best;
        }

        /// <summary>c 是否離清單中每一格都至少 sqrt(minSq)。</summary>
        private static bool FarFrom(CaveCell c, List<CaveCell> list, int minSq)
        {
            foreach (var o in list) if (c.DistSq(o) < minSq) return false;
            return true;
        }

        /// <summary>Fisher–Yates 洗牌（固定種子）。</summary>
        private void Shuffle(List<CaveCell> list)
        {
            for (int i = list.Count - 1; i > 0; i--)
            {
                int j = _rng.Next(i + 1);
                (list[i], list[j]) = (list[j], list[i]);
            }
        }

        /// <summary>矩形設為實心 / 空。</summary>
        private void SetRect(CaveRect r, bool solid)
        {
            for (int x = r.X; x < r.X + r.W; x++)
                for (int y = r.Y; y < r.Y + r.H; y++)
                    if (Inner(x, y)) _solid[x, y] = solid;
        }

        /// <summary>矩形標記為保護區。</summary>
        private void MarkProtected(CaveRect r)
        {
            for (int x = r.X; x < r.X + r.W; x++)
                for (int y = r.Y; y < r.Y + r.H; y++)
                    if (x >= 0 && y >= 0 && x < W && y < H) _protected[x, y] = true;
        }

        /// <summary>是否在外框以內。</summary>
        private bool Inner(int x, int y) => x >= _s.Border && y >= _s.Border && x < W - _s.Border && y < H - _s.Border;

        /// <summary>夾在範圍內。</summary>
        private static int Clamp(int v, int min, int max) => v < min ? min : v > max ? max : v;

        /// <summary>格座標打包成 key。</summary>
        private static long Key(CaveCell c) => ((long)c.X << 32) | (uint)c.Y;
    }
}
