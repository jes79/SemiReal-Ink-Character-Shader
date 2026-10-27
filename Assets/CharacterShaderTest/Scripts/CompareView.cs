using UnityEngine;
using UnityEngine.Rendering.Universal;

// A/B 비교 화면: 같은 자리에 겹쳐 둔 두 캐릭터를 레이어로 나눠 카메라 두 대로 그린다.
// 메인 카메라 = A 쪽 캐릭터, 비교 카메라(메인의 자식) = B 쪽 캐릭터.
// 와이프·B만 보기에서는 B 화면을 전체 크기 렌더 텍스처에 그려 두고, UI가 필요한 부분만 덮는다
// → 비네트·필름 그레인 같은 화면 효과가 양쪽에 똑같이 걸린다
[RequireComponent(typeof(Camera))]
public class CompareView : MonoBehaviour
{
    public enum Mode { Wipe, SideBySide, AOnly, BOnly }

    public Camera compareCamera;
    public string layerA = "ShaderCompare_Original";
    public string layerB = "ShaderCompare_New";

    public Mode mode = Mode.Wipe;
    [Range(0f, 1f)] public float wipe = 0.5f;

    Camera mainCamera;
    RenderTexture compareTexture;

    // UI가 그릴 B 화면 (와이프·B만 보기일 때만 값이 있다)
    public RenderTexture OverlayTexture => mode == Mode.Wipe || mode == Mode.BOnly ? compareTexture : null;

    void Awake()
    {
        mainCamera = GetComponent<Camera>();
        int a = LayerMask.NameToLayer(layerA);
        int b = LayerMask.NameToLayer(layerB);
        if (a < 0 || b < 0)
        {
            Debug.LogError($"레이어 '{layerA}' / '{layerB}'가 없습니다. Tools > Shader Test 메뉴로 장면을 다시 만드세요.");
            return;
        }
        mainCamera.cullingMask &= ~(1 << b);
        if (compareCamera != null)
            compareCamera.cullingMask = (mainCamera.cullingMask & ~(1 << a)) | (1 << b);
    }

    public void SetPostProcessing(bool on)
    {
        mainCamera.GetUniversalAdditionalCameraData().renderPostProcessing = on;
        if (compareCamera != null)
            compareCamera.GetUniversalAdditionalCameraData().renderPostProcessing = on;
    }

    public void Flip() => mode = mode == Mode.BOnly ? Mode.AOnly : Mode.BOnly;

    void LateUpdate()
    {
        if (compareCamera == null)
            return;

        // 비교 카메라는 메인 카메라의 자식이라 위치·방향은 자동으로 같다. 렌즈 설정만 맞춘다
        compareCamera.fieldOfView = mainCamera.fieldOfView;
        compareCamera.nearClipPlane = mainCamera.nearClipPlane;
        compareCamera.farClipPlane = mainCamera.farClipPlane;
        compareCamera.depth = mainCamera.depth + 1;

        switch (mode)
        {
            case Mode.SideBySide:
                // 화면 반씩: 각 카메라가 자기 칸 크기로 그린다 (후처리도 칸마다 따로)
                mainCamera.rect = new Rect(0f, 0f, 0.5f, 1f);
                compareCamera.targetTexture = null;
                compareCamera.rect = new Rect(0.5f, 0f, 0.5f, 1f);
                compareCamera.enabled = true;
                break;

            case Mode.AOnly:
                mainCamera.rect = new Rect(0f, 0f, 1f, 1f);
                compareCamera.enabled = false;
                break;

            default: // Wipe, BOnly
                mainCamera.rect = new Rect(0f, 0f, 1f, 1f);
                compareCamera.rect = new Rect(0f, 0f, 1f, 1f);
                compareCamera.targetTexture = GetCompareTexture();
                compareCamera.enabled = true;
                break;
        }
    }

    RenderTexture GetCompareTexture()
    {
        int w = Mathf.Max(1, Screen.width);
        int h = Mathf.Max(1, Screen.height);
        if (compareTexture != null && (compareTexture.width != w || compareTexture.height != h))
        {
            compareCamera.targetTexture = null;
            compareTexture.Release();
            Destroy(compareTexture);
            compareTexture = null;
        }
        if (compareTexture == null)
        {
            compareTexture = new RenderTexture(w, h, 24, RenderTextureFormat.ARGB32, RenderTextureReadWrite.sRGB)
            {
                name = "ShaderCompare_B",
                antiAliasing = Mathf.Max(1, QualitySettings.antiAliasing),
            };
            compareTexture.Create();
        }
        return compareTexture;
    }

    void OnDestroy()
    {
        if (compareTexture == null)
            return;
        if (compareCamera != null)
            compareCamera.targetTexture = null;
        compareTexture.Release();
        Destroy(compareTexture);
    }
}
