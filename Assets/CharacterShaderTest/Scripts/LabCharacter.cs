using System;
using System.Collections.Generic;
using UnityEngine;

// 쉐이더 랩에 올리는 캐릭터 하나.
// - 렌더러마다 원본(URP Lit) 머티리얼과 새 쉐이더 머티리얼을 둘 다 기억해 두고, 실행 중에 바꿔 끼운다
// - 새 쉐이더 머티리얼은 실행할 때 복사본을 만들어 쓴다 → 플레이 중에 무엇을 바꿔도 머티리얼 파일은 그대로
// - 커스터마이징 칸(옷·무기 등): 칸마다 후보 오브젝트 중 하나만 켠다
public class LabCharacter : MonoBehaviour
{
    [Serializable]
    public class RendererMaterials
    {
        public Renderer renderer;
        public Material[] lit;
        public Material[] lab;
    }

    [Serializable]
    public class CustomizationSlot
    {
        public string name;
        [Tooltip("비워 둘 수 있는 칸이면 '없음' 선택지가 생긴다 (무기 등)")]
        public bool allowNone;
        public List<GameObject> options = new();
        public int defaultIndex;
    }

    public List<RendererMaterials> renderers = new();
    public List<CustomizationSlot> slots = new();
    public Animator animator;
    [Tooltip("애니메이션 테스트에 쓸 클립")]
    public List<AnimationClip> clips = new();

    // 실행 중에 만든 새 쉐이더 머티리얼 복사본 (renderers와 같은 순서)
    readonly List<Material[]> labInstances = new();
    readonly Dictionary<Material, Material> instanceToAuthored = new();
    readonly Dictionary<string, int> slotSelection = new();
    bool initialized;

    public bool UsesLab { get; private set; }

    public void Initialize()
    {
        if (initialized)
            return;
        initialized = true;
        foreach (var entry in renderers)
        {
            var copies = new Material[entry.lab.Length];
            for (int i = 0; i < copies.Length; i++)
            {
                var authored = entry.lab[i];
                if (authored == null)
                    continue;
                copies[i] = new Material(authored) { name = authored.name + " (Lab)" };
                instanceToAuthored[copies[i]] = authored;
            }
            labInstances.Add(copies);
        }
        foreach (var slot in slots)
            SelectOption(slot.name, slot.defaultIndex);
    }

    void OnDestroy()
    {
        foreach (var copy in instanceToAuthored.Keys)
            Destroy(copy);
    }

    public IEnumerable<Material> LabMaterials
    {
        get
        {
            foreach (var set in labInstances)
                foreach (var m in set)
                    if (m != null)
                        yield return m;
        }
    }

    // 원본(URP Lit) ↔ 새 쉐이더
    public void UseLabShader(bool lab)
    {
        UsesLab = lab;
        for (int i = 0; i < renderers.Count; i++)
        {
            var entry = renderers[i];
            if (entry.renderer == null)
                continue;
            entry.renderer.sharedMaterials = lab ? labInstances[i] : entry.lit;
        }
    }

    // 스타일 적용: 머티리얼 파일에 저장된 원래 값으로 되돌린 뒤, 프리셋이 덮어쓰는 값만 바꾼다
    public void ApplyStyle(StylePreset preset)
    {
        foreach (var pair in instanceToAuthored)
        {
            pair.Key.CopyPropertiesFromMaterial(pair.Value);
            if (preset != null)
                preset.ApplyTo(pair.Key);
        }
    }

    // 맵·기능 스위치: "끄기만" 한다. 원래(또는 프리셋에서) 꺼진 것은 켜도 생기지 않는다.
    // ApplyStyle 다음에 불러야 프리셋 값 위에 스위치가 걸린다
    public void ApplySwitches(IReadOnlyDictionary<LabMap, bool> maps, IReadOnlyDictionary<LabFeature, bool> features)
    {
        foreach (var m in instanceToAuthored.Keys)
        {
            foreach (var map in maps)
                if (!map.Value)
                    m.SetFloat(ShaderLabProperties.Map(map.Key), 0f);
            foreach (var feature in features)
                if (!feature.Value)
                    m.SetFloat(ShaderLabProperties.Feature(feature.Key), 0f);
        }
    }

    // ---------------- 커스터마이징 ----------------
    public int GetSelection(string slotName) => slotSelection.TryGetValue(slotName, out int i) ? i : -1;

    // index = -1 이면 '없음' (allowNone 칸만)
    public void SelectOption(string slotName, int index)
    {
        var slot = slots.Find(s => s.name == slotName);
        if (slot == null)
            return;
        if (index < 0 && !slot.allowNone)
            index = 0;
        index = Mathf.Min(index, slot.options.Count - 1);
        for (int i = 0; i < slot.options.Count; i++)
            if (slot.options[i] != null)
                slot.options[i].SetActive(i == index);
        slotSelection[slotName] = index;
    }

    public Bounds GetBounds()
    {
        Bounds? b = null;
        foreach (var r in GetComponentsInChildren<Renderer>())
        {
            if (b == null) b = r.bounds;
            else { var bb = b.Value; bb.Encapsulate(r.bounds); b = bb; }
        }
        return b ?? new Bounds(transform.position + Vector3.up, new Vector3(1f, 2f, 1f));
    }
}
