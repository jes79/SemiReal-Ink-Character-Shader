using System;
using System.Collections;
using System.Text;
using System.Text.RegularExpressions;
using UnityEngine;
using UnityEngine.UIElements;

// 강의 모드 화면: 오른쪽 위 진행 막대 + 오른쪽 아래 담당자(픽셀아트 캐릭터·이름표·말풍선) + 아래쪽 키 안내
// + ③ 직접 보기 조작 카드 + 용어 목록(허브 대신)
// - 랩 패널(UI Toolkit)과 같은 PanelSettings 에 한 층 더 올린다 → 같은 한글 글꼴·같은 화면 비율(1920x1080 기준)
// - 랩 패널이 왼쪽(12~392 px)을 쓰므로 강의 UI 는 그 오른쪽에만 둔다 → ③ 에서 랩 패널을 그대로 눌러 볼 수 있다
// - 대사는 ShaderLabGlossary.json (용어·쉽게 말하면·개념·조작·확인 포인트·요청 예문)으로 조합한다
// LectureLaunch 가 장면이 열릴 때 만들어 두고, F4 / 명령줄 인자로 켠다
public class LectureOverlay : MonoBehaviour
{
    const float LabPanelRight = 404f;      // 랩 패널 오른쪽 끝 + 여백 (키 안내가 여기서 시작)
    const float RightColumnWidth = 680f;   // 진행 막대·③ 조작 카드 너비 (오른쪽 위)
    const float ProgressHeight = 88f;
    const float Margin = 24f;
    const float CharSlotWidth = 132f;      // 캐릭터 그림의 가장 넓은 폭 (상태마다 폭이 달라도 말풍선이 흔들리지 않게)
    const float CharBottom = 8f;           // 캐릭터가 서 있는 높이
    const float BubbleBigWidth = 960f, BubbleSmallWidth = 440f;
    const float BigBubbleY = 110f;         // 큰 말풍선 아래 높이
    const float CharsPerSecond = 45f;      // 말풍선 글자가 나오는 속도

    static readonly Color BubbleColor = new Color32(250, 250, 252, 255); // 불투명 (뒤 화면이 비치지 않게)
    static readonly Color InkColor = new Color32(32, 34, 44, 255);
    static readonly Color PanelColor = new Color(0.07f, 0.08f, 0.1f, 0.9f);
    static readonly Color ButtonColor = new Color(0.2f, 0.24f, 0.34f, 1f);
    static readonly Color ButtonHover = new Color(0.3f, 0.36f, 0.5f, 1f);

    ShaderLabController lab;
    ShaderLabUI labUI;
    bool ready;                 // 랩이 Start 를 마쳤는가 (그 전에는 랩을 준비할 수 없음)
    Action whenReady;

    GlossaryEntry entry;
    LecturePresenter presenter;
    LectureStep[] steps;

    VisualElement layer, lectureRoot;
    VisualElement progressFill;
    Label progressTitle, progressSteps;
    VisualElement demoCard;
    ScrollView demoScroll;       // 카드 내용이 말풍선 위까지 다 들어가지 않으면 휠로 내려 본다
    Label demoTitle, demoControls, demoCheck;
    Image character;
    VisualElement fallback;      // 그림이 없을 때 보이는 원 + 글자
    Label fallbackLetter;
    Vector2 characterSize;
    float characterScale = 2f;
    float bobTime;
    VisualElement bubble, bubbleTail, nameTag;
    Label nameTagText, bubbleText, continueMark;
    VisualElement promptBlock, cardsRow;
    Label badText, goodText;
    readonly Label[] elementNotes = new Label[4];
    readonly VisualElement[] elementBgs = new VisualElement[4];
    VisualElement termList;
    Label termNotice;

    string line = "";
    float typed;
    int shownChars, totalChars;
    bool typing;
    bool bubbleHidden;

    public LectureStep CurrentStep => steps[LectureMode.StepIndex];
    bool TermListOpen => termList.style.display == DisplayStyle.Flex;

    // 랩 패널과 같은 PanelSettings 를 쓰는 UIDocument 를 하나 더 만들어 그 위에 강의 UI 를 그린다
    public static LectureOverlay Create(ShaderLabController lab, ShaderLabUI labUI)
    {
        var go = new GameObject("Lecture");
        go.SetActive(false);
        var labDoc = labUI.GetComponent<UIDocument>();
        var doc = go.AddComponent<UIDocument>();
        doc.panelSettings = labDoc.panelSettings;
        doc.sortingOrder = labDoc.sortingOrder + 1; // 랩 패널 위
        var overlay = go.AddComponent<LectureOverlay>();
        overlay.lab = lab;
        overlay.labUI = labUI;
        go.SetActive(true); // OnEnable 에서 화면을 만든다
        return overlay;
    }

    // 랩(ShaderLabController.Start)이 준비된 뒤에 할 일 (명령줄로 시작할 때 LectureLaunch 가 사용)
    public void RunWhenReady(Action action)
    {
        if (ready) action();
        else whenReady += action;
    }

    IEnumerator Start()
    {
        yield return null; // 모든 Start 가 끝난 다음 프레임
        ready = true;
        whenReady?.Invoke();
        whenReady = null;
    }

    void OnEnable()
    {
        var doc = GetComponent<UIDocument>();
        doc.rootVisualElement.pickingMode = PickingMode.Ignore;
        Stretch(doc.rootVisualElement); // 화면 전체 (오른쪽·아래 기준 배치가 화면 가장자리를 따르게)
        layer = new VisualElement { name = "lecture-layer", pickingMode = PickingMode.Ignore };
        Stretch(layer);
        doc.rootVisualElement.Add(layer);
        if (labUI.KoreanFont != null)
            layer.style.unityFontDefinition = FontDefinition.FromSDFFont(labUI.KoreanFont);

        lectureRoot = new VisualElement { name = "lecture", pickingMode = PickingMode.Ignore };
        Stretch(lectureRoot);
        layer.Add(lectureRoot);
        BuildProgress(lectureRoot);
        BuildDemoCard(lectureRoot);
        BuildCharacter(lectureRoot);
        BuildBubble(lectureRoot);
        BuildKeyGuide(lectureRoot);
        BuildTermList(layer);

        // ③ 조작 카드가 말풍선을 덮지 않게: 말풍선·캐릭터·화면 크기가 바뀔 때마다 카드 높이를 다시 맞춘다
        bubble.RegisterCallback<GeometryChangedEvent>(_ => FitDemoCard());
        character.RegisterCallback<GeometryChangedEvent>(_ => FitDemoCard());
        demoCard.RegisterCallback<GeometryChangedEvent>(_ => FitDemoCard());

        lectureRoot.style.display = DisplayStyle.None;
        termList.style.display = DisplayStyle.None;
    }

    // ───── 켜고 끄기 ─────

    // 용어 목록에서 고른 용어부터 (디렉터 인사 → 분야 소개 → ①)
    public void StartAt(GlossaryEntry e)
    {
        LectureMode.StartAt();
        OpenEntry(e);
    }

    // 명령줄로 한 용어부터 (① 소개부터, 분야 첫 용어면 분야 소개부터)
    public void StartFromTerm(GlossaryEntry e)
    {
        LectureMode.StartFromTerm(e);
        OpenEntry(e);
    }

    // 강의 없이 그 용어 상태로 랩만 준비 (-glossaryStart 만 줬을 때)
    public void PrepareLabOnly(GlossaryEntry e)
    {
        LectureLabSetup.Apply(lab, labUI, e.lab, $"[Lecture] {e.id} lab");
    }

    // 허브 대신: 강의를 멈추고 랩을 처음 상태로 돌린 뒤 용어 목록을 띄움 (용어를 누르면 디렉터 인사부터 다시)
    public void OpenTermList(string notice)
    {
        LectureMode.SetEnabled(false);
        lectureRoot.style.display = DisplayStyle.None;
        termNotice.text = notice ?? "";
        termNotice.style.display = string.IsNullOrEmpty(notice) ? DisplayStyle.None : DisplayStyle.Flex;
        termList.style.display = DisplayStyle.Flex;
    }

    // F4 로 끄기: 강의 UI·용어 목록을 닫고 지금 랩 상태 그대로 자유 모드
    void CloseAll()
    {
        LectureMode.SetEnabled(false);
        typing = false;
        lectureRoot.style.display = DisplayStyle.None;
        termList.style.display = DisplayStyle.None;
    }

    void OpenEntry(GlossaryEntry e)
    {
        entry = e;
        presenter = LecturePresenters.For(e.cat);
        steps = LectureMode.BeginEntry(e);
        LectureLabSetup.Apply(lab, labUI, e.lab, $"[Lecture] {e.id} lab"); // 용어집의 '그 용어 씬 열기'에 해당

        progressFill.style.backgroundColor = presenter.Accent;
        demoTitle.text = $"<color=#FFD479>③ 직접 보기</color>   {e.term}";
        demoControls.text = $"<color=#9BE89B>조작</color>\n{e.controls}";
        demoCheck.text = $"<color=#FFD479>확인 포인트</color>\n{e.check}";
        demoScroll.scrollOffset = Vector2.zero;

        termList.style.display = DisplayStyle.None;
        lectureRoot.style.display = DisplayStyle.Flex;
        ShowStep();
    }

    // ───── 입력 ─────

    void Update()
    {
        if (!ready) return;

        if (LectureMode.KeyDown(LectureMode.ToggleKey))
        {
            if (LectureMode.Enabled || TermListOpen) CloseAll();
            else OpenTermList(null);
            return;
        }

        if (!LectureMode.Enabled)
        {
            // 강의 플레이어에서만: 강의 중이 아닐 때(용어 목록·랩 자유 모드) Esc = 종료 (테두리 없는 전체화면이라 닫을 방법이 필요)
            if (LectureLaunch.IsLecturePlayer && !Application.isEditor && LectureMode.KeyDown(LectureMode.QuitKey))
                Application.Quit();
            return;
        }

        UpdateTyping();
        UpdateBob();

        if (LectureMode.KeyDown(LectureMode.NextKey) || LectureMode.KeyDown(LectureMode.NextKeyAlt)) Next();
        else if (LectureMode.KeyDown(LectureMode.PrevKey) || LectureMode.KeyDown(LectureMode.PrevKeyAlt)) Prev();
        else if (LectureMode.KeyDown(LectureMode.NextTermKey)) GoNeighbor(+1, false);
        else if (LectureMode.KeyDown(LectureMode.PrevTermKey)) GoNeighbor(-1, false);
        else if (LectureMode.KeyDown(LectureMode.HubKey)) GoHub();
        else if (LectureMode.KeyDown(LectureMode.BubbleKey)) ToggleBubble();
    }

    // 다음: 글자가 나오는 중이면 먼저 전부 보여 주고, 다 나왔으면 다음 단계 / 다음 용어
    public void Next()
    {
        if (typing)
        {
            SkipTyping();
            return;
        }
        if (LectureMode.StepIndex < steps.Length - 1)
        {
            LectureMode.StepIndex++;
            ShowStep();
        }
        else if (!GoNeighbor(+1, false))
        {
            GoHub(); // 마지막 용어까지 끝나면 용어 목록으로
        }
    }

    public void Prev()
    {
        if (LectureMode.StepIndex > 0)
        {
            LectureMode.StepIndex--;
            ShowStep();
        }
        else
        {
            GoNeighbor(-1, true); // 이전 용어의 마지막 단계(④)로
        }
    }

    bool GoNeighbor(int dir, bool atLastStep)
    {
        if (!LectureMode.TryNeighbor(entry.id, dir, out var next)) return false;
        LectureMode.Go(atLastStep);
        OpenEntry(next);
        return true;
    }

    void GoHub()
    {
        LectureLabSetup.Apply(lab, labUI, null, "[Lecture] 용어 목록");
        OpenTermList(null);
    }

    void ToggleBubble()
    {
        bubbleHidden = !bubbleHidden;
        bubble.style.display = bubbleHidden ? DisplayStyle.None : DisplayStyle.Flex;
    }

    // ───── 단계 표시 ─────

    void ShowStep()
    {
        var step = CurrentStep;
        bool compact = step == LectureStep.Demo; // ③ 직접 보기: 말풍선과 캐릭터를 줄여 랩이 잘 보이게

        // ③ 에서는 '조작 + 확인 포인트' 카드를 오른쪽 위에, 나머지 단계에서는 숨김
        demoCard.style.display = compact ? DisplayStyle.Flex : DisplayStyle.None;

        // 강의 여는 인사는 디렉터, 나머지는 분야 담당자
        var who = step == LectureStep.Opening ? LecturePresenters.Director : presenter;
        nameTagText.text = who.Role;
        nameTag.style.backgroundColor = who.Accent;

        // 픽셀아트는 정수 배로만 키움 (2배 = 높이 360, ③ 은 1배)
        characterScale = compact ? 1f : 2f;
        SetCharacter(who, StateFor(step));
        float slot = CharSlotWidth * characterScale;

        if (compact)
        {
            // 캐릭터 머리 위에 작은 말풍선, 꼬리는 아래(캐릭터) 방향
            bubble.style.width = BubbleSmallWidth;
            bubble.style.right = Margin;
            bubble.style.bottom = CharBottom + characterSize.y + 24f;
            bubbleTail.style.right = slot * 0.5f - 15f;
            bubbleTail.style.bottom = -15f;
            bubbleText.style.fontSize = 22f;
        }
        else
        {
            // 캐릭터 왼쪽에 큰 말풍선, 꼬리는 오른쪽(캐릭터) 방향
            bubble.style.width = BubbleBigWidth;
            bubble.style.right = Margin + slot + 12f;
            bubble.style.bottom = BigBubbleY;
            bubbleTail.style.right = -15f;
            bubbleTail.style.bottom = 70f - 15f;
            bubbleText.style.fontSize = 27f;
        }

        // ④ 와 디렉터 인사에서 '좋은 요청의 4요소' 블록을 씀 (나쁜/좋은 요청 카드는 ④ 에서만)
        promptBlock.style.display = step == LectureStep.Prompt || step == LectureStep.Opening ? DisplayStyle.Flex : DisplayStyle.None;
        cardsRow.style.display = step == LectureStep.Prompt ? DisplayStyle.Flex : DisplayStyle.None;
        if (step == LectureStep.Prompt) FillPromptBlock();
        else if (step == LectureStep.Opening) FillOpeningElements();

        bubbleHidden = false;
        bubble.style.display = DisplayStyle.Flex;
        StartTyping(LineFor(step));
        UpdateProgress();
    }

    // 단계별 대사: 용어 데이터 문장에 담당자 말투의 앞뒤 문장을 붙인다
    string LineFor(LectureStep step)
    {
        int index = LectureMode.IndexInCategory(entry);
        int count = LectureMode.CountInCategory(entry.cat);
        string term = $"<b><color=#C0571A>{entry.term}</color></b>";

        switch (step)
        {
            case LectureStep.Opening:
                return "안녕하세요, 디렉터예요! 오늘은 개발실 팀원들이 쉐이더 랩을 직접 돌려 보면서 그래픽 용어를 하나씩 설명할 거예요.\n" +
                       "용어를 알면 AI에게 원하는 걸 정확하게 부탁할 수 있어요. 좋은 요청에는 아래 네 가지가 들어가요.\n" +
                       "예) '<b>주인공 캐릭터</b>에 <b>림 라이트</b>를 <b>세기 0.6</b>으로 넣어줘. <b>Unity 6 · URP</b> 프로젝트야.'";

            case LectureStep.SelfIntro:
                // 같은 담당자가 여러 분야를 맡으므로 분야가 바뀔 때마다 '이 분야에서 회사가 하는 일'을 소개
                string hello = LectureMode.SamePresenterAsBefore
                    ? $"또 저예요, {presenter.Role} 담당!"
                    : $"안녕하세요! {presenter.Role} 담당이에요.";
                return $"{hello}\n" +
                       $"이번엔 '{entry.CategoryName}' 분야예요. {LecturePresenters.CategoryJob(entry.cat)}\n" +
                       $"이 분야 용어는 모두 <b>{count}개</b>, {term}부터 시작할게요!";

            case LectureStep.Intro:
                string summary = string.IsNullOrEmpty(entry.easy) ? "" : $"\n쉽게 말하면 <b>'{entry.easy}'</b>, 이런 뜻이에요.";
                // 명령줄로 이 용어부터 바로 시작했고 분야 소개가 없으면 담당자가 먼저 인사 (다음 용어부터는 평소 대사)
                if (LectureMode.CurrentIsDirectStart && !LectureMode.CurrentHasSelfIntro)
                    return $"안녕하세요, {presenter.Role} 담당이에요!\n이번에 볼 용어는 {term}!{summary}";
                string opener = LectureMode.CurrentHasSelfIntro
                    ? (index == 1 ? "첫 번째 용어부터 갈게요." : "그럼 바로 시작해 볼게요.")
                    : presenter.NextLine;
                return $"{opener}\n이번 용어는 {term}!{summary}";

            case LectureStep.Concept:
                return $"조금 더 자세히 볼게요.\n{entry.concept}";

            case LectureStep.Demo:
                return presenter.DemoLine;

            case LectureStep.Prompt:
                return "이제 AI에게 부탁해 볼까요? 왼쪽처럼 막연하게 말하면 AI가 무엇을 얼마나 만들지 몰라 엉뚱한 결과가 나와요. " +
                       "오른쪽 예문에서 출발해서, 아래 4요소 중 <b>빠진 칸을 채워</b> 내 프로젝트에 맞게 고쳐 보세요.";
        }
        return "";
    }

    // 단계 → 캐릭터 그림 상태
    static string StateFor(LectureStep step)
    {
        switch (step)
        {
            case LectureStep.Intro: return "rest";    // 기본
            case LectureStep.Concept: return "work";  // 키보드 들고 생각
            case LectureStep.Prompt: return "ask";    // 손 들고 물음표
            default: return "review";                 // 인사·분야 소개·직접 보기: 반짝이며 웃음
        }
    }

    // 캐릭터 그림을 원본 비율 그대로 정수 배 확대해서 오른쪽 아래에 세움
    void SetCharacter(LecturePresenter who, string state)
    {
        var tex = LecturePresenters.Sprite(who, state);
        character.image = tex;
        fallback.style.display = tex == null ? DisplayStyle.Flex : DisplayStyle.None;
        fallback.style.backgroundColor = who.Accent;
        if (tex != null)
        {
            characterSize = new Vector2(tex.width, tex.height) * characterScale;
        }
        else
        {
            characterSize = new Vector2(90f, 90f) * characterScale;
            fallbackLetter.text = who.Role.Substring(0, 1);
            fallbackLetter.style.fontSize = 40f * characterScale;
        }
        character.style.width = characterSize.x;
        character.style.height = characterSize.y;
        // 발끝 가운데를 캐릭터 칸 가운데에 맞춤
        character.style.right = Margin + CharSlotWidth * characterScale * 0.5f - characterSize.x * 0.5f;
        character.style.bottom = CharBottom;
    }

    // 말하는 동안(글자가 나오는 중) 캐릭터가 살짝 통통 튐. 픽셀이 뭉개지지 않게 그림 1픽셀 단위로 움직임
    void UpdateBob()
    {
        float hop = 0f;
        if (typing)
        {
            bobTime += Time.unscaledDeltaTime;
            hop = Mathf.Round(Mathf.Abs(Mathf.Sin(bobTime * 9f)) * 3f) * characterScale;
        }
        else
        {
            bobTime = 0f;
        }
        character.style.bottom = CharBottom + hop;
    }

    // 디렉터 인사: 4요소를 예시 문장으로 짚음
    void FillOpeningElements()
    {
        SetElement(0, "무엇에? 주인공 캐릭터", true);
        SetElement(1, "정확한 이름: 림 라이트", true);
        SetElement(2, "얼마나? 세기 0.6", true);
        SetElement(3, "어디서? Unity 6 · URP", true);
    }

    // ④ 막연한 요청 / 구체적인 요청 / 좋은 요청의 4요소
    // 4요소 칸: 예문에 들어 있으면 초록 "들어 있음", 빠졌으면 주황 "빠짐 → 예: …" (학생이 채워 넣을 거리)
    void FillPromptBlock()
    {
        var category = ShaderLabGlossary.Category(entry.cat);
        badText.text = $"\"{entry.vague}\"";
        goodText.text = entry.prompt;

        string prompt = entry.prompt ?? "";
        string target = FindTarget(prompt);
        string termFound = FindTerm(entry.term, prompt);
        string number = FindNumber(prompt);
        string env = FindEnvironment(prompt);

        SetElement(0, target != null ? $"'{target}' 들어 있음" : $"빠짐 → 예: {category.targetExample}", target != null);
        SetElement(1, termFound != null ? $"'{termFound}' 들어 있음" : $"빠짐 → 예: '{ShortTerm(entry.term)}'", termFound != null);
        SetElement(2, number != null ? $"'{number}' 들어 있음" : $"빠짐 → 예: {category.numberExample}", number != null);
        SetElement(3, env != null ? $"'{env}' 들어 있음" : "빠짐 → 예: Unity 6 · URP · 3D", env != null);
    }

    void SetElement(int i, string note, bool found)
    {
        elementNotes[i].text = note;
        elementBgs[i].style.backgroundColor = found ? (Color)new Color32(214, 242, 220, 255)  // 들어 있음: 초록
                                                    : (Color)new Color32(255, 236, 204, 255); // 빠짐: 주황 (채울 거리)
    }

    // "Rim Light (림 라이트 · 역광)" → "Rim Light"
    static string ShortTerm(string term)
    {
        int p = term.IndexOf(" (", StringComparison.Ordinal);
        return p > 0 ? term.Substring(0, p) : term;
    }

    // 요청 대상(무엇에 적용할지)으로 쓰는 말: JSON 의 targetWords (앞에 있을수록 먼저 찾음, 앱의 판정과 같은 목록)
    static string FindTarget(string prompt)
    {
        var words = ShaderLabGlossary.Data.targetWords;
        if (words == null) return null;
        foreach (string w in words)
            if (!string.IsNullOrEmpty(w) && prompt.Contains(w)) return w;
        return null;
    }

    // 용어(또는 괄호 안 말, '/' '·' ',' 로 나뉜 각 부분)가 요청 예문에 들어 있는지
    static string FindTerm(string term, string prompt)
    {
        string flat = prompt.Replace(" ", "").ToLowerInvariant();
        foreach (string part in term.Split('(', ')', '/', ',', '·'))
        {
            string p = part.Trim();
            if (p.Length < 2) continue;
            if (flat.Contains(p.Replace(" ", "").ToLowerInvariant())) return p;
        }
        return null;
    }

    // 숫자 + 단위. 영문 이름 속 숫자(Vector3)·환경 표기(3D)·버전 번호(6000.6.7f1)는 수치로 치지 않음
    static readonly Regex NumberRegex = new Regex(@"(?<![A-Za-z\d.])\d+(\.\d+)?(?![\dDd])(?!\.\d)\s?(초|도|ms|m|%|개|배|프레임|px|픽셀|fps|번|칸|분|단계|가지|EV|°)?");

    static string FindNumber(string prompt)
    {
        foreach (Match m in NumberRegex.Matches(prompt))
        {
            // 'Unity 6', 'URP 17' 같은 버전은 수치가 아님
            if (m.Index >= 6 && prompt.Substring(m.Index - 6, 6) == "Unity ") continue;
            if (m.Index >= 4 && prompt.Substring(m.Index - 4, 4) == "URP ") continue;
            return m.Value.Trim();
        }
        return null;
    }

    // 내 환경 표기. 다른 낱말 속 글자는 빼고 낱말 단위로 찾음
    static readonly Regex EnvironmentRegex =
        new Regex(@"(?<![A-Za-z])(Unity 6|URP|HDRP|2D|3D|모바일|PC|Android|iOS|WebGL)(?![A-Za-z])");

    static string FindEnvironment(string prompt)
    {
        var m = EnvironmentRegex.Match(prompt);
        return m.Success ? m.Value : null;
    }

    void UpdateProgress()
    {
        int index = LectureMode.IndexInCategory(entry);
        int count = LectureMode.CountInCategory(entry.cat);
        progressTitle.text = $"{entry.cat:D2} {entry.CategoryName}  ›  <color=#FFD479>{entry.id} {entry.term}</color>  <color=#A8B0C0>({index}/{count})</color>";

        var sb = new StringBuilder();
        for (int i = 0; i < steps.Length; i++)
        {
            if (i > 0) sb.Append("<color=#5A6070>  ›  </color>");
            string label = StepLabel(steps[i]);
            if (i == LectureMode.StepIndex) sb.Append("<b><color=#FFFFFF>").Append(label).Append("</color></b>");
            else sb.Append("<color=#7A8294>").Append(label).Append("</color>");
        }
        progressSteps.text = sb.ToString();

        // 분야 안 진행률 (이 용어의 단계까지 반영)
        float t = (index - 1 + (LectureMode.StepIndex + 1f) / steps.Length) / Mathf.Max(1, count);
        progressFill.style.width = Length.Percent(Mathf.Clamp01(t) * 100f);
    }

    static string StepLabel(LectureStep step)
    {
        switch (step)
        {
            case LectureStep.Opening: return "시작";
            case LectureStep.SelfIntro: return "분야 소개";
            case LectureStep.Intro: return "① 소개";
            case LectureStep.Concept: return "② 개념";
            case LectureStep.Demo: return "③ 직접 보기";
            default: return "④ AI에게 요청";
        }
    }

    // ───── 한 글자씩 나오는 효과 ─────
    // 아직 안 나온 글자도 투명하게 미리 그려 둬서 말풍선 크기가 글자가 나오는 동안 흔들리지 않는다

    void StartTyping(string text)
    {
        line = text;
        totalChars = VisibleLength(text);
        typed = 0f;
        shownChars = 0;
        typing = true;
        bubbleText.text = Reveal(text, 0);
        continueMark.style.display = DisplayStyle.None;
        promptBlock.style.opacity = 0f; // ④ 카드는 대사가 끝난 뒤에 보여 줌 (자리는 미리 잡아 둠)
    }

    void UpdateTyping()
    {
        if (!typing) return;
        typed += Time.unscaledDeltaTime * CharsPerSecond;
        int n = (int)typed;
        if (n >= totalChars)
        {
            SkipTyping();
            return;
        }
        if (n == shownChars) return;
        shownChars = n;
        bubbleText.text = Reveal(line, n);
    }

    public void SkipTyping()
    {
        typing = false;
        bubbleText.text = line;
        continueMark.style.display = DisplayStyle.Flex;
        promptBlock.style.opacity = 1f;
    }

    // 태그(<b>, <color=...>)를 뺀 글자 수
    static int VisibleLength(string rich)
    {
        int n = 0;
        for (int i = 0; i < rich.Length; i++)
        {
            if (rich[i] == '<')
            {
                int end = rich.IndexOf('>', i);
                if (end > i) { i = end; continue; }
            }
            n++;
        }
        return n;
    }

    // 앞에서 visible 글자까지만 보이게: 그 뒤는 투명 색으로 감싸고, 안쪽 색 태그는 빼서 투명이 덮이지 않게 함
    static string Reveal(string rich, int visible)
    {
        var sb = new StringBuilder(rich.Length + 24);
        int shown = 0;
        bool hiding = false;
        for (int i = 0; i < rich.Length; i++)
        {
            if (rich[i] == '<')
            {
                int end = rich.IndexOf('>', i);
                if (end > i)
                {
                    string tag = rich.Substring(i, end - i + 1);
                    bool colorTag = tag.StartsWith("<color", StringComparison.Ordinal) || tag.StartsWith("</color", StringComparison.Ordinal);
                    if (!(hiding && colorTag)) sb.Append(tag);
                    i = end;
                    continue;
                }
            }
            if (!hiding && shown >= visible)
            {
                sb.Append("<color=#00000000>");
                hiding = true;
            }
            sb.Append(rich[i]);
            shown++;
        }
        return sb.ToString();
    }

    // ───── UI 생성 ─────

    // 오른쪽 위 진행 막대 (왼쪽 랩 패널과 가운데 위 A/B 이름표를 피함)
    void BuildProgress(VisualElement parent)
    {
        var progress = Box(parent, "Progress", PanelColor);
        progress.style.position = Position.Absolute;
        progress.style.top = 16f;
        progress.style.right = 16f;
        progress.style.width = RightColumnWidth;
        progress.style.height = ProgressHeight;

        progressTitle = NewText(progress, "", 22f, Color.white);
        FitOneLine(progressTitle, 14f, 22f);
        Place(progressTitle, 16f, 16f, 10f, 30f);

        progressSteps = NewText(progress, "", 18f, Color.white);
        FitOneLine(progressSteps, 13f, 18f);
        Place(progressSteps, 16f, 16f, 44f, 26f);

        var bar = Box(progress, "Bar", new Color(1f, 1f, 1f, 0.12f));
        bar.style.position = Position.Absolute;
        bar.style.left = 16f;
        bar.style.right = 16f;
        bar.style.bottom = 8f;
        bar.style.height = 6f;

        progressFill = Box(bar, "Fill", Color.white);
        progressFill.style.height = Length.Percent(100f);
        progressFill.style.width = 0f;
    }

    // ③ 직접 보기 카드: 진행 막대 바로 아래에 조작 + 확인 포인트
    // 높이는 FitDemoCard 가 말풍선 위에서 멈추게 줄인다 (넘치는 내용은 카드 안에서 휠로 스크롤)
    void BuildDemoCard(VisualElement parent)
    {
        demoCard = Box(parent, "DemoCard", PanelColor);
        demoCard.style.position = Position.Absolute;
        demoCard.style.top = 16f + ProgressHeight + 8f;
        demoCard.style.right = 16f;
        demoCard.style.width = RightColumnWidth;
        demoCard.style.overflow = Overflow.Hidden;
        Padding(demoCard, 18f, 14f);

        demoScroll = new ScrollView(ScrollViewMode.Vertical);
        demoScroll.style.flexShrink = 1f;
        demoScroll.style.minHeight = 0f;
        demoCard.Add(demoScroll);

        demoTitle = NewText(demoScroll, "", 22f, Color.white);
        demoTitle.style.unityFontStyleAndWeight = FontStyle.Bold;
        demoTitle.style.marginBottom = 8f;
        demoControls = NewText(demoScroll, "", 19f, new Color(0.85f, 0.95f, 0.85f));
        demoControls.style.marginBottom = 8f;
        demoCheck = NewText(demoScroll, "", 18f, new Color(0.95f, 0.9f, 0.78f));
    }

    // ③ 카드 아래 끝이 작은 말풍선(숨겼으면 캐릭터) 위 16px 에서 멈추게 카드 최대 높이를 정한다
    // 말풍선·캐릭터·화면 크기가 바뀔 때마다(GeometryChangedEvent) 다시 계산
    void FitDemoCard()
    {
        if (demoCard.resolvedStyle.display == DisplayStyle.None) return;
        var below = bubble.resolvedStyle.display == DisplayStyle.None ? (VisualElement)character : bubble;
        float maxHeight = Mathf.Max(120f, below.layout.yMin - 16f - demoCard.layout.yMin);
        if (Mathf.Abs(demoCard.resolvedStyle.maxHeight.value - maxHeight) > 0.5f)
            demoCard.style.maxHeight = maxHeight;
    }

    // 오른쪽 아래에 서 있는 캐릭터 (프레임 없이 투명 배경 그림 그대로. 그림이 없으면 직군 첫 글자를 원 안에)
    void BuildCharacter(VisualElement parent)
    {
        character = new Image { name = "Character", scaleMode = ScaleMode.StretchToFill, pickingMode = PickingMode.Ignore };
        character.style.position = Position.Absolute;
        parent.Add(character);

        fallback = new VisualElement { name = "Fallback", pickingMode = PickingMode.Ignore };
        Stretch(fallback);
        Radius(fallback, Length.Percent(50f));
        character.Add(fallback);
        fallbackLetter = NewText(fallback, "", 80f, InkColor);
        Stretch(fallbackLetter);
        fallbackLetter.style.unityTextAlign = TextAnchor.MiddleCenter;
        fallbackLetter.style.unityFontStyleAndWeight = FontStyle.Bold;
    }

    void BuildBubble(VisualElement parent)
    {
        bubble = Box(parent, "Bubble", BubbleColor);
        bubble.style.position = Position.Absolute;
        bubble.style.paddingLeft = 28f;
        bubble.style.paddingRight = 28f;
        bubble.style.paddingTop = 32f;
        bubble.style.paddingBottom = 30f;
        Radius(bubble, 6f);
        // 말풍선 클릭 = 다음 (말풍선 자체만 마우스를 받고 안쪽 글자는 통과)
        bubble.pickingMode = PickingMode.Position;
        bubble.RegisterCallback<ClickEvent>(_ => Next());

        // 꼬리: 45도 돌린 사각형을 말풍선 가장자리에 반쯤 걸쳐 둠
        bubbleTail = Box(bubble, "Tail", BubbleColor);
        bubbleTail.style.position = Position.Absolute;
        bubbleTail.style.width = 30f;
        bubbleTail.style.height = 30f;
        bubbleTail.style.rotate = new Rotate(45f);

        // 이름표: 말풍선 위 테두리에 걸친 탭 (색과 글자는 단계마다 ShowStep 에서: 디렉터 / 분야 담당자)
        nameTag = Box(bubble, "NameTag", Color.white);
        nameTag.style.position = Position.Absolute;
        nameTag.style.left = 22f;
        nameTag.style.top = 0f;
        nameTag.style.translate = new Translate(0f, Length.Percent(-50f));
        Padding(nameTag, 16f, 5f);
        Radius(nameTag, 4f);
        nameTagText = NewText(nameTag, "", 21f, InkColor);
        nameTagText.style.unityFontStyleAndWeight = FontStyle.Bold;
        nameTagText.style.whiteSpace = WhiteSpace.NoWrap;

        bubbleText = NewText(bubble, "", 27f, InkColor);

        BuildPromptBlock(bubble);

        continueMark = NewText(bubble, "PgDn ▶", 16f, new Color(0.45f, 0.48f, 0.56f));
        continueMark.style.position = Position.Absolute;
        continueMark.style.right = 14f;
        continueMark.style.bottom = 6f;
    }

    // ④ 단계 블록: [막연한 요청] [구체적인 요청] + 좋은 요청의 4요소 (용어가 들어 있는지는 4요소 칸에서만 표시)
    void BuildPromptBlock(VisualElement parent)
    {
        promptBlock = new VisualElement { name = "PromptBlock", pickingMode = PickingMode.Ignore };
        promptBlock.style.marginTop = 14f;
        parent.Add(promptBlock);

        cardsRow = new VisualElement { name = "Cards", pickingMode = PickingMode.Ignore };
        cardsRow.style.flexDirection = FlexDirection.Row;
        cardsRow.style.marginBottom = 12f;
        promptBlock.Add(cardsRow);

        badText = Card(cardsRow, "막연한 요청", new Color32(190, 50, 50, 255), new Color32(253, 232, 232, 255));
        badText.parent.style.marginRight = 16f;
        goodText = Card(cardsRow, "구체적인 요청", new Color32(30, 130, 70, 255), new Color32(224, 246, 230, 255));

        var title = NewText(promptBlock, "좋은 요청의 4요소", 19f, InkColor);
        title.style.unityFontStyleAndWeight = FontStyle.Bold;
        title.style.marginBottom = 8f;

        var row = new VisualElement { name = "Elements", pickingMode = PickingMode.Ignore };
        row.style.flexDirection = FlexDirection.Row;
        promptBlock.Add(row);

        string[] names = { "대상", "기능 용어", "조건·수치", "내 환경" };
        for (int i = 0; i < names.Length; i++)
        {
            var chip = Box(row, "Element" + i, Color.white);
            EqualWidth(chip);
            if (i < names.Length - 1) chip.style.marginRight = 10f;
            Padding(chip, 12f, 8f);
            elementBgs[i] = chip;
            NewText(chip, $"<b>{names[i]}</b>", 18f, InkColor);
            elementNotes[i] = NewText(chip, "", 15f, new Color(0.3f, 0.32f, 0.38f));
        }
    }

    static Label Card(VisualElement parent, string header, Color headerColor, Color bg)
    {
        var card = Box(parent, header, bg);
        EqualWidth(card);
        card.style.paddingLeft = 16f;
        card.style.paddingRight = 16f;
        card.style.paddingTop = 10f;
        card.style.paddingBottom = 12f;
        var h = NewText(card, header, 18f, headerColor);
        h.style.unityFontStyleAndWeight = FontStyle.Bold;
        h.style.marginBottom = 4f;
        return NewText(card, "", 21f, InkColor);
    }

    // 아래쪽 키 안내 (작게, 랩 패널 오른쪽)
    void BuildKeyGuide(VisualElement parent)
    {
        var guide = Box(parent, "KeyGuide", new Color(0f, 0f, 0f, 0.55f));
        guide.style.position = Position.Absolute;
        guide.style.left = LabPanelRight;
        guide.style.bottom = 0f;
        Padding(guide, 12f, 2f);
        var text = NewText(guide, LectureMode.KeyGuide, 15f, new Color(0.82f, 0.85f, 0.9f));
        text.style.whiteSpace = WhiteSpace.NoWrap;
    }

    // 용어 목록 (용어집의 허브 대신): 분야별 용어 버튼. 누르면 디렉터 인사부터 강의 시작
    void BuildTermList(VisualElement parent)
    {
        termList = Box(parent, "TermList", PanelColor);
        termList.pickingMode = PickingMode.Position;
        termList.style.position = Position.Absolute;
        termList.style.top = 16f;
        termList.style.bottom = 16f;
        termList.style.right = 16f;
        termList.style.width = 860f;
        Padding(termList, 22f, 18f);
        Radius(termList, 8f);

        var title = NewText(termList, $"{ShaderLabGlossary.Data.title} 강의", 30f, new Color(1f, 0.86f, 0.4f));
        title.style.unityFontStyleAndWeight = FontStyle.Bold;
        NewText(termList, "용어를 누르면 디렉터 인사부터 강의를 시작해요. 강의 중에는 오른쪽 아래 말풍선을 누르거나 PgDn 으로 넘겨요.",
            17f, new Color(0.75f, 0.78f, 0.85f)).style.marginBottom = 6f;
        termNotice = NewText(termList, "", 17f, new Color(1f, 0.7f, 0.4f));
        termNotice.style.marginBottom = 6f;

        var scroll = new ScrollView(ScrollViewMode.Vertical);
        scroll.style.flexGrow = 1f;
        termList.Add(scroll);

        foreach (var category in ShaderLabGlossary.Data.categories)
        {
            var who = LecturePresenters.For(category.n);
            var header = NewText(scroll, $"{category.n:D2} {category.name}   <size=15><color=#A8B0C0>{who.Role} 담당</color></size>", 20f, who.Accent);
            header.style.unityFontStyleAndWeight = FontStyle.Bold;
            header.style.marginTop = 12f;
            header.style.marginBottom = 4f;

            var row = new VisualElement { pickingMode = PickingMode.Ignore };
            row.style.flexDirection = FlexDirection.Row;
            row.style.flexWrap = Wrap.Wrap;
            scroll.Add(row);
            foreach (var e in ShaderLabGlossary.Entries)
            {
                if (e.cat != category.n) continue;
                var picked = e;
                var b = NewButton(row, $"<color=#FFD479>{e.id}</color>  {e.term}", () => StartAt(picked));
                b.style.width = Length.Percent(49f);
                b.style.marginRight = Length.Percent(1f);
            }
        }

        var footer = new VisualElement { pickingMode = PickingMode.Ignore };
        footer.style.flexDirection = FlexDirection.Row;
        footer.style.alignItems = Align.Center;
        footer.style.marginTop = 10f;
        termList.Add(footer);
        var close = NewButton(footer, "랩 자유 모드로 (F4)", CloseAll);
        close.style.width = 240f;
        close.style.unityTextAlign = TextAnchor.MiddleCenter;
        if (LectureLaunch.IsLecturePlayer)
            NewText(footer, "   Esc  강의 프로그램 끝내기", 16f, new Color(0.6f, 0.64f, 0.72f));
    }

    // ───── UI 헬퍼 ─────

    static VisualElement Box(VisualElement parent, string name, Color color)
    {
        var e = new VisualElement { name = name, pickingMode = PickingMode.Ignore };
        e.style.backgroundColor = color;
        parent.Add(e);
        return e;
    }

    static Label NewText(VisualElement parent, string text, float size, Color color)
    {
        var l = new Label(text) { pickingMode = PickingMode.Ignore, enableRichText = true };
        l.style.fontSize = size;
        l.style.color = color;
        l.style.whiteSpace = WhiteSpace.Normal;
        l.style.marginLeft = l.style.marginRight = l.style.marginTop = l.style.marginBottom = 0f;
        l.style.paddingLeft = l.style.paddingRight = l.style.paddingTop = l.style.paddingBottom = 0f;
        parent.Add(l);
        return l;
    }

    static Button NewButton(VisualElement parent, string text, Action onClick)
    {
        // focusable = false: 누른 뒤 포커스가 남아 Space(랩 자동 회전)·Enter 가 이 버튼을 다시 누르지 않게
        var b = new Button(onClick) { text = text, enableRichText = true, focusable = false };
        b.style.backgroundColor = ButtonColor;
        b.style.color = Color.white;
        b.style.fontSize = 17f;
        b.style.unityTextAlign = TextAnchor.MiddleLeft;
        b.style.whiteSpace = WhiteSpace.Normal;
        b.style.borderLeftWidth = b.style.borderRightWidth = b.style.borderTopWidth = b.style.borderBottomWidth = 0f;
        Radius(b, 4f);
        Padding(b, 12f, 7f);
        b.style.marginLeft = 0f;
        b.style.marginTop = 0f;
        b.style.marginBottom = 6f;
        b.RegisterCallback<PointerEnterEvent>(_ => b.style.backgroundColor = ButtonHover);
        b.RegisterCallback<PointerLeaveEvent>(_ => b.style.backgroundColor = ButtonColor);
        parent.Add(b);
        return b;
    }

    // 한 줄 글자: 칸에 맞춰 글자 크기를 줄임
    static void FitOneLine(Label l, float min, float max)
    {
        l.style.whiteSpace = WhiteSpace.NoWrap;
        l.style.overflow = Overflow.Hidden;
        l.style.unityTextAutoSize = new TextAutoSize(TextAutoSizeMode.BestFit, min, max);
    }

    // 부모 위쪽에 붙은 가로 띠 (left·right 여백, 위에서 top 만큼, 높이 height)
    static void Place(VisualElement e, float left, float right, float top, float height)
    {
        e.style.position = Position.Absolute;
        e.style.left = left;
        e.style.right = right;
        e.style.top = top;
        e.style.height = height;
    }

    static void Stretch(VisualElement e)
    {
        e.style.position = Position.Absolute;
        e.style.left = e.style.top = e.style.right = e.style.bottom = 0f;
    }

    // 가로 줄 안의 칸들을 글자 길이와 상관없이 같은 너비로
    static void EqualWidth(VisualElement e)
    {
        e.style.flexGrow = 1f;
        e.style.flexBasis = 0f;
    }

    static void Padding(VisualElement e, float x, float y)
    {
        e.style.paddingLeft = e.style.paddingRight = x;
        e.style.paddingTop = e.style.paddingBottom = y;
    }

    static void Radius(VisualElement e, Length r)
    {
        e.style.borderTopLeftRadius = e.style.borderTopRightRadius = r;
        e.style.borderBottomLeftRadius = e.style.borderBottomRightRadius = r;
    }
}
