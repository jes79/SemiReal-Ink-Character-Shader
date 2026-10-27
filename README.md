# 세미리얼 먹(墨) 캐릭터 쉐이더 — Unity URP 쉐이더 랩

VARCO 3D 캐릭터에 입힌 **세미리얼 + 3포인트 조명 + 약한 먹 아웃라인** 쉐이더와,
그 쉐이더를 여러 조건에서 바로 바꿔 보며 공부하는 **쉐이더 랩(Shader Lab)** 장면입니다.

> **브랜치 안내**
> - `main`: 처음 공개한 쉐이더 예제 (원본 비교 뷰어 + 얼어붙은 세상 장면). 참고용으로 그대로 둡니다
> - `feature/shader-lab`: 이 문서가 설명하는 쉐이더 랩 (맵·기능 스위치, 스타일·환경·조명 프리셋, A/B 비교, UI Toolkit 패널)

![A 원본(URP Lit) / B 스타일라이즈드 와이프 비교](Docs/images/lab_compare_stylized.png)

| 맵 끄고 켜기 (베이스·노멀 끔) | 디버그 보기 (노멀) |
|---|---|
| ![맵 스위치](Docs/images/lab_maps_toggle.png) | ![노멀 보기](Docs/images/lab_debug_normal.png) |

| 환경 프리셋 (노을) | 디버그 보기 (이미시브) |
|---|---|
| ![노을](Docs/images/lab_environment_sunset.png) | ![이미시브](Docs/images/lab_debug_emission.png) |

---

## 1. 준비물

| 항목 | 버전 |
|---|---|
| Unity | **6000.6.7f1** (Unity 6) |
| 렌더 파이프라인 | URP 17.6 (Forward+) |
| 입력 | Input System 패키지 |
| UI | UI Toolkit (Unity 기본 내장) |
| Git LFS | **필수** — 모델(약 300MB)·텍스처를 LFS로 올려 두었습니다 |

## 2. 받는 방법

```bash
git lfs install          # 처음 한 번만
git clone https://github.com/jes79/SemiReal-Ink-Character-Shader.git
cd SemiReal-Ink-Character-Shader
git switch feature/shader-lab   # 쉐이더 랩 (main은 처음 공개한 예제)
```

> **주의:** GitHub의 "Download ZIP"은 LFS 파일(모델·텍스처)이 빠진 채로 받아집니다. 꼭 `git clone`으로 받으세요.
> 받은 뒤 텍스처가 1KB 정도의 텍스트 파일이면 `git lfs pull`을 실행하세요.

## 3. 실행

1. Unity Hub에서 프로젝트 폴더를 엽니다 (처음 열 때 Library를 만드느라 몇 분 걸립니다).
2. `Assets/CharacterShaderTest/Scenes/CharacterShaderTest.unity`를 엽니다.
3. ▶ 플레이를 누릅니다. 왼쪽에 **SHADER LAB** 패널이 나옵니다.

장면을 처음부터 다시 만들려면 메뉴 **Tools → Shader Test**를 씁니다.

| 메뉴 | 하는 일 |
|---|---|
| Build Test Scene | 장면을 다시 만듭니다. 머티리얼·후처리·프리셋에서 조정한 값은 그대로 둡니다 |
| Reset Materials And Post To Presets | 머티리얼·후처리·스타일·환경 프리셋을 기본값으로 되돌린 뒤 장면을 만듭니다 |

## 4. 쉐이더 랩 패널

| 탭 | 하는 일 |
|---|---|
| 비교 | 와이프 / 나란히 / A만 / B만. **A(왼쪽)·B(오른쪽)에 원본(URP Lit) 또는 스타일 프리셋을 골라** 비교 |
| 쉐이더 | B 쪽 스타일 프리셋(리얼·세미리얼·스타일라이즈드), **기능 끄고 켜기** (반사 하이라이트·환경 반사·보조광·역광·피부 SSS·천 광택·그늘 색·먹 아웃라인) |
| 맵·보기 | **텍스처 맵 끄고 켜기** (베이스·노멀·메탈릭·러프니스·AO·이미시브), **디버그 보기** (맵 하나만 / 빛만 / 반사광만 / 재질 판별) |
| 환경 | 얼어붙은 세상 · 중립 스튜디오 · 맑은 한낮 · 노을 · 달밤 (하늘·햇빛·환경광·안개·후처리·반사가 한 번에 바뀜) |
| 조명 | 환경 기본 · 정면광 · 45° 키 라이트 · 측면광 · 역광 · 탑라이트 · 3점 조명 + 포인트, 햇빛 방향 슬라이더 |
| 후처리 | 후처리 전체 끄기, 톤매핑(None·Neutral·ACES), 노출, 효과 하나씩 끄기 (블룸·비네트·필름 그레인 …) |
| 커스텀 | 옷 바꿔 입히기: 갑옷 · 한복 · 마법사 로브 (VARCO, A·B 캐릭터가 같이 바뀜) |

> 캐릭터는 처음에 **갑옷(VARCO)** 을 입고, 애니메이션 없이 기본 자세(T 포즈)로 서 있습니다.
> 애니메이션·무기·FinalPlayer(조선 스팀펑크) 캐릭터는 다음 **'캐릭터·애니메이션'** 파트에서 다룹니다 (모델·클립 파일은 프로젝트에 그대로 있음).

**스위치 규칙**: 맵·기능 스위치는 "끄기만" 합니다. 머티리얼이나 프리셋에서 원래 꺼져 있는 것은 켜도 생기지 않습니다
(예: 리얼 프리셋은 아웃라인이 없으므로 아웃라인 스위치를 켜도 그려지지 않음). 스위치는 A·B 두 쪽에 똑같이 걸립니다.

| 조작 | 기능 |
|---|---|
| 왼쪽 드래그 | 캐릭터 돌려 보기 |
| 휠 | 확대·축소 (확대할수록 시점이 얼굴 쪽으로 올라감) |
| 오른쪽 드래그 | 위아래 이동 |
| 가운데 선 드래그 | 와이프 비교 위치 |
| 1 / 2 / 3 / 4 | 와이프 / 나란히 / A만 / B만 |
| Tab | A ↔ B 빠르게 전환 |
| P | 후처리 켜기·끄기 |
| M | 재질 판별 보기 (노랑 = 금속, 흰색 = 은·거울, 초록 = 보석, 붉은 기 = 피부, 푸른 기 = 천). VARCO 옷은 메탈릭 맵을 써서 색 판별을 꺼 두었으므로 천(한복·로브)만 푸르게 보입니다 |
| O | 먹 아웃라인 켜기·끄기 |
| Space | 자동 회전 |
| F | 처음 시점 |
| H | 패널 접기·펴기 |

비교는 **A 캐릭터와 B 캐릭터를 같은 자리에 겹쳐 두고**, 레이어를 나눠 카메라 두 대로 그리는 방식입니다.
와이프 모드에서는 B 화면을 전체 크기로 따로 그린 뒤 오른쪽만 덮기 때문에, 후처리(비네트·필름 그레인)가 양쪽에 똑같이 걸립니다.
플레이 중에 바꾼 값은 실행할 때 만든 머티리얼 복사본에만 걸려서, 머티리얼·프로필 파일은 바뀌지 않습니다.

### 4-1. 강의 모드

플레이 중 **F4**를 누르면 강의 용어 목록이 열리고, 용어를 누르면 개발실 팀원이 단계별(분야 소개 → ① 소개 → ② 개념 → ③ 직접 보기 → ④ AI에게 요청)로 설명합니다.
용어로 들어갈 때마다 랩(탭·비교 방식·스타일·디버그 보기·환경·조명·옷)이 그 용어를 보기 좋게 자동으로 준비됩니다.

| 키 | 기능 |
|---|---|
| PgDn / ] · PgUp / [ | 다음 · 이전 단계 |
| End · Home | 다음 · 이전 용어 |
| F3 | 말풍선 숨기기·보이기 |
| Backspace | 용어 목록 (랩을 처음 상태로) |
| F4 | 강의 모드 끄기·켜기 |

- 용어 데이터: `Lecture/Resources/ShaderLabGlossary.json` (OMP Studio 앱의 강의 준비 창도 같은 파일을 읽음). 용어별 랩 준비는 각 용어의 `lab` 칸
- 강의 프로그램: **Tools → Shader Test → Build Lecture Player** → `Builds/LecturePlayer/ShaderLab.exe` (git에 올리지 않으므로 PC마다 한 번 빌드)
  - `ShaderLab.exe -glossaryStart 0304 -glossaryLecture` : 그 용어부터 강의 · 인자 없음 : 그냥 쉐이더 랩 · 강의 중이 아닐 때 Esc : 종료

## 5. 쉐이더 구조 — `ShaderTest/SemiRealInk`

### 표면 맵
| 칸 | 꺼져 있을 때 |
|---|---|
| Base Map | Base Color만 |
| Normal Map | 평평한 면 |
| Metallic Map (채널 R/G/B/A 선택) | `Metallic` 슬라이더 값 |
| Roughness Map (채널 선택, `Map Stores Smoothness`로 Unity식 스무스니스 맵도 읽음) | `Smoothness` 슬라이더 값 |
| Ambient Occlusion Map (채널 선택) | 가림 없음. 켜면 환경광·환경 반사만 가림 (URP Lit과 같은 규칙) |
| Emission Map | 빛나지 않음. 맵 칸을 비운 채 켜면 `Emission Color`로 전체가 빛남 |

ORM처럼 한 장에 여러 값을 담은 텍스처는 **같은 텍스처를 세 칸에 넣고 채널만 다르게** 고릅니다 (glTF ORM = R AO, G 러프니스, B 메탈릭).
장면 메뉴는 원본 URP Lit 머티리얼에 메탈릭(`_MetallicGlossMap`)·AO·이미시브 맵이 있으면 자동으로 옮겨 켭니다.

### 조명 (3포인트)
| 조명 | 출처 | 설명 |
|---|---|---|
| 키 | 장면의 디렉셔널 라이트 | 실사(람버트)와 부드러운 2단 명암을 `Realism` 슬라이더로 섞음. 그림자 포함 |
| 필 | 쉐이더 안 (카메라 기준 방향) | 카메라를 돌려도 항상 같은 쪽에서 비치는 보조광. 금속에는 두 번째 하이라이트도 만듦 |
| 림 | 쉐이더 안 (카메라 기준 뒤쪽) | 윤곽을 살리는 역광 |

그 밖에 환경광(SH)·반사 프로브·SSAO·추가 라이트(포인트·스팟)를 받습니다.

### 재질
| 재질 | 방식 |
|---|---|
| 피부 (`Material Type = Skin`) | 빨강이 더 깊이 퍼지는 랩 라이팅, 붉은 반그림자, 얇은 곳의 투과광, 확산광용 노멀 흐리게 |
| 천 (`Cloth`) | 가장자리 솜털 광택(sheen), 하이라이트 약하게. 금실 자수는 금속으로 판별된 곳만 반짝임 |
| 금속 | URP BRDF + 반사 프로브. `Metal Shine Boost`로 더 반짝이게, `Metal Color Saturation`으로 금빛 유지 |
| 보석·유리 | 반사율 높게, 정면에서 볼수록 속이 은은하게 빛남 (불투명) |

### 재질 판별: 마스크 맵 또는 자동 판별
- **마스크 맵** (`Use Mask Map` 켬): `*_Mask.png` (다음 파트에서 쓸 FinalPlayer용이 `Textures/FinalPlayer/Masks/`에 있음)
  - **R = 금속**(금·황동·구리·금실), **G = 보석·유리**, **B = 은·강철·거울**
  - 각 채널 세기는 `Metal / Gem / Silver Detect` 슬라이더로 조절
  - 마스크는 색이 아니라 데이터라서 sRGB를 끕니다 (`Editor/MaskTextureImporter.cs`가 `_Mask.png`를 자동 처리)
- **자동 판별** (마스크 없을 때): 베이스 텍스처 색을 색상·채도·밝기(HSV)로 보고 금·은·보석을 찾습니다. 베이스 맵을 꺼도 판별은 텍스처 원래 색으로 합니다.
- 메탈릭 맵과 판별 결과는 더해집니다 (둘 중 하나라도 금속이면 금속).

### 기능 스위치 (Features)
머티리얼 인스펙터의 체크박스 하나로 기능을 뺍니다: `Specular Highlight`, `Environment Reflection`, `Fill Light`, `Rim Light`, `Skin Subsurface`, `Cloth Sheen`, `Shadow Tint`, `Ink Outline`.
키워드가 아니라 값으로 처리해서 체크를 바꿔도 쉐이더 종류(배리언트)가 늘지 않습니다.

### 디버그 보기
전역 값 `_ShaderLabDebugView` 하나로 장면 전체의 새 쉐이더 머티리얼이 같은 보기로 바뀝니다.
1 베이스 컬러 · 2 노멀 · 3 메탈릭 · 4 러프니스 · 5 AO · 6 이미시브 · 7 빛만(회색 점토) · 8 반사광만 · 9 재질 판별

### 그 밖의 기능
- **양면(2-Side)**: `Render Face = Off`로 한 겹짜리 옷 안쪽도 그림. 안쪽은 노멀을 뒤집어 빛을 제대로 받고, `Back Face Color`로 안감 색 지정
- **먹 아웃라인**: 노멀 방향으로 부풀린 뒷면을 반투명 먹색으로 그림. 화면 픽셀 기준 굵기, 붓처럼 굵기·농도가 들쭉날쭉

## 6. 프리셋

### 스타일 프리셋 (`Settings/Styles`)
머티리얼마다 손으로 맞춘 값 위에 **덮어쓸 값만** 적은 에셋입니다. 체크하지 않은 값은 머티리얼 값을 그대로 씁니다.

| 프리셋 | 내용 |
|---|---|
| 리얼 | 실사 명암만. 보조광·역광·그늘 색·외곽선 없음 → URP Lit과 가장 비슷 |
| 세미리얼 | 아무것도 덮어쓰지 않음 = 부위별로 맞춘 기본값 |
| 스타일라이즈드 | 또렷한 2단 명암, 강한 역광 테두리, 굵은 먹 선, 반사 줄임 |

새 프리셋은 Project 창에서 **Create → Shader Lab → Style Preset**으로 만들고, 장면의 `Shader Lab` 오브젝트 `Styles` 목록에 넣습니다.

### 환경 프리셋 (`Settings/Environments`)
| 환경 | 특징 | 후처리 |
|---|---|---|
| 얼어붙은 세상 | 차가운 겨울 해, 눈 바닥·얼음판·눈발, 푸른 그늘 | Neutral, 차갑게, 블룸·비네트·그레인 |
| 중립 스튜디오 | 어두운 배경, 흰 빛 | Neutral만 → 쉐이더 자체 평가용 |
| 맑은 한낮 | 높은 해, 진한 그림자 | ACES, 대비·채도 약간 |
| 노을 | 낮은 주황빛 해, 보랏빛 그늘 | ACES, 따뜻하게, 블룸 |
| 달밤 | 푸른 달빛, 어두운 환경광 | Neutral, 채도 낮춤, 그레인 |

환경마다 반사 큐브맵(`*_Reflection.exr`)을 장면 메뉴가 구워 둡니다. 새 환경은 **Create → Shader Lab → Environment Preset**.

## 6-1. 테스트 캐릭터 — VARCO 3D

| 갑옷 | 한복 | 마법사 로브 |
|---|---|---|
| ![갑옷](Docs/varco/concept_armor.png) | ![한복](Docs/varco/concept_hanbok.png) | ![로브](Docs/varco/concept_magerobe.png) |

같은 얼굴·체형의 여성 캐릭터를 옷만 바꿔 3벌 만들었습니다. 무기 3개(검·활·지팡이)와 애니메이션 10개도 만들어 두었지만 쉐이더 랩에서는 쓰지 않고, 다음 '캐릭터·애니메이션' 파트에서 씁니다.

| 단계 | 도구 | 내용 |
|---|---|---|
| 1. 컨셉 이미지 | VARCO 3D Generate Image / Edit Image | 갑옷 캐릭터를 먼저 그리고, 그 그림을 고쳐 한복·로브를 만듦 (얼굴·체형·T 포즈 유지) |
| 2. 3D 모델 | VARCO 3D Generate 3D | T 포즈, PBR 텍스처(베이스·ORM·노멀) 2048, 캐릭터 4만·무기 1.2만 폴리곤 |
| 3. 뼈대 | VARCO 3D Rig (humanoid) | 세 벌 모두 같은 뼈 이름(Hips·Spine·LeftArm …) → Unity Humanoid로 자동 매핑 |
| 4. 애니메이션 | VARCO 3D Animate | 대기·걷기·달리기·점프·검 베기·연속 베기·활 쏘기·지팡이 돌리기·손 마법·피격 (10개) |
| 5. 변환 | Blender (배치 스크립트) | glb → FBX, 텍스처 PNG로 꺼내기, 이미시브 맵(룬 문자·수정) 만들기, URP Lit용 메탈릭 맵 만들기 |
| 6. 가져오기 | `Editor/VarcoAssetImporter.cs` | Humanoid, 키 1.7m, ORM·메탈릭 맵 sRGB 끔 |

- **ORM 한 장**: 새 쉐이더는 ORM 텍스처 한 장을 메탈릭(B)·러프니스(G)·AO(R) 세 칸에 채널만 바꿔 넣어 씁니다. 원본(URP Lit) 쪽은 URP 규칙(R 메탈릭, A 스무스니스)에 맞춘 텍스처를 씁니다.

## 7. 폴더 구조

```
Assets/CharacterShaderTest/
├─ Shaders/SemiRealInk.shader          쉐이더 본체
├─ Scripts/
│  ├─ ShaderLabController.cs           쉐이더 랩 상태 관리 (A/B·스위치·환경·조명·후처리·커스텀)
│  ├─ ShaderLabUI.cs                   UI Toolkit 패널 (탭 내용 만들기·단축키·와이프 덮개)
│  ├─ LabCharacter.cs                  캐릭터: 원본/새 쉐이더 머티리얼, 커스텀(옷) 칸
│  ├─ StylePreset.cs                   스타일 프리셋 에셋 + 맵·기능 스위치 이름
│  ├─ EnvironmentPreset.cs             환경 프리셋 에셋
│  ├─ OrbitCamera.cs                   둘러보기 카메라
│  └─ CompareView.cs                   A/B 카메라 두 대 비교 화면
├─ Lecture/                            강의 모드: Scripts/(진행·화면·랩 준비·명령줄), Editor/(강의 프로그램 빌드), Resources/(용어 JSON·팀원 그림)
├─ UI/ShaderLab.uxml · ShaderLab.uss   패널 구조 · 색과 모양
├─ Editor/SemiRealInkTestSetup.cs      장면·머티리얼·프리셋·후처리 생성 메뉴
├─ Editor/MaskTextureImporter.cs       *_Mask.png 자동 데이터 텍스처 처리 (다음 파트의 FinalPlayer 마스크용)
├─ Editor/VarcoAssetImporter.cs        VARCO 모델·텍스처·클립 가져오기 규칙 (Humanoid·크기·sRGB)
├─ Materials/Environment/              하늘·바닥·눈송이
├─ Models/Varco/                       VARCO 3D 캐릭터 3벌 (랩에서 씀) · 무기 3개 · Animations/ 클립 10개 (다음 파트용) (Git LFS)
├─ Textures/Varco/                     VARCO 텍스처: BaseColor·ORM·Normal·Emission·MetallicSmoothness
├─ Materials/Varco/ · Varco_SemiRealInk/  VARCO 원본(URP Lit) · 새 쉐이더 머티리얼
├─ Models/FinalPlayer.fbx · Materials/FinalPlayer*/ · Textures/FinalPlayer/   조선 스팀펑크 캐릭터 (랩에서는 안 씀, 다음 파트용, Git LFS)
├─ Settings/                           후처리 프로필, Styles/ 스타일 프리셋, Environments/ 환경 프리셋·반사
└─ Scenes/CharacterShaderTest.unity    쉐이더 랩 장면
```

## 8. 알려진 한계

- 보석·유리 판별은 진짜 투명 유리가 아니라 반짝이는 불투명 유리로 그립니다.
- 나란히 보기에서는 후처리가 화면 반쪽마다 따로 걸립니다 (와이프 모드는 같게 걸림).
- 피부 SSS는 실시간용 근사치입니다 (두께 맵 없이 투과광을 계산).
- VARCO 캐릭터는 몸·옷·얼굴이 머티리얼 하나라서 피부 SSS를 쓰지 않습니다 (한복·로브는 천 타입). 그래서 쉐이더 탭의 '피부 SSS' 체크는 지금 캐릭터에서 변화가 없습니다.
- 맵·기능 스위치와 디버그 보기는 새 쉐이더 쪽에만 걸립니다. A를 원본(URP Lit)으로 두면 A는 그대로입니다.
- UI 한글은 컴퓨터에 깔린 한글 글꼴(맑은 고딕 등)로 그립니다. 한글 글꼴이 없는 컴퓨터에서는 네모로 보일 수 있습니다.

## 9. 라이선스

- **쉐이더·스크립트**: 학습·참고용으로 자유롭게 쓸 수 있습니다.
- **캐릭터 모델·텍스처**: 이 예제를 실행하고 공부하는 용도로만 쓰세요. 다른 곳에 다시 배포하지 마세요.
