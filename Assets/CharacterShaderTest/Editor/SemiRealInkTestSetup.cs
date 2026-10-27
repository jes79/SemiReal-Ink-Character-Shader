using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

// 메뉴
//  Tools > Shader Test > Build Test Scene
//      장면을 다시 만든다. 머티리얼·후처리에서 조정한 값은 그대로 둔다
//      (아직 프리셋을 한 번도 받지 않은 머티리얼만 프리셋을 넣는다)
//  Tools > Shader Test > Reset Materials And Post To Presets
//      모든 새 쉐이더 머티리얼과 후처리 프로필을 프리셋 값으로 되돌린 뒤 장면을 만든다
//
// 장면: 얼어붙은 세상(차가운 하늘·눈 바닥·얼음판·눈발·차가운 색보정)
//       원본(URP Lit)과 새 쉐이더 캐릭터를 같은 자리에 겹쳐 두고, 레이어로 나눠 비교 카메라 두 대로 본다
public static class SemiRealInkTestSetup
{
    const string Root = "Assets/CharacterShaderTest";
    const string ModelPath = Root + "/Models/FinalPlayer.fbx";
    const string SourceMaterialDir = Root + "/Materials/FinalPlayer";
    const string TargetMaterialDir = Root + "/Materials/FinalPlayer_SemiRealInk";
    const string EnvironmentMaterialDir = Root + "/Materials/Environment";
    const string SettingsDir = Root + "/Settings";
    const string VolumeProfilePath = SettingsDir + "/FrozenWorld_Volume.asset";
    const string ReflectionPath = SettingsDir + "/FrozenWorld_Reflection.exr";
    const string ScenePath = Root + "/Scenes/CharacterShaderTest.unity";
    const string ShaderName = "ShaderTest/SemiRealInk";
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
                "새 쉐이더 머티리얼과 후처리에서 조정한 값이 모두 프리셋 값으로 바뀝니다. 계속할까요?", "되돌리기", "취소"))
            Build(resetAssets: true);
    }

    static void Build(bool resetAssets)
    {
        var shader = Shader.Find(ShaderName);
        if (shader == null)
        {
            Debug.LogError($"쉐이더 '{ShaderName}'를 찾지 못했습니다. 쉐이더 컴파일 에러를 먼저 확인하세요.");
            return;
        }
        var model = AssetDatabase.LoadAssetAtPath<GameObject>(ModelPath);
        if (model == null)
        {
            Debug.LogError($"모델을 찾지 못했습니다: {ModelPath}");
            return;
        }
        if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo())
            return;

        EnsureFolder(TargetMaterialDir);
        EnsureFolder(EnvironmentMaterialDir);
        EnsureFolder(SettingsDir);

        int originalLayer = EnsureLayer(OriginalLayer);
        int newLayer = EnsureLayer(NewLayer);
        var remap = CreateMaterials(shader, resetAssets);
        var profile = CreateVolumeProfile(resetAssets);
        BuildScene(model, remap, profile, originalLayer, newLayer);
    }

    // ------------------------------------------------------------------
    // 머티리얼
    // ------------------------------------------------------------------
    static System.Collections.Generic.Dictionary<Material, Material> CreateMaterials(Shader shader, bool reset)
    {
        var remap = new System.Collections.Generic.Dictionary<Material, Material>();
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
    // 후처리: 차갑고 맑은 겨울 공기
    // ------------------------------------------------------------------
    static VolumeProfile CreateVolumeProfile(bool reset)
    {
        var existing = AssetDatabase.LoadAssetAtPath<VolumeProfile>(VolumeProfilePath);
        if (existing != null && !reset)
            return existing;
        if (existing != null)
            AssetDatabase.DeleteAsset(VolumeProfilePath);

        var profile = ScriptableObject.CreateInstance<VolumeProfile>();
        AssetDatabase.CreateAsset(profile, VolumeProfilePath);

        var tone = profile.Add<Tonemapping>();
        tone.mode.Override(TonemappingMode.Neutral);   // 재질 색을 덜 비틀어 비교하기 좋다

        var white = profile.Add<WhiteBalance>();
        white.temperature.Override(-22f);   // 차갑게
        white.tint.Override(3f);

        var color = profile.Add<ColorAdjustments>();
        color.postExposure.Override(0.2f);
        color.contrast.Override(12f);
        color.saturation.Override(-10f);    // 얼어붙은 공기: 채도를 살짝 뺀다
        color.colorFilter.Override(new Color(0.93f, 0.97f, 1f));

        var smh = profile.Add<ShadowsMidtonesHighlights>();
        smh.shadows.Override(new Vector4(0.9f, 0.97f, 1.15f, 0f));   // 어두운 곳은 푸르게
        smh.highlights.Override(new Vector4(1f, 1f, 1.03f, 0f));

        var bloom = profile.Add<Bloom>();
        bloom.threshold.Override(0.9f);
        bloom.intensity.Override(0.6f);     // 금속·눈 반짝임
        bloom.scatter.Override(0.65f);
        bloom.tint.Override(new Color(0.85f, 0.93f, 1f));

        var vignette = profile.Add<Vignette>();
        vignette.intensity.Override(0.25f);
        vignette.smoothness.Override(0.45f);
        vignette.color.Override(new Color(0.04f, 0.07f, 0.12f));

        var grain = profile.Add<FilmGrain>();
        grain.type.Override(FilmGrainLookup.Thin1);
        grain.intensity.Override(0.12f);

        foreach (var component in profile.components)
            AssetDatabase.AddObjectToAsset(component, profile);
        EditorUtility.SetDirty(profile);
        AssetDatabase.SaveAssets();
        return profile;
    }

    // ------------------------------------------------------------------
    // 장면
    // ------------------------------------------------------------------
    static void BuildScene(GameObject model, System.Collections.Generic.Dictionary<Material, Material> remap,
        VolumeProfile profile, int originalLayer, int newLayer)
    {
        var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);

        // ---- 캐릭터: 같은 자리에 겹치고 레이어로 나눈다 ----
        var original = (GameObject)PrefabUtility.InstantiatePrefab(model);
        original.name = "FinalPlayer_Original (URP Lit)";
        SetLayerRecursively(original, originalLayer);

        var stylized = (GameObject)PrefabUtility.InstantiatePrefab(model);
        stylized.name = "FinalPlayer_SemiRealInk";
        SetLayerRecursively(stylized, newLayer);
        foreach (var renderer in stylized.GetComponentsInChildren<Renderer>())
        {
            var mats = renderer.sharedMaterials;
            for (int i = 0; i < mats.Length; i++)
                if (mats[i] != null && remap.TryGetValue(mats[i], out var replaced))
                    mats[i] = replaced;
            renderer.sharedMaterials = mats;
        }
        int characterMask = (1 << originalLayer) | (1 << newLayer);

        // 모델 기준점이 몸 가운데라서, 발이 눈 바닥(y = 0)에 닿도록 두 캐릭터를 함께 올린다
        var bounds = GetBounds(original);
        float height = Mathf.Max(bounds.size.y, 0.1f);
        var lift = new Vector3(0f, -bounds.min.y, 0f);
        original.transform.position += lift;
        stylized.transform.position += lift;

        // ---- 햇빛: 낮게 뜬 차가운 겨울 해 ----
        var lightGo = new GameObject("Winter Sun");
        var light = lightGo.AddComponent<Light>();
        light.type = LightType.Directional;
        light.color = new Color(0.86f, 0.92f, 1f);
        light.intensity = 1.5f;
        light.shadows = LightShadows.Soft;
        light.shadowStrength = 0.85f;
        lightGo.transform.rotation = Quaternion.Euler(28f, 150f, 0f);   // 카메라 기준 오른쪽 위 앞 (캐릭터 정면 = +Z)

        // ---- 하늘·환경광·안개 ----
        var sky = CreateOrLoadMaterial(EnvironmentMaterialDir + "/FrozenSky.mat", "Skybox/Procedural");
        sky.SetFloat("_SunSize", 0.03f);
        sky.SetFloat("_SunSizeConvergence", 8f);
        sky.SetFloat("_AtmosphereThickness", 0.9f);
        sky.SetColor("_SkyTint", new Color(0.72f, 0.79f, 0.88f));   // 옅고 차가운 겨울 하늘
        sky.SetColor("_GroundColor", new Color(0.85f, 0.88f, 0.93f));
        sky.SetFloat("_Exposure", 1.25f);
        RenderSettings.skybox = sky;
        RenderSettings.sun = light;
        RenderSettings.ambientMode = AmbientMode.Trilight;
        RenderSettings.ambientSkyColor = new Color(0.55f, 0.66f, 0.85f);
        RenderSettings.ambientEquatorColor = new Color(0.47f, 0.54f, 0.64f);
        RenderSettings.ambientGroundColor = new Color(0.58f, 0.63f, 0.72f);   // 눈에 반사된 빛
        RenderSettings.fog = true;
        RenderSettings.fogMode = FogMode.ExponentialSquared;
        RenderSettings.fogDensity = 0.035f;
        RenderSettings.fogColor = new Color(0.7f, 0.78f, 0.88f);

        // ---- 눈 바닥 + 발밑 얼음판 ----
        var snow = CreateOrLoadMaterial(EnvironmentMaterialDir + "/Snow.mat", "Universal Render Pipeline/Lit");
        snow.SetColor("_BaseColor", new Color(0.86f, 0.9f, 0.95f));
        snow.SetFloat("_Smoothness", 0.35f);
        var ground = GameObject.CreatePrimitive(PrimitiveType.Plane);
        ground.name = "Snow Ground";
        ground.transform.localScale = Vector3.one * 3f;
        ground.GetComponent<Renderer>().sharedMaterial = snow;

        var ice = CreateOrLoadMaterial(EnvironmentMaterialDir + "/Ice.mat", "Universal Render Pipeline/Lit");
        ice.SetColor("_BaseColor", new Color(0.74f, 0.83f, 0.9f));   // 옅은 얼음색 (진하면 플라스틱처럼 보인다)
        ice.SetFloat("_Smoothness", 0.92f);
        var icePlate = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
        icePlate.name = "Ice Plate";
        icePlate.transform.position = new Vector3(0f, -0.01f, 0f);
        icePlate.transform.localScale = new Vector3(2.2f, 0.02f, 2.2f);
        icePlate.GetComponent<Renderer>().sharedMaterial = ice;
        Object.DestroyImmediate(icePlate.GetComponent<Collider>());

        // 반사 프로브에 찍히도록 배경은 static
        GameObjectUtility.SetStaticEditorFlags(ground, StaticEditorFlags.ReflectionProbeStatic);
        GameObjectUtility.SetStaticEditorFlags(icePlate, StaticEditorFlags.ReflectionProbeStatic);

        // ---- 눈발 ----
        CreateSnowfall();

        // 위에서 바꾼 하늘·눈·얼음·눈송이 머티리얼 값을 파일에 저장 (반사 프로브를 굽기 전에)
        EditorUtility.SetDirty(sky);
        EditorUtility.SetDirty(snow);
        EditorUtility.SetDirty(ice);
        AssetDatabase.SaveAssets();

        // ---- 후처리 볼륨 ----
        var volumeGo = new GameObject("Global Volume (Frozen World)");
        var volume = volumeGo.AddComponent<Volume>();
        volume.isGlobal = true;
        volume.sharedProfile = profile;

        // ---- 카메라: 메인 = 원본, 자식 = 새 쉐이더 ----
        var camGo = new GameObject("Main Camera");
        camGo.tag = "MainCamera";
        var cam = camGo.AddComponent<Camera>();
        cam.clearFlags = CameraClearFlags.Skybox;
        cam.nearClipPlane = 0.05f;
        cam.farClipPlane = 200f;
        camGo.AddComponent<AudioListener>();
        cam.GetUniversalAdditionalCameraData().renderPostProcessing = true;

        var compareGo = new GameObject("Compare Camera (New Shader)");
        compareGo.transform.SetParent(camGo.transform, false);
        var compareCam = compareGo.AddComponent<Camera>();
        compareCam.clearFlags = CameraClearFlags.Skybox;
        compareCam.GetUniversalAdditionalCameraData().renderPostProcessing = true;

        // 캐릭터를 올린 뒤라 발 = y 0
        var lookAt = new Vector3(0f, height * 0.55f, 0f);
        camGo.transform.position = lookAt + new Vector3(0f, height * 0.05f, height * 1.5f);
        camGo.transform.LookAt(lookAt);

        var viewer = camGo.AddComponent<CharacterCompareViewer>();
        viewer.target = stylized.transform;
        viewer.compareCamera = compareCam;
        viewer.keyLight = light;
        viewer.originalLayerName = OriginalLayer;
        viewer.newLayerName = NewLayer;

        // ---- 반사 프로브: 하늘·눈·얼음을 금속·보석에 비춘다 (캐릭터는 제외) ----
        var probeGo = new GameObject("Reflection Probe (Frozen World)");
        probeGo.transform.position = new Vector3(0f, height * 0.6f, 0f);
        var probe = probeGo.AddComponent<ReflectionProbe>();
        probe.mode = ReflectionProbeMode.Baked;
        probe.size = new Vector3(40f, 20f, 40f);
        probe.cullingMask = ~characterMask;
        probe.hdr = true;

        EditorSceneManager.SaveScene(scene, ScenePath);
        DynamicGI.UpdateEnvironment();
        if (!Lightmapping.BakeReflectionProbe(probe, ReflectionPath))
            Debug.LogWarning("반사 프로브 굽기에 실패했습니다. 금속 반사가 하늘만 비칠 수 있습니다.");
        EditorSceneManager.SaveScene(scene, ScenePath);

        Selection.activeGameObject = stylized;
        Debug.Log($"테스트 장면 생성 완료: {ScenePath} (새 쉐이더 머티리얼 {remap.Count}개). 플레이(▶)를 눌러 비교하세요.");
    }

    static void CreateSnowfall()
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

    // ------------------------------------------------------------------
    // 도우미
    // ------------------------------------------------------------------
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
        throw new System.InvalidOperationException($"빈 레이어 칸이 없어 '{layerName}' 레이어를 만들 수 없습니다.");
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

    static Bounds GetBounds(GameObject go)
    {
        var renderers = go.GetComponentsInChildren<Renderer>();
        if (renderers.Length == 0)
            return new Bounds(Vector3.up, new Vector3(1, 2, 1));
        var b = renderers[0].bounds;
        foreach (var r in renderers)
            b.Encapsulate(r.bounds);
        return b;
    }
}
