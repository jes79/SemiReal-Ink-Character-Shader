using UnityEngine;
using UnityEngine.Rendering;

// 환경 프리셋: 하늘 + 햇빛 + 환경광 + 안개 + 후처리 + 반사(구워 둔 큐브맵) + 배경 소품을 한 세트로 묶는다.
// 같은 캐릭터·같은 쉐이더가 환경에 따라 얼마나 달라 보이는지 확인하는 용도
[CreateAssetMenu(menuName = "Shader Lab/Environment Preset", fileName = "EnvironmentPreset")]
public class EnvironmentPreset : ScriptableObject
{
    public string displayName = "Environment";
    [TextArea] public string description;

    [Header("하늘")]
    public Material skybox;

    [Header("햇빛 (메인 디렉셔널 라이트)")]
    public Color sunColor = Color.white;
    public float sunIntensity = 1.2f;
    [Tooltip("x = 높이 각도, y = 방향 각도 (캐릭터 정면 = +Z, 180 = 정면에서 비춤)")]
    public Vector2 sunAngles = new(35f, 150f);
    [Range(0f, 1f)] public float shadowStrength = 0.85f;

    [Header("환경광 (하늘·지평선·바닥 3색)")]
    public Color ambientSky = new(0.5f, 0.55f, 0.6f);
    public Color ambientEquator = new(0.4f, 0.4f, 0.4f);
    public Color ambientGround = new(0.3f, 0.3f, 0.3f);

    [Header("안개")]
    public bool fog;
    public Color fogColor = Color.gray;
    public float fogDensity = 0.02f;

    [Header("후처리·반사")]
    public VolumeProfile profile;
    [Tooltip("Tools > Shader Test 메뉴가 이 환경을 켜 놓고 구운 반사 큐브맵")]
    public Texture reflection;

    [Header("배경 소품")]
    [Tooltip("장면에서 이 이름의 오브젝트만 켜고, 다른 환경의 소품은 끈다")]
    public string propsRoot;
}
