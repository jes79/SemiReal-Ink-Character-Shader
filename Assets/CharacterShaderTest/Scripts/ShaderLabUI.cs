using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using UnityEngine.TextCore.Text;
using UnityEngine.UIElements;

// 쉐이더 랩 조작 패널 (UI Toolkit).
// 화면 구조는 ShaderLab.uxml, 색·모양은 ShaderLab.uss. 이 스크립트는 탭 내용을 만들고 ShaderLabController에 연결한다
[RequireComponent(typeof(UIDocument))]
public class ShaderLabUI : MonoBehaviour
{
    public ShaderLabController lab;

    static readonly string[] KoreanFonts = { "Malgun Gothic", "Apple SD Gothic Neo", "Noto Sans CJK KR", "NanumGothic" };

    static readonly (LabMap map, string label, string help)[] MapInfo =
    {
        (LabMap.Base, "베이스 컬러 (디퓨즈)", "끄면 색 없이 Base Color만 남는다"),
        (LabMap.Normal, "노멀 맵", "끄면 표면의 잔주름·요철이 사라진다"),
        (LabMap.Metallic, "메탈릭 맵", "끄면 슬라이더 값(Metallic)만 쓴다"),
        (LabMap.Roughness, "러프니스 맵", "끄면 슬라이더 값(Smoothness)만 쓴다"),
        (LabMap.Occlusion, "앰비언트 오클루전 (AO)", "끄면 틈새·접힌 곳의 그늘이 사라진다"),
        (LabMap.Emission, "이미시브 맵", "끄면 스스로 빛나는 부분(룬 문자·수정)이 꺼진다"),
    };

    static readonly (LabFeature feature, string label, string help)[] FeatureInfo =
    {
        (LabFeature.Specular, "반사 하이라이트", "빛이 반짝 맺히는 점"),
        (LabFeature.EnvReflection, "환경 반사", "금속·보석에 하늘·주변이 비치는 것"),
        (LabFeature.FillLight, "보조광 (필 라이트)", "어두운 쪽을 살짝 밝히는 카메라 기준 빛"),
        (LabFeature.RimLight, "역광 (림 라이트)", "윤곽을 따라 생기는 밝은 테두리"),
        (LabFeature.Subsurface, "피부 SSS", "빛이 피부 속에서 퍼지는 붉은 기운"),
        (LabFeature.Sheen, "천 광택 (Sheen)", "천 가장자리의 솜털 광택"),
        (LabFeature.ShadowTint, "그늘 색", "그늘을 차갑게(파랗게) 물들이기"),
        (LabFeature.Outline, "먹 아웃라인", "붓으로 그린 듯한 외곽선"),
    };

    static readonly (LabDebugView view, string label)[] DebugInfo =
    {
        (LabDebugView.Off, "보통 화면"),
        (LabDebugView.BaseColor, "베이스 컬러만"),
        (LabDebugView.Normal, "노멀 (방향 색)"),
        (LabDebugView.Metallic, "메탈릭 (흰색 = 금속)"),
        (LabDebugView.Roughness, "러프니스 (흰색 = 거침)"),
        (LabDebugView.Occlusion, "AO (검을수록 가려짐)"),
        (LabDebugView.Emission, "이미시브"),
        (LabDebugView.LightingOnly, "빛만 (회색 점토)"),
        (LabDebugView.SpecularOnly, "반사광만"),
        (LabDebugView.MaterialMasks, "재질 판별 (노랑 금·흰 은·초록 보석)"),
    };

    VisualElement root, panel, tabBar, overlay, handle;
    ScrollView content;
    Button expandButton;
    Label labelA, labelB;
    Image overlayImage;
    FontAsset koreanFont;

    readonly List<(string name, Action<VisualElement> build)> tabs = new();
    readonly List<Button> tabButtons = new();
    int currentTab;
    bool draggingWipe;
    int builtEnvironment = -1;

    // 상태가 바뀔 때 다시 칠할 것들
    readonly List<Action> refreshers = new();
    // 매 프레임 갱신 (애니메이션 시간)
    Action tick;

    CompareView Compare => lab.compareView;

    void OnEnable()
    {
        var doc = GetComponent<UIDocument>();
        // 패널 밖 빈 곳은 마우스를 통과시킨다 (카메라 조작)
        doc.rootVisualElement.pickingMode = PickingMode.Ignore;
        root = doc.rootVisualElement.Q("root");
        panel = root.Q("side-panel");
        tabBar = root.Q("tab-bar");
        content = root.Q<ScrollView>("tab-content");
        overlay = root.Q("wipe-overlay");
        handle = root.Q("wipe-handle");
        labelA = root.Q<Label>("label-a");
        labelB = root.Q<Label>("label-b");
        expandButton = root.Q<Button>("expand-button");

        ApplyKoreanFont(doc.rootVisualElement);

        overlayImage = new Image { scaleMode = ScaleMode.StretchToFill, pickingMode = PickingMode.Ignore };
        overlay.Add(overlayImage);

        root.Q<Button>("collapse-button").clicked += () => SetPanelVisible(false);
        expandButton.clicked += () => SetPanelVisible(true);
        SetPanelVisible(true);

        handle.RegisterCallback<PointerDownEvent>(e => { draggingWipe = true; handle.CapturePointer(e.pointerId); e.StopPropagation(); });
        handle.RegisterCallback<PointerMoveEvent>(e =>
        {
            if (draggingWipe && root.resolvedStyle.width > 0f)
                Compare.wipe = Mathf.Clamp01(e.position.x / root.resolvedStyle.width);
        });
        handle.RegisterCallback<PointerUpEvent>(e => { draggingWipe = false; handle.ReleasePointer(e.pointerId); });

        tabs.Clear();
        tabs.Add(("비교", BuildCompareTab));
        tabs.Add(("쉐이더", BuildShaderTab));
        tabs.Add(("맵·보기", BuildMapsTab));
        tabs.Add(("환경", BuildEnvironmentTab));
        tabs.Add(("조명", BuildLightingTab));
        tabs.Add(("후처리", BuildPostTab));
        tabs.Add(("애니메이션", BuildAnimationTab));
        tabs.Add(("커스텀", BuildCustomizeTab));
        tabBar.Clear();
        tabButtons.Clear();
        for (int i = 0; i < tabs.Count; i++)
        {
            int index = i;
            var b = new Button(() => SelectTab(index)) { text = tabs[i].name };
            b.AddToClassList("tab-button");
            tabBar.Add(b);
            tabButtons.Add(b);
        }

        if (lab.orbitCamera != null)
            lab.orbitCamera.isPointerOverUi = IsPointerOverUi;
        lab.Changed += OnLabChanged;
        SelectTab(0);
    }

    void OnDisable()
    {
        if (lab != null)
            lab.Changed -= OnLabChanged;
        if (koreanFont != null)
            Destroy(koreanFont);
    }

    // 기본 UI 글꼴에는 한글이 없어서, 컴퓨터에 깔린 한글 글꼴로 글꼴 에셋을 만든다 (글꼴 파일을 프로젝트에 넣지 않아도 된다)
    void ApplyKoreanFont(VisualElement element)
    {
        foreach (var family in KoreanFonts)
        {
            koreanFont = FontAsset.CreateFontAsset(family, "Regular");
            if (koreanFont != null)
                break;
        }
        if (koreanFont == null)
        {
            Debug.LogWarning("한글 글꼴을 찾지 못했습니다. UI 글자가 네모로 보이면 PanelSettings의 Text Settings에 한글 글꼴을 넣으세요.");
            return;
        }
        element.style.unityFontDefinition = FontDefinition.FromSDFFont(koreanFont);
    }

    void SetPanelVisible(bool visible)
    {
        panel.style.display = visible ? DisplayStyle.Flex : DisplayStyle.None;
        expandButton.style.display = visible ? DisplayStyle.None : DisplayStyle.Flex;
    }

    bool IsPointerOverUi(Vector2 screenPos)
    {
        if (draggingWipe)
            return true;
        if (root?.panel == null)
            return false;
        // Input System 좌표(아래가 0) → 패널 좌표(위가 0)
        var panelPos = RuntimePanelUtils.ScreenToPanel(root.panel, new Vector2(screenPos.x, Screen.height - screenPos.y));
        var picked = root.panel.Pick(panelPos);
        return picked != null;
    }

    void SelectTab(int index)
    {
        currentTab = index;
        for (int i = 0; i < tabButtons.Count; i++)
            tabButtons[i].EnableInClassList("tab-button--selected", i == index);
        RebuildContent();
    }

    void RebuildContent()
    {
        content.Clear();
        refreshers.Clear();
        tick = null;
        builtEnvironment = lab.Environment;
        tabs[currentTab].build(content);
        Refresh();
    }

    void OnLabChanged()
    {
        // 환경이 바뀌면 후처리 효과 목록이 달라진다 → 탭을 다시 만든다
        if (builtEnvironment != lab.Environment)
            RebuildContent();
        else
            Refresh();
    }

    void Refresh()
    {
        foreach (var r in refreshers)
            r();
    }

    void Update()
    {
        HandleShortcuts();
        UpdateCompareOverlay();
        tick?.Invoke();
    }

    void HandleShortcuts()
    {
        var k = Keyboard.current;
        if (k == null)
            return;
        bool compareChanged = false;
        if (k.digit1Key.wasPressedThisFrame) { Compare.mode = CompareView.Mode.Wipe; compareChanged = true; }
        if (k.digit2Key.wasPressedThisFrame) { Compare.mode = CompareView.Mode.SideBySide; compareChanged = true; }
        if (k.digit3Key.wasPressedThisFrame) { Compare.mode = CompareView.Mode.AOnly; compareChanged = true; }
        if (k.digit4Key.wasPressedThisFrame) { Compare.mode = CompareView.Mode.BOnly; compareChanged = true; }
        if (k.tabKey.wasPressedThisFrame) { Compare.Flip(); compareChanged = true; }
        if (k.pKey.wasPressedThisFrame) lab.SetPostProcessing(!lab.PostProcessing);
        if (k.mKey.wasPressedThisFrame)
            lab.SetDebugView(lab.DebugView == LabDebugView.MaterialMasks ? LabDebugView.Off : LabDebugView.MaterialMasks);
        if (k.oKey.wasPressedThisFrame) lab.SetFeature(LabFeature.Outline, !lab.Features[LabFeature.Outline]);
        if (k.spaceKey.wasPressedThisFrame && lab.orbitCamera != null) { lab.orbitCamera.AutoRotate = !lab.orbitCamera.AutoRotate; compareChanged = true; }
        if (k.fKey.wasPressedThisFrame && lab.orbitCamera != null) lab.orbitCamera.ResetView(snap: false);
        if (k.hKey.wasPressedThisFrame) SetPanelVisible(panel.style.display == DisplayStyle.None);
        if (compareChanged)
            Refresh();
    }

    // 와이프 덮개·손잡이·이름표 위치
    void UpdateCompareOverlay()
    {
        var rt = Compare.OverlayTexture;
        bool wipe = Compare.mode == CompareView.Mode.Wipe;
        float split = Compare.mode == CompareView.Mode.BOnly ? 0f : Compare.wipe;

        bool showOverlay = rt != null && split < 1f;
        overlay.style.display = showOverlay ? DisplayStyle.Flex : DisplayStyle.None;
        if (showOverlay)
        {
            overlayImage.image = rt;
            overlay.style.left = Length.Percent(split * 100f);
            // 이미지의 오른쪽 부분만 보이게 텍스처 좌표를 자른다
            overlayImage.uv = new Rect(split, 0f, 1f - split, 1f);
        }

        handle.style.display = wipe ? DisplayStyle.Flex : DisplayStyle.None;
        handle.style.left = Length.Percent(Compare.wipe * 100f);

        string a = "A  " + lab.StyleName(lab.StyleA);
        string b = "B  " + lab.StyleName(lab.StyleB);
        labelA.text = a;
        labelB.text = b;
        switch (Compare.mode)
        {
            case CompareView.Mode.Wipe:
                PlaceLabel(labelA, Compare.wipe, rightAligned: true, visible: Compare.wipe > 0.15f);
                PlaceLabel(labelB, Compare.wipe, rightAligned: false, visible: Compare.wipe < 0.85f);
                break;
            case CompareView.Mode.SideBySide:
                PlaceLabel(labelA, 0.5f, rightAligned: true, visible: true);
                PlaceLabel(labelB, 0.5f, rightAligned: false, visible: true);
                break;
            case CompareView.Mode.AOnly:
                PlaceLabel(labelA, 0.5f, rightAligned: false, visible: true);
                PlaceLabel(labelB, 0f, false, visible: false);
                break;
            default:
                PlaceLabel(labelA, 0f, false, visible: false);
                PlaceLabel(labelB, 0.5f, rightAligned: false, visible: true);
                break;
        }
    }

    static void PlaceLabel(Label label, float x, bool rightAligned, bool visible)
    {
        label.style.display = visible ? DisplayStyle.Flex : DisplayStyle.None;
        if (rightAligned)
        {
            label.style.left = StyleKeyword.Auto;
            label.style.right = Length.Percent((1f - x) * 100f);
            label.style.marginRight = 12f;
            label.style.marginLeft = 0f;
        }
        else
        {
            label.style.right = StyleKeyword.Auto;
            label.style.left = Length.Percent(x * 100f);
            label.style.marginLeft = 12f;
            label.style.marginRight = 0f;
        }
    }

    // ================= 탭 =================
    void BuildCompareTab(VisualElement c)
    {
        Section(c, "비교 방식");
        var modes = new[] { (CompareView.Mode.Wipe, "와이프 (1)"), (CompareView.Mode.SideBySide, "나란히 (2)"),
                            (CompareView.Mode.AOnly, "A만 (3)"), (CompareView.Mode.BOnly, "B만 (4)") };
        var row = Row(c);
        foreach (var (mode, label) in modes)
        {
            var m = mode;
            var b = Segment(row, label, () => { Compare.mode = m; Refresh(); });
            refreshers.Add(() => b.EnableInClassList("segment--selected", Compare.mode == m));
        }
        var flip = new Button(() => { Compare.Flip(); Refresh(); }) { text = "A ↔ B 빠르게 전환 (Tab)" };
        flip.AddToClassList("wide-button");
        c.Add(flip);

        var wipeSlider = new Slider("와이프 위치", 0f, 1f);
        wipeSlider.RegisterValueChangedCallback(e => Compare.wipe = e.newValue);
        c.Add(wipeSlider);
        tick += () => wipeSlider.SetValueWithoutNotify(Compare.wipe);

        Section(c, "A / B 에 무엇을 놓을까");
        Help(c, "왼쪽(A)과 오른쪽(B)에 원본 또는 스타일 프리셋을 골라 나란히 비교한다. 맵·기능 스위치는 두 쪽에 똑같이 걸린다");
        var choices = new List<string> { lab.StyleName(ShaderLabController.LitStyle) };
        for (int i = 0; i < lab.styles.Count; i++)
            choices.Add(lab.StyleName(i));
        var dropA = new DropdownField("A (왼쪽)", choices, 0);
        dropA.RegisterValueChangedCallback(_ => lab.SetStyleA(dropA.index - 1));
        var dropB = new DropdownField("B (오른쪽)", choices, 0);
        dropB.RegisterValueChangedCallback(_ => lab.SetStyleB(dropB.index - 1));
        c.Add(dropA);
        c.Add(dropB);
        refreshers.Add(() =>
        {
            dropA.SetValueWithoutNotify(choices[lab.StyleA + 1]);
            dropB.SetValueWithoutNotify(choices[lab.StyleB + 1]);
        });

        Section(c, "카메라");
        if (lab.orbitCamera != null)
        {
            var auto = LabToggle(c, "자동 회전 (Space)", null, v => lab.orbitCamera.AutoRotate = v);
            refreshers.Add(() => auto.SetValueWithoutNotify(lab.orbitCamera.AutoRotate));
            var reset = new Button(() => lab.orbitCamera.ResetView(snap: false)) { text = "처음 시점 (F)" };
            reset.AddToClassList("wide-button");
            c.Add(reset);
        }
    }

    void BuildShaderTab(VisualElement c)
    {
        Section(c, "스타일 프리셋 (B 쪽)");
        Help(c, "같은 머티리얼에 값만 바꿔 리얼 ↔ 세미리얼 ↔ 스타일라이즈드를 오간다. A 쪽은 '비교' 탭에서 고른다");
        for (int i = 0; i < lab.styles.Count; i++)
        {
            int index = i;
            var card = Card(c, lab.styles[i].displayName, lab.styles[i].description, () => lab.SetStyleB(index));
            refreshers.Add(() => card.EnableInClassList("card--selected", lab.StyleB == index));
        }

        Section(c, "기능 켜고 끄기");
        Help(c, "끄면 그 기능만 빠진다. 프리셋에서 원래 꺼져 있는 기능은 켜도 생기지 않는다");
        foreach (var (feature, label, help) in FeatureInfo)
        {
            var f = feature;
            var t = LabToggle(c, label, help, v => lab.SetFeature(f, v));
            refreshers.Add(() => t.SetValueWithoutNotify(lab.Features[f]));
        }
    }

    void BuildMapsTab(VisualElement c)
    {
        Section(c, "텍스처 맵 켜고 끄기");
        Help(c, "맵을 하나씩 꺼 보며 각 맵이 화면에 무엇을 더하는지 확인한다. 머티리얼에 없는 맵은 켜도 변화가 없다");
        foreach (var (map, label, help) in MapInfo)
        {
            var m = map;
            var t = LabToggle(c, label, help, v => lab.SetMap(m, v));
            refreshers.Add(() => t.SetValueWithoutNotify(lab.Maps[m]));
        }

        Section(c, "디버그 보기");
        Help(c, "화면을 이루는 재료를 한 장씩 본다. 새 쉐이더 쪽에만 적용된다");
        var row = Row(c);
        foreach (var (view, label) in DebugInfo)
        {
            var v = view;
            var b = Segment(row, label, () => lab.SetDebugView(v));
            b.style.minWidth = Length.Percent(45f);
            refreshers.Add(() => b.EnableInClassList("segment--selected", lab.DebugView == v));
        }
    }

    void BuildEnvironmentTab(VisualElement c)
    {
        Section(c, "환경 프리셋");
        Help(c, "하늘·햇빛·환경광·안개·후처리·반사를 한 번에 바꾼다");
        for (int i = 0; i < lab.environments.Count; i++)
        {
            int index = i;
            var env = lab.environments[i];
            var card = Card(c, env.displayName, env.description, () => lab.SetEnvironment(index));
            refreshers.Add(() => card.EnableInClassList("card--selected", lab.Environment == index));
        }
    }

    void BuildLightingTab(VisualElement c)
    {
        Section(c, "조명 프리셋");
        Help(c, "같은 환경에서 빛의 방향만 바꿔 명암·SSS·림 라이트가 어떻게 달라지는지 본다");
        for (int i = 0; i < lab.lightingPresets.Count; i++)
        {
            int index = i;
            var preset = lab.lightingPresets[i];
            var card = Card(c, preset.displayName, preset.description, () => lab.SetLighting(index));
            refreshers.Add(() => card.EnableInClassList("card--selected", lab.Lighting == index));
        }

        Section(c, "햇빛 돌리기");
        var yaw = new Slider("방향 (도)", -180f, 180f);
        yaw.RegisterValueChangedCallback(e => lab.SetSunYawOffset(e.newValue));
        c.Add(yaw);
        refreshers.Add(() => yaw.SetValueWithoutNotify(lab.SunYawOffset));
    }

    void BuildPostTab(VisualElement c)
    {
        Section(c, "후처리 전체");
        var master = LabToggle(c, "후처리 켜기 (P)", "끄면 색보정·블룸·비네트 등이 모두 빠진 원래 렌더링", lab.SetPostProcessing);
        refreshers.Add(() => master.SetValueWithoutNotify(lab.PostProcessing));

        Section(c, "톤매핑 · 노출");
        Help(c, "톤매핑은 밝은 색을 화면에 담는 방식. ACES는 영화처럼 대비가 강하고, Neutral은 색을 덜 비튼다");
        var toneChoices = new List<string> { "None", "Neutral", "ACES" };
        var tone = new DropdownField("톤매핑", toneChoices, 0);
        tone.RegisterValueChangedCallback(_ => lab.SetTonemapping((TonemappingMode)tone.index));
        c.Add(tone);
        refreshers.Add(() => tone.SetValueWithoutNotify(toneChoices[(int)lab.CurrentTonemapping]));
        var exposure = new Slider("노출 (EV)", -3f, 3f);
        exposure.RegisterValueChangedCallback(e => lab.SetExposure(e.newValue));
        c.Add(exposure);
        refreshers.Add(() => exposure.SetValueWithoutNotify(lab.Exposure));

        Section(c, "효과 하나씩 끄기");
        Help(c, "지금 환경의 후처리 프로필에 들어 있는 효과들");
        foreach (var effect in lab.PostEffects)
        {
            var e = effect;
            var t = LabToggle(c, PostEffectName(e), null, v => lab.SetPostEffect(e, v));
            refreshers.Add(() => t.SetValueWithoutNotify(e.active));
        }
    }

    static string PostEffectName(VolumeComponent c) => c switch
    {
        Tonemapping => "톤매핑",
        WhiteBalance => "화이트 밸런스 (색온도)",
        ColorAdjustments => "색 조정 (노출·대비·채도)",
        ShadowsMidtonesHighlights => "어두운 곳·중간·밝은 곳 색",
        Bloom => "블룸 (빛 번짐)",
        Vignette => "비네트 (가장자리 어둡게)",
        FilmGrain => "필름 그레인 (입자)",
        DepthOfField => "피사계 심도 (배경 흐림)",
        ChromaticAberration => "색수차",
        LiftGammaGain => "리프트·감마·게인",
        _ => c.GetType().Name,
    };

    void BuildAnimationTab(VisualElement c)
    {
        var clips = lab.Clips;
        Section(c, "클립");
        if (clips.Count == 0)
        {
            Help(c, "이 캐릭터에는 아직 애니메이션 클립이 없다. 캐릭터의 LabCharacter > Clips에 클립을 넣으면 여기에 나온다");
            return;
        }
        Help(c, "원본과 새 쉐이더 캐릭터가 같은 동작을 같이 재생한다. 움직이는 동안 명암·아웃라인이 어떻게 변하는지 본다");
        var names = new List<string>();
        foreach (var clip in clips)
            names.Add(clip != null ? clip.name : "(비어 있음)");
        var drop = new DropdownField("클립", names, -1);
        drop.RegisterValueChangedCallback(_ => { if (drop.index >= 0) lab.PlayClip(clips[drop.index]); });
        c.Add(drop);
        refreshers.Add(() =>
        {
            int i = -1;
            for (int k = 0; k < clips.Count; k++)
                if (clips[k] == lab.Animation.Clip) i = k;
            drop.SetValueWithoutNotify(i >= 0 ? names[i] : "");
        });

        var row = Row(c);
        var play = Segment(row, "재생", () => { lab.Animation.SetPlaying(!lab.Animation.Playing); Refresh(); });
        Segment(row, "◀ 1프레임", () => { lab.Animation.StepFrames(-1); Refresh(); });
        Segment(row, "1프레임 ▶", () => { lab.Animation.StepFrames(1); Refresh(); });
        Segment(row, "멈춤 (기본 자세)", lab.StopAnimation);
        refreshers.Add(() => play.text = lab.Animation.Playing ? "일시정지" : "재생");

        var time = new Slider("시간", 0f, 1f);
        time.RegisterValueChangedCallback(e =>
        {
            lab.Animation.SetPlaying(false);
            lab.Animation.Seek(e.newValue * lab.Animation.Length);
            Refresh();
        });
        c.Add(time);
        var timeLabel = new Label();
        timeLabel.AddToClassList("time-label");
        c.Add(timeLabel);
        tick += () =>
        {
            float len = lab.Animation.Length;
            time.SetValueWithoutNotify(len > 0f ? lab.Animation.Time / len : 0f);
            timeLabel.text = $"{lab.Animation.Time:0.00}s / {len:0.00}s";
        };

        var speed = new Slider("속도", 0f, 2f);
        speed.RegisterValueChangedCallback(e => lab.Animation.SetSpeed(e.newValue));
        c.Add(speed);
        refreshers.Add(() => speed.SetValueWithoutNotify(lab.Animation.Speed));

        var loop = LabToggle(c, "반복 재생", null, v => lab.Animation.Loop = v);
        refreshers.Add(() => loop.SetValueWithoutNotify(lab.Animation.Loop));
        var root = LabToggle(c, "루트 모션 (제자리에서 벗어나 이동)", "끄면 제자리에서 동작만 한다", lab.SetRootMotion);
        refreshers.Add(() => root.SetValueWithoutNotify(lab.Animation.RootMotion));
    }

    void BuildCustomizeTab(VisualElement c)
    {
        var slots = lab.Slots;
        if (slots.Count == 0)
        {
            Section(c, "커스터마이징");
            Help(c, "이 캐릭터에는 아직 바꿀 수 있는 옷·무기가 없다. 캐릭터의 LabCharacter > Slots에 후보를 넣으면 여기에 나온다");
            return;
        }
        foreach (var slot in slots)
        {
            Section(c, slot.name);
            var row = Row(c);
            var s = slot;
            if (slot.allowNone)
            {
                var none = Segment(row, "없음", () => lab.SelectSlotOption(s.name, -1));
                refreshers.Add(() => none.EnableInClassList("segment--selected", lab.GetSlotSelection(s.name) < 0));
            }
            for (int i = 0; i < slot.options.Count; i++)
            {
                int index = i;
                string label = slot.options[i] != null ? slot.options[i].name : "(비어 있음)";
                var b = Segment(row, label, () => lab.SelectSlotOption(s.name, index));
                refreshers.Add(() => b.EnableInClassList("segment--selected", lab.GetSlotSelection(s.name) == index));
            }
        }
    }

    // ================= 작은 도우미 =================
    static void Section(VisualElement parent, string text)
    {
        var l = new Label(text);
        l.AddToClassList("section-title");
        parent.Add(l);
    }

    static void Help(VisualElement parent, string text)
    {
        var l = new Label(text);
        l.AddToClassList("help");
        parent.Add(l);
    }

    static VisualElement Row(VisualElement parent)
    {
        var r = new VisualElement();
        r.AddToClassList("row");
        parent.Add(r);
        return r;
    }

    static Button Segment(VisualElement parent, string text, Action onClick)
    {
        var b = new Button(onClick) { text = text };
        b.AddToClassList("segment");
        parent.Add(b);
        return b;
    }

    static VisualElement Card(VisualElement parent, string title, string text, Action onClick)
    {
        var card = new VisualElement();
        card.AddToClassList("card");
        var t = new Label(title);
        t.AddToClassList("card__title");
        card.Add(t);
        if (!string.IsNullOrEmpty(text))
        {
            var d = new Label(text);
            d.AddToClassList("card__text");
            card.Add(d);
        }
        card.RegisterCallback<ClickEvent>(_ => onClick());
        parent.Add(card);
        return card;
    }

    static Toggle LabToggle(VisualElement parent, string label, string help, Action<bool> onChange)
    {
        var t = new Toggle { text = label };
        t.AddToClassList("lab-toggle");
        t.RegisterValueChangedCallback(e => onChange(e.newValue));
        parent.Add(t);
        if (!string.IsNullOrEmpty(help))
        {
            var h = new Label(help);
            h.AddToClassList("help");
            h.AddToClassList("lab-toggle__help");
            parent.Add(h);
        }
        return t;
    }
}
