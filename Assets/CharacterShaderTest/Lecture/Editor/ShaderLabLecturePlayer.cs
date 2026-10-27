using UnityEditor;
using UnityEditor.Build.Reporting;
using UnityEngine;

// 수업용 쉐이더 랩 강의 프로그램 Windows 빌드 (Builds/ 는 git 에 올리지 않음 → PC마다 한 번 빌드)
// SHADER_LAB_LECTURE_PLAYER 정의를 붙여 빌드 → 테두리 없는 창 전체화면으로 시작, 강의 중이 아닐 때 Esc 로 종료 (LectureLaunch)
// 실행 예: Builds/LecturePlayer/ShaderLab.exe -glossaryStart 0304 -glossaryLecture   (그 용어부터 강의)
//          Builds/LecturePlayer/ShaderLab.exe -glossaryStart 0304                    (강의 없이 랩만 그 용어에 맞게)
//          Builds/LecturePlayer/ShaderLab.exe                                        (지금처럼 랩)
// 명령줄: Unity -batchmode -projectPath <프로젝트> -executeMethod ShaderLabLecturePlayer.BuildFromCommandLine -quit -logFile <로그>
public static class ShaderLabLecturePlayer
{
    public const string OutputPath = "Builds/LecturePlayer/ShaderLab.exe"; // 프로젝트 폴더 기준
    public const string Define = "SHADER_LAB_LECTURE_PLAYER";
    public const string ScenePath = "Assets/CharacterShaderTest/Scenes/CharacterShaderTest.unity";

    [MenuItem("Tools/Shader Test/Build Lecture Player")]
    static void BuildFromMenu()
    {
        if (Build()) EditorUtility.RevealInFinder(OutputPath);
    }

    // 배치 모드용: 실패하면 종료 코드 1
    public static void BuildFromCommandLine()
    {
        if (!Build()) EditorApplication.Exit(1);
    }

    public static bool Build()
    {
        // 쉐이더 랩은 장면 하나라서 Build Settings 목록 대신 이 장면만 넣는다 (Build Settings 는 건드리지 않음)
        var report = BuildPipeline.BuildPlayer(new BuildPlayerOptions
        {
            scenes = new[] { ScenePath },
            locationPathName = OutputPath,
            target = BuildTarget.StandaloneWindows64,
            options = BuildOptions.None,
            extraScriptingDefines = new[] { Define },
        });
        Debug.Log($"[ShaderLabLecturePlayer] {report.summary.result} {report.summary.totalSize / (1024 * 1024)}MB {report.summary.totalTime} → {System.IO.Path.GetFullPath(OutputPath)}");
        return report.summary.result == BuildResult.Succeeded;
    }
}
