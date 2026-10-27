using System.Collections.Generic;
using UnityEngine.InputSystem;

// 강의 모드 한 용어 안의 진행 단계
public enum LectureStep
{
    Opening,   // 디렉터의 강의 여는 인사 (용어 목록에서 강의를 시작했을 때 첫 용어 앞에 한 번)
    SelfIntro, // 분야 소개 (분야가 바뀔 때마다 그 분야 담당자가)
    Intro,     // ① 소개: 용어 이름 + 쉽게 말하면
    Concept,   // ② 개념
    Demo,      // ③ 직접 보기 (말풍선을 줄이고 랩 패널을 직접 조작)
    Prompt,    // ④ AI에게 이렇게 요청하기
}

// 강의 모드 진행 상태 (어느 용어의 몇 번째 단계인가)
// - 실제 화면(담당자·말풍선·진행 막대)과 랩 준비는 LectureOverlay 가 맡는다
// - 쉐이더 랩은 씬이 하나라서 용어를 옮길 때 씬을 다시 불러오지 않고, 랩 상태만 그 용어에 맞게 바꾼다
public static class LectureMode
{
    // ───── 강의 키 ─────
    // 랩 단축키(1~4·Tab·P·M·O·Space·F·H, 마우스)와 겹치지 않는 키만 고름.
    // 프레젠터 리모컨은 보통 PageDown/PageUp 을 보내므로 그대로 넘기기에 쓸 수 있다.
    public const Key NextKey = Key.PageDown;        // 다음 단계 (마지막 단계면 다음 용어)
    public const Key NextKeyAlt = Key.RightBracket; // ]
    public const Key PrevKey = Key.PageUp;          // 이전 단계 (첫 단계면 이전 용어)
    public const Key PrevKeyAlt = Key.LeftBracket;  // [
    public const Key NextTermKey = Key.End;         // 다음 용어로 바로
    public const Key PrevTermKey = Key.Home;        // 이전 용어로 바로
    public const Key HubKey = Key.Backspace;        // 용어 목록으로 (허브 대신)
    public const Key BubbleKey = Key.F3;            // 말풍선 숨기기/보이기
    public const Key ToggleKey = Key.F4;            // 강의 모드 켜기/끄기
    public const Key QuitKey = Key.Escape;          // 강의 플레이어 종료 (강의 진행 중이 아닐 때만)

    public const string KeyGuide =
        "PgDn / ]  다음   ·   PgUp / [  이전   ·   End  다음 용어   ·   Home  이전 용어   ·   F3  말풍선   ·   Backspace  용어 목록   ·   F4  강의 모드 끄기";

    public static bool Enabled { get; private set; }

    // 지금 진행 중인 용어와 그 안의 단계 번호
    public static string CurrentId { get; private set; }
    public static int StepIndex;
    public static bool CurrentHasSelfIntro { get; private set; }
    public static bool CurrentHasOpening { get; private set; }
    public static bool SamePresenterAsBefore { get; private set; } // 앞 분야와 담당자가 같으면 인사를 짧게
    public static bool CurrentIsDirectStart { get; private set; } // 명령줄로 바로 시작한 첫 용어 (① 소개를 인사로 시작)

    // 분야 소개를 마친 분야 번호 (0 = 아직 없음)
    static int introducedCategory;
    static bool pendingOpening;
    static bool pendingDirectStart;
    static bool pendingLast;

    // 이 프로젝트는 플레이할 때 도메인을 다시 불러오지 않는다(Enter Play Mode Options) → static 값이 지난 플레이에서 남는다.
    // 플레이를 시작할 때마다 처음 상태로 되돌린다
    [UnityEngine.RuntimeInitializeOnLoadMethod(UnityEngine.RuntimeInitializeLoadType.SubsystemRegistration)]
    static void ResetStatics()
    {
        Enabled = false;
        CurrentId = null;
        StepIndex = 0;
        introducedCategory = 0;
        pendingOpening = pendingDirectStart = pendingLast = false;
    }

    public static void SetEnabled(bool on)
    {
        Enabled = on;
        if (!on) CurrentId = null;
    }

    // 용어 목록에서 고른 용어부터 강의 시작 (디렉터 인사 → 분야 소개부터). 이어서 LectureOverlay 가 그 용어를 연다
    public static void StartAt()
    {
        Enabled = true;
        introducedCategory = 0;
        Go(false);
        pendingOpening = true; // Go 다음에 둬야 지워지지 않음
    }

    // 명령줄(-glossaryStart 0304 -glossaryLecture)로 한 용어부터 바로 강의 (LectureLaunch). 이어서 LectureOverlay 가 그 용어를 연다
    // 디렉터 인사 없이 ① 소개부터. 분야의 첫 용어면 분야 소개부터
    public static void StartFromTerm(GlossaryEntry entry)
    {
        Enabled = true;
        // 분야 첫 용어가 아니면 '이 분야는 이미 소개했다'로 두어 분야 소개를 건너뜀
        introducedCategory = IndexInCategory(entry) == 1 ? 0 : entry.cat;
        Go(false);
        pendingDirectStart = true; // Go 다음에 둬야 지워지지 않음
    }

    // 다른 용어로 옮길 준비. atLastStep = 이전 용어로 거꾸로 갈 때 마지막 단계(④)부터
    public static void Go(bool atLastStep)
    {
        CurrentId = null;
        pendingLast = atLastStep;
        pendingOpening = false;
        pendingDirectStart = false;
    }

    // 용어를 열 때 LectureOverlay 가 호출: 이 용어의 단계 목록과 시작 단계를 정함
    public static LectureStep[] BeginEntry(GlossaryEntry entry)
    {
        // 분야의 첫 용어이거나, 다른 분야에서 넘어왔으면 담당자가 먼저 분야 소개
        CurrentHasSelfIntro = IndexInCategory(entry) == 1 || introducedCategory != entry.cat;
        SamePresenterAsBefore = introducedCategory > 0 &&
                                LecturePresenters.For(introducedCategory).Key == LecturePresenters.For(entry.cat).Key;
        introducedCategory = entry.cat;
        CurrentHasOpening = pendingOpening;
        pendingOpening = false;
        CurrentIsDirectStart = pendingDirectStart;
        pendingDirectStart = false;

        var steps = StepsFor(entry, CurrentHasOpening, CurrentHasSelfIntro);
        CurrentId = entry.id;
        StepIndex = pendingLast ? steps.Length - 1 : 0;
        pendingLast = false;
        return steps;
    }

    static LectureStep[] StepsFor(GlossaryEntry entry, bool opening, bool selfIntro)
    {
        var list = new List<LectureStep>(6);
        if (opening) list.Add(LectureStep.Opening);
        if (selfIntro) list.Add(LectureStep.SelfIntro);
        list.Add(LectureStep.Intro);
        if (!string.IsNullOrEmpty(entry.concept)) list.Add(LectureStep.Concept);
        list.Add(LectureStep.Demo);
        list.Add(LectureStep.Prompt);
        return list.ToArray();
    }

    // ───── 용어 순서 계산 (JSON entries 순서) ─────

    public static bool TryNeighbor(string id, int dir, out GlossaryEntry entry)
    {
        var all = ShaderLabGlossary.Entries;
        for (int i = 0; i < all.Length; i++)
        {
            if (all[i].id != id) continue;
            int j = i + dir;
            if (j >= 0 && j < all.Length)
            {
                entry = all[j];
                return true;
            }
            break;
        }
        entry = null;
        return false;
    }

    // 분야 안에서 몇 번째 용어인가 (1부터)
    public static int IndexInCategory(GlossaryEntry entry)
    {
        int n = 0;
        foreach (var e in ShaderLabGlossary.Entries)
        {
            if (e.cat != entry.cat) continue;
            n++;
            if (e.id == entry.id) return n;
        }
        return 0;
    }

    public static int CountInCategory(int category)
    {
        int n = 0;
        foreach (var e in ShaderLabGlossary.Entries) if (e.cat == category) n++;
        return n;
    }

    public static bool KeyDown(Key key) => Keyboard.current != null && Keyboard.current[key].wasPressedThisFrame;
}
