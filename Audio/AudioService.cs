using System;
using System.Collections.Generic;
using BounceHeroes.Core;
using FMOD.Studio;
using FMODPlus;
using FMODUnity;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace BounceHeroes.Audio
{
    /// <summary>
    /// FMOD Plus의 <see cref="FMODAudioSource"/>로 사운드를 재생하는 서비스입니다.
    /// BGM은 지속 emitter로, SFX는 일회성(PlayOneShot) emitter로 재생하며,
    /// BGM/SFX 볼륨은 FMOD VCA(<c>vca:/BGM</c>, <c>vca:/SFX</c>)로 제어합니다.
    /// "언제 무엇을 울릴지"는 결정하지 않고, id를 <see cref="AudioDatabase"/>로 해석해 재생만 담당합니다.
    /// </summary>
    public sealed class AudioService : MonoBehaviour, IAudioService
    {
        private const string RootName = "[Audio]";
        private const string BgmVcaPath = "vca:/BGM";
        private const string SfxVcaPath = "vca:/SFX";
        private const string BgmEnabledKey = "Audio.BgmEnabled";
        private const string SfxEnabledKey = "Audio.SfxEnabled";

        private static AudioService _instance;

        private readonly Dictionary<AudioId, AudioDatabase.Entry> _byId = new();
        private FMODAudioSource _bgmSource;
        private FMODAudioSource _sfxSource;
        private FMODAudioSource _snapshotSource;

        private AudioDatabase _database;
        private bool _isInitialized;
        private AudioId _currentBgm = AudioId.None;
        private AudioId _currentSnapshot = AudioId.None;

        public static AudioService GetOrCreate(AudioDatabase database)
        {
            if (_instance == null)
            {
                _instance = FindAnyObjectByType<AudioService>();

                if (_instance == null)
                {
                    var root = new GameObject(RootName);
                    _instance = root.AddComponent<AudioService>();
                }
            }

            _instance.Initialize(database);
            return _instance;
        }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetStatics()
        {
            _instance = null;
        }

        private void Awake()
        {
            if (_instance != null && _instance != this)
            {
                Destroy(gameObject);
                return;
            }

            _instance = this;
            gameObject.name = RootName;
            transform.SetParent(null);
            DontDestroyOnLoad(gameObject);

            EnsureSources();
            SceneManager.activeSceneChanged += OnActiveSceneChanged;
        }

        private void Initialize(AudioDatabase database)
        {
            EnsureSources();

            if (_isInitialized && _database == database)
                return;

            _database = database;
            _byId.Clear();

            if (database == null)
            {
                Debug.LogError("[AudioService] AudioDatabase is not assigned to the active LifetimeScope.");
                return;
            }

            foreach (AudioDatabase.Entry entry in database.Entries)
            {
                if (entry.id == AudioId.None)
                    continue;

                _byId[entry.id] = entry;
            }

            _isInitialized = true;

            // 저장된 켬/끔 상태를 초기 VCA에 반영합니다.
            SetBgmEnabled(PlayerPrefs.GetInt(BgmEnabledKey, 1) == 1);
            SetSfxEnabled(PlayerPrefs.GetInt(SfxEnabledKey, 1) == 1);
        }

        public void PlaySfx(AudioId id, Vector3 position = default)
        {
            if (TryResolve(id, out EventReference reference))
                _sfxSource.PlayOneShot(reference, 1f, position);
        }

        public void PlayBgm(AudioId id)
        {
            if (_currentBgm == id && _bgmSource.isPlaying)
                return;

            StartBgm(id, true);
        }

        public void RestartBgm(AudioId id)
        {
            StartBgm(id, false);
        }

        private void StartBgm(AudioId id, bool fadeOutCurrent)
        {
            if (!TryResolve(id, out EventReference reference))
                return;

            _bgmSource.Stop(fadeOutCurrent);
            _bgmSource.clip = reference;
            _bgmSource.Play();
            _currentBgm = id;
        }

        public void StopBgm(bool fade = true)
        {
            _bgmSource.Stop(fade);
            _currentBgm = AudioId.None;
        }

        public void SetBgmParameter(string parameterName, float value)
        {
            if (string.IsNullOrEmpty(parameterName))
                return;

            // FMODAudioSource.SetParameter는 instance.isValid() 가드가 있어 BGM 미재생 시 안전하게 무시됩니다.
            _bgmSource.SetParameter(parameterName, value);
        }

        public void PlaySnapshot(AudioId id)
        {
            if (_currentSnapshot == id && _snapshotSource.isPlaying)
                return;

            if (!TryResolve(id, out EventReference reference))
                return;

            _snapshotSource.Stop(true);
            _snapshotSource.clip = reference;
            _snapshotSource.Play();
            _currentSnapshot = id;
        }

        public void StopSnapshot(bool fade = true)
        {
            _snapshotSource.Stop(fade);
            _currentSnapshot = AudioId.None;
        }

        public void SetBgmEnabled(bool enabled) => SetVcaVolume(BgmVcaPath, enabled ? 1f : 0f);

        public void SetSfxEnabled(bool enabled) => SetVcaVolume(SfxVcaPath, enabled ? 1f : 0f);

        private void OnDestroy()
        {
            if (_instance != this)
                return;

            SceneManager.activeSceneChanged -= OnActiveSceneChanged;
            _instance = null;
            _byId.Clear();
        }

        private void OnActiveSceneChanged(Scene previous, Scene current)
        {
            if (_currentSnapshot != AudioId.None)
                StopSnapshot();
        }

        private void EnsureSources()
        {
            if (_bgmSource == null)
                _bgmSource = FindOrCreateChildSource("BGM");

            if (_sfxSource == null)
                _sfxSource = FindOrCreateChildSource("SFX");

            if (_snapshotSource == null)
                _snapshotSource = FindOrCreateChildSource("Snapshot");
        }

        private FMODAudioSource FindOrCreateChildSource(string label)
        {
            Transform existing = transform.Find(label);
            if (existing != null && existing.TryGetComponent(out FMODAudioSource source))
                return source;

            var child = new GameObject(label);
            child.transform.SetParent(transform, false);

            FMODAudioSource created = child.AddComponent<FMODAudioSource>();
            created.playOnAwake = false;
            return created;
        }

        /// <summary>id를 EventReference로 해석합니다. Key 모드면 해당 버스의 KeyList에서 조회합니다.</summary>
        private bool TryResolve(AudioId id, out EventReference reference)
        {
            reference = default;

            if (!_byId.TryGetValue(id, out AudioDatabase.Entry entry))
                return false;

            if (entry.useKey)
                return TryResolveKey(entry.bus, entry.key, out reference);

            reference = entry.reference;
            return !reference.IsNull;
        }

        /// <summary>버스에 해당하는 전역 KeyList에서 Key를 조회합니다. (미스 시 에러 로그를 피하려 TryGetValue 사용)</summary>
        private static bool TryResolveKey(AudioBusType bus, string key, out EventReference reference)
        {
            reference = default;

            if (string.IsNullOrEmpty(key))
                return false;

            switch (bus)
            {
                case AudioBusType.Bgm:
                    return BGMKeyList.Instance != null && BGMKeyList.Instance.TryGetValue(key, out reference);
                case AudioBusType.Sfx:
                    return SFXKeyList.Instance != null && SFXKeyList.Instance.TryGetValue(key, out reference);
                case AudioBusType.Amb:
                    return AMBKeyList.Instance != null && AMBKeyList.Instance.TryGetValue(key, out reference);
                default:
                    return false;
            }
        }

        /// <summary>VCA 볼륨을 설정합니다. 뱅크/VCA가 아직 준비되지 않았으면 조용히 무시합니다(뱅크 빌드 후 정상 동작).</summary>
        private static void SetVcaVolume(string path, float volume)
        {
            if (!RuntimeManager.IsInitialized)
                return;

            try
            {
                VCA vca = RuntimeManager.GetVCA(path);
                if (vca.isValid())
                    vca.setVolume(volume);
            }
            catch (Exception)
            {
                // VCA 미존재/뱅크 미로드 상태. FMOD Studio에서 vca:/BGM·vca:/SFX 오써링 및 뱅크 빌드 후 동작합니다.
            }
        }
    }
}
