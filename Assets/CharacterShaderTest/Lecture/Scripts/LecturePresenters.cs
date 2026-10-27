using UnityEngine;

// 강의 모드에 나오는 '개발실' 팀원 캐릭터 (아트·프로그래머·디렉터 ...)
// 말투·색은 ShaderLabGlossary.json 의 presenters, 분야 담당은 categories[].presenter 에서 읽는다
// 그림: Resources/Lecture/{Key}_{상태}.png  (상태: rest 기본 / work 생각 중 / review 반짝 웃음 / ask 손 들고 질문)
public readonly struct LecturePresenter
{
    public readonly string Key;      // 파일 이름 앞부분: art, dev, director ...
    public readonly string Role;     // 이름표에 쓰는 직군
    public readonly string NextLine; // ① 소개 단계를 여는 한마디
    public readonly string DemoLine; // ③ 직접 보기 단계의 짧은 말풍선
    public readonly Color Accent;    // 이름표·진행 막대 색

    public LecturePresenter(string key, PresenterData data)
    {
        Key = key;
        Role = data?.role ?? key;
        NextLine = data?.nextLine ?? "";
        DemoLine = data?.demoLine ?? "";
        Accent = data != null && ColorUtility.TryParseHtmlString(data.accent, out var c) ? c : Color.gray;
    }
}

public static class LecturePresenters
{
    public static LecturePresenter Director => Get("director");

    // 분야 번호 → 담당자
    public static LecturePresenter For(int category) => Get(ShaderLabGlossary.Category(category).presenter);

    // 분야가 바뀔 때 담당자가 하는 소개: 회사에서 이 분야를 맡은 사람이 하는 일
    public static string CategoryJob(int category) => ShaderLabGlossary.Category(category).intro;

    static LecturePresenter Get(string key)
    {
        var table = ShaderLabGlossary.Data.presenters;
        PresenterData data = null;
        if (table != null)
        {
            data = key switch
            {
                "art" => table.art,
                "dev" => table.dev,
                "director" => table.director,
                "plan" => table.plan,
                "sound" => table.sound,
                _ => null,
            };
        }
        return new LecturePresenter(key, data);
    }

    // 단계별 캐릭터 그림 (없으면 null → 직군 첫 글자 원으로 대체)
    public static Texture2D Sprite(LecturePresenter presenter, string state)
    {
        return Resources.Load<Texture2D>($"Lecture/{presenter.Key}_{state}");
    }
}
