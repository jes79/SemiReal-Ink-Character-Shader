using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.Rendering.Universal;

// 캐릭터 살펴보기 + 원본/새 쉐이더 비교 뷰어. 메인 카메라에 붙인다.
// 원본 캐릭터와 새 쉐이더 캐릭터를 같은 자리에 두고, 서로 다른 레이어에 넣는다.
// 메인 카메라는 원본을, 비교 카메라(자식)는 새 쉐이더를 그린다.
//
// 조작: 왼쪽 드래그 = 돌려 보기 / 휠 = 확대 (확대할수록 얼굴 쪽으로) / 오른쪽 드래그 = 위아래 이동
//       와이프 모드에서 가운데 선 드래그 = 비교 위치 / 1~4 = 비교 방식 / Tab = 원본↔새 쉐이더 빠르게 전환
//       P = 후처리 켜기·끄기 / M = 재질 판별 보기 / O = 먹 아웃라인 / Space = 자동 회전 / F = 처음 시점
//       H = 조작 패널 접기·펴기
[RequireComponent(typeof(Camera))]
public class CharacterCompareViewer : MonoBehaviour
{
    public enum CompareMode { Wipe, SideBySide, OriginalOnly, NewOnly }

    [Header("대상")]
    public Transform target;
    public Camera compareCamera;
    public string originalLayerName = "ShaderCompare_Original";
    public string newLayerName = "ShaderCompare_New";
    public Light keyLight;

    [Header("비교")]
    public CompareMode mode = CompareMode.Wipe;
    [Range(0f, 1f)] public float wipe = 0.5f;

    [Header("회전")]
    public float rotateSpeed = 0.25f;
    public float minPitch = -20f;
    public float maxPitch = 60f;
    public float autoRotateSpeed = 20f;

    [Header("확대")]
    public float zoomSpeed = 0.0015f;
    public float minDistanceRatio = 0.22f;
    public float maxDistanceRatio = 2.2f;
    [Tooltip("최대로 확대했을 때 바라보는 높이 (키 대비, 얼굴 높이)")]
    public float closeFocusHeight = 0.88f;
    [Tooltip("최대로 축소했을 때 바라보는 높이 (키 대비, 몸 중앙)")]
    public float farFocusHeight = 0.5f;

    [Header("이동")]
    public float panSpeed = 0.002f;
    public float smoothTime = 0.12f;

    static readonly int DebugMasksId = Shader.PropertyToID("_ShaderTestDebugMasks");
    static readonly int HideOutlineId = Shader.PropertyToID("_ShaderTestHideOutline");

    Camera mainCamera;
    float height = 2f;
    Vector3 basePoint;
    float yaw, pitch, zoom01, panY;
    float curYaw, curPitch, curZoom01, curPanY;
    float yawVel, pitchVel, zoomVel, panVel;
    bool autoRotate;
    bool postProcessing = true;
    bool showMasks;
    bool showOutline = true;
    bool showPanel = true;
    bool draggingDivider;
    float lightYaw;
    readonly List<Rect> guiRects = new List<Rect>();
    RenderTexture compareTexture;

    void Start()
    {
        mainCamera = GetComponent<Camera>();
        int originalLayer = LayerMask.NameToLayer(originalLayerName);
        int newLayer = LayerMask.NameToLayer(newLayerName);
        if (originalLayer < 0 || newLayer < 0)
            Debug.LogError($"레이어 '{originalLayerName}' / '{newLayerName}'가 없습니다. Tools > Shader Test 메뉴로 장면을 다시 만드세요.");
        else
        {
            mainCamera.cullingMask &= ~(1 << newLayer);
            if (compareCamera != null)
                compareCamera.cullingMask = mainCamera.cullingMask & ~(1 << originalLayer) | (1 << newLayer);
        }

        if (keyLight != null)
            lightYaw = keyLight.transform.eulerAngles.y;

        ResetView(snap: true);
        ApplyPostProcessing();
    }

    void OnDisable()
    {
        Shader.SetGlobalFloat(DebugMasksId, 0f);
        Shader.SetGlobalFloat(HideOutlineId, 0f);
    }

    void ResetView(bool snap)
    {
        if (target == null)
            return;
        var bounds = GetBounds(target);
        height = Mathf.Max(bounds.size.y, 0.1f);
        basePoint = new Vector3(bounds.center.x, bounds.min.y, bounds.center.z);

        // 캐릭터 정면(+Z 쪽)에서 바라보는 시점
        yaw = target.eulerAngles.y + 180f;
        pitch = 8f;
        zoom01 = 0.35f;
        panY = 0f;
        if (snap)
        {
            curYaw = yaw; curPitch = pitch; curZoom01 = zoom01; curPanY = panY;
            yawVel = pitchVel = zoomVel = panVel = 0f;
        }
    }

    void Update()
    {
        // 게임 창이 활성 상태가 아닐 때(에디터의 다른 창을 드래그하는 중 등)는 마우스로 시점이 돌지 않게 한다
        var mouse = Application.isFocused ? Mouse.current : null;
        var keyboard = Keyboard.current;

        if (mouse != null)
        {
            Vector2 pos = mouse.position.ReadValue();
            bool overGui = IsOverGui(pos);
            float dividerX = wipe * Screen.width;

            if (mouse.leftButton.wasPressedThisFrame && !overGui && mode == CompareMode.Wipe && Mathf.Abs(pos.x - dividerX) < 14f)
                draggingDivider = true;
            if (!mouse.leftButton.isPressed)
                draggingDivider = false;

            if (draggingDivider)
                wipe = Mathf.Clamp01(pos.x / Mathf.Max(1f, Screen.width));
            else if (!overGui)
            {
                Vector2 delta = mouse.delta.ReadValue();
                if (mouse.leftButton.isPressed)
                {
                    yaw += delta.x * rotateSpeed;
                    pitch -= delta.y * rotateSpeed;
                }
                if (mouse.rightButton.isPressed || mouse.middleButton.isPressed)
                    panY -= delta.y * panSpeed * height * Mathf.Lerp(0.3f, 1f, zoom01);
                zoom01 -= mouse.scroll.ReadValue().y * zoomSpeed;
            }
        }

        if (keyboard != null)
        {
            if (keyboard.digit1Key.wasPressedThisFrame) mode = CompareMode.Wipe;
            if (keyboard.digit2Key.wasPressedThisFrame) mode = CompareMode.SideBySide;
            if (keyboard.digit3Key.wasPressedThisFrame) mode = CompareMode.OriginalOnly;
            if (keyboard.digit4Key.wasPressedThisFrame) mode = CompareMode.NewOnly;
            if (keyboard.tabKey.wasPressedThisFrame) FlipSingle();
            if (keyboard.pKey.wasPressedThisFrame) { postProcessing = !postProcessing; ApplyPostProcessing(); }
            if (keyboard.mKey.wasPressedThisFrame) showMasks = !showMasks;
            if (keyboard.oKey.wasPressedThisFrame) showOutline = !showOutline;
            if (keyboard.hKey.wasPressedThisFrame) showPanel = !showPanel;
            if (keyboard.spaceKey.wasPressedThisFrame) autoRotate = !autoRotate;
            if (keyboard.fKey.wasPressedThisFrame) ResetView(snap: false);
        }

        if (autoRotate)
            yaw += autoRotateSpeed * Time.deltaTime;
        pitch = Mathf.Clamp(pitch, minPitch, maxPitch);
        zoom01 = Mathf.Clamp01(zoom01);
        panY = Mathf.Clamp(panY, -height * 0.5f, height * 0.5f);

        Shader.SetGlobalFloat(DebugMasksId, showMasks ? 1f : 0f);
        Shader.SetGlobalFloat(HideOutlineId, showOutline ? 0f : 1f);
        if (keyLight != null)
        {
            var e = keyLight.transform.eulerAngles;
            keyLight.transform.rotation = Quaternion.Euler(e.x, lightYaw, e.z);
        }
    }

    // 원본만 ↔ 새 쉐이더만 (와이프·나란히 상태에서 누르면 새 쉐이더만부터)
    void FlipSingle()
    {
        mode = mode == CompareMode.NewOnly ? CompareMode.OriginalOnly : CompareMode.NewOnly;
    }

    void LateUpdate()
    {
        if (target == null)
            return;

        curYaw = Mathf.SmoothDampAngle(curYaw, yaw, ref yawVel, smoothTime);
        curPitch = Mathf.SmoothDamp(curPitch, pitch, ref pitchVel, smoothTime);
        curZoom01 = Mathf.SmoothDamp(curZoom01, zoom01, ref zoomVel, smoothTime);
        curPanY = Mathf.SmoothDamp(curPanY, panY, ref panVel, smoothTime);

        float distance = Mathf.Lerp(minDistanceRatio, maxDistanceRatio, curZoom01) * height;
        float focusHeight = Mathf.Lerp(closeFocusHeight, farFocusHeight, curZoom01) * height + curPanY;
        Vector3 focus = basePoint + Vector3.up * focusHeight;
        Quaternion rot = Quaternion.Euler(curPitch, curYaw, 0f);
        transform.SetPositionAndRotation(focus + rot * new Vector3(0f, 0f, -distance), rot);

        ApplyCompareLayout();
    }

    void ApplyCompareLayout()
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
            case CompareMode.SideBySide:
                // 화면 반씩: 각 카메라가 자기 칸 크기로 그린다 (후처리도 칸마다 따로)
                mainCamera.rect = new Rect(0f, 0f, 0.5f, 1f);
                compareCamera.targetTexture = null;
                compareCamera.rect = new Rect(0.5f, 0f, 0.5f, 1f);
                compareCamera.enabled = true;
                break;

            case CompareMode.OriginalOnly:
                mainCamera.rect = new Rect(0f, 0f, 1f, 1f);
                compareCamera.enabled = false;
                break;

            default: // Wipe, NewOnly
                // 새 쉐이더 화면을 메인과 똑같은 전체 크기로 따로 그려 두고, OnGUI에서 오른쪽 부분만 덮는다.
                // 화면을 잘라서 그리면 비네트·필름 그레인이 반쪽마다 따로 걸려 경계가 생기기 때문
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
                name = "ShaderCompare_New",
                antiAliasing = Mathf.Max(1, QualitySettings.antiAliasing),
            };
            compareTexture.Create();
        }
        return compareTexture;
    }

    void OnDestroy()
    {
        if (compareTexture != null)
        {
            if (compareCamera != null)
                compareCamera.targetTexture = null;
            compareTexture.Release();
            Destroy(compareTexture);
        }
    }

    void ApplyPostProcessing()
    {
        mainCamera.GetUniversalAdditionalCameraData().renderPostProcessing = postProcessing;
        if (compareCamera != null)
            compareCamera.GetUniversalAdditionalCameraData().renderPostProcessing = postProcessing;
    }

    void OnGUI()
    {
        guiRects.Clear();

        // ---- 새 쉐이더 화면 덮기 (와이프: 선 오른쪽만, 새 쉐이더만: 전체). 픽셀 단위로 맞춘다 ----
        float splitX = mode == CompareMode.NewOnly ? 0f : Mathf.Round(wipe * Screen.width);
        if ((mode == CompareMode.Wipe || mode == CompareMode.NewOnly) && compareTexture != null
            && Event.current.type == EventType.Repaint && splitX < Screen.width)
        {
            float u = splitX / Screen.width;
            GUI.DrawTextureWithTexCoords(new Rect(splitX, 0f, Screen.width - splitX, Screen.height),
                compareTexture, new Rect(u, 0f, 1f - u, 1f), false);
        }

        // ---- 화면 위 이름표와 와이프 선 ----
        var labelStyle = new GUIStyle(GUI.skin.box) { fontSize = 14 };
        switch (mode)
        {
            case CompareMode.Wipe:
                float x = splitX;
                GUI.color = new Color(1f, 1f, 1f, 0.85f);
                GUI.DrawTexture(new Rect(x - 1f, 0f, 2f, Screen.height), Texture2D.whiteTexture);
                GUI.DrawTexture(new Rect(x - 7f, Screen.height * 0.5f - 22f, 14f, 44f), Texture2D.whiteTexture);
                GUI.color = Color.white;
                if (wipe > 0.08f) GUI.Label(new Rect(x - 170f, 10f, 160f, 26f), "원본 (URP Lit)", labelStyle);
                if (wipe < 0.92f) GUI.Label(new Rect(x + 10f, 10f, 160f, 26f), "새 쉐이더", labelStyle);
                break;
            case CompareMode.SideBySide:
                GUI.Label(new Rect(Screen.width * 0.25f - 80f, 10f, 160f, 26f), "원본 (URP Lit)", labelStyle);
                GUI.Label(new Rect(Screen.width * 0.75f - 80f, 10f, 160f, 26f), "새 쉐이더", labelStyle);
                break;
            case CompareMode.OriginalOnly:
                GUI.Label(new Rect(Screen.width * 0.5f - 80f, 10f, 160f, 26f), "원본 (URP Lit)", labelStyle);
                break;
            case CompareMode.NewOnly:
                GUI.Label(new Rect(Screen.width * 0.5f - 80f, 10f, 160f, 26f), "새 쉐이더", labelStyle);
                break;
        }

        // ---- 조작 패널 (H로 접기) ----
        if (!showPanel)
        {
            var openButton = new Rect(10f, 46f, 140f, 28f);
            guiRects.Add(openButton);
            if (GUI.Button(openButton, "조작 패널 (H)")) showPanel = true;
            return;
        }
        var panel = new Rect(10f, 46f, 340f, 410f);
        guiRects.Add(panel);
        GUILayout.BeginArea(panel, GUI.skin.box);
        // 버튼을 눌러 모드가 바뀌어도 이번 프레임의 패널 구성은 그대로 둔다 (Layout/Repaint 단계 불일치 방지)
        var panelMode = mode;

        GUILayout.Label("비교 방식");
        GUILayout.BeginHorizontal();
        if (GUILayout.Toggle(mode == CompareMode.Wipe, "와이프(1)", GUI.skin.button)) mode = CompareMode.Wipe;
        if (GUILayout.Toggle(mode == CompareMode.SideBySide, "나란히(2)", GUI.skin.button)) mode = CompareMode.SideBySide;
        GUILayout.EndHorizontal();
        GUILayout.BeginHorizontal();
        if (GUILayout.Toggle(mode == CompareMode.OriginalOnly, "원본만(3)", GUI.skin.button)) mode = CompareMode.OriginalOnly;
        if (GUILayout.Toggle(mode == CompareMode.NewOnly, "새 쉐이더만(4)", GUI.skin.button)) mode = CompareMode.NewOnly;
        GUILayout.EndHorizontal();
        if (GUILayout.Button("원본 ↔ 새 쉐이더 빠르게 전환 (Tab)")) FlipSingle();

        if (panelMode == CompareMode.Wipe)
        {
            GUILayout.Label($"와이프 위치: {wipe * 100f:0}%");
            wipe = GUILayout.HorizontalSlider(wipe, 0f, 1f);
        }

        GUILayout.Space(4f);
        if (keyLight != null)
        {
            GUILayout.Label($"햇빛 방향: {Mathf.Repeat(lightYaw, 360f):0}°");
            lightYaw = GUILayout.HorizontalSlider(Mathf.Repeat(lightYaw, 360f), 0f, 360f);
        }

        bool newPost = GUILayout.Toggle(postProcessing, " 후처리 (P)");
        if (newPost != postProcessing) { postProcessing = newPost; ApplyPostProcessing(); }
        showMasks = GUILayout.Toggle(showMasks, " 재질 판별 보기 (M)  노랑 금속·흰색 은·초록 보석");
        showOutline = GUILayout.Toggle(showOutline, " 먹 아웃라인 (O)");
        autoRotate = GUILayout.Toggle(autoRotate, " 자동 회전 (Space)");
        if (GUILayout.Button("처음 시점 (F)")) ResetView(snap: false);

        GUILayout.Label("왼쪽 드래그: 돌리기   휠: 확대   오른쪽 드래그: 위아래");
        if (GUILayout.Button("패널 접기 (H)")) showPanel = false;
        GUILayout.EndArea();
    }

    bool IsOverGui(Vector2 screenPos)
    {
        // Input System 좌표(아래가 0) → IMGUI 좌표(위가 0)
        var guiPos = new Vector2(screenPos.x, Screen.height - screenPos.y);
        foreach (var r in guiRects)
            if (r.Contains(guiPos))
                return true;
        return false;
    }

    static Bounds GetBounds(Transform root)
    {
        var renderers = root.GetComponentsInChildren<Renderer>();
        if (renderers.Length == 0)
            return new Bounds(root.position + Vector3.up, new Vector3(1f, 2f, 1f));
        var b = renderers[0].bounds;
        foreach (var r in renderers)
            b.Encapsulate(r.bounds);
        return b;
    }
}
