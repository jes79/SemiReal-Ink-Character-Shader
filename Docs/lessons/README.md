# Unity URP 쉐이더 랩 — 강의 교안

> 강사: 동해님 · 프로젝트: `SemiReal-Ink-Character-Shader` · 장면: `Assets/CharacterShaderTest/Scenes/CharacterShaderTest.unity`

이 폴더는 **쉐이더 랩(Shader Lab)** 장면으로 URP 캐릭터 쉐이더를 배우는 8차시 교안입니다.
학생은 랩 패널에서 값을 바꿔 보며 눈으로 먼저 이해하고, 그다음 쉐이더·C# 코드를 조금씩 고쳐 봅니다.

---

## 1. 과정 개요

| 항목 | 내용 |
|---|---|
| 대상 | Unity 기초(씬·게임 오브젝트·인스펙터·C# 스크립트 붙이기)를 아는 게임 개발 과정 수강생. 쉐이더는 처음이어도 된다 |
| 목표 | ① 캐릭터 화면이 "텍스처 맵 + 빛 + 재질 + 후처리"로 만들어진다는 것을 설명한다 ② 쉐이더 코드에서 원하는 부분을 찾아 값·기능을 하나 더할 수 있다 ③ 프리셋 에셋·UI·임포트 규칙으로 아트 작업 흐름을 만든다 |
| 분량 | 8차시 × 3시간 (180분) = 24시간 |
| 진행 방식 | 매 차시 **시연 → 개념 설명 → 따라 하기 실습 → 정리 → 과제**. 실습은 학생 각자 자기 브랜치에서 한다 |
| 평가 | 차시 과제 8개(각 채점 기준 표 있음) + 확인 질문 |

### 준비물

| 항목 | 버전·내용 | 비고 |
|---|---|---|
| Unity | **6000.6.7f1** (Unity 6) | 다른 6000.x도 대개 열리지만 수업은 이 버전 기준 |
| 렌더 파이프라인 | URP 17.6 (Forward+) | 프로젝트에 들어 있음 |
| 입력 | Input System 패키지 | 프로젝트에 들어 있음 |
| UI | UI Toolkit | Unity에 기본 내장 |
| Git + **Git LFS** | Git 2.23 이상 (`git switch` 명령) | 모델(약 300MB)·텍스처가 LFS에 있음. **ZIP 다운로드 금지** |
| 한글 글꼴 | 맑은 고딕 등 | 없으면 패널 글자가 네모로 보임 |
| 코드 편집기 | Visual Studio / Rider / VS Code | `.shader` 파일 색 표시가 되면 편함 |
| Blender | 4.x 또는 5.x | 8차시 선택 실습만 (VARCO 변환 스크립트) |

### 진행 방식 (한 차시 180분)

| 구간 | 시간 | 하는 일 |
|---|---|---|
| 도입·지난 차시 확인 | 10분 | 지난 과제 1~2개 같이 보기, 오늘 저장 지점 받기 |
| 시연 | 25분 | 강사가 랩에서 누르고 보여 준다 (학생은 보기만) |
| 개념 설명 | 45분 | 쉬운 말 + 쉐이더 코드 몇 줄 |
| 쉬는 시간 | 10분 | |
| 실습 | 70분 | 학생이 단계별로 따라 한다. 단계마다 확인 포인트 |
| 정리·확인 질문·과제 안내 | 20분 | |

---

## 2. 차시표

| 차시 | 파일 | 차시 제목 | 핵심 개념 | 쓰는 랩 탭 | 시작 저장 지점 | 과제 |
|---|---|---|---|---|---|---|
| 1 | [01_쉐이더랩_둘러보기.md](01_쉐이더랩_둘러보기.md) | 쉐이더 랩 둘러보기와 A/B 비교 | 쉐이더·머티리얼·패스, Properties, 레이어 2개+카메라 2대 비교, 머티리얼 복사본 | 비교 (쉐이더·맵·보기는 짧게, 전체 훑기) | `main` → `shader-lab-v1` | A/B 비교 캡처 3장 + 랩 구조도 |
| 2 | [02_조명모델_키필림.md](02_조명모델_키필림.md) | 조명 모델: 키·필·림 | 노멀, N·L(람버트), Realism 2단 명암, 그림자, 환경광·그늘 색, 카메라 기준 필·림 | 조명, 쉐이더, 맵·보기(빛만), 환경, 비교 | `shader-lab-v1` | 조명 비교 시트 + 림 색 순도 값 추가 |
| 3 | [03_재질_피부_천_금속_보석.md](03_재질_피부_천_금속_보석.md) | 재질: 피부 SSS·천·금속·보석 | Material Type 키워드, SSS 근사, sheen, 금속 BRDF, 마스크 맵 vs 자동 판별 | 맵·보기(재질 판별), 쉐이더, 조명, 환경, 비교 | `shader-lab-v1` | 부위 하나 재질 튜닝 보고서 |
| 4 | [04_먹아웃라인_스타일프리셋.md](04_먹아웃라인_스타일프리셋.md) | 먹 아웃라인과 스타일 프리셋 | 뒷면 부풀리기 패스, 픽셀 굵기, ToggleUI vs 키워드, StylePreset 덮어쓰기 | 쉐이더, 비교, 맵·보기 | `shader-lab-v1` | 새 스타일 프리셋 2개 + 기능 스위치 1개 더 |
| 5 | [05_텍스처맵_PBR_ORM.md](05_텍스처맵_PBR_ORM.md) | 텍스처 맵과 PBR, ORM 채널 패킹 | 베이스·노멀·메탈릭·러프니스·AO·이미시브, sRGB vs 데이터, ORM·채널 선택 | 맵·보기, 커스텀(옷) | `shader-lab-v2` | 맵 분석 보고서 + MetallicSmoothness로 바꿔 끼우기 |
| 6 | [06_환경_조명_후처리.md](06_환경_조명_후처리.md) | 환경·조명·후처리 | 하늘·환경광·안개, 반사 프로브, Volume 프로필, 톤매핑·노출 | 환경, 조명, 후처리 | `shader-lab-v2` | 나만의 환경·조명 프리셋 + 소품 버그 고치기 |
| 7 | [07_UI_Toolkit_패널.md](07_UI_Toolkit_패널.md) | UI Toolkit 패널 | UIDocument, UXML·USS·C# 분업, USS 변수, 탭 만들기, 단축키 | 전체 (패널 자체) | `shader-lab-v2` | 테마 바꾸기 + "정보" 탭 버튼 |
| 8 | [08_AI_3D캐릭터_파이프라인.md](08_AI_3D캐릭터_파이프라인.md) | AI 3D 캐릭터 파이프라인 | VARCO 3D → Blender 스크립트 → AssetPostprocessor, Humanoid, Playable, HandGrip | 애니메이션, 커스텀 | `shader-lab-v2` | 동작×스타일 비교 + 소품 칸 추가 |

### 왜 이 순서인가

- **1~4차시는 `shader-lab-v1`**, **5~8차시는 `shader-lab-v2`** 에서 시작합니다. 저장 지점을 한 번만 크게 바꾸면 Unity가 모델(약 300MB)을 다시 가져오는 시간을 줄일 수 있습니다.
- v1의 캐릭터(FinalPlayer)는 **피부·천·금장 자수·보석**이 다 있어서 빛과 재질(2~4차시)을 보기 좋습니다.
- v1 캐릭터에는 메탈릭·러프니스·AO 맵이 없습니다. 그래서 **텍스처 맵 차시(5차시)** 는 ORM 텍스처가 있는 VARCO 캐릭터(v2)에서 합니다. 2차시에서 노멀 맵만 먼저 짧게 다룹니다.
- 5차시에서 VARCO 캐릭터를 먼저 만지고, 8차시에서 "그 캐릭터를 어떻게 만들었나"로 마무리합니다.

---

## 3. 저장 지점 받는 법

### 저장 지점 3개

| 저장 지점 | 종류 | 내용 | 패널 |
|---|---|---|---|
| `main` | 브랜치 | 처음 공개한 쉐이더 예제. 원본 비교 뷰어 + 얼어붙은 세상 장면 | IMGUI "조작 패널 (H)" |
| `shader-lab-v1` | 태그 | 쉐이더 맵·기능 스위치·스타일/환경/조명 프리셋·디버그 보기·UI Toolkit 패널. 모델은 FinalPlayer만 | SHADER LAB (8개 탭, 애니메이션·커스텀 탭은 비어 있음) |
| `shader-lab-v2` | 태그 | v1 + VARCO 캐릭터 3벌·무기 3개·애니메이션 10개·옷/무기 커스텀 (최종) | SHADER LAB (8개 탭 모두 내용 있음) |

> **태그(tag)**: 커밋 하나에 붙인 이름표. 브랜치처럼 앞으로 움직이지 않는 "고정된 저장 지점"입니다.

### 처음 한 번 (복제)

```bash
git lfs install                 # 처음 한 번만
git clone https://github.com/jes79/SemiReal-Ink-Character-Shader.git
cd SemiReal-Ink-Character-Shader
git fetch --all --tags          # 브랜치·태그 모두 받기
git tag                         # shader-lab-v1, shader-lab-v2 가 보이면 OK
```

### 차시를 시작할 때 (권장: 태그에서 내 연습 브랜치 만들기)

**Unity를 먼저 닫고** 아래를 실행합니다.

```bash
git status                              # 바뀐 파일이 있으면 먼저 commit 하거나 git stash
git switch -c lesson02 shader-lab-v1    # 태그 shader-lab-v1 에서 'lesson02' 브랜치를 새로 만들어 이동
git lfs pull                            # 모델·텍스처 실제 파일 받기
```

- 차시마다 `lesson01`, `lesson02` … 처럼 새 브랜치를 만듭니다. 지난 차시에 고친 내용은 그 브랜치에 그대로 남습니다.
- 실습이 끝나면 `git add -A` → `git commit -m "2차시 실습"`으로 저장합니다.

### 다른 방법

| 하고 싶은 일 | 명령 |
|---|---|
| `main` 브랜치로 이동 | `git switch main` |
| 태그를 그냥 구경만 (브랜치 없이) | `git switch --detach shader-lab-v1` |
| 예전 Git 명령으로 태그 받기 | `git checkout shader-lab-v2` (= detached HEAD. 고치려면 `git switch -c 이름`) |
| 예전 Git 명령으로 브랜치 만들기 | `git checkout -b lesson05 shader-lab-v2` |
| 두 저장 지점의 차이 보기 | `git diff shader-lab-v1 shader-lab-v2 --stat` |

> **detached HEAD**: 브랜치가 아닌 커밋 위에 서 있는 상태. 구경은 괜찮지만, 여기서 commit하면 나중에 찾기 어렵습니다. 고칠 거면 브랜치를 만드세요.

### 받은 뒤 확인

1. 텍스처 파일이 1KB 정도의 글자 파일이면 LFS를 못 받은 것 → `git lfs pull`
2. Unity Hub에서 프로젝트 폴더 열기 (처음은 몇 분 걸림)
3. `Assets/CharacterShaderTest/Scenes/CharacterShaderTest.unity` 열기 → ▶ 플레이
4. 왼쪽에 **SHADER LAB** 패널이 보이면 성공 (`main`은 왼쪽 위 IMGUI 조작 패널)

---

## 4. 강사 준비 체크

| 확인 | 방법 |
|---|---|
| 태그·브랜치가 원격 저장소에 올라가 있는가 | `git ls-remote --tags origin` 에 `shader-lab-v1`, `shader-lab-v2`가 보여야 함. 없으면 `git push origin feature/shader-lab --tags` |
| 학생 PC에 Git LFS | `git lfs version` |
| 장면을 다시 만들어야 할 때 | 메뉴 **Tools → Shader Test → Build Test Scene** (값은 유지). 기본값으로 되돌리려면 **Reset Materials And Post To Presets** |
| 학생 실습 중 장면이 망가졌을 때 | `git restore Assets/CharacterShaderTest/Scenes/CharacterShaderTest.unity` |

> **주의:** Build Test Scene은 장면을 **새로 만듭니다**. 학생이 장면에 넣은 것(새 스타일·환경을 `Styles`·`Environments` 목록에 넣은 것, 새 조명 프리셋, 소품 칸 등)은 목록에서 빠집니다. 에셋 파일은 남으니 목록에 다시 넣으면 됩니다.

---

## 5. 차시 파일 구성

모든 차시 파일은 같은 순서입니다.

1. 맨 위 표: 수업 시간 · 난이도 · 시작 저장 지점 · 쓰는 랩 탭
2. 학습 목표 (3줄)
3. 시작 저장 지점 (명령)
4. 시연 순서 — 강사가 무엇을 누르고 무엇을 보여 줄지
5. 개념 설명 — 쉬운 말 + 코드 몇 줄과 줄 위치
6. 실습 — 학생이 따라 하는 단계 + 확인 포인트
7. 정리
8. 과제 — 제출물·채점 기준 표
9. 확인 질문 3개 (정답 포함)
10. 자주 막히는 곳

**줄 번호 안내**: 교안의 줄 번호는 그 차시의 시작 저장 지점 기준입니다. 학생이 앞 실습에서 줄을 더했다면 번호가 밀리므로, 함께 적어 둔 **찾을 글자(Ctrl+F)** 로 찾습니다.

## 6. 그림 자료

교안은 프로젝트의 스크린샷을 상대 경로로 씁니다.

| 폴더 | 내용 |
|---|---|
| `Docs/images/` | 랩 화면 (비교·맵 스위치·디버그 보기·환경·애니메이션·커스텀) |
| `Docs/varco/` | VARCO 3D 컨셉 이미지 (갑옷·한복·마법사 로브) |

그림이 아직 없는 자리는 `[그림: …]`으로 표시했습니다. 강사가 수업 전에 캡처해 넣습니다.
