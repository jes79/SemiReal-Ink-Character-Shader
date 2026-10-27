# 세미리얼 먹(墨) 캐릭터 쉐이더 — Unity URP 쉐이더 랩

조선 스팀펑크 캐릭터에 입힌 **세미리얼 + 3포인트 조명 + 약한 먹 아웃라인** 쉐이더와,
그 쉐이더를 여러 조건에서 바로 바꿔 보며 공부하는 **쉐이더 랩(Shader Lab)** 장면입니다.

> **브랜치 안내**
> - `main`: 처음 공개한 쉐이더 예제 (원본 비교 뷰어 + 얼어붙은 세상 장면). 참고용으로 그대로 둡니다
> - `feature/shader-lab`: 이 문서가 설명하는 쉐이더 랩 (맵·기능 스위치, 스타일·환경·조명 프리셋, A/B 비교, UI Toolkit 패널)

![A 원본(URP Lit) / B 스타일라이즈드 와이프 비교](Docs/images/lab_compare_stylized.png)

| 맵 끄고 켜기 (베이스·노멀 끔) | 디버그 보기 (노멀) |
|---|---|
| ![맵 스위치](Docs/images/lab_maps_toggle.png) | ![노멀 보기](Docs/images/lab_debug_normal.png) |

| 환경 프리셋 (노을) | 재질 판별 보기 |
|---|---|
| ![노을](Docs/images/lab_environment_sunset.png) | ![재질 판별](Docs/images/material_masks.png) |

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
| 애니메이션 | 클립 재생·일시정지·1프레임 이동·시간 이동·속도·반복·루트 모션 (A·B 캐릭터가 같이 움직임) |
| 커스텀 | 옷·무기 등 바꿔 끼우기 (A·B 캐릭터가 같이 바뀜) |

> 애니메이션·커스텀 탭은 캐릭터의 `LabCharacter`에 클립·후보가 있을 때 내용이 나옵니다. 지금 들어 있는 FinalPlayer 모델에는 둘 다 없습니다.

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
| M | 재질 판별 보기 (노랑 = 금속, 흰색 = 은·거울, 초록 = 보석, 붉은 기 = 피부, 푸른 기 = 천) |
| O | 먹 아웃라인 켜기·끄기 |
| Space | 자동 회전 |
| F | 처음 시점 |
| H | 패널 접기·펴기 |

비교는 **A 캐릭터와 B 캐릭터를 같은 자리에 겹쳐 두고**, 레이어를 나눠 카메라 두 대로 그리는 방식입니다.
와이프 모드에서는 B 화면을 전체 크기로 따로 그린 뒤 오른쪽만 덮기 때문에, 후처리(비네트·필름 그레인)가 양쪽에 똑같이 걸립니다.
플레이 중에 바꾼 값은 실행할 때 만든 머티리얼 복사본에만 걸려서, 머티리얼·프로필 파일은 바뀌지 않습니다.

## 5. 쉐이더 구조 — `ShaderTest/SemiRealInk`

### 표면 맵
| 칸 | 꺼져 있을 때 |
|---|---|
| Base Map | Base Color만 |
| Normal Map | 평평한 면 |
| Metallic Map (채널 R/G/B/A 선택) | `Metallic` 슬라이더 값 |
| Roughness Map (채널 선택, `Map Stores Smoothness`로 Unity식 스무스니스 맵도 읽음) | `Smoothness` 슬라이더 값 |
| Ambient Occlusion Map (채널 선택) | 가림 없음. 켜면 환경광·환경 반사만 가림 (URP Lit과 같은 규칙) |
| Emission Map | `Emission Color`만 |

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
- **마스크 맵** (`Use Mask Map` 켬): `Textures/FinalPlayer/Masks/*_Mask.png`
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

## 7. 폴더 구조

```
Assets/CharacterShaderTest/
├─ Shaders/SemiRealInk.shader          쉐이더 본체
├─ Scripts/
│  ├─ ShaderLabController.cs           쉐이더 랩 상태 관리 (A/B·스위치·환경·조명·후처리·애니메이션·커스텀)
│  ├─ ShaderLabUI.cs                   UI Toolkit 패널 (탭 내용 만들기·단축키·와이프 덮개)
│  ├─ LabCharacter.cs                  캐릭터: 원본/새 쉐이더 머티리얼, 커스텀 칸, 애니메이션 클립
│  ├─ StylePreset.cs                   스타일 프리셋 에셋 + 맵·기능 스위치 이름
│  ├─ EnvironmentPreset.cs             환경 프리셋 에셋
│  ├─ AnimationTester.cs               Playable로 클립 재생·스크럽
│  ├─ OrbitCamera.cs                   둘러보기 카메라
│  └─ CompareView.cs                   A/B 카메라 두 대 비교 화면
├─ UI/ShaderLab.uxml · ShaderLab.uss   패널 구조 · 색과 모양
├─ Editor/SemiRealInkTestSetup.cs      장면·머티리얼·프리셋·후처리 생성 메뉴, 부위별 프리셋
├─ Editor/MaskTextureImporter.cs       *_Mask.png 자동 데이터 텍스처 처리
├─ Materials/FinalPlayer/              원본 머티리얼 (URP Lit)
├─ Materials/FinalPlayer_SemiRealInk/  새 쉐이더 머티리얼
├─ Materials/Environment/              하늘·바닥·눈송이
├─ Models/FinalPlayer.fbx              캐릭터 (Git LFS)
├─ Textures/FinalPlayer/               베이스·노멀 텍스처, Masks/ 마스크 맵 (Git LFS)
├─ Settings/                           후처리 프로필, Styles/ 스타일 프리셋, Environments/ 환경 프리셋·반사
└─ Scenes/CharacterShaderTest.unity    쉐이더 랩 장면
```

## 8. 알려진 한계

- 고글 렌즈·보석은 가죽·금속과 같은 텍스처·메쉬에 섞여 있어 **진짜 투명 유리는 아닙니다** (반짝이는 불투명 유리로 표현).
- 나란히 보기에서는 후처리가 화면 반쪽마다 따로 걸립니다 (와이프 모드는 같게 걸림).
- 피부 SSS는 실시간용 근사치입니다 (두께 맵 없이 투과광을 계산).
- FinalPlayer 모델에는 메탈릭·러프니스·AO 맵이 없어서, 그 스위치는 이 모델에서 변화가 없습니다 (금속은 마스크 맵으로 판별).
- 맵·기능 스위치와 디버그 보기는 새 쉐이더 쪽에만 걸립니다. A를 원본(URP Lit)으로 두면 A는 그대로입니다.
- UI 한글은 컴퓨터에 깔린 한글 글꼴(맑은 고딕 등)로 그립니다. 한글 글꼴이 없는 컴퓨터에서는 네모로 보일 수 있습니다.

## 9. 라이선스

- **쉐이더·스크립트**: 학습·참고용으로 자유롭게 쓸 수 있습니다.
- **캐릭터 모델·텍스처**: 이 예제를 실행하고 공부하는 용도로만 쓰세요. 다른 곳에 다시 배포하지 마세요.
