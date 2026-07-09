using UnityEngine;

namespace HangeulAdventure.Game
{
    /// <summary>
    /// 절차 합성 효과음 (외부 에셋 없음, 결정로그 참조).
    /// 짧은 사인/삼각파에 감쇠 엔벌로프를 씌운 레트로 톤.
    /// </summary>
    public class SfxPlayer : MonoBehaviour
    {
        private static SfxPlayer _instance;

        /// <summary>파괴된 경우 진짜 null을 반환해 ?. 호출이 안전하도록 함.</summary>
        public static SfxPlayer Instance => _instance != null ? _instance : null;

        private AudioSource _source;
        private AudioClip _move, _compose, _split, _fail, _collect, _clear;

        private void OnDestroy()
        {
            if (_instance == this) _instance = null;
        }

        private void Awake()
        {
            _instance = this;
            _source = gameObject.AddComponent<AudioSource>();
            _source.playOnAwake = false;
            _source.volume = 0.5f;

            _move = Tone("sfx_move", new[] { (440f, 0.00f, 0.06f) });
            _compose = Tone("sfx_compose", new[] { (523f, 0.00f, 0.08f), (784f, 0.06f, 0.10f) });
            _split = Tone("sfx_split", new[] { (660f, 0.00f, 0.08f), (440f, 0.06f, 0.10f) });
            _fail = Tone("sfx_fail", new[] { (180f, 0.00f, 0.12f) });
            _collect = Tone("sfx_collect", new[] { (880f, 0.00f, 0.07f), (1175f, 0.05f, 0.12f) });
            _clear = Tone("sfx_clear", new[] { (523f, 0.00f, 0.10f), (659f, 0.09f, 0.10f), (784f, 0.18f, 0.10f), (1047f, 0.27f, 0.22f) });
        }

        public void Move() => Play(_move);
        public void Compose() => Play(_compose);
        public void Split() => Play(_split);
        public void Fail() => Play(_fail, 0.35f);
        public void Collect() => Play(_collect);
        public void Clear() => Play(_clear);

        private void Play(AudioClip clip, float volume = 0.5f)
        {
            if (clip != null) _source.PlayOneShot(clip, volume);
        }

        /// <summary>(주파수, 시작 시각, 길이)의 노트들을 겹쳐 하나의 클립으로.</summary>
        private static AudioClip Tone(string name, (float freq, float start, float dur)[] notes)
        {
            const int rate = 44100;
            float total = 0f;
            foreach (var n in notes) total = Mathf.Max(total, n.start + n.dur);
            int samples = Mathf.CeilToInt(total * rate) + 64;
            var data = new float[samples];

            foreach (var (freq, start, dur) in notes)
            {
                int s0 = Mathf.FloorToInt(start * rate);
                int len = Mathf.FloorToInt(dur * rate);
                for (int i = 0; i < len && s0 + i < samples; i++)
                {
                    float t = (float)i / rate;
                    float env = 1f - (float)i / len;           // 선형 감쇠
                    env *= Mathf.Min(1f, i / (rate * 0.005f)); // 클릭 방지 어택
                    data[s0 + i] += Mathf.Sin(2 * Mathf.PI * freq * t) * env * 0.5f;
                }
            }

            var clip = AudioClip.Create(name, samples, 1, rate, false);
            clip.SetData(data, 0);
            return clip;
        }
    }
}
