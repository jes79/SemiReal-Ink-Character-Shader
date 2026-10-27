using System;
using System.Collections.Generic;
using UnityEngine;

// 스타일 프리셋 (리얼 / 세미리얼 / 스타일라이즈드 …)
// 머티리얼마다 손으로 맞춘 값(부위별 프리셋) 위에 "덮어쓸 값"만 적는다.
// 체크하지 않은 값은 머티리얼에 원래 있던 값을 그대로 쓴다 → 세미리얼은 아무것도 덮어쓰지 않는 프리셋
[CreateAssetMenu(menuName = "Shader Lab/Style Preset", fileName = "StylePreset")]
public class StylePreset : ScriptableObject
{
    public enum Switch { Keep, On, Off }

    [Serializable]
    public struct FloatOverride
    {
        public bool enabled;
        public float value;

        public FloatOverride(float value) { enabled = true; this.value = value; }
    }

    [Tooltip("UI에 보이는 이름")]
    public string displayName = "Preset";
    [TextArea] public string description;

    [Header("명암")]
    [Tooltip("0 = 2단 명암(스타일라이즈드), 1 = 실사 명암")]
    public FloatOverride realism;
    public FloatOverride shadeSoftness;
    public FloatOverride shadeOffset;
    public FloatOverride ambientStrength;

    [Header("반사")]
    public FloatOverride specularStrength;
    public FloatOverride reflectionStrength;

    [Header("보조광·역광")]
    public FloatOverride fillIntensity;
    public FloatOverride rimIntensity;
    public FloatOverride rimWidth;
    public FloatOverride rimSoftness;

    [Header("먹 아웃라인")]
    public FloatOverride outlineWidth;
    public FloatOverride outlineOpacity;
    public FloatOverride outlineNoiseAmount;

    [Header("기능 (Keep = 머티리얼 값 그대로)")]
    public Switch specular;
    public Switch envReflection;
    public Switch fillLight;
    public Switch rimLight;
    public Switch subsurface;
    public Switch sheen;
    public Switch shadowTint;
    public Switch outline;

    // 머티리얼에 덮어쓴다. 원래 값으로 되돌리는 일은 LabCharacter가 한다
    public void ApplyTo(Material m)
    {
        Set(m, "_Realism", realism);
        Set(m, "_ShadeSoftness", shadeSoftness);
        Set(m, "_ShadeOffset", shadeOffset);
        Set(m, "_AmbientStrength", ambientStrength);
        Set(m, "_SpecularStrength", specularStrength);
        Set(m, "_ReflectionStrength", reflectionStrength);
        Set(m, "_FillIntensity", fillIntensity);
        Set(m, "_RimIntensity", rimIntensity);
        Set(m, "_RimWidth", rimWidth);
        Set(m, "_RimSoftness", rimSoftness);
        Set(m, "_OutlineWidth", outlineWidth);
        Set(m, "_OutlineOpacity", outlineOpacity);
        Set(m, "_OutlineNoiseAmount", outlineNoiseAmount);

        Set(m, ShaderLabProperties.Feature(LabFeature.Specular), specular);
        Set(m, ShaderLabProperties.Feature(LabFeature.EnvReflection), envReflection);
        Set(m, ShaderLabProperties.Feature(LabFeature.FillLight), fillLight);
        Set(m, ShaderLabProperties.Feature(LabFeature.RimLight), rimLight);
        Set(m, ShaderLabProperties.Feature(LabFeature.Subsurface), subsurface);
        Set(m, ShaderLabProperties.Feature(LabFeature.Sheen), sheen);
        Set(m, ShaderLabProperties.Feature(LabFeature.ShadowTint), shadowTint);
        Set(m, ShaderLabProperties.Feature(LabFeature.Outline), outline);
    }

    static void Set(Material m, string property, FloatOverride o)
    {
        if (o.enabled && m.HasFloat(property))
            m.SetFloat(property, o.value);
    }

    static void Set(Material m, int property, Switch s)
    {
        if (s != Switch.Keep && m.HasFloat(property))
            m.SetFloat(property, s == Switch.On ? 1f : 0f);
    }
}

// 쉐이더의 맵·기능 스위치 이름을 한곳에 모아 둔다 (쉐이더 Properties와 같은 이름)
public enum LabMap { Base, Normal, Metallic, Roughness, Occlusion, Emission }
public enum LabFeature { Specular, EnvReflection, FillLight, RimLight, Subsurface, Sheen, ShadowTint, Outline }

// 디버그 보기 번호 = 쉐이더 _ShaderLabDebugView 값
public enum LabDebugView
{
    Off = 0, BaseColor = 1, Normal = 2, Metallic = 3, Roughness = 4, Occlusion = 5, Emission = 6,
    LightingOnly = 7, SpecularOnly = 8, MaterialMasks = 9,
}

public static class ShaderLabProperties
{
    public const string ShaderName = "ShaderTest/SemiRealInk";
    public static readonly int DebugView = Shader.PropertyToID("_ShaderLabDebugView");

    static readonly Dictionary<LabMap, int> MapIds = new()
    {
        { LabMap.Base, Shader.PropertyToID("_UseBaseMap") },
        { LabMap.Normal, Shader.PropertyToID("_UseNormalMap") },
        { LabMap.Metallic, Shader.PropertyToID("_UseMetallicMap") },
        { LabMap.Roughness, Shader.PropertyToID("_UseRoughnessMap") },
        { LabMap.Occlusion, Shader.PropertyToID("_UseOcclusionMap") },
        { LabMap.Emission, Shader.PropertyToID("_UseEmissionMap") },
    };

    static readonly Dictionary<LabFeature, int> FeatureIds = new()
    {
        { LabFeature.Specular, Shader.PropertyToID("_EnableSpecular") },
        { LabFeature.EnvReflection, Shader.PropertyToID("_EnableEnvReflection") },
        { LabFeature.FillLight, Shader.PropertyToID("_EnableFillLight") },
        { LabFeature.RimLight, Shader.PropertyToID("_EnableRimLight") },
        { LabFeature.Subsurface, Shader.PropertyToID("_EnableSubsurface") },
        { LabFeature.Sheen, Shader.PropertyToID("_EnableSheen") },
        { LabFeature.ShadowTint, Shader.PropertyToID("_EnableShadowTint") },
        { LabFeature.Outline, Shader.PropertyToID("_EnableOutline") },
    };

    public static int Map(LabMap map) => MapIds[map];
    public static int Feature(LabFeature feature) => FeatureIds[feature];
}
