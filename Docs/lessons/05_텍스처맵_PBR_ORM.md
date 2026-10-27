# 5차시. 텍스처 맵과 PBR, ORM 채널 패킹

| 항목 | 내용 |
|---|---|
| 수업 시간 | 180분 (시연 25 · 개념 45 · 쉬는 시간 10 · 실습 70 · 정리·과제 20 · 도입 10) |
| 난이도 | ★★☆ (값이 "색"인지 "데이터"인지 구분하는 날) |
| 시작 저장 지점 | `shader-lab-v2` |
| 쓰는 랩 탭 | **맵·보기**, **커스텀** (옷·무기 바꾸기). 보조로 **비교**(B만 보기), **환경**(중립 스튜디오) |

## 학습 목표

1. 베이스·노멀·메탈릭·러프니스·AO·이미시브 맵이 화면에 각각 무엇을 더하는지 랩에서 끄고 켜며 말할 수 있다.
2. 색 텍스처(sRGB)와 데이터 텍스처(선형)를 구분하고, 임포트 설정이 틀리면 무엇이 망가지는지 설명한다.
3. ORM 한 장을 채널만 골라 세 칸에 넣는 방식(채널 패킹)을 이해하고, URP Lit식 MetallicSmoothness 텍스처로 바꿔 끼울 수 있다.

## 시작 저장 지점

오늘부터 `shader-lab-v2`(VARCO 캐릭터 3벌·무기 3개가 들어간 최종 지점)에서 시작합니다. **Unity를 닫고** 실행합니다.

```bash
git status                              # 바뀐 파일이 있으면 먼저 commit 하거나 git stash
git switch -c lesson05 shader-lab-v2    # 태그 shader-lab-v2 에서 'lesson05' 브랜치를 새로 만들어 이동
git lfs pull                            # VARCO 모델·텍스처 실제 파일 받기
```

> v1에서 v2로 처음 바꾸면 Unity가 VARCO 모델(약 300MB 중 대부분)을 새로 가져옵니다. 수업 전에 한 번 열어 두면 좋습니다.

---

## 시연 순서

시연 내내 **비교** 탭 → **B만 (4)** 로 두면 새 쉐이더 쪽만 크게 보입니다. 맵 스위치와 디버그 보기는 새 쉐이더(B)에만 걸리기 때문입니다.
**환경** 탭에서 **중립 스튜디오**를 고르면 색이 덜 비틀려 맵을 보기 좋습니다.

### A. 맵을 하나씩 꺼 보기 — 갑옷 (8분)

1. `CharacterShaderTest.unity` ▶ 플레이. 캐릭터는 처음에 **갑옷 (VARCO)** 을 입고 있다.
2. **맵·보기** 탭 → "텍스처 맵 켜고 끄기"의 체크를 위에서부터 하나씩 끄고, 다시 켠다.

| 끄는 체크 | 보여 줄 것 |
|---|---|
| 베이스 컬러 (디퓨즈) | 색이 다 빠지고 흰 석고처럼 된다 (Base Color만 남음) |
| 노멀 맵 | 갑옷 판의 홈·주름이 평평해진다 |
| 메탈릭 맵 | 금속 판이 반사를 잃고 플라스틱처럼 된다 |
| 러프니스 맵 | 반짝임이 판 전체에 고르게 퍼진다 (슬라이더 값 하나만 남음) |
| 앰비언트 오클루전 (AO) | 겹친 판 사이·겨드랑이의 어두운 틈이 옅어진다 |
| 이미시브 맵 | 갑옷에는 이미시브 맵이 없어 **변화 없음** → "없는 맵은 켜도 안 생긴다" |

### B. 디버그 보기 + 옷 바꾸기 (10분)

3. "디버그 보기"에서 **베이스 컬러만 → 노멀 (방향 색) → 메탈릭 (흰색 = 금속) → 러프니스 (흰색 = 거침) → AO (검을수록 가려짐)** 를 차례로 누른다. "화면 한 장이 이런 재료 다섯 장으로 만들어진다"를 강조.
4. **커스텀** 탭 → "옷"에서 **마법사 로브 (VARCO)** → 다시 **맵·보기** → 디버그 **이미시브**. 룬 문자·수정만 빛난다.
5. **이미시브 맵** 체크를 끄면 빛이 꺼진다. 다시 켜고 **보통 화면**.
6. **커스텀** → "무기" → **지팡이**. 지팡이의 수정도 이미시브로 빛난다.
7. "옷" → **조선 스팀펑크 (FinalPlayer)** → **메탈릭 맵** 체크를 끈다. **변화 없음.** 이 모델에는 메탈릭·러프니스·AO 맵이 없다(README 알려진 한계). 그래서 오늘 수업은 VARCO 옷으로 한다.
8. "옷" → **갑옷 (VARCO)** 로 되돌린다.

![마법사 로브 — A 원본(URP Lit) / B 이미시브 보기](../images/lab_debug_emission.png)

### C. 텍스처 파일과 머티리얼 들여다보기 (7분)

9. 플레이를 멈춘다. Project 창 `Assets/CharacterShaderTest/Textures/Varco/` 를 연다. 옷마다 `_BaseColor`, `_Normal`, `_ORM`, `_MetallicSmoothness` (+ 로브·지팡이는 `_Emission`)가 있다.
10. `Armor_BaseColor.png` 와 `Armor_ORM.png` 를 번갈아 선택해 인스펙터의 **sRGB (Color Texture)** 체크를 비교한다. BaseColor는 켜짐, ORM은 꺼짐.
11. `Armor_ORM.png` 미리 보기에서 채널(R·G·B)을 하나씩 본다. "색처럼 보이지만 그림이 아니라 숫자 세 장".
12. `Assets/CharacterShaderTest/Materials/Varco_SemiRealInk/Armor_Lit.mat` 선택 → **PBR Maps** 머리글. **같은 `Armor_ORM`이 세 칸에** 들어 있고 채널만 B / G / R 로 다르다.

[그림: Armor_Lit.mat(새 쉐이더) 인스펙터 PBR Maps 부분. Metallic Map·Roughness Map·Ambient Occlusion Map 세 칸에 같은 Armor_ORM 썸네일, 옆 채널 드롭다운 B / G / R 에 빨간 동그라미]

---

## 개념 설명

### 1. 용어

| 용어 | 한 줄 풀이 | 이 프로젝트에서 |
|---|---|---|
| PBR (물리 기반 렌더링) | 빛이 실제 물체에 닿는 규칙을 흉내 내서 색을 계산하는 방식 | URP Lit, 새 쉐이더의 `InitializeBRDFData` |
| 텍스처 맵 | 모델 표면의 점마다 값 하나(또는 여러 개)를 적어 둔 그림 파일 | `Textures/Varco/*.png` |
| 채널 | 그림 한 픽셀이 가진 R·G·B·A 네 칸 숫자 중 하나 | ORM은 R·G·B에 서로 다른 값 |
| 메탈릭 (Metallic) | 금속인가(1) 아닌가(0) | ORM의 **B** |
| 러프니스 (Roughness) | 표면이 거친 정도. 1 = 거칠어 빛이 퍼짐, 0 = 매끈해 또렷이 반사 | ORM의 **G** |
| 스무스니스 (Smoothness) | 러프니스를 뒤집은 값 (= 1 − 러프니스). Unity가 주로 쓰는 말 | MetallicSmoothness의 **A** |
| AO (앰비언트 오클루전) | 틈·접힌 곳처럼 주변 빛이 덜 드는 정도. 1 = 다 받음, 0 = 다 가려짐 | ORM의 **R** |
| 이미시브 (Emission) | 빛을 받지 않아도 스스로 내는 빛 | `MageRobe_Emission`, `Staff_Emission` |
| sRGB | 사람 눈에 맞춰 어두운 쪽을 더 촘촘하게 저장한 색 표기 방식 | 베이스·이미시브 텍스처 |
| 선형 (Linear) | 숫자를 있는 그대로 저장. 계산용 데이터는 이렇게 둔다 | ORM·메탈릭·노멀 텍스처 |
| 채널 패킹 | 흑백 맵 여러 장을 한 장의 R·G·B·A에 나눠 담는 것 | ORM, MetallicSmoothness |
| HDR 색 | 1보다 밝은 값을 가질 수 있는 색 | `_EmissionColor` (`[HDR]`) |

### 2. 맵 여섯 개 한눈에

| 맵 | 담긴 것 | 색 / 데이터 | 쉐이더 칸 (줄) | 끄면 | 디버그 보기 |
|---|---|---|---|---|---|
| 베이스 | 물체 고유의 색 | **색** (sRGB 켬) | `_BaseMap` (18), 체크 `_UseBaseMap` (20) | Base Color만 | 베이스 컬러만 |
| 노멀 | 표면의 작은 기울기 (요철) | 데이터 (Normal map) | `_BumpMap` (21), `_UseNormalMap` (23) | 평평한 면 | 노멀 (방향 색) |
| 메탈릭 | 금속 여부 | 데이터 | `_MetallicMap` (31), `_MetallicChannel` (32), `_UseMetallicMap` (33) | `Metallic (No Map)` 슬라이더 (30) | 메탈릭 (흰색 = 금속) |
| 러프니스 | 거친 정도 | 데이터 | `_RoughnessMap` (34), `_RoughnessChannel` (35), `_RoughnessIsSmoothness` (36), `_UseRoughnessMap` (37) | `Smoothness (No Map)` 슬라이더 (24) | 러프니스 (흰색 = 거침) |
| AO | 틈의 가림 | 데이터 | `_OcclusionMap` (38), `_OcclusionChannel` (39), `_OcclusionStrength` (40), `_UseOcclusionMap` (41) | 가림 없음 | AO (검을수록 가려짐) |
| 이미시브 | 스스로 빛나는 곳의 색 | **색** (sRGB 켬) | `_EmissionMap` (42), `[HDR] _EmissionColor` (43), `_UseEmissionMap` (44) | 빛 꺼짐 | 이미시브 |

줄 번호는 `Assets/CharacterShaderTest/Shaders/SemiRealInk.shader` (v2) 기준. 찾을 글자: `[Header(PBR Maps)]` (29행).

```hlsl
// SemiRealInk.shader 30–37행
_Metallic ("Metallic (No Map)", Range(0, 1)) = 0
[NoScaleOffset] _MetallicMap ("Metallic Map", 2D) = "black" {}
[Enum(R, 0, G, 1, B, 2, A, 3)] _MetallicChannel ("Metallic Channel", Float) = 0
[ToggleUI] _UseMetallicMap ("Use Metallic Map", Float) = 0
[NoScaleOffset] _RoughnessMap ("Roughness Map", 2D) = "white" {}
[Enum(R, 0, G, 1, B, 2, A, 3)] _RoughnessChannel ("Roughness Channel", Float) = 0
[ToggleUI] _RoughnessIsSmoothness ("Map Stores Smoothness (Unity Style)", Float) = 0
[ToggleUI] _UseRoughnessMap ("Use Roughness Map", Float) = 0
```

| 부분 | 뜻 |
|---|---|
| `[Enum(R, 0, G, 1, B, 2, A, 3)]` | 인스펙터에 R/G/B/A 드롭다운. 고르면 숫자 0~3이 저장됨 |
| `[NoScaleOffset]` | 이 칸은 Tiling·Offset 칸을 숨긴다. 모든 맵이 **Base Map의 Tiling**을 같이 쓴다 (329행 `TRANSFORM_TEX(input.uv, _BaseMap)`) |
| `[Normal]` (21행) | 노멀 맵이 아닌 텍스처를 넣으면 인스펙터가 경고한다 |
| `= "black"` / `"white"` / `"bump"` | 칸이 비었을 때 쓰는 기본 텍스처 |

![맵 끄고 켜기 — 베이스·노멀을 끈 모습 (v1 FinalPlayer 화면)](../images/lab_maps_toggle.png)

### 3. sRGB vs 선형 — "색"과 "숫자"는 다르게 가져온다

이 프로젝트는 **Linear 색 공간**입니다 (`ProjectSettings/ProjectSettings.asset`의 `m_ActiveColorSpace: 1`). 쉐이더는 모든 계산을 선형 숫자로 합니다.

| 텍스처 종류 | 임포트 설정 | GPU가 읽을 때 | 왜 |
|---|---|---|---|
| 색 (베이스·이미시브) | **sRGB (Color Texture) 켬** | sRGB → 선형으로 바꿔 줌 | 사람이 그린 색은 sRGB로 저장돼 있다 |
| 데이터 (ORM·MetallicSmoothness·마스크) | **sRGB 끔** | 그대로 읽음 | 0.5는 "반쯤 거침"이라는 **숫자**. 바꾸면 안 된다 |
| 노멀 | **Texture Type = Normal map** | 노멀용으로 풀어 읽음 (`UnpackNormalScale`, 255행) | 방향 벡터라서 색 변환·일반 압축을 쓰면 안 된다 |

**sRGB를 잘못 켜면**: 데이터 0.5가 약 **0.21**로 읽힙니다. 중간값이 모두 어두워집니다.

| 잘못 켠 맵 | 결과 |
|---|---|
| 러프니스 (G) | 거칠기가 낮게 읽힘 → 실제보다 **반짝반짝** |
| AO (R) | 가림이 진하게 읽힘 → 틈이 **더 시커멓게** |
| 메탈릭 (B) | 0과 1은 그대로, 그 사이 값만 줄어듦 |

이 규칙을 사람이 매번 지키지 않도록 **가져오기 규칙(AssetPostprocessor)** 이 자동으로 처리합니다.

> **AssetPostprocessor**: 에셋을 Unity로 가져올(import) 때마다 끼어들어 설정을 바꾸는 에디터 스크립트.

```csharp
// Editor/VarcoAssetImporter.cs 63–72행 (찾을 글자: OnPreprocessTexture)
void OnPreprocessTexture()
{
    if (!assetPath.StartsWith(TextureDir))
        return;
    var importer = (TextureImporter)assetImporter;
    if (assetPath.EndsWith("_Normal.png"))
        importer.textureType = TextureImporterType.NormalMap;
    else if (assetPath.EndsWith("_ORM.png") || assetPath.EndsWith("_MetallicSmoothness.png"))
        importer.sRGBTexture = false;
}
```

| 줄 | 하는 일 |
|---|---|
| 65 | `Textures/Varco/` 안 파일만 (`TextureDir`, 10행) |
| 68–69 | 이름이 `_Normal.png`로 끝나면 Normal map |
| 70–71 | `_ORM.png`, `_MetallicSmoothness.png`로 끝나면 sRGB 끔 |
| (그 밖) | `_BaseColor`, `_Emission`은 손대지 않음 → 기본값 sRGB 켬 |

**핵심: 규칙은 "파일 이름 끝"을 본다.** 이름 규칙을 어기면 데이터 텍스처가 sRGB 켠 채로 들어온다 (실습 4에서 일부러 재현).

> 반대 방향 예: 쉐이더 359행 `RgbToHsv(LinearToSRGB(baseLinear))` — 자동 재질 판별은 사람이 보는 색 기준으로 해야 해서, 선형으로 읽은 베이스 색을 **다시 sRGB로** 바꿔서 색상·채도를 잰다.

### 4. 쉐이더가 맵을 읽는 코드

**(1) 선언 — 147–153행, 머티리얼 값 — 160–179행**

```hlsl
TEXTURE2D(_MetallicMap);    SAMPLER(sampler_MetallicMap);     // 150
TEXTURE2D(_RoughnessMap);   SAMPLER(sampler_RoughnessMap);    // 151
TEXTURE2D(_OcclusionMap);   SAMPLER(sampler_OcclusionMap);    // 152
TEXTURE2D(_EmissionMap);    SAMPLER(sampler_EmissionMap);     // 153
```

채널 번호·체크 값(`_MetallicChannel`, `_UseMetallicMap` …)은 `CBUFFER_START(UnityPerMaterial)`(160행) 안에 있습니다. 159행 주석대로, 모든 패스가 같은 버퍼를 써야 **SRP Batcher**가 동작합니다.

> **SRP Batcher**: 같은 쉐이더를 쓰는 머티리얼들을 빠르게 몰아 그리는 URP 기능. Properties에 칸을 더하면 CBUFFER에도 같은 이름을 넣어야 한다.

**(2) 채널 고르기 — 237–241행** (찾을 글자: `SelectChannel`)

```hlsl
half SelectChannel(half4 v, half channel)
{
    return channel < 0.5h ? v.r : channel < 1.5h ? v.g : channel < 2.5h ? v.b : v.a;
}
```

드롭다운에서 고른 0/1/2/3에 따라 R/G/B/A 중 하나만 꺼냅니다.

**(3) 픽셀마다 맵 읽기 — `Frag` 안 446–460행** (찾을 글자: `---- PBR 맵`)

```hlsl
half mapMetallic = _UseMetallicMap > 0.5h
    ? SelectChannel(SAMPLE_TEXTURE2D(_MetallicMap, sampler_MetallicMap, input.uv), _MetallicChannel)
    : _Metallic;
half roughSample = SelectChannel(SAMPLE_TEXTURE2D(_RoughnessMap, sampler_RoughnessMap, input.uv), _RoughnessChannel);
half mapSmoothness = _UseRoughnessMap > 0.5h
    ? (_RoughnessIsSmoothness > 0.5h ? roughSample : 1.0h - roughSample)
    : _Smoothness;
half occlusion = _UseOcclusionMap > 0.5h
    ? lerp(1.0h, SelectChannel(SAMPLE_TEXTURE2D(_OcclusionMap, sampler_OcclusionMap, input.uv), _OcclusionChannel), _OcclusionStrength)
    : 1.0h;
half3 emission = _UseEmissionMap > 0.5h
    ? _EmissionColor.rgb * SAMPLE_TEXTURE2D(_EmissionMap, sampler_EmissionMap, input.uv).rgb
    : half3(0, 0, 0);
```

| 줄 | 읽는 법 |
|---|---|
| 447–449 | 체크가 켜져 있으면 맵의 고른 채널, 꺼져 있으면 `Metallic (No Map)` 슬라이더 |
| 450–453 | 러프니스를 읽어 **스무스니스로 바꿔** 쓴다 (`1 − 값`). `Map Stores Smoothness`를 켜면 이미 스무스니스라 그대로 |
| 454–456 | `Occlusion Strength`가 0이면 1(가림 없음), 1이면 맵 값 그대로 |
| 458–460 | 이미시브 = `Emission Color` × 맵 색. 체크를 끄면 0 (맵 칸이 비어 있으면 기본 흰 텍스처라 전체가 Emission Color로 빛남) |

베이스·노멀은 그 위에서 읽습니다: 429–430행(베이스, 찾을 글자: `baseSample`), 436행 `SampleNormalTS`(함수는 251–256행).

> ORM을 세 칸에 넣으면 같은 텍스처를 **세 번** 읽습니다(448·450·455행). 칸마다 다른 텍스처를 넣어도 되게 만든 구조입니다.

**(4) 읽은 값이 쓰이는 곳**

| 값 | 쓰이는 줄 | 하는 일 |
|---|---|---|
| `metallic` | 465 `saturate(mapMetallic + maskMetal)` | 메탈릭 맵 + 재질 판별 결과를 더함 (VARCO는 판별을 꺼 둠 → 맵만) |
| `smoothness` | 499–500 | 판별된 금속·보석은 `Metal Smoothness`·`Gem Smoothness`로 바꿈 |
| 둘 다 | 504 `InitializeBRDFData(albedo, metallic, …, smoothness, …)` | URP의 PBR 계산 재료를 만든다 |
| `occlusion` | 532 `aoIndirect = occlusion` → 546(환경광), 551(환경 반사) | **환경광·환경 반사만** 가린다. 키 라이트는 안 가림 (URP Lit과 같은 규칙) |
| `emission` | 608 `color += emission` | 빛 계산이 다 끝난 뒤 그냥 더한다 → 그늘에서도 빛남 |

VARCO 머티리얼이 판별을 끄는 곳: `Editor/SemiRealInkTestSetup.cs` 196–203행 `ApplyVarcoPreset` (`_MetalDetect`, `_SilverDetect`, `_GemDetect` = 0).

### 5. ORM과 MetallicSmoothness — 같은 정보, 다른 포장

VARCO 3D는 glTF 규칙의 **ORM**을 줍니다. URP Lit은 다른 모양을 원합니다. 그래서 Blender 변환 스크립트가 한 장 더 만듭니다.

| 채널 | `*_ORM.png` (glTF) | `*_MetallicSmoothness.png` (URP Lit용) |
|---|---|---|
| R | AO | 메탈릭 |
| G | 러프니스 | AO |
| B | 메탈릭 | (비움, 0) |
| A | — | **스무스니스** (= 1 − 러프니스) |

```python
# Tools/varco_to_unity.py 125–132행
out[..., 0] = px[..., 2]          # 메탈릭
out[..., 1] = px[..., 0]          # AO (URP Lit Occlusion Map은 G를 읽음)
out[..., 3] = 1.0 - px[..., 1]    # 스무스니스
```

[그림: 왼쪽 ORM 한 장(R=AO, G=러프니스, B=메탈릭)에서 화살표 세 개가 새 쉐이더의 Ambient Occlusion Map(R) · Roughness Map(G) · Metallic Map(B) 칸으로. 오른쪽 MetallicSmoothness 한 장(R=메탈릭, G=AO, A=스무스니스)에서 URP Lit의 Metallic Map · Occlusion Map 칸으로]

**누가 무엇을 쓰나** — `Editor/SemiRealInkTestSetup.cs`

| 머티리얼 | 폴더 | 쓰는 텍스처 | 코드 |
|---|---|---|---|
| 원본 URP Lit (A 쪽) | `Materials/Varco/` | `_MetallicSmoothness` 를 `_MetallicGlossMap`과 `_OcclusionMap`에 | 154–161행 |
| 새 쉐이더 (B 쪽) | `Materials/Varco_SemiRealInk/` | `_ORM` 을 세 칸에 (채널 B / G / R) | 178–194행 `AssignOrmMap` |

```csharp
// SemiRealInkTestSetup.cs 184–193행 (찾을 글자: AssignOrmMap)
m.SetTexture("_MetallicMap", orm);
m.SetFloat("_MetallicChannel", 2f);          // B
m.SetFloat("_UseMetallicMap", 1f);
m.SetTexture("_RoughnessMap", orm);
m.SetFloat("_RoughnessChannel", 1f);         // G
m.SetFloat("_RoughnessIsSmoothness", 0f);    // 러프니스 그대로 저장돼 있음
m.SetFloat("_UseRoughnessMap", 1f);
m.SetTexture("_OcclusionMap", orm);
m.SetFloat("_OcclusionChannel", 0f);         // R
m.SetFloat("_UseOcclusionMap", 1f);
```

재미있는 점: 새 쉐이더 머티리얼을 만들 때 **먼저** `CopyTextures`(124행)가 원본의 MetallicSmoothness를 옮겨 넣고(229–247행: 메탈릭 R, 러프니스 A + `Map Stores Smoothness` 켬, AO G), **그다음** `AssignOrmMap`(128행)이 ORM으로 덮어씁니다. 즉 **새 쉐이더는 두 포장을 모두 읽을 수 있습니다.** 과제 2가 이것을 손으로 해 보는 것입니다.

> 새 쉐이더 머티리얼 파일 이름도 `Armor_Lit.mat` 입니다 (원본 이름을 그대로 씀, 109행). 폴더(`Varco_SemiRealInk`)와 인스펙터 맨 위 **Shader**(`ShaderTest/SemiRealInk`)로 구분합니다.

### 6. 디버그 보기는 정확히 무엇을 보여 주나

`_ShaderLabDebugView` 값에 따라 469–475행에서 바로 그 값을 칠하고 끝냅니다.

| 버튼 | 번호 | 칠하는 값 (줄) | 주의 |
|---|---|---|---|
| 베이스 컬러만 | 1 | `albedo` (470) | 베이스 맵 × Base Color |
| 노멀 (방향 색) | 2 | `N * 0.5 + 0.5` (471) | 노멀 맵이 적용된 **월드 방향**. 노멀 텍스처 색과 다르다 |
| 메탈릭 (흰색 = 금속) | 3 | `metallic` (472) | 맵 + 재질 판별. VARCO는 판별이 꺼져 있어 맵 그대로 |
| 러프니스 (흰색 = 거침) | 4 | `1 − mapSmoothness` (473) | 금속·보석 판별로 바꾸기 **전** 값 |
| AO (검을수록 가려짐) | 5 | `occlusion` (474) | AO 맵만. 화면 SSAO는 안 들어감 |
| 이미시브 | 6 | `emission` (475) | Emission Color × 맵 |

---

## 실습

> 실습 4·5는 `Materials/Varco_SemiRealInk/Armor_Lit.mat` **파일**을 고칩니다. 끝나면 실습 5 마지막 단계의 `git restore`로 되돌립니다.

### 실습 1. v2 받고 갑옷 확인 (10분)

1. 위 "시작 저장 지점" 명령으로 `lesson05` 브랜치를 만든다.
   - 확인: `git branch` 에 `* lesson05`
2. 탐색기에서 `Assets/CharacterShaderTest/Textures/Varco/Armor_ORM.png` 크기를 본다.
   - 확인: 수 MB. 1KB면 `git lfs pull`
3. Unity로 열고 `CharacterShaderTest.unity` ▶.
   - 확인: 갑옷 캐릭터, 패널 **커스텀** 탭에 "옷"·"무기" 두 줄이 보인다
4. **비교** 탭 → **B만 (4)**, **환경** 탭 → **중립 스튜디오**.

### 실습 2. 맵 지도 그리기 — 끄고 켜고, 디버그 보기 (15분)

1. **맵·보기** 탭에서 아래 표를 채운다. 옷은 **커스텀** 탭 "옷"에서 바꾼다.
2. 칸에는 "맵을 껐을 때 가장 크게 달라진 곳"을 한 단어로 쓴다. 변화가 없으면 "없음".

| 끈 체크 | 갑옷 (VARCO) | 한복 (VARCO) | 마법사 로브 (VARCO) | 조선 스팀펑크 (FinalPlayer) |
|---|---|---|---|---|
| 노멀 맵 | | | | |
| 메탈릭 맵 | | | | |
| 러프니스 맵 | | | | |
| 앰비언트 오클루전 (AO) | | | | |
| 이미시브 맵 | | | | |

   - 확인 포인트: FinalPlayer는 메탈릭·러프니스·AO가 "없음"이어야 정상. 이미시브는 로브에서만 변화.
3. 갑옷으로 돌아와 디버그 보기 **메탈릭 (흰색 = 금속)** → **러프니스 (흰색 = 거침)** → **AO (검을수록 가려짐)**.
   - 확인: 메탈릭에서 하얀 곳이 러프니스 보기에서는 밝은지(거침) 어두운지(매끈) 적어 둔다. 과제 1에서 쓴다
4. 디버그 **보통 화면**으로 돌린다.

### 실습 3. 임포트 설정 확인 (10분)

1. 플레이를 멈춘다.
2. `Textures/Varco/` 에서 아래 파일을 하나씩 선택하고 인스펙터 맨 위 **Texture Type**, **sRGB (Color Texture)** 를 적는다.

| 파일 | Texture Type | sRGB | 색 / 데이터 |
|---|---|---|---|
| `Armor_BaseColor.png` | | | |
| `Armor_Normal.png` | | | |
| `Armor_ORM.png` | | | |
| `Armor_MetallicSmoothness.png` | | | |
| `MageRobe_Emission.png` | | | |

정답: BaseColor = Default · 켬 · 색 / Normal = Normal map · (Normal map은 sRGB 칸이 없음) · 데이터 / ORM = Default · 끔 · 데이터 / MetallicSmoothness = Default · 끔 · 데이터 / Emission = Default · 켬 · 색

3. `Editor/VarcoAssetImporter.cs` 를 열고 63–72행에서 각 줄이 표의 어느 행을 만들었는지 연결한다.
4. `Armor_ORM.png` 를 선택하고 아래 미리 보기에서 채널 버튼을 R → G → B 로 바꿔 본다.
   - 확인: R(AO)은 거의 흰색, B(메탈릭)는 흑백 대비가 큼. 이 그림이 디버그 보기 AO·메탈릭과 같은 정보다

### 실습 4. 일부러 실수하기 — 데이터 텍스처를 sRGB로 (15분)

규칙이 이름 끝(`_ORM.png`)을 보므로, **이름이 다른 복사본**은 규칙을 피해 갑니다. 이걸로 실수를 재현합니다.

1. `Armor_ORM.png` 선택 → `Ctrl+D` (복제).
   - 확인: `Armor_ORM 1.png` 가 생긴다 (이름이 `_ORM.png`로 끝나지 않음)
2. `Armor_ORM 1.png` 선택 → **sRGB (Color Texture)** 를 **켜고** → **Apply**.
3. `Materials/Varco_SemiRealInk/Armor_Lit.mat` 선택 → **PBR Maps** 에서 **Roughness Map** 과 **Ambient Occlusion Map** 칸에 `Armor_ORM 1` 을 끌어다 놓는다. (채널은 그대로 G, R)
4. ▶ 플레이 → **B만 (4)** → 디버그 **러프니스 (흰색 = 거침)**, **AO (검을수록 가려짐)**, **보통 화면**을 캡처한다.
   - 확인: 러프니스 보기가 전체적으로 **어두워진다**(덜 거침) → 보통 화면에서 갑옷이 더 번들거린다. AO 보기에서는 원래 회색이던 틈이 더 짙어진다 (거의 흰 곳은 1에 가까워 그대로)
5. 멈추고 3번의 두 칸을 원래 `Armor_ORM` 으로 되돌린다. `Armor_ORM 1.png` 는 지운다 (선택 → `Delete`).
6. (덤) 원본 `Armor_ORM.png` 에서 sRGB를 켜고 Apply 해 본다.
   - 확인: 다시 가져올 때 `VarcoAssetImporter`(70–71행)가 sRGB를 끄고 가져오므로 **화면 결과는 바뀌지 않는다**. 이것이 가져오기 규칙을 두는 이유다
   - 끝나면 `git restore Assets/CharacterShaderTest/Textures/Varco/Armor_ORM.png.meta` (설정 파일이 바뀌었을 때를 대비)

### 실습 5. 채널 잘못 고르기 (20분)

플레이 중에 머티리얼 **파일**을 고친 뒤, **맵·보기**의 아무 맵 체크를 한 번 껐다 켜면 화면에 반영됩니다.
(체크를 바꿀 때마다 `LabCharacter.ApplyStyle`(LabCharacter.cs 107–115행)이 파일 값을 복사본에 다시 복사하기 때문)

1. ▶ 플레이 → **B만 (4)**. Project 창에서 `Materials/Varco_SemiRealInk/Armor_Lit.mat` 를 선택해 둔다.
2. **Metallic Channel** 을 `B` → `G` 로 바꾸고, **메탈릭 맵** 체크를 껐다 켠다. 디버그 **메탈릭 (흰색 = 금속)**.
   - 확인: 금속 자리가 아니라 **거친 곳**이 하얗게 나온다 (러프니스를 메탈릭으로 읽음). 보통 화면에서 엉뚱한 곳이 금속처럼 어둡게·반사로 보인다
3. **Metallic Channel** 을 `B` 로 되돌린다.
4. **Map Stores Smoothness (Unity Style)** 를 **켜고** 체크를 껐다 켠다. 디버그 **러프니스 (흰색 = 거침)**.
   - 확인: 흑백이 **뒤집힌다**. 쉐이더 452행에서 `1 − 값`을 건너뛰었기 때문
5. 다시 끈다. **Occlusion Strength** 를 `1` → `0` 으로. 디버그 **AO (검을수록 가려짐)**.
   - 확인: 전체가 흰색 (454–456행 `lerp(1, 맵, 0)` = 1)
6. `1` 로 되돌리고 플레이를 멈춘다.
7. 고친 머티리얼을 되돌린다.

```bash
git restore Assets/CharacterShaderTest/Materials/Varco_SemiRealInk/Armor_Lit.mat
git status    # Armor_Lit.mat 이 목록에 없으면 OK
```

8. 실습 2·3 표는 공책이나 내 메모 파일에 남긴다. 7번에서 머티리얼을 되돌렸으므로 commit할 코드 변경은 없다.

---

## 정리

- 맵 여섯 개 = 색 둘(베이스·이미시브) + 데이터 넷(노멀·메탈릭·러프니스·AO).
- 색은 **sRGB 켬**, 데이터는 **sRGB 끔**, 노멀은 **Normal map**. `VarcoAssetImporter`가 **파일 이름 끝**을 보고 자동으로 맞춘다.
- 채널 패킹: 흑백 맵 여러 장을 한 장에. 새 쉐이더는 **같은 텍스처 + 칸마다 채널 선택**(`SelectChannel`)으로 읽는다.
- ORM(R AO · G 러프니스 · B 메탈릭)과 MetallicSmoothness(R 메탈릭 · G AO · A 스무스니스)는 같은 정보, 다른 포장. 러프니스 ↔ 스무스니스는 `Map Stores Smoothness` 하나로 뒤집는다.
- AO는 환경광·환경 반사만 가리고, 이미시브는 빛 계산 뒤에 더한다.

## 과제

**과제 1. 맵 분석 보고서** — 갑옷·한복·마법사 로브 3벌 + 무기 1개(검·활·지팡이 중 선택)를 분석한다.

1. 환경 **중립 스튜디오**, 비교 **B만 (4)**, **처음 시점 (F)** 으로 맞춘다.
2. 대상마다 디버그 보기 **메탈릭 / 러프니스 / AO** 3장을 캡처한다 (로브·지팡이는 **이미시브**까지 4장).
3. 아래 표를 채운다.

| 대상 | 금속인 곳 | 가장 매끈한 곳 / 가장 거친 곳 | AO가 진한 곳 | 이미시브 | 맵 하나를 끈다면 가장 티 나는 맵과 이유 |
|---|---|---|---|---|---|
| 갑옷 | | | | | |
| 한복 | | | | | |
| 마법사 로브 | | | | | |
| (무기) | | | | | |

4. 마지막에 3~5줄: "ORM을 sRGB 켠 채 가져오면 이 캐릭터의 어디가 어떻게 달라질까" (실습 4 결과를 근거로).

**과제 2. MetallicSmoothness로 바꿔 끼우기** — 새 쉐이더(B)의 갑옷을 ORM 대신 URP Lit용 텍스처로 돌리고, 결과가 같은지 확인한다.

코드 확인 (이 방식이 실제로 지원되는지):

| 필요한 것 | 쉐이더가 지원하나 | 근거 |
|---|---|---|
| R 채널을 메탈릭으로 | 예 | `_MetallicChannel` Enum R=0 (32행), `SelectChannel` (238–241행) |
| A 채널을 러프니스 칸에 | 예 | `_RoughnessChannel` Enum A=3 (35행) |
| A가 스무스니스라 뒤집지 않기 | 예 | `_RoughnessIsSmoothness` (36행) → 452행에서 그대로 사용 |
| G 채널을 AO로 | 예 | `_OcclusionChannel` Enum G=1 (39행) |
| 장면 메뉴도 같은 설정을 씀 | 예 | `SemiRealInkTestSetup.cs` 229–247행 `CopyTextures` |

단계:

1. 플레이를 멈춘 상태. **바꾸기 전** 캡처: ▶ → 중립 스튜디오 · B만 (4) · 처음 시점 (F) → 디버그 **메탈릭 / 러프니스 / AO / 보통 화면** 4장. 멈춘다.
2. Project 창 `Assets/CharacterShaderTest/Materials/Varco_SemiRealInk/Armor_Lit.mat` 선택.
   - 확인: 인스펙터 맨 위 Shader = `ShaderTest/SemiRealInk` (`Materials/Varco/Armor_Lit.mat` 은 URP Lit 원본이니 고르지 않는다)
3. **PBR Maps** 머리글 아래를 이렇게 바꾼다. 텍스처는 `Textures/Varco/Armor_MetallicSmoothness.png` 를 각 칸에 끌어다 놓는다.

| 인스펙터 칸 | 바꾸기 전 (ORM) | 바꾼 뒤 (MetallicSmoothness) |
|---|---|---|
| Metallic Map | `Armor_ORM` | `Armor_MetallicSmoothness` |
| Metallic Channel | B | **R** |
| Use Metallic Map | ☑ | ☑ |
| Roughness Map | `Armor_ORM` | `Armor_MetallicSmoothness` |
| Roughness Channel | G | **A** |
| Map Stores Smoothness (Unity Style) | ☐ | **☑** |
| Use Roughness Map | ☑ | ☑ |
| Ambient Occlusion Map | `Armor_ORM` | `Armor_MetallicSmoothness` |
| Occlusion Channel | R | **G** |
| Use Occlusion Map | ☑ | ☑ |

4. 메뉴 **File → Save Project** 로 저장.
5. **바꾼 뒤** 캡처: 1번과 같은 조건으로 4장.
   - 확인: 바꾸기 전과 **거의 같아야** 한다 (같은 정보를 다른 포장으로 읽었으므로). 확 다르면 3번 표의 굵은 글씨 칸을 다시 본다
6. **일부러 틀리기** 1장: 플레이 중에 **Map Stores Smoothness** 만 ☐ 로 끄고, **맵·보기**의 맵 체크를 한 번 껐다 켠 뒤(파일 값 다시 읽기) 디버그 **러프니스** 캡처 → 다시 ☑ → 멈추고 **File → Save Project**.
7. **비교** 탭 → **와이프 (1)**, A = `원본 (URP Lit)`, B = `세미리얼` 로 보통 화면 1장. A(URP Lit 원본)도 `Armor_MetallicSmoothness` 를 쓰므로 이제 두 쪽이 **같은 텍스처**를 읽는다.
8. `lesson05` 브랜치에 commit 한다 (`git add -A` → `git commit -m "5차시 과제 2"`). 다음 차시는 새 브랜치에서 시작하므로 영향이 없다. 되돌리고 싶으면 `git restore`(실습 5의 7단계) 또는 메뉴 **Tools → Shader Test → Reset Materials And Post To Presets** (다른 머티리얼·프리셋도 전부 기본값이 되니 주의).
9. 보고서에 4줄: 바꾼 칸 4개(Metallic Channel, Roughness Channel, Map Stores Smoothness, Occlusion Channel)가 각각 **왜** 그 값인지.

| 제출물 | 채점 기준 | 배점 |
|---|---|---|
| 과제 1 캡처 (13장 이상) | 대상·디버그 보기 이름이 파일 이름에 있다 | 15 |
| 과제 1 표 | 4행 모두 채움. "금속인 곳"이 메탈릭 보기 흰 곳과 맞다 | 20 |
| 과제 1 sRGB 글 | 러프니스(더 반짝)·AO(더 어두움) 중 하나 이상을 근거와 함께 썼다 | 10 |
| 과제 2 전·후 캡처 8장 | 같은 시점·환경. 전과 후가 거의 같다 | 20 |
| 과제 2 틀리기 캡처 | 러프니스 흑백이 뒤집힌 것이 보인다 | 10 |
| 과제 2 설명 4줄 | R·A·☑·G 네 값의 이유를 채널 표(ORM vs MetallicSmoothness)로 설명했다 | 15 |
| 공통 | 파일 이름 `이름_05_번호.png` 규칙, commit 1개 이상 | 10 |

## 확인 질문

1. 새 쉐이더의 갑옷 머티리얼은 `Armor_ORM` 한 장을 세 칸에 넣는다. Metallic·Roughness·Occlusion Channel은 각각 무엇인가?
   - **정답**: Metallic = B, Roughness = G, Occlusion = R (glTF ORM = R AO, G 러프니스, B 메탈릭). `SemiRealInkTestSetup.cs` `AssignOrmMap`(178–194행).
2. `Armor_ORM.png` 를 sRGB 켠 채로 가져오면 화면이 어떻게 달라지나? 그리고 이 프로젝트에서 그런 일이 잘 안 생기는 이유는?
   - **정답**: GPU가 sRGB → 선형 변환을 해서 중간값이 어두워진다(0.5 → 약 0.21). 러프니스가 낮게 읽혀 더 번들거리고 AO 틈이 더 어두워진다. `VarcoAssetImporter.OnPreprocessTexture`(63–72행)가 이름이 `_ORM.png`로 끝나는 파일의 sRGB를 가져올 때마다 끄기 때문.
3. URP Lit용 `Armor_MetallicSmoothness` 를 새 쉐이더의 Roughness Map에 A 채널로 넣었다. `Map Stores Smoothness (Unity Style)` 를 꼭 켜야 하는 이유는?
   - **정답**: A에는 러프니스가 아니라 **스무스니스(1 − 러프니스)** 가 들어 있다. 쉐이더는 기본으로 "러프니스"라 보고 `1 − 값`을 하므로(452행), 켜지 않으면 거친 곳과 매끈한 곳이 뒤집힌다.

## 자주 막히는 곳

| 증상 | 원인 | 해결 |
|---|---|---|
| v2로 바꾸고 처음 열 때 아주 오래 걸림 | VARCO 모델·텍스처를 새로 가져오는 중 | 기다린다. 다음부터는 빠르다 |
| 캐릭터가 분홍색·흰색 | LFS 파일을 못 받음 | `git lfs pull` |
| 맵 체크를 켰는데 아무 변화 없음 | 스위치는 "끄기만" 한다 / 그 머티리얼에 맵이 없음 (FinalPlayer, 갑옷의 이미시브) | 끄면서 비교한다. 맵이 있는 VARCO 옷으로 바꾼다 |
| 디버그 보기를 눌렀는데 A 쪽이 그대로 | A가 원본 (URP Lit). 디버그 보기는 새 쉐이더에만 | **B만 (4)** 로 본다 |
| 플레이 중 `.mat` 을 고쳤는데 화면이 그대로 | 화면은 플레이 시작 때 만든 복사본 `(Lab)`을 쓴다 | **맵·보기**의 맵 체크를 한 번 껐다 켜거나 스타일 카드를 다시 누른다 (파일 값을 다시 복사) |
| 플레이 중 Hierarchy의 렌더러에서 머티리얼을 고쳤더니 사라짐 | 그건 `Armor_Lit (Lab)` 복사본. 체크를 바꾸면 파일 값으로 덮이고, 멈추면 없어진다 | Project 창의 `.mat` 파일을 고친다 |
| 플레이를 멈췄는데 `.mat` 이 고친 채로 남음 | 머티리얼 **파일**은 플레이를 멈춰도 되돌아가지 않는다 | `git restore Assets/CharacterShaderTest/Materials/Varco_SemiRealInk/Armor_Lit.mat` |
| `Armor_ORM.png` 의 sRGB를 켜도 결과가 같음 | `VarcoAssetImporter`가 가져올 때 다시 끈다 (정상) | 실험은 이름이 다른 복사본(`Armor_ORM 1.png`)으로 |
| 고친 머티리얼이 `Materials/Varco/` 쪽 | 그건 URP Lit 원본. 새 쉐이더 칸(Metallic Channel 등)이 없다 | `Materials/Varco_SemiRealInk/` 의 같은 이름 파일을 고른다 |
| Normal Map 칸 아래 경고와 **Fix Now** 버튼 | 노멀 맵으로 가져오지 않은 텍스처를 넣음 (`[Normal]` 속성) | `_Normal.png` 파일을 넣는다. 이름 규칙을 지키면 자동으로 Normal map |
| 바꿔 끼운 뒤 갑옷이 이상하게 번들거리거나 까매짐 | 채널을 하나 덜 바꿈 (예: Roughness Channel이 G로 남아 AO를 러프니스로 읽음) | 과제 2 표의 굵은 칸 4개를 다시 확인 |
