# Roguelike-Bowling

마우스 기반 볼링 조작에 로그라이크 요소(장애물, 아이템, 스킬)를 결합한 3D 볼링 게임입니다.

## 프로젝트 개요

| 항목 | 내용 |
| --- | --- |
| 장르 | 3D 로그라이크 볼링 게임 |
| 타겟 플랫폼 | 웹 빌드 (WebGL) |
| 아트 스타일 | 로우폴리 |
| 핵심 재미 요소 | 마우스 기반 볼링 조작 + 로그라이크 요소(장애물, 아이템, 스킬) |

자세한 기획 배경과 스펙은 [`Assets/Docs/GDD_BowlingRoguelike.md`](Assets/Docs/GDD_BowlingRoguelike.md) 참고.

## 기술 스택

- **Unity**: 6000.3.6f1
- **렌더 파이프라인**: URP 17.3.0
- **입력**: Unity 새 Input System (`com.unity.inputsystem`)
- **UI**: uGUI + TextMeshPro
- **에디터 연동**: MCP for Unity (Claude Code 연동용)

## 팀원 및 역할

### 박준선 — 기획 / 맵·레벨 디자인
- 게임 기획 문서(GDD) 작성 및 지속 업데이트 — 레퍼런스 설정, 스테이지·레인·핀·장애물 기획, 투구 시스템 스펙, 로그라이크 정체성 설계
- 맵·씬 제작: 레인 배치 및 장소별 테마 꾸미기 — `Main`, `Map`, `Desert`, `Ice`, `Mountain`, `Volcano`, `Field`, `Scene_Stage_BowlingAlley`, `Scene_Stage_Ice`, `Scene_Stage_Map`, `Scene_Stage_Mountain` 등 다수의 스테이지·테마 씬 제작
- 프로젝트 초기 세팅: 초기 프로젝트 구조 및 시스템 뼈대 구성

### 김나형 — 개발 (볼 물리·투구 시스템)
- 투구 입력 시스템: 마우스 드래그 기반 방향·각도·곡률 계산 (`ThrowInputHandler.cs`)
- 공 발사·물리: 드래그 속도 기반 발사 속도 계산, 곡률 기반 스핀 적용, Magnus 효과로 스핀 궤적 구현 (`BallLauncher.cs`, `BallMagnusEffect.cs`)
- 공 리스폰 시스템: 낙하·타임아웃 감지 후 삭제·재생성 방식으로 리스폰 (`BallSpawner.cs`, `BallResetter.cs`)
- 파워 게이지 UI: 투구 파워를 원형 게이지로 시각화 (`PowerGaugeUI.cs`)
- 투구 튜토리얼 시스템: 포지셔닝·파워·스핀 3단계 튜토리얼, ScriptableObject 기반 단계 데이터 구조 설계 (`TutorialController.cs`, `TutorialStepData.cs`)

### 김종하 — 개발 (게임 시스템·UI)
- 핀 판정 시스템: 핀 회전각·밀림 거리 기반 쓰러짐 판정, 프레임 간 리셋 처리 (`PinDeckManager.cs`, `BowlingPin.cs`)
- 스테이지 진행 시스템: 10프레임 볼링 규칙 기반 점수 계산 및 목표 점수 클리어 조건 구현 (`StageManager.cs`)
- 경제·저장 시스템: 상점 아이템, 인벤토리, PlayerPrefs 기반 세이브·로드 (`ShopItem.cs`, `InventoryManager.cs`, `SaveManager.cs`)
- UI 제작: 메인 메뉴·설정·상점 화면 등 UI 골격 구성 (`MainMenuController.cs` 및 관련 UI 프리팹)



## 협업 방식

Git 기반, 기능별 브랜치(`Ball`, `Map`, `Roguelike&UI` 등) → PR → `main` 병합 흐름으로 작업합니다.

## 폴더 구조

```
Assets/
├─ Scripts/
│  ├─ Ball/              # 공 물리 — 발사, 스핀 제약, Magnus 효과, 리스폰
│  ├─ Player/             # 투구 입력 처리 및 발사 로직
│  ├─ Managers/            # 골드/인벤토리/세이브/사운드/아이템 효과 등 매니저
│  ├─ UI/                 # 메인메뉴·상점·컬렉션·파워게이지 등 UI
│  ├─ Tutorial/            # 튜토리얼 시스템
│  ├─ Gameplay/            # 카메라 등 게임플레이 보조 스크립트
│  ├─ Editor/              # 에디터 확장
│  └─ (루트)               # 스테이지 장애물, 핀 판정, 레인 기믹 등 개별 스크립트
├─ Prefabs/                # 공, 플레이어, UI 프리팹
├─ Scenes/                 # 메인 메뉴, 스테이지별(사막/얼음/화산/설원 등), 테스트 씬
├─ Animations/              # 애니메이터 컨트롤러 및 애니메이션 클립
├─ Materials/ Models/ Textures/  # 아트 리소스
├─ ScriptableObjects/       # 튜토리얼 단계 데이터 등
├─ FreeLicenseAssets/       # 외부 무료 라이선스 에셋 (Kenney, Thoth 등)
└─ Docs/                   # GDD 등 기획 문서
```

## 외부 에셋

`Assets/FreeLicenseAssets/` 하위의 외부 무료/유료 에셋 팩 목록과 라이선스입니다.

| 팩 | 버전 | 경로 | 라이선스 |
| --- | --- | --- | --- |
| Cube Pets | 2.0 | `Assets/FreeLicenseAssets/kenney_cube-pets_1.0` | CC0 1.0 |
| Nature Kit | 2.1 | `Assets/FreeLicenseAssets/kenney_nature-kit` | CC0 1.0 |
| Platformer Kit | 4.1 | `Assets/FreeLicenseAssets/kenney_platformer-kit` | CC0 1.0 |
| Minimalist Cartoon UI Pack | 1.2 | `Assets/FreeLicenseAssets/Thoth` | Standard Unity Asset Store EULA |
| Cute GUI-Pack-Lite | 1.0.0 | `Assets/FreeLicenseAssets/Cute-GUI-Pack-Lite` | Standard Unity Asset Store EULA |

## 씬

- `Main.unity` — 메인 메뉴
- `MainGame.unity` — 실제 게임플레이
- `Desert.unity` / `Ice.unity` / `Mountain.unity` / `Volcano.unity` / `Field.unity` / `Scene_Stage_*.unity` — 테마별 스테이지
- `PlayTestScene.unity` / `TestUI.unity` / `SampleScene.unity` — 프로토타입·테스트용
