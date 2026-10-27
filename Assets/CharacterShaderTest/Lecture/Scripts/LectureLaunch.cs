using UnityEngine;

// 쉐이더 랩 강의 시작 처리 (강의 프로그램 Builds/LecturePlayer/ShaderLab.exe 와 에디터 플레이 둘 다)
// - 장면에 쉐이더 랩(ShaderLabController + ShaderLabUI)이 있으면 강의 화면(LectureOverlay)을 숨긴 채 만들어 둔다
//   → 에디터에서 플레이 중 F4 를 누르면 용어 목록이 열리고, 용어를 누르면 강의가 시작된다
// - 명령줄 인자 (OMP Studio 앱의 ▶ 예제 실행이 이렇게 실행한다)
//     -glossaryStart 0304 -glossaryLecture : 그 용어부터 강의 모드 (① 소개부터, 분야 첫 용어면 분야 소개부터)
//     -glossaryStart 0304                  : 강의 없이 랩만 그 용어에 맞게 준비
//     인자 없음                            : 지금처럼 랩 자유 모드
//   없는 번호를 주면 용어 목록을 열고 위쪽에 알림을 한 줄 띄운다
// - 강의 프로그램 빌드에서만: 테두리 없는 창 전체화면(현재 모니터 해상도)으로 시작
//   (Tools > Shader Test > Build Lecture Player 가 SHADER_LAB_LECTURE_PLAYER 정의를 붙여 빌드함)
public static class LectureLaunch
{
    public const string StartArg = "-glossaryStart";
    public const string LectureArg = "-glossaryLecture";

    // 상수(const) 대신 속성으로 둬서 '실행되지 않는 코드' 경고가 나지 않게 함
#if SHADER_LAB_LECTURE_PLAYER
    public static bool IsLecturePlayer => true;
#else
    public static bool IsLecturePlayer => false;
#endif

    // 첫 장면이 뜨기 전에 창 모양부터 정함
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    static void SetupWindow()
    {
        if (!IsLecturePlayer || Application.isEditor) return;

        // 앱(OMP Studio)이 창 포커스를 가져가도 강의(글자 타이핑·랩 준비)가 멈추지 않게 (프로젝트 설정은 그대로, 강의 프로그램에서만)
        Application.runInBackground = true;

        // -screen-width / -screen-fullscreen 처럼 창 크기를 직접 준 실행은 그대로 둔다
        foreach (string arg in System.Environment.GetCommandLineArgs())
            if (arg.StartsWith("-screen-") || arg == "-popupwindow" || arg == "-window-mode") return;

        // 해상도·창 모드는 레지스트리에 남아 다음 실행에 다시 쓰이므로 매번 맞춰 둔다
        var display = Screen.mainWindowDisplayInfo;
        int width = display.width > 0 ? display.width : Screen.currentResolution.width;
        int height = display.height > 0 ? display.height : Screen.currentResolution.height;
        Screen.SetResolution(width, height, FullScreenMode.FullScreenWindow);
    }

    // 장면이 열린 뒤: 강의 화면을 만들고 인자를 본다
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    static void HandleArgs()
    {
        var lab = Object.FindAnyObjectByType<ShaderLabController>();
        var labUI = Object.FindAnyObjectByType<ShaderLabUI>();
        if (lab == null || labUI == null) return; // 쉐이더 랩 장면이 아님

        var lecture = LectureOverlay.Create(lab, labUI);

        string[] args = System.Environment.GetCommandLineArgs();
        string id = null;
        bool lectureMode = false;
        for (int i = 0; i < args.Length; i++)
        {
            if (args[i] == LectureArg) lectureMode = true;
            if (args[i] == StartArg && i + 1 < args.Length) id = args[i + 1].Trim();
        }
        if (id == null) return;

        if (!ShaderLabGlossary.TryFind(id, out var entry))
        {
            Debug.LogWarning($"[LectureLaunch] {StartArg} {id}: 없는 용어 → 용어 목록");
            lecture.RunWhenReady(() => lecture.OpenTermList($"'{id}' 번 용어가 없어 용어 목록을 열었습니다. 용어 번호는 4자리입니다 (예: 0304)."));
            return;
        }

        Debug.Log($"[LectureLaunch] {entry.id} {entry.term} 열기 (강의 모드 {(lectureMode ? "켬" : "끔")})");
        // 랩이 Start 에서 처음 상태를 잡은 다음에 용어에 맞게 바꿔야 덮어쓰이지 않는다
        if (lectureMode)
            lecture.RunWhenReady(() => lecture.StartFromTerm(entry));
        else
            lecture.RunWhenReady(() => lecture.PrepareLabOnly(entry));
    }
}
