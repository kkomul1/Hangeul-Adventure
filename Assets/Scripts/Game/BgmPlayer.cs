using UnityEngine;

namespace HangeulAdventure.Game
{
    /// <summary>
    /// 배경음악 재생 (Resources/Audio/{트랙}.mp3): 트랙 전환·루프·볼륨.
    /// 볼륨은 PlayerPrefs "bgm_volume"에 저장 (설정 화면에서 조절).
    /// 맵별 트랙은 맵 JSON의 선택 필드 bgm으로 지정, 없으면 bgm_forest.
    /// </summary>
    public class BgmPlayer : MonoBehaviour
    {
        private static BgmPlayer _instance;

        /// <summary>파괴된 경우 진짜 null을 반환해 ?. 호출이 안전하도록 함.</summary>
        public static BgmPlayer Instance => _instance != null ? _instance : null;

        public const string VolumePref = "bgm_volume";
        private const float DefaultVolume = 0.6f;

        private AudioSource _source;
        private string _current;

        private void OnDestroy()
        {
            if (_instance == this) _instance = null;
        }

        private void Awake()
        {
            _instance = this;
            _source = gameObject.AddComponent<AudioSource>();
            _source.playOnAwake = false;
            _source.loop = true;
            _source.volume = PlayerPrefs.GetFloat(VolumePref, DefaultVolume);
        }

        public float Volume
        {
            get => _source.volume;
            set
            {
                _source.volume = Mathf.Clamp01(value);
                PlayerPrefs.SetFloat(VolumePref, _source.volume);
            }
        }

        /// <summary>트랙 전환 (같은 트랙이면 유지). 클립이 없으면 현재 곡을 유지한다 (미도착 곡 대비).</summary>
        public void Play(string track)
        {
            if (string.IsNullOrEmpty(track) || track == _current) return;
            var clip = Resources.Load<AudioClip>("Audio/" + track);
            if (clip == null)
            {
                Debug.LogWarning($"BGM 없음: Resources/Audio/{track} — 현재 곡 유지");
                return;
            }
            _current = track;
            _source.clip = clip;
            _source.Play();
        }

        public void Stop()
        {
            _current = null;
            _source.Stop();
            _source.clip = null;
        }
    }
}
