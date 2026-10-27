using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using UnityEngine.UIElements;
using Object = UnityEngine.Object;

// 메뉴
//  Tools > Shader Test > Build Test Scene
//      장면을 다시 만든다. 머티리얼·후처리·프리셋에서 조정한 값은 그대로 둔다
//      (아직 프리셋을 한 번도 받지 않은 머티리얼만 프리셋을 넣는다)
//  Tools > Shader Test > Reset Materials And Post To Presets
//      모든 새 쉐이더 머티리얼, 후처리 프로필, 스타일·환경 프리셋을 기본값으로 되돌린 뒤 장면을 만든다
//
// 장면: 쉐이더 랩
//  - 원본(URP Lit)과 새 쉐이더 캐릭터를 같은 자리에 겹쳐 두고, 레이어로 나눠 카메라 두 대로 A/B 비교
//  - 환경 5종(얼어붙은 세상·스튜디오·한낮·노을·달밤), 조명 프리셋, 스타일 프리셋 3종(리얼·세미리얼·스타일라이즈드)
//  - UI Toolkit 조작 패널
public static class SemiRealInkTestSetup
{
    const string Root = "Assets/CharacterShaderTest";
    const string ModelPath = Root + "/Models/FinalPlayer.fbx";
    const string SourceMaterialDir = Root + "/Materials/FinalPlayer";
    const string TargetMaterialDir = Root + "/Materials/FinalPlayer_SemiRealInk";
    const string EnvironmentMaterialDir = Root + "/Materials/Environment";
    const string SettingsDir = Root + "/Settings";
    const string StyleDir = SettingsDir + "/Styles";
    const string EnvironmentDir = SettingsDir + "/Environments";
    const string UiDir = Root + "/UI";
    const string ScenePath = Root + "/Scenes/CharacterShaderTest.unity";
    const string OriginalLayer = "ShaderCompare_Original";
    const string NewLayer = "ShaderCompare_New";

    const string MaskDir = Root + "/Textures/FinalPlayer/Masks";
    // 프리셋 내용을 바꾸면 이 값을 올린다 → 이전 프리셋을 받은 머티리얼도 다음 Build 때 새 프리셋을 받는다
    const string PresetTag = "ShaderTestPreset";
    const string PresetVersion = "4";

    [MenuItem("Tools/Shader Test/Build Test Scene")]
    static void BuildKeepTuning() => Build(resetAssets: false);

    [MenuItem("Tools/Shader Test/Reset Materials And Post To Presets")]
    static void BuildWithReset()
    {
        if (EditorUtility.DisplayDialog("프리셋으로 되돌리기",
                "새 쉐이더 머티리얼, 후처리, 스타일·환경 프리셋에서 조정한 값이 모두 기본값으로 바뀝니다. 계속할까요?", "되돌리기", "취소"))
            Build(resetAssets: true);
    }

    // 배치 모드용: Unity.exe -batchmode -executeMethod SemiRealInkTestSetup.BuildFromCommandLine
    public static void BuildFromCommandLine() => Build(resetAssets: false, interactive: false);

    static void Build(bool resetAssets, bool interactive = true)
    {
        var shader = Shader.Find(ShaderLabProperties.ShaderName);
        if (shader == null)
        {
            Debug.LogError($"쉐이더 '{ShaderLabProperties.ShaderName}'를 찾지 못했습니다. 쉐이더 컴파일 에러를 먼저 확인하세요.");
            return;
        }
        var model = AssetDatabase.LoadAssetAtPath<GameObject>(ModelPath);
        if (model == null)
        {
            Debug.LogError($"모델을 찾지 못했습니다: {ModelPath}");
            return;
        }
        if (interactive && !EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo())
            return;

        EnsureFolder(TargetMaterialDir);
        EnsureFolder(EnvironmentMaterialDir);
        EnsureFolder(StyleDir);
        EnsureFolder(EnvironmentDir);
        EnsureFolder(UiDir);

        int originalLayer = EnsureLayer(OriginalLayer);
        int newLayer = EnsureLayer(NewLayer);
        var remap = CreateMaterials(shader, resetAssets);
        var styles = CreateStylePresets(resetAssets);
        var environments = CreateEnvironmentPresets(resetAssets);
        BuildScene(model, remap, styles, environments, originalLayer, newLayer);
    }

    // ------------------------------------------------------------------
    // 머티리얼
    // ------------------------------------------------------------------
    static Dictionary<Material, Material> CreateMaterials(Shader shader, bool reset)
    {
        var remap = new Dictionary<Material, Material>();
        foreach (var guid in AssetDatabase.FindAssets("t:Material", new[] { SourceMaterialDir }))
        {
            var sourcePath = AssetDatabase.GUIDToAssetPath(guid);
            var source = AssetDatabase.LoadAssetAtPath<Material>(sourcePath);
            var targetPath = $"{TargetMaterialDir}/{Path.GetFileName(sourcePath)}";

            var target = AssetDatabase.LoadAssetAtPath<Material>(targetPath);
            if (target == null)
            {
                target = new Material(shader) { name = source.name };
                AssetDatabase.CreateAsset(target, targetPath);
            }
            if (reset || target.GetTag(PresetTag, false) != PresetVersion)
            {
                // 이전 값(실험하며 바꾼 값 포함)을 쉐이더 기본값으로 비운 뒤 프리셋을 넣는다 → 결과가 항상 같다
                target.shader = shader;
                var defaults = new Material(shader);
                target.CopyPropertiesFromMaterial(defaults);
                Object.DestroyImmediate(defaults);
                CopyTextures(source, target);
                ApplyPreset(target, source.name);
                AssignMaskMap(target);
                target.SetOverrideTag(PresetTag, PresetVersion);
                EditorUtility.SetDirty(target);
            }
            remap[source] = target;
        }
        AssetDatabase.SaveAssets();
        return remap;
    }

    // 원본(URP Lit) 머티리얼의 텍스처를 옮긴다. 메탈릭·AO·이미시브 맵이 있으면 그것도 켠다
    static void CopyTextures(Material source, Material target)
    {
        if (source.HasTexture("_BaseMap"))
        {
            target.SetTexture("_BaseMap", source.GetTexture("_BaseMap"));
            target.SetTextureScale("_BaseMap", source.GetTextureScale("_BaseMap"));
            target.SetTextureOffset("_BaseMap", source.GetTextureOffset("_BaseMap"));
        }
        if (source.HasTexture("_BumpMap"))
            target.SetTexture("_BumpMap", source.GetTexture("_BumpMap"));
        if (source.HasFloat("_BumpScale"))
            target.SetFloat("_BumpScale", source.GetFloat("_BumpScale"));
        if (source.HasColor("_BaseColor"))
            target.SetColor("_BaseColor", source.GetColor("_BaseColor"));

        // URP Lit의 메탈릭 맵: R = 메탈릭, A = 스무스니스
        var metallicGloss = source.HasTexture("_MetallicGlossMap") ? source.GetTexture("_MetallicGlossMap") : null;
        if (metallicGloss != null)
        {
            target.SetTexture("_MetallicMap", metallicGloss);
            target.SetFloat("_MetallicChannel", 0f);
            target.SetFloat("_UseMetallicMap", 1f);
            target.SetTexture("_RoughnessMap", metallicGloss);
            target.SetFloat("_RoughnessChannel", 3f);
            target.SetFloat("_RoughnessIsSmoothness", 1f);
            target.SetFloat("_UseRoughnessMap", 1f);
        }
        var occlusion = source.HasTexture("_OcclusionMap") ? source.GetTexture("_OcclusionMap") : null;
        if (occlusion != null)
        {
            target.SetTexture("_OcclusionMap", occlusion);
            target.SetFloat("_OcclusionChannel", 1f);   // URP Lit은 G 채널을 읽는다
            target.SetFloat("_UseOcclusionMap", 1f);
        }
        var emission = source.HasTexture("_EmissionMap") ? source.GetTexture("_EmissionMap") : null;
        if (emission != null && source.IsKeywordEnabled("_EMISSION"))
        {
            target.SetTexture("_EmissionMap", emission);
            target.SetColor("_EmissionColor", source.GetColor("_EmissionColor"));
            target.SetFloat("_UseEmissionMap", 1f);
        }
    }

    // 굽어 둔 마스크 맵(Textures/FinalPlayer/Masks/<베이스 텍스처 이름>_Mask.png)이 있으면 연결한다
    // R = 금속(금·황동·금실 자수), G = 유리·보석, B = 은·거울
    static void AssignMaskMap(Material m)
    {
        var baseMap = m.GetTexture("_BaseMap");
        Texture2D mask = null;
        if (baseMap != null)
        {
            var maskPath = $"{MaskDir}/{baseMap.name}_Mask.png";
            EnsureMaskImport(maskPath);
            mask = AssetDatabase.LoadAssetAtPath<Texture2D>(maskPath);
        }
        m.SetTexture("_MaskMap", mask);
        m.SetFloat("_UseMaskMap", mask != null ? 1f : 0f);
        if (mask != null) m.EnableKeyword("_MASKMAP");
        else m.DisableKeyword("_MASKMAP");
    }

    // 마스크는 색이 아니라 데이터 → sRGB 끔 (MaskTextureImporter가 먼저 처리하지만, 그 전에 들어온 경우를 대비)
    static void EnsureMaskImport(string path)
    {
        if (AssetImporter.GetAtPath(path) is TextureImporter importer && importer.sRGBTexture)
        {
            importer.sRGBTexture = false;
            importer.SaveAndReimport();
        }
    }

    enum SurfaceType { Standard = 0, Skin = 1, Cloth = 2 }

    // 텍스처를 보고 정한 부위별 재질 프리셋.
    // 마스크 맵이 있으면 Metal/Silver/Gem Detect 값은 각 마스크 채널의 세기로 쓰인다
    static void ApplyPreset(Material m, string sourceName)
    {
        // 기본: 가죽·금속·보석이 섞인 장신구류
        SetSurfaceType(m, SurfaceType.Standard);
        m.SetFloat("_Cull", (float)CullMode.Off);   // 한 겹짜리 부품이 뚫려 보이지 않게 양면
        m.SetColor("_BackFaceColor", new Color(0.55f, 0.1f, 0.1f, 0f));
        m.SetFloat("_Smoothness", 0.4f);
        m.SetFloat("_SpecularStrength", 1f);
        m.SetFloat("_MetalDetect", 1f);
        m.SetFloat("_MetalHue", 0.115f);
        m.SetFloat("_MetalHueWidth", 0.055f);
        m.SetFloat("_MetalSmoothness", 0.78f);
        m.SetFloat("_MetalBoost", 1.2f);
        m.SetFloat("_SilverDetect", 1f);
        m.SetFloat("_GemDetect", 1f);

        if (sourceName.Contains("Skin"))
        {
            // 몸·얼굴·허벅지: 피부 SSS.
            // 동해님이 몸 피부에서 잡은 방향(실사 명암·진한 역광·또렷한 외곽선)을 살리되,
            // 얼굴이 하얗게 날아가던 값(보조광 1.09·역광 3)은 낮췄다
            SetSurfaceType(m, SurfaceType.Skin);
            m.SetFloat("_Cull", (float)CullMode.Back);
            m.SetFloat("_Smoothness", 0.5f);
            m.SetFloat("_SpecularStrength", 0.7f);
            m.SetFloat("_MetalDetect", 0f);
            m.SetFloat("_SilverDetect", 0f);
            m.SetFloat("_GemDetect", 0f);
            m.SetFloat("_BumpScale", 1.06f);
            m.SetFloat("_Realism", 1f);
            m.SetFloat("_AmbientStrength", 0.8f);
            m.SetFloat("_FillIntensity", 0.45f);
            m.SetFloat("_RimIntensity", 1.6f);
            m.SetFloat("_RimWidth", 0.45f);
            m.SetFloat("_RimSoftness", 0.4f);
            m.SetFloat("_OutlineWidth", 1.4f);
            m.SetFloat("_OutlineOpacity", 0.85f);
            m.SetFloat("_OutlineBaseBlend", 0f);
            m.SetFloat("_OutlineNoiseAmount", 0f);
        }
        else if (sourceName.Contains("Jeogori") || sourceName.Contains("Chima"))
        {
            // 저고리·치마: 천 + 금장 자수(마스크 R). 금실은 "완전한 금속"이 아니라 70%만 금속으로 →
            // 반사가 없는 각도에서도 검게 죽지 않고, 빛을 받으면 반짝인다. 안감은 빨강
            SetSurfaceType(m, SurfaceType.Cloth);
            m.SetFloat("_Smoothness", 0.3f);
            m.SetFloat("_MetalDetect", 0.7f);
            m.SetFloat("_MetalSmoothness", 0.75f);
            m.SetFloat("_MetalBoost", 1.8f);
            m.SetFloat("_MetalSaturation", 1.5f);   // 금장이 은색으로 보이지 않게 금빛을 살린다
            m.SetFloat("_SheenIntensity", 0.6f);
            m.SetFloat("_RimIntensity", 1.2f);
            if (sourceName.Contains("RedInside"))
                m.SetColor("_BackFaceColor", new Color(0.55f, 0.1f, 0.1f, 0.85f));
        }
        else if (sourceName == "material_0.007")
        {
            // 머리카락: 천 타입의 부드러운 윤기 + 하이라이트 조금 더
            SetSurfaceType(m, SurfaceType.Cloth);
            m.SetFloat("_Smoothness", 0.55f);
            m.SetFloat("_ClothSpecular", 0.8f);
            m.SetFloat("_SheenIntensity", 0.25f);
        }
        else if (sourceName == "material_0.004")
        {
            // 칼: 금 장식 + 은빛 칼날
            m.SetFloat("_Smoothness", 0.45f);
        }
        else if (sourceName == "material_0.018")
        {
            // 고글: 구리 링 + 초록 렌즈 + 금속 테 (마스크 없을 때 판별 색 범위도 구리 쪽으로)
            m.SetFloat("_MetalHue", 0.09f);
            m.SetFloat("_MetalHueWidth", 0.075f);
            m.SetFloat("_SilverDetect", 0.5f);
        }
        else if (sourceName.Contains("Boots"))
        {
            m.SetFloat("_Smoothness", 0.45f);
        }
    }

    static void SetSurfaceType(Material m, SurfaceType type)
    {
        m.SetFloat("_MaterialType", (float)type);
        m.DisableKeyword("_MATERIALTYPE_STANDARD");
        m.DisableKeyword("_MATERIALTYPE_SKIN");
        m.DisableKeyword("_MATERIALTYPE_CLOTH");
        m.EnableKeyword("_MATERIALTYPE_" + type.ToString().ToUpperInvariant());
    }

    // ------------------------------------------------------------------
    // 스타일 프리셋: 리얼 / 세미리얼 / 스타일라이즈드
    // ------------------------------------------------------------------
    static List<StylePreset> CreateStylePresets(bool reset)
    {
        var real = CreateOrLoadAsset<StylePreset>($"{StyleDir}/1_Real.asset", reset, p =>
        {
            p.displayName = "리얼";
            p.description = "물리 기반 조명만: 실사 명암, 보조광·역광·그늘 색·외곽선 없음. URP Lit과 가장 비슷하다";
            p.realism = new StylePreset.FloatOverride(1f);
            p.fillLight = StylePreset.Switch.Off;
            p.rimLight = StylePreset.Switch.Off;
            p.shadowTint = StylePreset.Switch.Off;
            p.outline = StylePreset.Switch.Off;
        });
        var semiReal = CreateOrLoadAsset<StylePreset>($"{StyleDir}/2_SemiReal.asset", reset, p =>
        {
            p.displayName = "세미리얼";
            p.description = "머티리얼마다 맞춘 기본값 그대로: 실사에 가까운 명암 + 약한 보조광·역광 + 먹 아웃라인";
        });
        var stylized = CreateOrLoadAsset<StylePreset>($"{StyleDir}/3_Stylized.asset", reset, p =>
        {
            p.displayName = "스타일라이즈드";
            p.description = "또렷한 2단 명암, 강한 역광 테두리, 굵은 먹 선. 반사는 줄여 그림처럼";
            p.realism = new StylePreset.FloatOverride(0f);
            p.shadeSoftness = new StylePreset.FloatOverride(0.05f);
            p.shadeOffset = new StylePreset.FloatOverride(0.05f);
            p.ambientStrength = new StylePreset.FloatOverride(1.1f);
            p.specularStrength = new StylePreset.FloatOverride(0.5f);
            p.reflectionStrength = new StylePreset.FloatOverride(0.5f);
            p.fillIntensity = new StylePreset.FloatOverride(0.5f);
            p.rimIntensity = new StylePreset.FloatOverride(1.5f);
            p.rimWidth = new StylePreset.FloatOverride(0.3f);
            p.rimSoftness = new StylePreset.FloatOverride(0.05f);
            p.outlineWidth = new StylePreset.FloatOverride(2.6f);
            p.outlineOpacity = new StylePreset.FloatOverride(0.95f);
            p.outlineNoiseAmount = new StylePreset.FloatOverride(0.35f);
            p.fillLight = StylePreset.Switch.On;
            p.rimLight = StylePreset.Switch.On;
            p.shadowTint = StylePreset.Switch.On;
            p.outline = StylePreset.Switch.On;
        });
        return new List<StylePreset> { real, semiReal, stylized };
    }

    // ------------------------------------------------------------------
    // 환경 프리셋: 하늘·햇빛·환경광·안개·후처리 (반사는 장면을 만든 뒤 굽는다)
    // ------------------------------------------------------------------
    const string FrozenProps = "Env_FrozenWorld";
    const string StudioProps = "Env_Studio";
    const string OutdoorProps = "Env_Outdoor";

    static List<EnvironmentPreset> CreateEnvironmentPresets(bool reset)
    {
        var list = new List<EnvironmentPreset>();

        list.Add(CreateOrLoadAsset<EnvironmentPreset>($"{EnvironmentDir}/1_FrozenWorld.asset", reset, e =>
        {
            e.displayName = "얼어붙은 세상";
            e.description = "차가운 겨울 해, 눈 바닥·얼음판, 푸른 그늘. 피부의 붉은 혈색만 따뜻하게 남는다";
            e.skybox = ProceduralSky("FrozenSky", 0.9f, new Color(0.72f, 0.79f, 0.88f), new Color(0.85f, 0.88f, 0.93f), 1.25f);
            e.sunColor = new Color(0.86f, 0.92f, 1f);
            e.sunIntensity = 1.5f;
            e.sunAngles = new Vector2(28f, 150f);
            e.shadowStrength = 0.85f;
            e.ambientSky = new Color(0.55f, 0.66f, 0.85f);
            e.ambientEquator = new Color(0.47f, 0.54f, 0.64f);
            e.ambientGround = new Color(0.58f, 0.63f, 0.72f);   // 눈에 반사된 빛
            e.fog = true;
            e.fogColor = new Color(0.7f, 0.78f, 0.88f);
            e.fogDensity = 0.035f;
            e.propsRoot = FrozenProps;
        }));
        list[^1].profile = CreateProfile("FrozenWorld_Volume", reset, p =>
        {
            p.Add<Tonemapping>().mode.Override(TonemappingMode.Neutral);   // 재질 색을 덜 비틀어 비교하기 좋다
            var white = p.Add<WhiteBalance>();
            white.temperature.Override(-22f);   // 차갑게
            white.tint.Override(3f);
            var color = p.Add<ColorAdjustments>();
            color.postExposure.Override(0.2f);
            color.contrast.Override(12f);
            color.saturation.Override(-10f);    // 얼어붙은 공기: 채도를 살짝 뺀다
            color.colorFilter.Override(new Color(0.93f, 0.97f, 1f));
            var smh = p.Add<ShadowsMidtonesHighlights>();
            smh.shadows.Override(new Vector4(0.9f, 0.97f, 1.15f, 0f));   // 어두운 곳은 푸르게
            smh.highlights.Override(new Vector4(1f, 1f, 1.03f, 0f));
            AddBloom(p, 0.9f, 0.6f, new Color(0.85f, 0.93f, 1f));       // 금속·눈 반짝임
            AddVignette(p, 0.25f, new Color(0.04f, 0.07f, 0.12f));
            AddGrain(p, 0.12f);
        });

        list.Add(CreateOrLoadAsset<EnvironmentPreset>($"{EnvironmentDir}/2_Studio.asset", reset, e =>
        {
            e.displayName = "중립 스튜디오";
            e.description = "어두운 배경·흰 빛·후처리는 톤매핑만. 쉐이더 자체를 평가할 때 쓰는 기준 환경";
            e.skybox = ProceduralSky("StudioSky", 0.3f, new Color(0.5f, 0.5f, 0.5f), new Color(0.22f, 0.22f, 0.23f), 0.35f);
            e.sunColor = Color.white;
            e.sunIntensity = 1.3f;
            e.sunAngles = new Vector2(40f, 145f);
            e.shadowStrength = 0.7f;
            e.ambientSky = new Color(0.46f, 0.46f, 0.48f);
            e.ambientEquator = new Color(0.38f, 0.38f, 0.38f);
            e.ambientGround = new Color(0.26f, 0.26f, 0.26f);
            e.fog = false;
            e.propsRoot = StudioProps;
        }));
        list[^1].profile = CreateProfile("Studio_Volume", reset, p =>
        {
            p.Add<Tonemapping>().mode.Override(TonemappingMode.Neutral);
            p.Add<ColorAdjustments>().postExposure.Override(0f);
        });

        list.Add(CreateOrLoadAsset<EnvironmentPreset>($"{EnvironmentDir}/3_Noon.asset", reset, e =>
        {
            e.displayName = "맑은 한낮";
            e.description = "높이 뜬 강한 해, 푸른 하늘 환경광, 진한 그림자. 금속 반사와 그림자 경계 확인용";
            e.skybox = ProceduralSky("NoonSky", 1f, new Color(0.5f, 0.55f, 0.65f), new Color(0.45f, 0.42f, 0.38f), 1.3f);
            e.sunColor = new Color(1f, 0.96f, 0.9f);
            e.sunIntensity = 1.9f;
            e.sunAngles = new Vector2(60f, 160f);
            e.shadowStrength = 0.9f;
            e.ambientSky = new Color(0.55f, 0.65f, 0.82f);
            e.ambientEquator = new Color(0.5f, 0.5f, 0.48f);
            e.ambientGround = new Color(0.34f, 0.31f, 0.27f);
            e.fog = true;
            e.fogColor = new Color(0.75f, 0.82f, 0.9f);
            e.fogDensity = 0.008f;
            e.propsRoot = OutdoorProps;
        }));
        list[^1].profile = CreateProfile("Noon_Volume", reset, p =>
        {
            p.Add<Tonemapping>().mode.Override(TonemappingMode.ACES);
            var color = p.Add<ColorAdjustments>();
            color.contrast.Override(5f);
            color.saturation.Override(8f);
            AddBloom(p, 1f, 0.4f, Color.white);
            AddVignette(p, 0.15f, Color.black);
        });

        list.Add(CreateOrLoadAsset<EnvironmentPreset>($"{EnvironmentDir}/4_Sunset.asset", reset, e =>
        {
            e.displayName = "노을";
            e.description = "낮게 깔린 주황빛 해와 보랏빛 그늘. 따뜻한 빛과 차가운 그늘이 섞일 때 피부·천 확인";
            e.skybox = ProceduralSky("SunsetSky", 1.6f, new Color(0.6f, 0.45f, 0.45f), new Color(0.3f, 0.24f, 0.22f), 1.1f);
            e.sunColor = new Color(1f, 0.62f, 0.36f);
            e.sunIntensity = 1.5f;
            e.sunAngles = new Vector2(8f, 125f);
            e.shadowStrength = 0.8f;
            e.ambientSky = new Color(0.42f, 0.38f, 0.55f);
            e.ambientEquator = new Color(0.62f, 0.43f, 0.33f);
            e.ambientGround = new Color(0.24f, 0.19f, 0.17f);
            e.fog = true;
            e.fogColor = new Color(0.78f, 0.55f, 0.42f);
            e.fogDensity = 0.015f;
            e.propsRoot = OutdoorProps;
        }));
        list[^1].profile = CreateProfile("Sunset_Volume", reset, p =>
        {
            p.Add<Tonemapping>().mode.Override(TonemappingMode.ACES);
            p.Add<WhiteBalance>().temperature.Override(18f);
            var color = p.Add<ColorAdjustments>();
            color.contrast.Override(8f);
            color.saturation.Override(10f);
            AddBloom(p, 0.85f, 0.8f, new Color(1f, 0.8f, 0.6f));
            AddVignette(p, 0.3f, new Color(0.12f, 0.05f, 0.08f));
        });

        list.Add(CreateOrLoadAsset<EnvironmentPreset>($"{EnvironmentDir}/5_Night.asset", reset, e =>
        {
            e.displayName = "달밤";
            e.description = "푸른 달빛과 어두운 환경광. '3점 조명 + 포인트' 조명과 함께 쓰면 횃불 같은 따뜻한 빛이 더해진다";
            e.skybox = ProceduralSky("NightSky", 0.6f, new Color(0.2f, 0.25f, 0.4f), new Color(0.05f, 0.05f, 0.07f), 0.15f);
            e.sunColor = new Color(0.55f, 0.65f, 1f);
            e.sunIntensity = 0.45f;
            e.sunAngles = new Vector2(45f, 200f);
            e.shadowStrength = 0.7f;
            e.ambientSky = new Color(0.08f, 0.1f, 0.18f);
            e.ambientEquator = new Color(0.06f, 0.07f, 0.1f);
            e.ambientGround = new Color(0.04f, 0.04f, 0.05f);
            e.fog = true;
            e.fogColor = new Color(0.05f, 0.07f, 0.12f);
            e.fogDensity = 0.03f;
            e.propsRoot = OutdoorProps;
        }));
        list[^1].profile = CreateProfile("Night_Volume", reset, p =>
        {
            p.Add<Tonemapping>().mode.Override(TonemappingMode.Neutral);
            p.Add<WhiteBalance>().temperature.Override(-15f);
            var color = p.Add<ColorAdjustments>();
            color.postExposure.Override(0.4f);
            color.saturation.Override(-25f);
            AddBloom(p, 0.8f, 1f, new Color(1f, 0.85f, 0.7f));
            AddVignette(p, 0.35f, Color.black);
            AddGrain(p, 0.25f);
        });

        foreach (var e in list)
            EditorUtility.SetDirty(e);
        AssetDatabase.SaveAssets();
        return list;
    }

    static Material ProceduralSky(string name, float atmosphere, Color tint, Color ground, float exposure)
    {
        var sky = CreateOrLoadMaterial($"{EnvironmentMaterialDir}/{name}.mat", "Skybox/Procedural");
        sky.SetFloat("_SunSize", 0.03f);
        sky.SetFloat("_SunSizeConvergence", 8f);
        sky.SetFloat("_AtmosphereThickness", atmosphere);
        sky.SetColor("_SkyTint", tint);
        sky.SetColor("_GroundColor", ground);
        sky.SetFloat("_Exposure", exposure);
        EditorUtility.SetDirty(sky);
        return sky;
    }

    static void AddBloom(VolumeProfile p, float threshold, float intensity, Color tint)
    {
        var bloom = p.Add<Bloom>();
        bloom.threshold.Override(threshold);
        bloom.intensity.Override(intensity);
        bloom.scatter.Override(0.65f);
        bloom.tint.Override(tint);
    }

    static void AddVignette(VolumeProfile p, float intensity, Color color)
    {
        var vignette = p.Add<Vignette>();
        vignette.intensity.Override(intensity);
        vignette.smoothness.Override(0.45f);
        vignette.color.Override(color);
    }

    static void AddGrain(VolumeProfile p, float intensity)
    {
        var grain = p.Add<FilmGrain>();
        grain.type.Override(FilmGrainLookup.Thin1);
        grain.intensity.Override(intensity);
    }

    static VolumeProfile CreateProfile(string name, bool reset, Action<VolumeProfile> fill)
    {
        var path = $"{SettingsDir}/{name}.asset";
        var existing = AssetDatabase.LoadAssetAtPath<VolumeProfile>(path);
        if (existing != null && !reset)
            return existing;
        if (existing != null)
            AssetDatabase.DeleteAsset(path);

        var profile = ScriptableObject.CreateInstance<VolumeProfile>();
        AssetDatabase.CreateAsset(profile, path);
        fill(profile);
        foreach (var component in profile.components)
            AssetDatabase.AddObjectToAsset(component, profile);
        EditorUtility.SetDirty(profile);
        AssetDatabase.SaveAssets();
        return profile;
    }

    // ------------------------------------------------------------------
    // 장면
    // ------------------------------------------------------------------
    static void BuildScene(GameObject model, Dictionary<Material, Material> remap, List<StylePreset> styles,
        List<EnvironmentPreset> environments, int originalLayer, int newLayer)
    {
        // 새 장면을 열면 쓰이지 않는 에셋이 메모리에서 내려가 C# 변수가 '없는 객체'가 된다 → 경로를 적어 두었다가 다시 읽는다
        var modelPath = AssetDatabase.GetAssetPath(model);
        var remapPaths = remap.Select(p => (AssetDatabase.GetAssetPath(p.Key), AssetDatabase.GetAssetPath(p.Value))).ToList();
        var stylePaths = styles.Select(AssetDatabase.GetAssetPath).ToList();
        var environmentPaths = environments.Select(AssetDatabase.GetAssetPath).ToList();

        var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);

        model = AssetDatabase.LoadAssetAtPath<GameObject>(modelPath);
        remap = remapPaths.ToDictionary(p => AssetDatabase.LoadAssetAtPath<Material>(p.Item1), p => AssetDatabase.LoadAssetAtPath<Material>(p.Item2));
        styles = stylePaths.Select(AssetDatabase.LoadAssetAtPath<StylePreset>).ToList();
        environments = environmentPaths.Select(AssetDatabase.LoadAssetAtPath<EnvironmentPreset>).ToList();

        // ---- 캐릭터: 같은 자리에 겹치고 레이어로 나눈다 (A = 메인 카메라, B = 비교 카메라) ----
        var characterA = CreateCharacter(model, "Character A (FinalPlayer)", originalLayer, remap, startWithLab: false);
        var characterB = CreateCharacter(model, "Character B (FinalPlayer)", newLayer, remap, startWithLab: true);
        int characterMask = (1 << originalLayer) | (1 << newLayer);

        // 모델 기준점이 몸 가운데라서, 발이 바닥(y = 0)에 닿도록 두 캐릭터를 함께 올린다
        var bounds = characterA.GetBounds();
        float height = Mathf.Max(bounds.size.y, 0.1f);
        var lift = new Vector3(0f, -bounds.min.y, 0f);
        characterA.transform.position += lift;
        characterB.transform.position += lift;

        // ---- 햇빛 (환경 프리셋이 색·세기·방향을 정한다) ----
        var lightGo = new GameObject("Sun");
        var sun = lightGo.AddComponent<Light>();
        sun.type = LightType.Directional;
        sun.shadows = LightShadows.Soft;
        RenderSettings.sun = sun;

        // ---- 보조 포인트 라이트 (조명 프리셋 '3점 조명 + 포인트'에서 켠다) ----
        var accentRoot = new GameObject("Accent Lights");
        var warm = CreatePointLight(accentRoot.transform, "Warm Key (Torch)", new Vector3(1.1f, height * 0.75f, 1.0f), new Color(1f, 0.65f, 0.35f), 2.5f, height * 2.5f);
        var cool = CreatePointLight(accentRoot.transform, "Cool Rim", new Vector3(-0.9f, height * 0.9f, -1.1f), new Color(0.5f, 0.7f, 1f), 3f, height * 2.5f);

        // ---- 배경 소품 (환경마다 하나씩 켠다) ----
        CreateFrozenProps();
        CreateStudioProps();
        CreateOutdoorProps();
        AssetDatabase.SaveAssets();

        // ---- 후처리 볼륨 ----
        var volumeGo = new GameObject("Global Volume");
        var volume = volumeGo.AddComponent<Volume>();
        volume.isGlobal = true;

        // ---- 카메라: 메인 = A, 자식 = B ----
        var camGo = new GameObject("Main Camera");
        camGo.tag = "MainCamera";
        var cam = camGo.AddComponent<Camera>();
        cam.clearFlags = CameraClearFlags.Skybox;
        cam.nearClipPlane = 0.05f;
        cam.farClipPlane = 200f;
        camGo.AddComponent<AudioListener>();
        cam.GetUniversalAdditionalCameraData().renderPostProcessing = true;

        var compareGo = new GameObject("Compare Camera (B)");
        compareGo.transform.SetParent(camGo.transform, false);
        var compareCam = compareGo.AddComponent<Camera>();
        compareCam.clearFlags = CameraClearFlags.Skybox;
        compareCam.GetUniversalAdditionalCameraData().renderPostProcessing = true;

        var lookAt = new Vector3(0f, height * 0.55f, 0f);
        camGo.transform.position = lookAt + new Vector3(0f, height * 0.05f, height * 1.5f);
        camGo.transform.LookAt(lookAt);

        var orbit = camGo.AddComponent<OrbitCamera>();
        orbit.target = characterB.transform;
        var compare = camGo.AddComponent<CompareView>();
        compare.compareCamera = compareCam;
        compare.layerA = OriginalLayer;
        compare.layerB = NewLayer;

        // ---- 반사 프로브: 하늘·바닥을 금속·보석에 비춘다 (캐릭터는 제외). 환경마다 구워 둔다 ----
        var probeGo = new GameObject("Reflection Probe");
        probeGo.transform.position = new Vector3(0f, height * 0.6f, 0f);
        var probe = probeGo.AddComponent<ReflectionProbe>();
        probe.mode = ReflectionProbeMode.Custom;
        probe.size = new Vector3(40f, 20f, 40f);
        probe.cullingMask = ~characterMask;
        probe.hdr = true;

        // ---- 쉐이더 랩 (상태 관리 + UI) ----
        var labGo = new GameObject("Shader Lab");
        var lab = labGo.AddComponent<ShaderLabController>();
        lab.characterA = characterA;
        lab.characterB = characterB;
        lab.compareView = compare;
        lab.orbitCamera = orbit;
        lab.sun = sun;
        lab.volume = volume;
        lab.reflectionProbe = probe;
        lab.accentLights = new List<Light> { warm, cool };
        lab.styles = styles;
        lab.defaultStyleB = 1;   // 세미리얼
        lab.environments = environments;
        lab.lightingPresets = CreateLightingPresets();

        var doc = labGo.AddComponent<UIDocument>();
        doc.panelSettings = CreatePanelSettings();
        doc.visualTreeAsset = AssetDatabase.LoadAssetAtPath<VisualTreeAsset>($"{UiDir}/ShaderLab.uxml");
        var ui = labGo.AddComponent<ShaderLabUI>();
        ui.lab = lab;

        EditorSceneManager.SaveScene(scene, ScenePath);
        BakeEnvironmentReflections(environments, sun, probe);
        // 처음 열었을 때 첫 환경이 보이게
        PreviewEnvironment(environments[0], sun, volume);
        EditorSceneManager.MarkSceneDirty(scene);
        EditorSceneManager.SaveScene(scene, ScenePath);

        Selection.activeGameObject = characterB.gameObject;
        Debug.Log($"쉐이더 랩 장면 생성 완료: {ScenePath} (새 쉐이더 머티리얼 {remap.Count}개, 환경 {environments.Count}개). 플레이(▶)를 누르세요.");
    }

    static LabCharacter CreateCharacter(GameObject model, string name, int layer, Dictionary<Material, Material> remap, bool startWithLab)
    {
        var go = (GameObject)PrefabUtility.InstantiatePrefab(model);
        go.name = name;
        SetLayerRecursively(go, layer);

        var character = go.AddComponent<LabCharacter>();
        foreach (var renderer in go.GetComponentsInChildren<Renderer>(true))
        {
            var lit = renderer.sharedMaterials;
            var lab = lit.Select(m => m != null && remap.TryGetValue(m, out var r) ? r : m).ToArray();
            character.renderers.Add(new LabCharacter.RendererMaterials { renderer = renderer, lit = lit, lab = lab });
            if (startWithLab)
                renderer.sharedMaterials = lab;
        }

        character.animator = go.GetComponent<Animator>();
        if (character.animator == null)
            character.animator = go.AddComponent<Animator>();
        character.animator.applyRootMotion = false;
        character.clips = AssetDatabase.LoadAllAssetsAtPath(AssetDatabase.GetAssetPath(model))
            .OfType<AnimationClip>()
            .Where(c => !c.name.StartsWith("__preview__"))
            .ToList();
        return character;
    }

    static List<ShaderLabController.LightingPreset> CreateLightingPresets() => new()
    {
        new() { displayName = "환경 기본", description = "환경 프리셋에 정해 둔 햇빛 그대로", useEnvironmentSun = true },
        new() { displayName = "정면광", description = "카메라 쪽에서 비추는 빛. 그림자가 거의 없어 평평하게 보인다", useEnvironmentSun = false, sunAngles = new Vector2(25f, 180f) },
        new() { displayName = "45° 키 라이트", description = "인물 사진의 기본 각도. 얼굴 한쪽에 삼각형 빛이 생긴다", useEnvironmentSun = false, sunAngles = new Vector2(40f, 135f) },
        new() { displayName = "측면광", description = "옆에서 비춰 명암 경계(2단 명암·SSS)가 가장 잘 보인다", useEnvironmentSun = false, sunAngles = new Vector2(10f, 90f) },
        new() { displayName = "역광", description = "뒤에서 비춰 윤곽만 밝다. 림 라이트·피부 투과광 확인용", useEnvironmentSun = false, sunAngles = new Vector2(20f, 0f), intensityScale = 1.2f },
        new() { displayName = "탑라이트", description = "머리 위에서 내리꽂는 빛. 눈두덩·턱 밑 그늘 확인", useEnvironmentSun = false, sunAngles = new Vector2(85f, 180f) },
        new() { displayName = "3점 조명 + 포인트", description = "환경 햇빛 + 따뜻한 포인트(횃불) + 차가운 역광 포인트. 추가 라이트 확인용", useEnvironmentSun = true, accentLights = true },
    };

    static Light CreatePointLight(Transform parent, string name, Vector3 position, Color color, float intensity, float range)
    {
        var go = new GameObject(name);
        go.transform.SetParent(parent, false);
        go.transform.position = position;
        var l = go.AddComponent<Light>();
        l.type = LightType.Point;
        l.color = color;
        l.intensity = intensity;
        l.range = range;
        l.shadows = LightShadows.None;
        go.SetActive(false);
        return l;
    }

    // ---- 배경 소품 ----
    static void CreateFrozenProps()
    {
        var root = new GameObject(FrozenProps);
        var snow = CreateOrLoadMaterial(EnvironmentMaterialDir + "/Snow.mat", "Universal Render Pipeline/Lit");
        snow.SetColor("_BaseColor", new Color(0.86f, 0.9f, 0.95f));
        snow.SetFloat("_Smoothness", 0.35f);
        var ground = CreateGround(root.transform, "Snow Ground", snow);

        var ice = CreateOrLoadMaterial(EnvironmentMaterialDir + "/Ice.mat", "Universal Render Pipeline/Lit");
        ice.SetColor("_BaseColor", new Color(0.74f, 0.83f, 0.9f));   // 옅은 얼음색 (진하면 플라스틱처럼 보인다)
        ice.SetFloat("_Smoothness", 0.92f);
        var icePlate = CreateDisc(root.transform, "Ice Plate", ice, 2.2f);

        CreateSnowfall(root.transform);
        EditorUtility.SetDirty(snow);
        EditorUtility.SetDirty(ice);
        MarkReflectionStatic(ground, icePlate);
    }

    static void CreateStudioProps()
    {
        var root = new GameObject(StudioProps);
        var floor = CreateOrLoadMaterial(EnvironmentMaterialDir + "/StudioFloor.mat", "Universal Render Pipeline/Lit");
        floor.SetColor("_BaseColor", new Color(0.32f, 0.32f, 0.33f));
        floor.SetFloat("_Smoothness", 0.3f);
        EditorUtility.SetDirty(floor);
        var ground = CreateGround(root.transform, "Studio Floor", floor);
        var disc = CreateDisc(root.transform, "Turntable", CreateTurntableMaterial(), 1.6f);
        MarkReflectionStatic(ground, disc);
        root.SetActive(false);
    }

    static Material CreateTurntableMaterial()
    {
        var m = CreateOrLoadMaterial(EnvironmentMaterialDir + "/Turntable.mat", "Universal Render Pipeline/Lit");
        m.SetColor("_BaseColor", new Color(0.18f, 0.18f, 0.19f));
        m.SetFloat("_Smoothness", 0.55f);
        EditorUtility.SetDirty(m);
        return m;
    }

    static void CreateOutdoorProps()
    {
        var root = new GameObject(OutdoorProps);
        var grass = CreateOrLoadMaterial(EnvironmentMaterialDir + "/Field.mat", "Universal Render Pipeline/Lit");
        grass.SetColor("_BaseColor", new Color(0.36f, 0.4f, 0.27f));
        grass.SetFloat("_Smoothness", 0.15f);
        EditorUtility.SetDirty(grass);
        var ground = CreateGround(root.transform, "Field Ground", grass);

        var stone = CreateOrLoadMaterial(EnvironmentMaterialDir + "/Stone.mat", "Universal Render Pipeline/Lit");
        stone.SetColor("_BaseColor", new Color(0.52f, 0.5f, 0.47f));
        stone.SetFloat("_Smoothness", 0.25f);
        EditorUtility.SetDirty(stone);
        var plate = CreateDisc(root.transform, "Stone Plate", stone, 2.2f);
        MarkReflectionStatic(ground, plate);
        root.SetActive(false);
    }

    static GameObject CreateGround(Transform parent, string name, Material material)
    {
        var ground = GameObject.CreatePrimitive(PrimitiveType.Plane);
        ground.name = name;
        ground.transform.SetParent(parent, false);
        ground.transform.localScale = Vector3.one * 3f;
        ground.GetComponent<Renderer>().sharedMaterial = material;
        return ground;
    }

    static GameObject CreateDisc(Transform parent, string name, Material material, float diameter)
    {
        var disc = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
        disc.name = name;
        disc.transform.SetParent(parent, false);
        disc.transform.position = new Vector3(0f, -0.01f, 0f);
        disc.transform.localScale = new Vector3(diameter, 0.02f, diameter);
        disc.GetComponent<Renderer>().sharedMaterial = material;
        Object.DestroyImmediate(disc.GetComponent<Collider>());
        return disc;
    }

    // 반사 프로브에 찍히도록 배경은 static
    static void MarkReflectionStatic(params GameObject[] objects)
    {
        foreach (var go in objects)
            GameObjectUtility.SetStaticEditorFlags(go, StaticEditorFlags.ReflectionProbeStatic);
    }

    static void CreateSnowfall(Transform parent)
    {
        var mat = CreateOrLoadMaterial(EnvironmentMaterialDir + "/Snowflake.mat", "Universal Render Pipeline/Particles/Unlit");
        mat.SetTexture("_BaseMap", AssetDatabase.GetBuiltinExtraResource<Texture2D>("Default-Particle.psd"));
        mat.SetColor("_BaseColor", new Color(1f, 1f, 1f, 0.85f));
        mat.SetFloat("_Surface", 1f);
        mat.SetFloat("_Blend", 0f);
        mat.SetFloat("_SrcBlend", (float)BlendMode.SrcAlpha);
        mat.SetFloat("_DstBlend", (float)BlendMode.OneMinusSrcAlpha);
        mat.SetFloat("_SrcBlendAlpha", (float)BlendMode.One);
        mat.SetFloat("_DstBlendAlpha", (float)BlendMode.OneMinusSrcAlpha);
        mat.SetFloat("_ZWrite", 0f);
        mat.SetOverrideTag("RenderType", "Transparent");
        mat.EnableKeyword("_SURFACE_TYPE_TRANSPARENT");
        mat.renderQueue = (int)RenderQueue.Transparent;
        EditorUtility.SetDirty(mat);

        var go = new GameObject("Snowfall");
        go.transform.SetParent(parent, false);
        go.transform.position = new Vector3(0f, 6f, 0f);
        var ps = go.AddComponent<ParticleSystem>();

        var main = ps.main;
        main.loop = true;
        main.prewarm = true;
        main.startLifetime = 8f;   // 6m 높이에서 바닥까지 내려오는 시간
        main.startSpeed = new ParticleSystem.MinMaxCurve(0.2f, 0.5f);
        main.startSize = new ParticleSystem.MinMaxCurve(0.012f, 0.035f);
        main.gravityModifier = 0.01f;
        main.maxParticles = 4000;
        main.simulationSpace = ParticleSystemSimulationSpace.World;

        var emission = ps.emission;
        emission.rateOverTime = 260f;

        var shape = ps.shape;
        shape.shapeType = ParticleSystemShapeType.Box;
        // 박스의 +Z 방향으로 날아가므로 X축으로 90° 돌려 아래를 향하게 한다.
        // 회전 전 기준 크기: X 14m, Y 14m(→ 월드 Z), Z 0.1m(→ 월드 Y 두께)
        shape.scale = new Vector3(14f, 14f, 0.1f);
        shape.rotation = new Vector3(90f, 0f, 0f);

        var noise = ps.noise;
        noise.enabled = true;
        noise.strength = 0.35f;
        noise.frequency = 0.25f;
        noise.scrollSpeed = 0.2f;

        var renderer = go.GetComponent<ParticleSystemRenderer>();
        renderer.sharedMaterial = mat;
        renderer.renderMode = ParticleSystemRenderMode.Billboard;
    }

    // ---- 환경마다 반사 큐브맵 굽기 ----
    static void BakeEnvironmentReflections(List<EnvironmentPreset> environments, Light sun, ReflectionProbe probe)
    {
        foreach (var env in environments)
        {
            PreviewEnvironment(env, sun, null);
            var path = $"{EnvironmentDir}/{env.name}_Reflection.exr";
            if (!Lightmapping.BakeReflectionProbe(probe, path))
            {
                Debug.LogWarning($"'{env.displayName}' 반사 굽기에 실패했습니다. 금속 반사가 하늘만 비칠 수 있습니다.");
                continue;
            }
            AssetDatabase.ImportAsset(path);
            env.reflection = AssetDatabase.LoadAssetAtPath<Texture>(path);
            EditorUtility.SetDirty(env);
        }
        AssetDatabase.SaveAssets();
    }

    // 에디터에서 환경을 미리 적용 (실행 중에는 ShaderLabController가 같은 일을 한다)
    static void PreviewEnvironment(EnvironmentPreset env, Light sun, Volume volume)
    {
        RenderSettings.skybox = env.skybox;
        RenderSettings.ambientMode = AmbientMode.Trilight;
        RenderSettings.ambientSkyColor = env.ambientSky;
        RenderSettings.ambientEquatorColor = env.ambientEquator;
        RenderSettings.ambientGroundColor = env.ambientGround;
        RenderSettings.fog = env.fog;
        RenderSettings.fogMode = FogMode.ExponentialSquared;
        RenderSettings.fogColor = env.fogColor;
        RenderSettings.fogDensity = env.fogDensity;
        sun.transform.rotation = Quaternion.Euler(env.sunAngles.x, env.sunAngles.y, 0f);
        sun.color = env.sunColor;
        sun.intensity = env.sunIntensity;
        sun.shadowStrength = env.shadowStrength;
        foreach (var name in new[] { FrozenProps, StudioProps, OutdoorProps })
        {
            var root = EditorSceneManager.GetActiveScene().GetRootGameObjects().FirstOrDefault(g => g.name == name);
            if (root != null)
                root.SetActive(name == env.propsRoot);
        }
        if (volume != null)
            volume.sharedProfile = env.profile;
        DynamicGI.UpdateEnvironment();
    }

    // ---- UI Toolkit 패널 설정 ----
    static PanelSettings CreatePanelSettings()
    {
        var themePath = $"{UiDir}/ShaderLabTheme.tss";
        if (!File.Exists(themePath))
        {
            File.WriteAllText(themePath, "@import url(\"unity-theme://default\");\n");
            AssetDatabase.ImportAsset(themePath);
        }
        var path = $"{UiDir}/ShaderLabPanelSettings.asset";
        var settings = AssetDatabase.LoadAssetAtPath<PanelSettings>(path);
        if (settings == null)
        {
            settings = ScriptableObject.CreateInstance<PanelSettings>();
            AssetDatabase.CreateAsset(settings, path);
        }
        settings.themeStyleSheet = AssetDatabase.LoadAssetAtPath<ThemeStyleSheet>(themePath);
        settings.scaleMode = PanelScaleMode.ScaleWithScreenSize;
        settings.referenceResolution = new Vector2Int(1920, 1080);
        settings.match = 0.5f;
        EditorUtility.SetDirty(settings);
        AssetDatabase.SaveAssets();
        return settings;
    }

    // ------------------------------------------------------------------
    // 도우미
    // ------------------------------------------------------------------
    static T CreateOrLoadAsset<T>(string path, bool reset, Action<T> fill) where T : ScriptableObject
    {
        var asset = AssetDatabase.LoadAssetAtPath<T>(path);
        if (asset != null && !reset)
            return asset;
        if (asset == null)
        {
            asset = ScriptableObject.CreateInstance<T>();
            AssetDatabase.CreateAsset(asset, path);
        }
        else
        {
            // 되돌리기: 빈 값으로 덮은 뒤 다시 채운다
            var blank = ScriptableObject.CreateInstance<T>();
            EditorUtility.CopySerialized(blank, asset);
            Object.DestroyImmediate(blank);
        }
        fill(asset);
        EditorUtility.SetDirty(asset);
        return asset;
    }

    static Material CreateOrLoadMaterial(string path, string shaderName)
    {
        var mat = AssetDatabase.LoadAssetAtPath<Material>(path);
        if (mat == null)
        {
            mat = new Material(Shader.Find(shaderName));
            AssetDatabase.CreateAsset(mat, path);
        }
        else if (mat.shader == null || mat.shader.name != shaderName)
            mat.shader = Shader.Find(shaderName);
        return mat;
    }

    static int EnsureLayer(string layerName)
    {
        int existing = LayerMask.NameToLayer(layerName);
        if (existing >= 0)
            return existing;

        var tagManager = new SerializedObject(AssetDatabase.LoadAllAssetsAtPath("ProjectSettings/TagManager.asset")[0]);
        var layers = tagManager.FindProperty("layers");
        for (int i = 8; i < layers.arraySize; i++)
        {
            var slot = layers.GetArrayElementAtIndex(i);
            if (string.IsNullOrEmpty(slot.stringValue))
            {
                slot.stringValue = layerName;
                tagManager.ApplyModifiedPropertiesWithoutUndo();
                return i;
            }
        }
        throw new InvalidOperationException($"빈 레이어 칸이 없어 '{layerName}' 레이어를 만들 수 없습니다.");
    }

    static void SetLayerRecursively(GameObject go, int layer)
    {
        go.layer = layer;
        foreach (Transform child in go.transform)
            SetLayerRecursively(child.gameObject, layer);
    }

    static void EnsureFolder(string path)
    {
        if (AssetDatabase.IsValidFolder(path))
            return;
        var parent = Path.GetDirectoryName(path).Replace('\\', '/');
        EnsureFolder(parent);
        AssetDatabase.CreateFolder(parent, Path.GetFileName(path));
    }
}
