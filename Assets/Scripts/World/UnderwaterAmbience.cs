using UnityEngine;

namespace DrownedDream
{
    /// <summary>
    /// 水下氛圍（F-MAP-10）：跟著攝影機的海雪粒子 + 從上方斜照、緩緩搖曳的光柱。
    /// 排在遠景之前、地圖之後，不受光、不參與碰撞。海雪用世界座標模擬，鏡頭移動時有景深感。
    /// </summary>
    public class UnderwaterAmbience : MonoBehaviour
    {
        [Header("共用")]
        /// <summary>不受光的 Sprite 材質。</summary>
        [SerializeField] private Material _material;
        /// <summary>海雪用的柔光點圖。</summary>
        [SerializeField] private Sprite _dotSprite;

        [Header("海雪")]
        /// <summary>每秒產生幾顆。</summary>
        [SerializeField] private float _snowRate = 14f;
        /// <summary>海雪顏色（含透明度）。</summary>
        [SerializeField] private Color _snowColor = new Color(0.8f, 0.92f, 1f, 0.35f);
        /// <summary>海雪大小範圍。</summary>
        [SerializeField] private Vector2 _snowSize = new Vector2(0.05f, 0.16f);
        /// <summary>海雪排序（遠景 -100 之前、地圖之後）。</summary>
        [SerializeField] private int _snowOrder = -60;

        [Header("光柱")]
        /// <summary>光柱數量。</summary>
        [SerializeField] private int _shaftCount = 4;
        /// <summary>光柱顏色（含最大透明度）。</summary>
        [SerializeField] private Color _shaftColor = new Color(0.55f, 0.85f, 1f, 0.12f);
        /// <summary>光柱傾斜角度（度，負 = 往右下照）。</summary>
        [SerializeField] private float _shaftAngle = -18f;
        /// <summary>光柱搖曳幅度（度）。</summary>
        [SerializeField] private float _shaftSway = 4f;
        /// <summary>光柱排序。</summary>
        [SerializeField] private int _shaftOrder = -90;

        /// <summary>攝影機。</summary>
        private Camera _camera;
        /// <summary>海雪粒子。</summary>
        private ParticleSystem _snow;
        /// <summary>光柱。</summary>
        private SpriteRenderer[] _shafts;
        /// <summary>各光柱在畫面上的水平位置比例（-0.5 ~ 0.5）。</summary>
        private float[] _shaftX;
        /// <summary>各光柱搖曳相位。</summary>
        private float[] _shaftPhase;
        /// <summary>各光柱寬度比例。</summary>
        private float[] _shaftWidth;
        /// <summary>光柱圖快取。</summary>
        private static Sprite s_shaftSprite;

        /// <summary>建立海雪與光柱。</summary>
        private void Start()
        {
            _camera = Camera.main;
            BuildSnow();
            BuildShafts();
        }

        /// <summary>海雪：在畫面範圍內隨機出現，緩慢下沉並左右漂、淡入淡出。</summary>
        private void BuildSnow()
        {
            if (_dotSprite == null) return;
            var go = new GameObject("MarineSnow");
            go.transform.SetParent(transform, false);
            _snow = go.AddComponent<ParticleSystem>();
            _snow.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);

            var main = _snow.main;
            main.startLifetime = new ParticleSystem.MinMaxCurve(6f, 10f);
            main.startSpeed = 0f;
            main.startSize = new ParticleSystem.MinMaxCurve(_snowSize.x, _snowSize.y);
            main.startColor = _snowColor;
            main.maxParticles = 300;
            main.simulationSpace = ParticleSystemSimulationSpace.World;
            main.prewarm = true;

            var emission = _snow.emission;
            emission.rateOverTime = _snowRate;
            var shape = _snow.shape;
            shape.shapeType = ParticleSystemShapeType.Box;
            shape.scale = new Vector3(34f, 22f, 0.1f);

            var velocity = _snow.velocityOverLifetime;
            velocity.enabled = true;
            velocity.space = ParticleSystemSimulationSpace.World;
            velocity.x = new ParticleSystem.MinMaxCurve(-0.15f, 0.15f);
            velocity.y = new ParticleSystem.MinMaxCurve(-0.35f, -0.1f);
            velocity.z = new ParticleSystem.MinMaxCurve(0f, 0f);

            var noise = _snow.noise;
            noise.enabled = true;
            noise.strength = 0.15f;
            noise.frequency = 0.3f;

            var fade = new Gradient();
            fade.SetKeys(
                new[] { new GradientColorKey(Color.white, 0f), new GradientColorKey(Color.white, 1f) },
                new[] { new GradientAlphaKey(0f, 0f), new GradientAlphaKey(1f, 0.2f), new GradientAlphaKey(1f, 0.8f), new GradientAlphaKey(0f, 1f) });
            var color = _snow.colorOverLifetime;
            color.enabled = true;
            color.color = fade;

            var sheet = _snow.textureSheetAnimation;
            sheet.enabled = true;
            sheet.mode = ParticleSystemAnimationMode.Sprites;
            sheet.SetSprite(0, _dotSprite);

            var renderer = go.GetComponent<ParticleSystemRenderer>();
            if (_material != null) renderer.sharedMaterial = _material;
            renderer.sortingOrder = _snowOrder;
            _snow.Play();
        }

        /// <summary>光柱：畫面上方幾道斜長柔光，位置、寬度隨機。</summary>
        private void BuildShafts()
        {
            int n = Mathf.Max(0, _shaftCount);
            _shafts = new SpriteRenderer[n];
            _shaftX = new float[n];
            _shaftPhase = new float[n];
            _shaftWidth = new float[n];
            for (int i = 0; i < n; i++)
            {
                var go = new GameObject($"LightShaft{i}");
                go.transform.SetParent(transform, false);
                var sr = go.AddComponent<SpriteRenderer>();
                sr.sprite = ShaftSprite;
                if (_material != null) sr.sharedMaterial = _material;
                sr.sortingOrder = _shaftOrder;
                _shafts[i] = sr;
                _shaftX[i] = (i + 0.5f) / n - 0.5f + Random.Range(-0.08f, 0.08f);
                _shaftPhase[i] = Random.Range(0f, 10f);
                _shaftWidth[i] = Random.Range(0.08f, 0.16f);
            }
        }

        /// <summary>跟著攝影機：海雪發射範圍置中於畫面，光柱依畫面大小擺放並搖曳、明暗呼吸。</summary>
        private void LateUpdate()
        {
            if (_camera == null) return;
            Vector3 cam = _camera.transform.position;
            transform.position = new Vector3(cam.x, cam.y, 0f);
            if (_shafts == null) return;

            float viewH = _camera.orthographicSize * 2f;
            float viewW = viewH * _camera.aspect;
            float t = Time.time;
            for (int i = 0; i < _shafts.Length; i++)
            {
                var tr = _shafts[i].transform;
                float sway = Mathf.Sin(t * 0.25f + _shaftPhase[i]) * _shaftSway;
                tr.localRotation = Quaternion.Euler(0f, 0f, _shaftAngle + sway);
                // 光柱頂端在畫面上緣外，往下照（圖的 pivot 在頂端中央）
                tr.localPosition = new Vector3(_shaftX[i] * viewW * 1.2f, viewH * 0.6f, 0f);
                tr.localScale = new Vector3(viewW * _shaftWidth[i] * 4f, viewH * 1.3f, 1f); // 圖寬 0.25、高 1 單位
                float breathe = 0.6f + 0.4f * Mathf.Sin(t * 0.4f + _shaftPhase[i] * 2f);
                _shafts[i].color = new Color(_shaftColor.r, _shaftColor.g, _shaftColor.b, _shaftColor.a * breathe);
            }
        }

        /// <summary>光柱圖：寬 0.25、高 1 單位，pivot 在頂端中央；由上往下淡出，左右邊緣柔化。</summary>
        private static Sprite ShaftSprite
        {
            get
            {
                if (s_shaftSprite != null) return s_shaftSprite;
                const int w = 32, h = 128;
                var tex = new Texture2D(w, h) { filterMode = FilterMode.Bilinear, wrapMode = TextureWrapMode.Clamp };
                for (int y = 0; y < h; y++)
                {
                    float v = (y + 0.5f) / h;                        // 0 底 → 1 頂
                    for (int x = 0; x < w; x++)
                    {
                        float u = Mathf.Abs((x + 0.5f) / w * 2f - 1f); // 0 中線 → 1 邊緣
                        float a = Mathf.Pow(1f - u, 1.8f) * Mathf.Pow(v, 1.5f);
                        tex.SetPixel(x, y, new Color(1f, 1f, 1f, a));
                    }
                }
                tex.Apply();
                s_shaftSprite = Sprite.Create(tex, new Rect(0, 0, w, h), new Vector2(0.5f, 1f), h);
                return s_shaftSprite;
            }
        }
    }
}
