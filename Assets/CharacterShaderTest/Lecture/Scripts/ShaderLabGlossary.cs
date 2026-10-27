using System;
using UnityEngine;

// 쉐이더 랩 강의 용어 데이터
// 원본은 Resources/ShaderLabGlossary.json 하나 (OMP Studio 앱의 강의 준비 창도 같은 파일을 읽는다)
// JsonUtility 로 읽으므로 JSON 칸 이름과 아래 변수 이름이 같아야 한다
[Serializable]
public class GlossaryEntry
{
    public string id;        // 4자리 번호 (예: 0304) = 명령줄 -glossaryStart 값
    public int cat;          // 분야 번호 (1부터)
    public string sub;
    public string term;      // 용어 이름
    public string easy;      // 쉽게 말하면
    public string concept;   // ② 개념
    public string controls;  // ③ 조작 ("조작 : 하는 일" 줄바꿈)
    public string check;     // ③ 확인 포인트
    public string vague;     // ④ 막연한 요청
    public string prompt;    // ④ 구체적인 요청
    public LabSetup lab;     // 이 용어로 들어올 때 랩을 어떻게 준비할지

    public string CategoryName => ShaderLabGlossary.Category(cat).name;
}

// 용어별 랩 준비 (비어 있는 칸 = 기본값 그대로). 값은 랩 패널에 보이는 이름과 같다
[Serializable]
public class LabSetup
{
    public string tab;          // 열어 둘 패널 탭: 비교 · 쉐이더 · 맵·보기 · 환경 · 조명 · 후처리 · 커스텀
    public string compare;      // 비교 방식: Wipe · SideBySide · AOnly · BOnly (CompareView.Mode)
    public string a;            // A (왼쪽) 스타일: 원본 (URP Lit) · 리얼 · 세미리얼 · 스타일라이즈드
    public string b;            // B (오른쪽) 스타일
    public string debug;        // 디버그 보기 (LabDebugView 이름: Normal, LightingOnly ...)
    public string environment;  // 환경 프리셋 이름
    public string lighting;     // 조명 프리셋 이름
    public string[] wear;       // 커스텀 탭에서 고를 옷 이름 (무기·애니메이션은 셰이더 강의에서 쓰지 않음)
}

[Serializable]
public class GlossaryCategory
{
    public int n;
    public string name;
    public string presenter;      // 담당 팀원 키 (art, dev ...)
    public string intro;          // 분야 소개: 회사에서 이 분야를 맡은 사람이 하는 일
    public string numberExample;  // ④ '조건·수치'가 빠졌을 때 보여 줄 예
    public string targetExample;  // ④ '대상'이 빠졌을 때 보여 줄 예
}

[Serializable]
public class PresenterData
{
    public string role;
    public string nextLine;
    public string demoLine;
    public string accent;   // #rrggbb
}

[Serializable]
public class PresenterTable
{
    public PresenterData art, dev, director, plan, sound;
}

[Serializable]
public class GlossaryData
{
    public string id;
    public string title;
    public string[] targetWords;   // ④ '대상' 판정 단어 (앞에 있을수록 먼저 찾음). 앱도 같은 목록을 쓴다
    public PresenterTable presenters;
    public GlossaryCategory[] categories;
    public GlossaryEntry[] entries;
}

public static class ShaderLabGlossary
{
    public const string ResourceName = "ShaderLabGlossary";

    // 플레이할 때마다 JSON 을 다시 읽는다 (도메인을 다시 불러오지 않는 설정이라 static 이 남기 때문. JSON 을 고친 뒤 바로 반영)
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    static void ResetCache() => data = null;

    static GlossaryData data;

    public static GlossaryData Data
    {
        get
        {
            if (data != null)
                return data;
            var file = Resources.Load<TextAsset>(ResourceName);
            if (file == null)
            {
                Debug.LogError($"[Lecture] Resources/{ResourceName}.json 이 없습니다.");
                data = new GlossaryData { categories = Array.Empty<GlossaryCategory>(), entries = Array.Empty<GlossaryEntry>() };
                return data;
            }
            data = JsonUtility.FromJson<GlossaryData>(file.text);
            return data;
        }
    }

    public static GlossaryEntry[] Entries => Data.entries;

    public static bool TryFind(string id, out GlossaryEntry entry)
    {
        entry = Array.Find(Entries, e => e.id == id);
        return entry != null;
    }

    public static GlossaryCategory Category(int n)
    {
        var all = Data.categories;
        return Array.Find(all, c => c.n == n) ?? new GlossaryCategory { n = n, name = "", presenter = "art", intro = "" };
    }
}
