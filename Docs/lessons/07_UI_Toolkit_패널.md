# 7차시. UI Toolkit 패널

| 항목 | 내용 |
|---|---|
| 수업 시간 | 180분 (시연 25 · 개념 45 · 쉬는 시간 10 · 실습 70 · 정리·과제 20 · 도입 10) |
| 난이도 | ★★☆ (USS 고치기 + C# 몇 줄 더하기) |
| 시작 저장 지점 | `shader-lab-v2` |
| 쓰는 랩 탭 | **전체** (오늘은 패널 자체가 공부 대상) |

> 기준 버전: Unity **6000.6.7f1** (Unity 6). UI Toolkit은 Unity에 기본으로 들어 있습니다 (패키지 설치 없음).
> 오늘 줄 번호는 모두 `shader-lab-v2` 기준입니다.

## 학습 목표

1. `UIDocument`·`PanelSettings`·UXML·USS·C#이 각각 무엇을 맡는지 말할 수 있다.
2. USS 변수(`--lab-accent` 등)를 바꿔 패널 전체 색(테마)을 바꿀 수 있다.
3. `ShaderLabUI.cs`의 탭 목록에 새 탭("정보")과 단축키를 더할 수 있다.

## 시작 저장 지점

```bash
# Unity를 닫고 실행
git switch -c lesson07 shader-lab-v2
git lfs pull
```

- 6차시 브랜치(`lesson06`)도 v2에서 시작했으므로 Unity가 모델을 다시 가져오지 않습니다. 금방 열립니다.
- 오늘 고칠 파일은 3개입니다: `UI/ShaderLab.uss`, `UI/ShaderLab.uxml`, `Scripts/ShaderLabUI.cs` (모두 `Assets/CharacterShaderTest/` 아래).

---

## 시연 순서

### A. 패널 뜯어보기 (10분)

1. `Assets/CharacterShaderTest/Scenes/CharacterShaderTest.unity`를 열고 ▶ 플레이.
2. 왼쪽 **SHADER LAB** 패널. 탭 8개 **비교 · 쉐이더 · 맵·보기 · 환경 · 조명 · 후처리 · 애니메이션 · 커스텀** 을 차례로 눌러 "버튼을 누르면 아래 내용이 통째로 바뀐다"를 보여 준다.
3. `H` → 패널이 사라지고 왼쪽 위에 **SHADER LAB (H)** 버튼만 남는다. 다시 누르면 펴진다. 오른쪽 위 **접기 (H)** 버튼도 같은 일을 한다.
4. Hierarchy에서 `Shader Lab` 오브젝트 선택 → 인스펙터에 컴포넌트 3개: `Shader Lab Controller`, `UI Document`, `Shader Lab UI`.
   - `UI Document`의 **Panel Settings** = `ShaderLabPanelSettings`, **Source Asset** = `ShaderLab` (UXML 파일)
   - `Shader Lab UI`의 **Lab** = 같은 오브젝트의 `Shader Lab Controller`
5. 메뉴 **Window → UI Toolkit → Debugger** → 왼쪽 위 목록에서 Game 뷰 패널을 고르고 **Pick Element** → 패널의 탭 버튼 하나를 클릭.
   - 오른쪽에 그 버튼의 클래스 `tab-button`(선택된 탭은 `tab-button--selected`도)와 적용된 USS 규칙이 보인다.

[그림: UI Toolkit Debugger에서 탭 버튼을 고른 모습. 왼쪽 요소 트리(root → side-panel → tab-bar → Button), 오른쪽 클래스 목록과 USS 규칙]

6. Game 뷰 창 크기를 줄였다 늘린다 → 패널도 같이 작아지고 커진다 (PanelSettings가 화면 크기에 맞춰 늘이기 때문).

### B. 세 파일 열어 보기 (8분)

7. `UI/ShaderLab.uxml`을 코드 편집기로 연다. 24행 `<ui:VisualElement name="tab-bar" class="tab-bar" />` — **속이 비어 있다**. "탭 버튼 8개는 어디서 왔을까?" 질문을 던진다.
8. `Scripts/ShaderLabUI.cs` 107–125행을 보여 준다. 탭 이름 목록을 돌며 버튼을 코드로 만든다 → 정답.
9. `UI/ShaderLab.uss` 1–10행 `:root { --lab-bg: … }` 를 보여 준다. 1행 주석 "색은 여기 변수만 바꾸면 전체가 바뀐다"를 읽어 준다.

### C. 한 줄 바꿔 보기 (7분)

10. 플레이를 **멈추고**, USS 8행 `--lab-accent: rgb(110, 168, 255);` 를 `rgb(255, 120, 80);` 로 바꿔 저장 → 다시 ▶.
    - 제목 `SHADER LAB`, 절 제목(파란 글씨였던 것), 마우스를 올린 버튼 테두리가 한꺼번에 주황으로 바뀐다.
11. 쉐이더 탭의 선택된 스타일 카드를 가리킨다 → 배경이 아직 **파르스름하다**. "왜 여기는 안 바뀌었을까?"(답: 208행에 색이 숫자로 직접 적혀 있음) — 실습 2에서 다룬다.
12. `P`, `M`, `O`, `Space`를 눌러 보이며 "이 키들은 어디에 적혀 있을까?" → `HandleShortcuts()` 216–236행. 패널 아래 안내 글(UXML 27행)은 **그냥 글자**라서 키를 더하면 손으로 같이 고쳐야 한다고 말한다.
13. 바꾼 색을 되돌린다: `git restore Assets/CharacterShaderTest/UI/ShaderLab.uss`.

---

## 개념 설명

### 1. 용어 일곱 개

| 용어 | 한 줄 풀이 | 이 프로젝트에서 |
|---|---|---|
| UI Toolkit | 웹 페이지처럼 "구조 파일 + 스타일 파일 + 코드"로 UI를 만드는 Unity UI 방식 | 쉐이더 랩 패널 전체 |
| IMGUI | 코드로 매 프레임 버튼을 그리는 옛 방식 UI (`OnGUI`) | `main`의 "조작 패널 (H)" (1차시) |
| VisualElement | UI Toolkit 화면을 이루는 상자 하나. 버튼·글자·스크롤 칸도 모두 이것의 한 종류 | `root`, `side-panel`, `tab-bar` … |
| UXML | 상자를 어떻게 끼워 넣을지 적은 **구조** 파일 (HTML과 비슷) | `UI/ShaderLab.uxml` |
| USS | 상자의 **색·크기·여백**을 적은 스타일 파일 (CSS와 비슷) | `UI/ShaderLab.uss` |
| UIDocument | 장면 안에서 "이 UXML을 이 PanelSettings로 화면에 띄워라"를 맡는 컴포넌트 | `Shader Lab` 오브젝트 |
| PanelSettings | 화면 크기 맞추기·테마·그리는 순서를 정한 에셋 | `UI/ShaderLabPanelSettings.asset` |

> **테마 스타일 시트(TSS)**: 버튼·체크박스 같은 기본 모양을 정한 스타일 묶음. `UI/ShaderLabTheme.tss`는 한 줄 `@import url("unity-theme://default");` — Unity 기본 테마를 그대로 가져온다.

### 2. 누가 무엇을 맡나 — UXML · USS · C# 분업

[그림: 세 칸 그림. 왼쪽 "UXML = 뼈대(상자 이름·class)", 가운데 "USS = 옷(색·여백)", 오른쪽 "C# = 손발(탭 내용 만들기·버튼 연결·단축키)". 셋을 UIDocument가 묶어 화면에 띄움]

| 파일 | 맡는 일 | 예 |
|---|---|---|
| `UI/ShaderLab.uxml` | 바뀌지 않는 **뼈대**. 상자마다 `name`(이름표)과 `class`(옷 이름)를 붙인다 | 패널·제목·탭 줄·스크롤 칸·와이프 손잡이·이름표 |
| `UI/ShaderLab.uss` | 모든 **색과 모양** | 패널 배경, 선택된 탭 색, 카드 테두리 |
| `Scripts/ShaderLabUI.cs` | 상황에 따라 **바뀌는 내용**과 **동작** | 탭 버튼·탭 내용 만들기, 버튼 → Controller 함수 연결, 단축키, 와이프 덮개 위치 |
| `Scripts/ShaderLabController.cs` | 랩의 **상태** (UI는 모름) | `SetStyleB()`, `SetEnvironment()` … |

- UXML 맨 위 2행 `<Style src="ShaderLab.uss" />` 가 USS를 붙인다.
- `ShaderLabUI.cs` 11행 주석이 이 분업을 그대로 적어 두었다: "화면 구조는 ShaderLab.uxml, 색·모양은 ShaderLab.uss. 이 스크립트는 탭 내용을 만들고 ShaderLabController에 연결한다".

**UIDocument와 PanelSettings는 누가 만들었나**: 장면 만들기 메뉴(`Editor/SemiRealInkTestSetup.cs`)가 만듭니다.

| 줄 | 코드 | 뜻 |
|---|---|---|
| 739–741 | `labGo.AddComponent<UIDocument>()` → `panelSettings`, `visualTreeAsset` | `Shader Lab` 오브젝트에 UIDocument를 붙이고 PanelSettings·UXML을 넣음 |
| 742–743 | `AddComponent<ShaderLabUI>()`, `ui.lab = lab` | 패널 스크립트를 붙이고 Controller를 연결 |
| 1111 | `themeStyleSheet = … ShaderLabTheme.tss` | 기본 테마 |
| 1112–1114 | `ScaleWithScreenSize`, `1920 × 1080`, `match = 0.5f` | 1920×1080 화면 기준으로 그리고, 화면이 바뀌면 가로·세로를 반반 섞어 늘인다 |

### 3. UXML 읽기 — `name`과 `class`

```xml
<!-- ShaderLab.uxml 18–28행 (찾을 글자: name="side-panel") -->
<ui:VisualElement name="side-panel" class="side-panel">
    <ui:VisualElement class="panel-header">
        <ui:Label text="SHADER LAB" class="panel-title" />
        <ui:Label text="세미리얼 먹 쉐이더 테스트" class="panel-subtitle" />
        <ui:Button name="collapse-button" text="접기 (H)" class="ghost-button" />
    </ui:VisualElement>
    <ui:VisualElement name="tab-bar" class="tab-bar" />
    <ui:ScrollView name="tab-content" class="tab-content" />
    <ui:Label name="footer" class="panel-footer" text="…" />
</ui:VisualElement>
```

| 속성 | 뜻 | 누가 쓰나 |
|---|---|---|
| `name` | 상자 하나만 가리키는 이름표 (한 번만 씀) | C#이 `root.Q("tab-bar")`로 찾을 때 |
| `class` | 옷 이름. 여러 상자가 같은 class를 가질 수 있다 | USS가 `.tab-bar { … }`로 색을 입힐 때 |
| `picking-mode="Ignore"` | 마우스 클릭이 이 상자를 **뚫고 지나간다** | 4행 `root`, 7행 `wipe-overlay`, 14–15행 이름표 |

C#은 `name`으로 상자를 찾아 변수에 담습니다 (`ShaderLabUI.cs` 80–88행):

```csharp
root = doc.rootVisualElement.Q("root");
panel = root.Q("side-panel");
tabBar = root.Q("tab-bar");
content = root.Q<ScrollView>("tab-content");
```

> **`Q`**: Query(찾기)의 줄임. `Q<ScrollView>("tab-content")`는 "이름이 tab-content인 ScrollView를 찾아라".

**왜 `root`는 클릭을 통과시키나**: `root`는 화면 전체를 덮습니다. 클릭을 막으면 캐릭터를 마우스로 돌릴 수 없습니다. 그래서 빈 곳은 통과시키고, 마우스 아래에 패널이 **진짜로 있는지**는 `IsPointerOverUi()`(164–174행)가 `root.panel.Pick()`으로 따로 확인해 카메라(`lab.orbitCamera.isPointerOverUi`, 127–128행)에 알려 줍니다.

### 4. USS 읽기 — 고르기(선택자)와 변수

**선택자(selector)**: "어떤 상자에 이 규칙을 입힐까"를 고르는 글자.

| 선택자 | 고르는 것 | 이 파일의 예 (줄) |
|---|---|---|
| `.이름` | 그 class를 가진 상자 | `.side-panel` (63) |
| `글자종류` | 그 종류의 상자 전부 | `.lab-root Button` (107) — 패널 안 모든 버튼 |
| `A B` (띄어쓰기) | A **안에 있는** B | `.panel-header .ghost-button` (86) |
| `:hover` | 마우스를 올린 동안 | `.lab-root Button:hover` (113) |
| `:root` | 맨 바깥 상자 | 변수 모음 (2–10) |

**class 이름 규칙**: `tab-button--selected`의 `--`는 "상태", `card__title`의 `__`는 "부품"이라는 뜻으로 붙였습니다. C#이 `EnableInClassList("tab-button--selected", 참/거짓)`으로 이 class를 붙였다 뗐다 하면 색이 바뀝니다.

**규칙이 겹치면**: 더 자세히 고른 쪽이 이깁니다. 탭 버튼은 `Button`이기도 하고 `.tab-button`이기도 해서, `.lab-root Button`(107행)보다 `.lab-root .tab-button`(122행)이 이깁니다. 그리고 **C# 코드에서 직접 넣은 스타일**(`b.style.minWidth = …`, 391행)은 USS보다 항상 이깁니다.

**USS 변수**: `--`로 시작하는 이름에 값을 담아 두고 `var(--이름)`으로 꺼내 씁니다 (`ShaderLab.uss` 2–10행).

```css
:root {
    --lab-bg: rgba(14, 17, 23, 0.88);
    --lab-bg-light: rgba(255, 255, 255, 0.06);
    --lab-line: rgba(255, 255, 255, 0.10);
    --lab-text: rgb(226, 231, 239);
    --lab-text-dim: rgb(140, 150, 165);
    --lab-accent: rgb(110, 168, 255);
    --lab-accent-dark: rgb(44, 82, 140);
}
```

| 변수 | 줄 | 어디에 쓰나 (대표) |
|---|---|---|
| `--lab-bg` | 3 | 패널 배경(67), A·B 이름표(58), **SHADER LAB (H)** 버튼(227) |
| `--lab-bg-light` | 4 | 버튼(108)·탭 버튼(127)·카드(203) 바탕 |
| `--lab-line` | 5 | 패널 테두리(70), 탭 줄 밑줄(104), 카드 테두리(202) |
| `--lab-text` | 6 | 기본 글자(16, 192), 버튼 글자(111) |
| `--lab-text-dim` | 7 | 부제목(83), 도움말(153), 아래 안내 글(93) |
| `--lab-accent` | 8 | 제목(80), 절 제목(148), 마우스 올림 테두리(113), 선택 카드 테두리(207) |
| `--lab-accent-dark` | 9 | 선택된 탭(132), 선택된 버튼 칸(173) |

> `rgba(빨, 초, 파, 불투명도)`: 색 세 개는 0~255, 마지막은 0(투명)~1(불투명).

**변수를 안 쓴 곳 (숫자로 직접 적은 색)** — 변수를 바꿔도 **안 바뀝니다**.

| 줄 | 찾을 글자 | 지금 색 |
|---|---|---|
| 115 | `background-color: rgba(255, 255, 255, 0.08);` | 드롭다운 칸 바탕 |
| 128 | `color: rgb(180, 190, 205);` | 선택 안 된 탭 글자 |
| 133 | `color: rgb(255, 255, 255);` | 선택된 탭 글자 (흰색) |
| 208 | `background-color: rgba(110, 168, 255, 0.16);` | 선택된 카드 바탕 (**강조색과 같은 파랑**) |

### 5. 탭은 어떻게 만들어지나

탭은 UXML에 없습니다. **"이름 + 내용 만드는 함수" 짝의 목록**을 C#이 들고 있습니다.

```csharp
// ShaderLabUI.cs 62행
readonly List<(string name, Action<VisualElement> build)> tabs = new();
```

> **`Action<VisualElement>`**: "VisualElement 하나를 받아 무언가 하는 함수"를 담는 변수 종류. 여기서는 "탭 내용을 이 상자 안에 채워 넣는 함수".

```csharp
// ShaderLabUI.cs 107–125행 (OnEnable 안, 찾을 글자: tabs.Add(("비교")
tabs.Clear();
tabs.Add(("비교", BuildCompareTab));
...
tabs.Add(("커스텀", BuildCustomizeTab));
tabBar.Clear();
tabButtons.Clear();
for (int i = 0; i < tabs.Count; i++)
{
    int index = i;
    var b = new Button(() => SelectTab(index)) { text = tabs[i].name };
    b.AddToClassList("tab-button");
    tabBar.Add(b);
    tabButtons.Add(b);
}
```

| 순서 | 줄 | 하는 일 |
|---|---|---|
| ① 목록 채우기 | 107–115 | 탭 이름과 내용 함수를 짝지어 넣는다 |
| ② 버튼 만들기 | 116–125 | 목록을 돌며 버튼을 만들어 UXML의 빈 `tab-bar`에 넣는다. class `tab-button`을 붙여 USS 122행 모양을 입힌다 |
| ③ 탭 고르기 | 176–182 `SelectTab()` | 고른 탭 버튼에만 `tab-button--selected`를 붙이고 `RebuildContent()` |
| ④ 내용 다시 만들기 | 184–192 `RebuildContent()` | 스크롤 칸을 비우고 `tabs[currentTab].build(content)` — 고른 탭의 함수를 부른다 |

`int index = i;`를 따로 두는 이유: 버튼 함수 `() => SelectTab(index)`가 **그때의 번호**를 기억하게 하려고. `i`를 그대로 쓰면 모든 버튼이 반복이 끝난 뒤의 `i`(= 8)를 보게 됩니다.

**탭 내용을 만드는 도우미** (561–623행). 도우미마다 USS class를 하나씩 붙입니다.

| 도우미 | 만드는 것 | 붙이는 class (USS 줄) |
|---|---|---|
| `Section(c, "제목")` | 절 제목 글자 | `section-title` (143) |
| `Help(c, "설명")` | 흐린 설명 글자 | `help` (151) |
| `Row(c)` | 가로 줄 상자 | `row` (157) |
| `Segment(row, "글자", 함수)` | 나란한 버튼 | `segment` (161) |
| `Card(c, 제목, 설명, 함수)` | 클릭되는 카드 | `card`, `card__title`, `card__text` (197–215) |
| `LabToggle(c, 이름, 설명, 함수)` | 체크박스 + 설명 | `lab-toggle`, `lab-toggle__help` (184–190) |

### 6. 상태 → 화면 맞추기: `refreshers`

랩 상태가 바뀌면(스타일·환경·스위치…) 패널의 선택 표시도 바뀌어야 합니다. 그 일을 하는 함수들을 목록에 모아 둡니다.

```csharp
// ShaderLabUI.cs 68–69행
// 상태가 바뀔 때 다시 칠할 것들
readonly List<Action> refreshers = new();
```

[그림: 화살표 흐름. 버튼 클릭 → lab.SetStyleB() → Controller의 NotifyChanged() → Changed 이벤트 → ShaderLabUI.OnLabChanged() → Refresh() → refreshers 목록의 함수들이 하나씩 실행 → 카드 선택 표시 갱신]

| 단계 | 위치 |
|---|---|
| 탭을 만들 때 "나중에 다시 칠하는 법"을 등록 | 예: 360행 `refreshers.Add(() => card.EnableInClassList("card--selected", lab.StyleB == index));` |
| Controller가 상태를 바꾸면 알림 | `ShaderLabController.cs` 97행 `NotifyChanged() => Changed?.Invoke();` |
| UI가 알림을 받음 | 129행 `lab.Changed += OnLabChanged;` → 194–201행 |
| 등록된 함수를 모두 실행 | 203–207행 `Refresh()` |
| 탭을 바꾸면 목록을 비움 | 187행 `refreshers.Clear();` (안 보이는 탭의 상자를 칠하지 않게) |

환경이 바뀌면 후처리 효과 목록 자체가 달라지므로, 이때는 칠하기만 하지 않고 **탭을 통째로 다시 만듭니다** (197–198행).

### 7. 단축키

```csharp
// ShaderLabUI.cs 209–214행
void Update()
{
    HandleShortcuts();
    UpdateCompareOverlay();
    tick?.Invoke();
}
```

`HandleShortcuts()`(216–236행)가 매 프레임 키보드를 봅니다.

| 키 | 줄 | 하는 일 |
|---|---|---|
| `1` `2` `3` `4` | 222–225 | 와이프 / 나란히 / A만 / B만 |
| `Tab` | 226 | `Compare.Flip()` |
| `P` | 227 | 후처리 켜기·끄기 |
| `M` | 228–229 | 재질 판별 보기 ↔ 보통 화면 |
| `O` | 230 | 먹 아웃라인 기능 스위치 |
| `Space` | 231 | 자동 회전 |
| `F` | 232 | 처음 시점 |
| `H` | 233 | 패널 접기·펴기 |

```csharp
var k = Keyboard.current;                 // Input System 패키지의 키보드
if (k.pKey.wasPressedThisFrame) lab.SetPostProcessing(!lab.PostProcessing);
```

> **`wasPressedThisFrame`**: "이번 프레임에 막 눌렸는가". 누르고 있는 동안 계속 참이 되는 것이 아니라 **누른 순간 한 번만** 참.
> 이 프로젝트는 옛 `Input.GetKeyDown` 대신 **Input System 패키지**(`UnityEngine.InputSystem`, 4행)를 씁니다.

- `1`~`4`, `Tab`, `Space`는 Controller를 거치지 않고 비교 화면·카메라를 직접 바꾸므로 `compareChanged = true` → 마지막에 `Refresh()`를 직접 부릅니다 (234–235행).
- 패널 아래 안내 글(UXML 26–27행 `name="footer"`)은 **코드와 연결되지 않은 글자**입니다. 키를 더하면 여기도 손으로 고칩니다.

### 8. 한글 글꼴

기본 UI 글꼴에는 한글이 없습니다. `ApplyKoreanFont()`(142–156행)가 컴퓨터에 깔린 글꼴(17행 `"Malgun Gothic", "Apple SD Gothic Neo", "Noto Sans CJK KR", "NanumGothic"` 순서)로 글꼴 에셋을 만들어 맨 바깥 상자에 넣습니다. 그래서 글꼴 파일을 프로젝트에 넣지 않아도 됩니다. 못 찾으면 Console에 "한글 글꼴을 찾지 못했습니다…" 경고가 뜹니다.

---

## 실습

### 실습 1. 저장 지점 받고 구조 찾기 (10분)

1. "시작 저장 지점" 명령으로 `lesson07` 브랜치를 만든다.
   - 확인: `git branch` 에 `* lesson07`
2. 장면을 열고 Hierarchy `Shader Lab` 선택.
   - 확인: `UI Document`의 **Panel Settings** = `ShaderLabPanelSettings`, **Source Asset** = `ShaderLab`
3. Project 창에서 `UI/ShaderLabPanelSettings`를 선택해 인스펙터를 본다.
   - 확인: Scale Mode = **Scale With Screen Size**, Reference Resolution = **1920 × 1080**, Theme Style Sheet = `ShaderLabTheme`
4. ▶ 플레이 → **Window → UI Toolkit → Debugger** → **Pick Element**로 아래 표의 상자를 하나씩 찍고 class를 적는다.

| 찍을 곳 | 보이는 class (정답) |
|---|---|
| 선택된 탭 버튼 | `tab-button`, `tab-button--selected` |
| **쉐이더** 탭의 스타일 카드 제목 | `card__title` |
| **비교** 탭의 "비교 방식" 글자 | `section-title` |

### 실습 2. USS 변수로 테마 바꾸기 (20분)

> USS는 플레이를 **멈춘 상태**에서 고치고 저장한 뒤 다시 ▶ 합니다.

**2-1. 강조색만 주황으로 (3줄)**

파일: `Assets/CharacterShaderTest/UI/ShaderLab.uss`

1. 8–9행 (찾을 글자: `--lab-accent:`)을 아래로 바꾼다.

```css
    --lab-accent: rgb(255, 160, 70);
    --lab-accent-dark: rgb(150, 80, 30);
```

2. ▶ 플레이.
   - 확인: 제목 `SHADER LAB`, 절 제목, 선택된 탭 바탕이 주황 계열
   - 확인: **쉐이더** 탭의 선택된 카드 바탕은 아직 **파르스름**하다 (208행은 변수를 안 씀)
3. 멈추고, 208행 (찾을 글자: `rgba(110, 168, 255, 0.16)`)을 아래로 바꾼다.

```css
    background-color: rgba(255, 160, 70, 0.16);
```

   - 확인: 다시 플레이하면 선택된 카드 바탕도 주황빛
4. `git add -A` → `git commit -m "7차시 주황 강조색"`.

**2-2. 밝은 "한지" 테마 (변수 7개 + 숫자 색 1곳)**

1. 2–10행 `:root { … }` 전체를 아래로 바꾼다.

```css
:root {
    --lab-bg: rgba(244, 238, 224, 0.94);
    --lab-bg-light: rgba(0, 0, 0, 0.05);
    --lab-line: rgba(0, 0, 0, 0.15);
    --lab-text: rgb(40, 36, 30);
    --lab-text-dim: rgb(110, 100, 88);
    --lab-accent: rgb(178, 58, 42);
    --lab-accent-dark: rgb(130, 40, 30);
}
```

2. 플레이해 본다.
   - 확인: 패널이 밝은 미색, 글자가 먹색, 강조가 주홍색
   - 확인: **선택 안 된 탭 글자**가 밝은 회색이라 잘 안 보인다 (128행 숫자 색)
3. 멈추고 128행 (찾을 글자: `color: rgb(180, 190, 205);`)을 변수로 바꾼다.

```css
    color: var(--lab-text-dim);
```

4. 208행도 새 강조색에 맞춘다.

```css
    background-color: rgba(178, 58, 42, 0.16);
```

5. 플레이해 탭 8개를 모두 눌러 본다.
   - 확인: 탭 글자가 다 읽힌다. 선택된 탭은 주홍 바탕 + 흰 글자(133행)
   - 확인: A·B 이름표(`--lab-bg` 사용)도 밝은 바탕으로 바뀌었다
6. 마음에 드는 테마로 정하고 `git add -A` → `git commit -m "7차시 테마"`.

[그림: 기본(남색) 테마와 한지 테마 패널을 나란히 놓은 캡처]

### 실습 3. "정보" 탭 버튼 추가 (35분)

목표: 탭 줄 맨 끝에 **정보** 탭을 만든다. 누르면 지금 A·B 스타일, 환경, 디버그 보기, 후처리 상태와 프리셋 개수, 단축키, Unity 버전이 나온다. `I` 키로도 연다.

> **고치는 순서가 중요합니다.** 파일 **아래쪽부터** 고치면 위쪽 줄 번호가 밀리지 않습니다. ① 559행 → ② 233행 → ③ 115행 순서로 갑니다. 다른 순서로 했다면 **찾을 글자**로 찾으세요.

**① 탭 내용 함수 만들기** — `Assets/CharacterShaderTest/Scripts/ShaderLabUI.cs`

1. 560행 `// ================= 작은 도우미 =================` 를 찾는다. 바로 위 558행 `}`는 `BuildCustomizeTab`의 끝이다.
2. 그 사이 559행(빈 줄)에 아래 코드를 **통째로** 넣는다.

```csharp

    // 7차시 실습: 정보 탭
    void BuildInfoTab(VisualElement c)
    {
        Section(c, "쉐이더 랩");
        Help(c, "화면 구조는 ShaderLab.uxml, 색·모양은 ShaderLab.uss, 탭 내용과 단축키는 ShaderLabUI.cs가 만든다");

        Section(c, "지금 상태");
        var stateA = new Label();
        var stateB = new Label();
        var stateEnv = new Label();
        var stateView = new Label();
        var statePost = new Label();
        c.Add(stateA);
        c.Add(stateB);
        c.Add(stateEnv);
        c.Add(stateView);
        c.Add(statePost);
        // 랩 상태가 바뀔 때마다 다시 칠한다
        refreshers.Add(() =>
        {
            stateA.text = "A (왼쪽): " + lab.StyleName(lab.StyleA);
            stateB.text = "B (오른쪽): " + lab.StyleName(lab.StyleB);
            stateEnv.text = "환경: " + (lab.environments.Count > 0 ? lab.environments[lab.Environment].displayName : "없음");
            string viewName = lab.DebugView.ToString();
            foreach (var (view, label) in DebugInfo)
                if (view == lab.DebugView)
                    viewName = label;
            stateView.text = "디버그 보기: " + viewName;
            statePost.text = "후처리: " + (lab.PostProcessing ? "켜짐" : "꺼짐");
        });

        Section(c, "들어 있는 것");
        Help(c, $"스타일 프리셋 {lab.styles.Count}개 · 환경 프리셋 {lab.environments.Count}개 · 조명 프리셋 {lab.lightingPresets.Count}개");
        Help(c, $"애니메이션 클립 {lab.Clips.Count}개 · 커스텀 칸 {lab.Slots.Count}개");

        Section(c, "단축키");
        Help(c, "1~4 비교 방식 · Tab A↔B · P 후처리 · M 재질 판별 · O 아웃라인 · Space 회전 · F 처음 시점 · H 패널 접기 · I 정보");

        Section(c, "버전");
        Help(c, "Unity " + Application.unityVersion);

        var back = new Button(() => SelectTab(0)) { text = "비교 탭으로 돌아가기" };
        back.AddToClassList("wide-button");
        c.Add(back);
    }
```

| 코드 | 쓴 것 | 원래 어디 있나 |
|---|---|---|
| `Section`, `Help` | 도우미 | 561, 568행 |
| `refreshers.Add(…)` | 상태 → 화면 맞추기 | 69행 목록 |
| `lab.StyleName`, `StyleA`, `StyleB` | Controller | `ShaderLabController.cs` 100, 48–49행 |
| `lab.environments[lab.Environment].displayName` | 환경 이름 | 44, 50행 / 환경 탭 403–404행과 같은 방식 |
| `DebugInfo` | 디버그 보기 한국어 이름표 | 41–53행 |
| `lab.Clips`, `lab.Slots` | 클립·커스텀 칸 목록 | `ShaderLabController.cs` 274, 295행 |
| `wide-button` | 넓은 버튼 class | USS 176행 |

**② 단축키 `I` 더하기** — 같은 파일

3. 233행 (찾을 글자: `if (k.hKey.wasPressedThisFrame)`) **바로 아래**, 234행 `if (compareChanged)` **위**에 넣는다.

```csharp
        if (k.iKey.wasPressedThisFrame)
        {
            int info = tabs.FindIndex(t => t.name == "정보");
            if (info >= 0)
            {
                SetPanelVisible(true);
                SelectTab(info);
            }
        }
```

   - 패널이 접혀 있어도 `I`를 누르면 펴면서 정보 탭을 연다.
   - 탭 번호를 숫자(8)로 적지 않고 이름으로 찾는다. 나중에 탭을 더해도 안 깨진다.
   - 키 `I`는 지금 아무 데도 안 쓴다 (216–236행에 없음).

**③ 탭 목록에 한 줄** — 같은 파일

4. 115행 `tabs.Add(("커스텀", BuildCustomizeTab));` **바로 아래**, 116행 `tabBar.Clear();` **위**에 넣는다.

```csharp
        tabs.Add(("정보", BuildInfoTab));
```

   - 꼭 `for` 반복(118–125행) **앞**이어야 한다. 반복 뒤에 넣으면 목록에는 있지만 **버튼이 만들어지지 않는다**.

**④ 아래 안내 글 고치기** — `Assets/CharacterShaderTest/UI/ShaderLab.uxml`

5. 27행 (찾을 글자: `Space 회전 · F 처음 시점`)을 아래 한 줄로 바꾼다.

```xml
                      text="왼쪽 드래그 돌리기 · 휠 확대 · 오른쪽 드래그 위아래&#10;1~4 비교 방식 · Tab A↔B · P 후처리 · M 재질 판별 · O 아웃라인 · Space 회전 · F 처음 시점 · I 정보" />
```

   - `&#10;`는 UXML 속 줄바꿈 기호. 지우지 않는다.

**⑤ 확인**

6. 저장하고 Unity로 돌아간다 (스크립트를 다시 컴파일한다).
   - 확인: Console에 빨간 오류가 없다
7. ▶ 플레이.
   - 확인: 탭 줄 맨 끝에 **정보** 버튼. 자리가 모자라면 다음 줄로 넘어간다 (USS 101행 `flex-wrap: wrap`)
8. **정보** 탭을 누른다.
   - 확인: `A (왼쪽): 원본 (URP Lit)`, `B (오른쪽): 세미리얼`, `환경: 얼어붙은 세상`, `디버그 보기: 보통 화면`, `후처리: 켜짐`
   - 확인: `스타일 프리셋 3개 · 환경 프리셋 5개 · 조명 프리셋 7개`, `애니메이션 클립 10개 · 커스텀 칸 2개`
9. 정보 탭을 연 채로 `M` → `M`, `P` → `P`.
   - 확인: "디버그 보기", "후처리" 줄이 **바로** 바뀐다 (refreshers 덕분)
10. **쉐이더** 탭에서 **스타일라이즈드** 카드, **환경** 탭에서 **노을** → `I`.
    - 확인: 정보 탭이 열리고 `B (오른쪽): 스타일라이즈드`, `환경: 노을`
11. `H`로 패널을 접고 `I`.
    - 확인: 패널이 펴지며 정보 탭이 열린다
12. **비교 탭으로 돌아가기** 버튼.
    - 확인: 비교 탭이 선택된다
13. `git add -A` → `git commit -m "7차시 정보 탭"`.

[그림: 한지 테마 + 정보 탭을 연 패널. 탭 줄 끝 "정보" 버튼이 선택된 상태]

### 실습 4. 정리 퀴즈 (5분)

짝과 서로 묻고 답한다.

1. 정보 탭을 만들 때 꼭 필요한 곳 두 군데는? (답: 내용 함수 `BuildInfoTab` 하나, `tabs.Add(("정보", BuildInfoTab))` 한 줄. 단축키 `I`와 안내 글은 덤)
2. 탭 버튼의 색을 바꾸려면 어느 파일? (답: `ShaderLab.uss` 122–134행)
3. 탭 버튼 글자를 바꾸려면 어느 파일? (답: `ShaderLabUI.cs`의 `tabs.Add(("이름", …))`)

---

## 정리

- **UIDocument**(장면) = UXML(뼈대) + PanelSettings(화면 맞추기·테마).
- **UXML**은 `name`(C#이 찾는 이름표)과 `class`(USS가 입히는 옷 이름)를 붙인 상자 구조. 바뀌는 내용은 비워 두고 C#이 채운다.
- **USS 변수** `--lab-…` 7개만 바꾸면 패널 대부분의 색이 바뀐다. 숫자로 직접 적은 색(115·128·133·208행)은 따로 고친다.
- **탭** = `(이름, 내용 함수)` 목록. 목록에 한 줄 + 함수 하나면 버튼과 내용이 생긴다. 버튼은 `for` 반복이 만든다.
- **refreshers**: 탭을 만들 때 "다시 칠하는 법"을 등록 → 상태가 바뀌면 `Changed` → `Refresh()`.
- **단축키**: `Update()` → `HandleShortcuts()`에서 `Keyboard.current.○Key.wasPressedThisFrame`. 안내 글은 손으로 같이 고친다.

## 과제

**과제 1. 나만의 테마** — `ShaderLab.uss`로 테마 하나를 만든다 (실습의 주황·한지와 다른 색).
- `:root` 변수 7개 중 5개 이상 바꾼다.
- 숫자로 직접 적은 색(115·128·133·208행) 중 **2곳 이상**을 `var(--lab-…)`로 바꾸거나 새 테마에 맞춘다.
- 캡처 2장: **비교** 탭, **쉐이더** 탭(선택된 카드가 보이게).

**과제 2. 정보 탭 넓히기** — 실습 3의 정보 탭에 "지금 상태" 줄을 **2개 더** 넣는다.
- 예: 조명 프리셋 이름(`lab.lightingPresets[lab.Lighting].displayName`), 톤매핑(`lab.CurrentTonemapping`), 노출(`lab.Exposure`), 비교 방식(`Compare.mode`), 자동 회전(`lab.orbitCamera.AutoRotate`).
- 값이 바뀌면 **정보 탭을 연 채로** 바로 바뀌어야 한다 (`refreshers` 안에 넣기). 비교 방식·자동 회전은 키(`1`~`4`, `Space`)로 바꾸므로 확인하기 쉽다.
- 목록이 비어 있을 수 있는 값은 실습 코드의 환경 줄처럼 `Count > 0`을 먼저 확인한다.

| 제출물 | 채점 기준 | 배점 |
|---|---|---|
| `ShaderLab.uss` 변경분 (`git diff` 캡처 또는 파일) | 변수 5개 이상 + 숫자 색 2곳 이상 정리 | 25 |
| 테마 캡처 2장 | 모든 글자가 읽힌다 (탭 글자·도움말·드롭다운 포함) | 15 |
| `ShaderLabUI.cs` 변경분 | 컴파일 오류 없음. `tabs.Add`가 `for` 반복 앞, 단축키 `I`가 `HandleShortcuts()` 안 | 25 |
| 정보 탭 동작 영상 또는 캡처 3장 | 정보 탭을 연 채로 키를 눌러 새 줄 2개가 바로 바뀌는 것이 보인다 | 25 |
| 공통 | 파일 이름 `이름_07_번호.png`, 커밋 메시지에 차시 표시 | 10 |

## 확인 질문

1. `ShaderLab.uxml`의 `tab-bar`는 속이 비어 있다. 그런데 플레이하면 탭 버튼 8개가 보인다. 버튼은 누가, 어디서 만드는가?
   - **정답**: `ShaderLabUI.cs`의 `OnEnable()`이 `tabs` 목록(107–115행)을 돌며 `new Button(…)`으로 만들어 `tabBar.Add(b)`로 넣는다 (118–125행).
2. `--lab-accent`를 주황으로 바꿨는데 **쉐이더** 탭의 선택된 카드 바탕은 파르스름하게 남았다. 왜이고, 어떻게 고치나?
   - **정답**: `.card--selected`의 바탕(208행)이 변수가 아니라 숫자 `rgba(110, 168, 255, 0.16)`로 적혀 있어서. 그 줄을 새 색으로 바꾼다.
3. 정보 탭을 연 채로 `M`을 누르면 "디버그 보기" 줄이 바로 바뀐다. 어떤 길을 거치는가?
   - **정답**: `HandleShortcuts()` → `lab.SetDebugView()` → `NotifyChanged()` → `Changed` 이벤트 → `OnLabChanged()` → `Refresh()` → 정보 탭이 `refreshers`에 넣어 둔 함수가 글자를 다시 쓴다.

## 자주 막히는 곳

| 증상 | 원인 | 해결 |
|---|---|---|
| USS를 고쳤는데 색이 그대로 | 저장 안 함 / 플레이 중에 고침 | 저장 → 플레이 멈춤 → 다시 ▶ |
| 어떤 칸만 색이 안 바뀜 | 그 줄은 변수가 아니라 숫자 색 | 개념 4의 "변수를 안 쓴 곳" 표(115·128·133·208행) |
| 변수를 바꿨는데 그 칸이 엉뚱한 색 | 변수 이름 오타 (`var(--lab-acent)`) 또는 `;` 빠짐 | `:root`의 이름과 `var(…)` 속 이름을 글자 그대로 맞춘다 |
| 밝은 테마에서 글자가 안 보임 | 128행 탭 글자가 밝은 회색 숫자 색 | 128행을 `var(--lab-text-dim)` 으로 |
| `error CS0103: The name 'BuildInfoTab' does not exist` | 함수를 클래스 밖(624행 `}` 뒤)에 넣었거나 이름 오타 | 함수는 558행 `}` 와 560행 주석 사이에 |
| `error CS1513: } expected` 등 괄호 오류 | 붙여 넣을 때 `{` `}` 짝이 깨짐 | 코드 블록을 처음부터 끝까지 다시 복사 |
| 정보 탭 버튼이 안 생김 (오류 없음) | `tabs.Add(("정보", …))`를 `for` 반복(118–125행) **뒤**에 넣음 | 115행 바로 아래로 옮긴다 |
| `I`를 눌러도 반응 없음 | 코드를 `HandleShortcuts()` 밖에 넣음 / Game 창에 초점 없음 | 233행 바로 아래인지 확인, Game 창 클릭 |
| 정보 탭의 상태 줄이 비어 있음 | `refreshers.Add` 안 넣고 `new Label()`만 함 | 실습 코드 그대로 `refreshers.Add(() => { … })` 안에서 `.text`를 쓴다 |
| 정보 탭 상태가 탭을 다시 열어야만 바뀜 | `.text`를 `refreshers` 밖에서 한 번만 씀 | 바뀌는 값은 `refreshers` 안에 |
| 안내 글에 `I 정보`가 안 보임 | UXML 27행을 안 고침 (글자는 코드와 연결 안 됨) | 실습 3 ④ |
| 패널 글자가 네모(□) | 한글 글꼴 없음 | 맑은 고딕 등 설치. Console "한글 글꼴을 찾지 못했습니다" 경고 확인 |
