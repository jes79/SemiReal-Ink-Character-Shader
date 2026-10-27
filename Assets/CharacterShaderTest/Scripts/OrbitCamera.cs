using System;
using UnityEngine;
using UnityEngine.InputSystem;

// 캐릭터 둘러보기 카메라
// 왼쪽 드래그 = 돌려 보기 / 휠 = 확대 (확대할수록 얼굴 쪽으로) / 오른쪽·가운데 드래그 = 위아래 이동
public class OrbitCamera : MonoBehaviour
{
    public Transform target;

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

    // 마우스가 UI 위에 있으면 시점을 돌리지 않는다 (UI가 알려 준다)
    public Func<Vector2, bool> isPointerOverUi;
    // 와이프 선을 끄는 중 등 다른 조작이 마우스를 쓰고 있을 때
    public bool inputBlocked;

    public bool AutoRotate { get; set; }

    float height = 2f;
    Vector3 basePoint;
    float yaw, pitch, zoom01, panY;
    float curYaw, curPitch, curZoom01, curPanY;
    float yawVel, pitchVel, zoomVel, panVel;
    bool dragging;

    void Start() => ResetView(snap: true);

    public void ResetView(bool snap)
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
        if (mouse != null && !inputBlocked)
        {
            Vector2 pos = mouse.position.ReadValue();
            bool anyPressed = mouse.leftButton.isPressed || mouse.rightButton.isPressed || mouse.middleButton.isPressed;
            bool overUi = isPointerOverUi != null && isPointerOverUi(pos);
            // UI 밖에서 누르기 시작한 드래그만 카메라가 받는다 (슬라이더를 끌다 화면 밖으로 나가도 시점이 돌지 않게)
            if (!anyPressed)
                dragging = false;
            else if (!dragging && !overUi && (mouse.leftButton.wasPressedThisFrame || mouse.rightButton.wasPressedThisFrame || mouse.middleButton.wasPressedThisFrame))
                dragging = true;

            if (dragging)
            {
                Vector2 delta = mouse.delta.ReadValue();
                if (mouse.leftButton.isPressed)
                {
                    yaw += delta.x * rotateSpeed;
                    pitch -= delta.y * rotateSpeed;
                }
                if (mouse.rightButton.isPressed || mouse.middleButton.isPressed)
                    panY -= delta.y * panSpeed * height * Mathf.Lerp(0.3f, 1f, zoom01);
            }
            if (!overUi)
                zoom01 -= mouse.scroll.ReadValue().y * zoomSpeed;
        }

        if (AutoRotate)
            yaw += autoRotateSpeed * Time.deltaTime;
        pitch = Mathf.Clamp(pitch, minPitch, maxPitch);
        zoom01 = Mathf.Clamp01(zoom01);
        panY = Mathf.Clamp(panY, -height * 0.5f, height * 0.5f);
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
