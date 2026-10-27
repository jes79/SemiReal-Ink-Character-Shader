using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

// 쉐이더 랩의 "상태"를 한곳에서 관리한다. UI는 이 클래스의 함수만 부른다.
// - A/B 비교: 각 쪽에 원본(URP Lit) 또는 스타일 프리셋을 고른다
// - 맵·기능 스위치, 디버그 보기
// - 환경(하늘·환경광·안개·후처리·반사), 조명 프리셋, 후처리 효과 켜고 끄기
// - 애니메이션 테스트, 커스터마이징(옷·무기)
public class ShaderLabController : MonoBehaviour
{
    [Serializable]
    public class LightingPreset
    {
        public string displayName = "Lighting";
        [TextArea] public string description;
        [Tooltip("켜면 환경 프리셋의 햇빛 방향을 그대로 쓴다")]
        public bool useEnvironmentSun = true;
        [Tooltip("x = 높이 각도, y = 방향 각도 (캐릭터 정면 = +Z, 180 = 정면에서 비춤)")]
        public Vector2 sunAngles = new(35f, 180f);
        public float intensityScale = 1f;
        [Tooltip("보조 포인트 라이트(따뜻한 키·차가운 역광)를 켠다")]
        public bool accentLights;
    }

    public const int LitStyle = -1;   // A/B 쪽 스타일 번호 -1 = 원본(URP Lit)

    [Header("장면")]
    public LabCharacter characterA;
    public LabCharacter characterB;
    public CompareView compareView;
    public OrbitCamera orbitCamera;
    public Light sun;
    public Volume volume;
    public ReflectionProbe reflectionProbe;
    public List<Light> accentLights = new();

    [Header("프리셋")]
    public List<StylePreset> styles = new();
    public int defaultStyleB = 1;
    public List<EnvironmentPreset> environments = new();
    public List<LightingPreset> lightingPresets = new();

    // ---- 현재 상태 (UI가 읽는다) ----
    public int StyleA { get; private set; } = LitStyle;
    public int StyleB { get; private set; }
    public int Environment { get; private set; }
    public int Lighting { get; private set; }
    public float SunYawOffset { get; private set; }
    public bool PostProcessing { get; private set; } = true;
    public LabDebugView DebugView { get; private set; }
    public AnimationTester Animation { get; private set; }
    public IReadOnlyDictionary<LabMap, bool> Maps => maps;
    public IReadOnlyDictionary<LabFeature, bool> Features => features;

    // 상태가 바뀌면 UI가 다시 그린다
    public event Action Changed;

    readonly Dictionary<LabMap, bool> maps = Enum.GetValues(typeof(LabMap)).Cast<LabMap>().ToDictionary(m => m, _ => true);
    readonly Dictionary<LabFeature, bool> features = Enum.GetValues(typeof(LabFeature)).Cast<LabFeature>().ToDictionary(f => f, _ => true);
    readonly List<(Transform t, Vector3 pos, Quaternion rot)> characterHomes = new();

    IEnumerable<LabCharacter> Characters
    {
        get
        {
            if (characterA != null) yield return characterA;
            if (characterB != null) yield return characterB;
        }
    }

    void Start()
    {
        foreach (var c in Characters)
        {
            c.Initialize();
            characterHomes.Add((c.transform, c.transform.position, c.transform.rotation));
        }
        Animation = new AnimationTester(Characters.Select(c => c.animator));

        StyleB = Mathf.Clamp(defaultStyleB, 0, styles.Count - 1);
        RefreshCharacters();
        if (environments.Count > 0)
            SetEnvironment(0);
        SetLighting(0);
        SetPostProcessing(true);
        SetDebugView(LabDebugView.Off);
    }

    void Update() => Animation?.Tick();

    void OnDestroy()
    {
        Animation?.Dispose();
        Shader.SetGlobalFloat(ShaderLabProperties.DebugView, 0f);
    }

    void NotifyChanged() => Changed?.Invoke();

    // ---------------- 스타일 · A/B ----------------
    public string StyleName(int style) => style == LitStyle ? "원본 (URP Lit)" : styles[style].displayName;

    public void SetStyleA(int style) { StyleA = style; RefreshCharacters(); }
    public void SetStyleB(int style) { StyleB = style; RefreshCharacters(); }

    public void SetMap(LabMap map, bool on) { maps[map] = on; RefreshCharacters(); }
    public void SetFeature(LabFeature feature, bool on) { features[feature] = on; RefreshCharacters(); }

    void RefreshCharacters()
    {
        Apply(characterA, StyleA);
        Apply(characterB, StyleB);
        NotifyChanged();
    }

    void Apply(LabCharacter character, int style)
    {
        if (character == null)
            return;
        bool lab = style != LitStyle && style < styles.Count;
        character.UseLabShader(lab);
        if (!lab)
            return;
        character.ApplyStyle(styles[style]);
        character.ApplySwitches(maps, features);
    }

    public void SetDebugView(LabDebugView view)
    {
        DebugView = view;
        Shader.SetGlobalFloat(ShaderLabProperties.DebugView, (float)view);
        NotifyChanged();
    }

    // ---------------- 환경 ----------------
    public void SetEnvironment(int index)
    {
        if (index < 0 || index >= environments.Count)
            return;
        Environment = index;
        var env = environments[index];

        RenderSettings.skybox = env.skybox;
        RenderSettings.ambientMode = AmbientMode.Trilight;
        RenderSettings.ambientSkyColor = env.ambientSky;
        RenderSettings.ambientEquatorColor = env.ambientEquator;
        RenderSettings.ambientGroundColor = env.ambientGround;
        RenderSettings.fog = env.fog;
        RenderSettings.fogMode = FogMode.ExponentialSquared;
        RenderSettings.fogColor = env.fogColor;
        RenderSettings.fogDensity = env.fogDensity;
        DynamicGI.UpdateEnvironment();

        if (reflectionProbe != null && env.reflection != null)
        {
            reflectionProbe.mode = ReflectionProbeMode.Custom;
            reflectionProbe.customBakedTexture = env.reflection;
        }

        foreach (var other in environments)
        {
            if (string.IsNullOrEmpty(other.propsRoot))
                continue;
            var root = FindSceneObject(other.propsRoot);
            if (root != null)
                root.SetActive(other == env);
        }

        if (volume != null)
        {
            volume.sharedProfile = env.profile;
            // volume.profile을 읽으면 복사본이 생긴다 → 플레이 중 후처리를 끄고 켜도 프로필 파일은 그대로
            if (env.profile != null)
                _ = volume.profile;
        }

        ApplyLighting();
        NotifyChanged();
    }

    static GameObject FindSceneObject(string name)
    {
        // 꺼져 있는 오브젝트도 찾아야 해서 GameObject.Find 대신 장면 루트에서 찾는다
        foreach (var root in UnityEngine.SceneManagement.SceneManager.GetActiveScene().GetRootGameObjects())
            if (root.name == name)
                return root;
        return null;
    }

    // ---------------- 조명 ----------------
    public void SetLighting(int index)
    {
        if (index < 0 || index >= lightingPresets.Count)
            return;
        Lighting = index;
        ApplyLighting();
        NotifyChanged();
    }

    public void SetSunYawOffset(float degrees)
    {
        SunYawOffset = degrees;
        ApplyLighting();
        NotifyChanged();
    }

    void ApplyLighting()
    {
        if (sun == null || environments.Count == 0)
            return;
        var env = environments[Environment];
        var preset = lightingPresets.Count > 0 ? lightingPresets[Lighting] : null;
        Vector2 angles = preset == null || preset.useEnvironmentSun ? env.sunAngles : preset.sunAngles;
        sun.transform.rotation = Quaternion.Euler(angles.x, angles.y + SunYawOffset, 0f);
        sun.color = env.sunColor;
        sun.intensity = env.sunIntensity * (preset?.intensityScale ?? 1f);
        sun.shadowStrength = env.shadowStrength;
        bool accents = preset != null && preset.accentLights;
        foreach (var l in accentLights)
            if (l != null)
                l.gameObject.SetActive(accents);
    }

    // ---------------- 후처리 ----------------
    public void SetPostProcessing(bool on)
    {
        PostProcessing = on;
        if (compareView != null)
            compareView.SetPostProcessing(on);
        NotifyChanged();
    }

    // 지금 환경의 후처리 효과 목록 (실행 중 복사본)
    public IReadOnlyList<VolumeComponent> PostEffects =>
        volume != null && volume.HasInstantiatedProfile() ? volume.profile.components : Array.Empty<VolumeComponent>();

    public void SetPostEffect(VolumeComponent component, bool on)
    {
        component.active = on;
        NotifyChanged();
    }

    public void SetTonemapping(TonemappingMode mode)
    {
        if (volume == null || !volume.HasInstantiatedProfile())
            return;
        if (!volume.profile.TryGet(out Tonemapping tone))
            tone = volume.profile.Add<Tonemapping>();
        tone.active = true;
        tone.mode.Override(mode);
        NotifyChanged();
    }

    public TonemappingMode CurrentTonemapping =>
        volume != null && volume.HasInstantiatedProfile() && volume.profile.TryGet(out Tonemapping tone) && tone.active
            ? tone.mode.value
            : TonemappingMode.None;

    public float Exposure =>
        volume != null && volume.HasInstantiatedProfile() && volume.profile.TryGet(out ColorAdjustments c) ? c.postExposure.value : 0f;

    public void SetExposure(float ev)
    {
        if (volume == null || !volume.HasInstantiatedProfile())
            return;
        if (!volume.profile.TryGet(out ColorAdjustments color))
            color = volume.profile.Add<ColorAdjustments>();
        color.active = true;
        color.postExposure.Override(ev);
        NotifyChanged();
    }

    // ---------------- 애니메이션 ----------------
    // 클립 목록은 B(새 쉐이더) 캐릭터 기준. 원본 캐릭터도 같은 클립을 같이 재생한다
    public IReadOnlyList<AnimationClip> Clips => characterB != null ? characterB.clips : Array.Empty<AnimationClip>();

    public void PlayClip(AnimationClip clip)
    {
        Animation.Play(clip);
        NotifyChanged();
    }

    public void StopAnimation()
    {
        Animation.Stop();
        ResetCharacterPositions();
        NotifyChanged();
    }

    public void SetRootMotion(bool on)
    {
        Animation.SetRootMotion(on);
        if (!on)
            ResetCharacterPositions();
        NotifyChanged();
    }

    void ResetCharacterPositions()
    {
        foreach (var (t, pos, rot) in characterHomes)
            t.SetPositionAndRotation(pos, rot);
    }

    // ---------------- 커스터마이징 ----------------
    public IReadOnlyList<LabCharacter.CustomizationSlot> Slots =>
        characterB != null ? characterB.slots : Array.Empty<LabCharacter.CustomizationSlot>();

    public int GetSlotSelection(string slot) => characterB != null ? characterB.GetSelection(slot) : -1;

    public void SelectSlotOption(string slot, int index)
    {
        // 두 캐릭터가 같은 옷·무기를 입어야 비교가 된다
        foreach (var c in Characters)
            c.SelectOption(slot, index);
        // 새로 켜진 옷의 머티리얼에도 스타일·스위치를 다시 건다
        RefreshCharacters();
    }
}
