using System;
using UnityEngine;

// 용어로 들어올 때마다 쉐이더 랩을 그 용어가 잘 보이는 상태로 준비한다
// (용어집은 용어마다 씬이 따로지만 쉐이더 랩은 씬 하나 → 씬을 새로 여는 대신 랩을 처음 상태로 되돌리고 JSON 의 lab 칸대로 바꾼다)
// 이름을 못 찾으면 경고만 남기고 그 칸은 기본값으로 둔다
public static class LectureLabSetup
{
    // setup = 용어의 lab 칸 (null 이면 처음 상태 그대로), where = 경고에 붙일 위치 (예: "[Lecture] 0304 lab")
    public static void Apply(ShaderLabController lab, ShaderLabUI ui, LabSetup setup, string where)
    {
        setup ??= new LabSetup();

        lab.ResetState();

        // 옷: 커스텀 탭 칸에서 그 이름의 후보를 찾아 고른다
        if (setup.wear != null)
        {
            foreach (string name in setup.wear)
            {
                if (!TryFindWear(lab, name, out string slot, out int index))
                {
                    Debug.LogWarning($"{where}.wear '{name}' 없음");
                    continue;
                }
                lab.SelectSlotOption(slot, index);
            }
        }

        if (!string.IsNullOrEmpty(setup.a)) lab.SetStyleA(FindStyle(lab, setup.a, where + ".a"));
        if (!string.IsNullOrEmpty(setup.b)) lab.SetStyleB(FindStyle(lab, setup.b, where + ".b"));

        if (!string.IsNullOrEmpty(setup.environment))
        {
            int env = lab.environments.FindIndex(e => e != null && e.displayName == setup.environment);
            if (env >= 0) lab.SetEnvironment(env);
            else Debug.LogWarning($"{where}.environment '{setup.environment}' 없음");
        }

        if (!string.IsNullOrEmpty(setup.lighting))
        {
            int light = lab.lightingPresets.FindIndex(l => l.displayName == setup.lighting);
            if (light >= 0) lab.SetLighting(light);
            else Debug.LogWarning($"{where}.lighting '{setup.lighting}' 없음");
        }

        if (!string.IsNullOrEmpty(setup.debug))
        {
            if (Enum.TryParse(setup.debug, out LabDebugView view)) lab.SetDebugView(view);
            else Debug.LogWarning($"{where}.debug '{setup.debug}' 없음");
        }

        // 비교 화면: 기본은 와이프 가운데
        if (lab.compareView != null)
        {
            lab.compareView.wipe = 0.5f;
            lab.compareView.mode = CompareView.Mode.Wipe;
            if (!string.IsNullOrEmpty(setup.compare))
            {
                if (Enum.TryParse(setup.compare, out CompareView.Mode mode)) lab.compareView.mode = mode;
                else Debug.LogWarning($"{where}.compare '{setup.compare}' 없음");
            }
        }

        // 카메라는 처음 시점으로
        if (lab.orbitCamera != null)
        {
            lab.orbitCamera.AutoRotate = false;
            lab.orbitCamera.ResetView(snap: false);
        }

        // 패널을 펼치고 이 용어를 조작하는 탭을 연다 (마지막에 해야 위에서 바꾼 상태가 탭에 그대로 보인다)
        if (ui != null) ui.ShowTab(setup.tab);
    }

    // "원본 (URP Lit)" = ShaderLabController.LitStyle, 나머지는 스타일 프리셋 이름
    static int FindStyle(ShaderLabController lab, string name, string where)
    {
        if (name == lab.StyleName(ShaderLabController.LitStyle))
            return ShaderLabController.LitStyle;
        int index = lab.styles.FindIndex(s => s != null && s.displayName == name);
        if (index >= 0)
            return index;
        Debug.LogWarning($"{where} '{name}' 없음 → 원본");
        return ShaderLabController.LitStyle;
    }

    static bool TryFindWear(ShaderLabController lab, string name, out string slot, out int index)
    {
        foreach (var s in lab.Slots)
        {
            int i = s.options.FindIndex(o => o != null && o.name == name);
            if (i < 0) continue;
            slot = s.name;
            index = i;
            return true;
        }
        slot = null;
        index = -1;
        return false;
    }
}
