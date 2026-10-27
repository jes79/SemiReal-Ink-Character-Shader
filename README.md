# 세미리얼 먹(墨) 캐릭터 쉐이더 — Unity URP 예제

조선 스팀펑크 캐릭터에 입힌 **세미리얼 + 3포인트 조명 + 약한 먹 아웃라인** 쉐이더입니다.
피부(SSS)·천·금속·보석이 한 텍스처 안에 섞여 있어도 **픽셀 단위로 재질을 나눠** 그리고,
"얼어붙은 세상" 분위기의 장면과 **원본(URP Lit)과 바로 비교하는 뷰어**가 함께 들어 있습니다.

![원본과 새 쉐이더 와이프 비교](Docs/images/overview_wipe.png)

| 금장 자수 (왼쪽 원본 / 오른쪽 새 쉐이더) | 재질 판별 보기 |
|---|---|
| ![금장 자수](Docs/images/gold_embroidery_wipe.png) | ![재질 판별](Docs/images/material_masks.png) |

| 얼굴 (피부 SSS) | 나란히 보기 |
|---|---|
| ![얼굴](Docs/images/face_wipe.png) | ![나란히](Docs/images/side_by_side.png) |

---

## 1. 준비물

| 항목 | 버전 |
|---|---|
| Unity | **6000.6.7f1** (Unity 6) |
| 렌더 파이프라인 | URP 17.6 (Forward+) |
| 입력 | Input System 패키지 |
| Git LFS | **필수** — 모델(약 300MB)·텍스처를 LFS로 올려 두었습니다 |

## 2. 받는 방법

```bash
git lfs install          # 처음 한 번만
git clone https://github.com/jes79/SemiReal-Ink-Character-Shader.git
```

> **주의:** GitHub의 "Download ZIP"은 LFS 파일(모델·텍스처)이 빠진 채로 받아집니다. 꼭 `git clone`으로 받으세요.
> 받은 뒤 텍스처가 1KB 정도의 텍스트 파일이면 `git lfs pull`을 실행하세요.

## 3. 실행

1. Unity Hub에서 프로젝트 폴더를 엽니다 (처음 열 때 Library를 만드느라 몇 분 걸립니다).
2. `Assets/CharacterShaderTest/Scenes/CharacterShaderTest.unity`를 엽니다.
3. ▶ 플레이를 누릅니다.

장면을 처음부터 다시 만들려면 메뉴 **Tools → Shader Test**를 씁니다.

| 메뉴 | 하는 일 |
|---|---|
| Build Test Scene | 장면을 다시 만듭니다. 머티리얼·후처리에서 조정한 값은 그대로 둡니다 |
| Reset Materials And Post To Presets | 머티리얼·후처리를 기본 프리셋으로 되돌린 뒤 장면을 만듭니다 |

## 4. 뷰어 조작

| 조작 | 기능 |
|---|---|
| 왼쪽 드래그 | 캐릭터 돌려 보기 |
| 휠 | 확대·축소 (확대할수록 시점이 얼굴 쪽으로 올라감) |
| 오른쪽 드래그 | 위아래 이동 |
| 가운데 선 드래그 | 와이프 비교 위치 |
| 1 / 2 / 3 / 4 | 와이프 / 나란히 / 원본만 / 새 쉐이더만 |
| Tab | 원본 ↔ 새 쉐이더 빠르게 전환 |
| P | 후처리 켜기·끄기 |
| M | 재질 판별 보기 (노랑 = 금속, 흰색 = 은·거울, 초록 = 보석, 붉은 기 = 피부, 푸른 기 = 천) |
| O | 먹 아웃라인 켜기·끄기 |
| Space | 자동 회전 |
| F | 처음 시점 |
| H | 조작 패널 접기·펴기 |
| 패널의 햇빛 방향 슬라이더 | 키 라이트를 돌려 SSS·금속 반사가 어떻게 바뀌는지 확인 |

비교는 **원본 캐릭터와 새 쉐이더 캐릭터를 같은 자리에 겹쳐 두고**, 레이어를 나눠 카메라 두 대로 그리는 방식입니다.
와이프 모드에서는 새 쉐이더 화면을 전체 크기로 따로 그린 뒤 오른쪽만 덮기 때문에, 후처리(비네트·필름 그레인)가 양쪽에 똑같이 걸립니다.

## 5. 쉐이더 구조 — `ShaderTest/SemiRealInk`

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
- **자동 판별** (마스크 없을 때): 베이스 텍스처 색을 색상·채도·밝기(HSV)로 보고 금·은·보석을 찾습니다. 슬라이더로 색 범위를 조절할 수 있습니다.

### 그 밖의 기능
- **양면(2-Side)**: `Render Face = Off`로 한 겹짜리 옷 안쪽도 그림. 안쪽은 노멀을 뒤집어 빛을 제대로 받고, `Back Face Color`로 안감 색 지정
- **먹 아웃라인**: 노멀 방향으로 부풀린 뒷면을 반투명 먹색으로 그림. 화면 픽셀 기준 굵기, 붓처럼 굵기·농도가 들쭉날쭉

## 6. 장면 — 얼어붙은 세상

| 요소 | 내용 |
|---|---|
| 하늘·해 | 옅고 차가운 하늘, 낮게 뜬 차가운 겨울 해 |
| 바닥 | 눈 바닥 + 발밑 얼음판, 반사 프로브에 구워 금속·보석에 비침 |
| 대기 | 차가운 안개, 내리는 눈 (파티클) |
| 후처리 | 화이트 밸런스(차갑게)·채도 약간 낮춤·어두운 곳 푸르게·블룸·비네트·필름 그레인 |

피부의 붉은 혈색(SSS)만 따뜻하게 남겨, 차가운 배경 속에서 사람이 살아 보이게 했습니다.

## 7. 폴더 구조

```
Assets/CharacterShaderTest/
├─ Shaders/SemiRealInk.shader          쉐이더 본체
├─ Scripts/CharacterCompareViewer.cs   비교·살펴보기 뷰어
├─ Editor/SemiRealInkTestSetup.cs      장면·머티리얼·후처리 생성 메뉴, 부위별 프리셋
├─ Editor/MaskTextureImporter.cs       *_Mask.png 자동 데이터 텍스처 처리
├─ Materials/FinalPlayer/              원본 머티리얼 (URP Lit)
├─ Materials/FinalPlayer_SemiRealInk/  새 쉐이더 머티리얼
├─ Materials/Environment/              하늘·눈·얼음·눈송이
├─ Models/FinalPlayer.fbx              캐릭터 (Git LFS)
├─ Textures/FinalPlayer/               베이스·노멀 텍스처, Masks/ 마스크 맵 (Git LFS)
├─ Settings/                           후처리 프로필, 구운 반사 프로브
└─ Scenes/CharacterShaderTest.unity    비교 장면
```

## 8. 알려진 한계

- 고글 렌즈·보석은 가죽·금속과 같은 텍스처·메쉬에 섞여 있어 **진짜 투명 유리는 아닙니다** (반짝이는 불투명 유리로 표현).
- 나란히 보기에서는 후처리가 화면 반쪽마다 따로 걸립니다 (와이프 모드는 같게 걸림).
- 피부 SSS는 실시간용 근사치입니다 (두께 맵 없이 투과광을 계산).

## 9. 라이선스

- **쉐이더·스크립트**: 학습·참고용으로 자유롭게 쓸 수 있습니다.
- **캐릭터 모델·텍스처**: 이 예제를 실행하고 공부하는 용도로만 쓰세요. 다른 곳에 다시 배포하지 마세요.
