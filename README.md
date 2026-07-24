# 🎮 Bounce Heroes - Code Architecture & Project Structure

본 저장소는 **Bounce Heroes** 유니티 프로젝트의 핵심 스크립트 및 아키텍처 코드를 담고 있는 포트폴리오 저장소입니다.  
VContainer 기반의 의존성 주입(DI), 인터페이스 기반 설계, 데이터 중심(ScriptableObject) 구조, 그리고 UI Toolkit 기반의 커스텀 툴 개발을 포함하고 있습니다.

---

## 🛠️ Tech Stack & Key Concepts
- **Engine**: Unity
- **Architecture**: Dependency Injection (`VContainer`), Event-Driven UI, Interface-driven Decoupling
- **Data Architecture**: ScriptableObject Data-Driven Design
- **Editor Extension**: UI Toolkit (`UXML`, `USS`, `EditorWindow`), AutoBind Custom Inspector

---

## 📁 Directory Structure

```text
Public-Code/
├── 🚀 Bootstrap/           # VContainer LifetimeScope (의존성 주입 및 수명주기 관리)
├── ⚙️ Core/                # 핵심 인터페이스, 공통 Enums, 글로벌 이벤트 정의
├── 📦 Data/                # 게임 데이터 구조체 및 런타임 상태 (DamageContext, WaveData 등)
├── 🎮 Gameplay/            # 볼 발사, 몬스터, 궤적 예측, 그리드 필드 및 전투 시스템
├── 🧠 Manager/             # 게임 루프, 웨이브 관리, 스킬 선택 및 사운드/Juice 매니저
├── 📊 ScriptableObjects/   # 데이터 중심 설계를 위한 ScriptableObject 데이터 및 스킬 정의
├── 🎨 UI/                  # UI Architecture (View - Controller - Event 분리 구조)
├── 🏆 Leaderboard/         # PlayFab & Local 팩토리 패턴 기반 리더보드 시스템
├── 🔊 FX & Audio/          # 이펙트 풀링 서비스 및 사운드 관리 시스템
├── 🎯 Input/               # Pointer & Swipe 입력 감지 시스템
├── 💯 Score/               # 점수 계산 및 성적 등급 산정 서비스
├── 🛠️ Editor/             # UI Toolkit 기반 Wave Designer 및 커스텀 에디터 툴
├── 🧪 Debugging/           # 런타임 스킬 및 게임플레이 테스트용 Harness
└── 🔧 Utility/             # 애니메이션 이벤트, 해상도 대응 Camera Fitter 등 유틸리티
```

---

## 🔍 Module Details

### 1. 🚀 Bootstrap (`/Bootstrap`)
- **`GameLifetimeScope.cs` / `HomeLifetimeScope.cs` / `IntroLifetimeScope.cs`**:
  - `VContainer`를 활용해 씬별 서비스 객체들의 생명주기를 관리하고 의존성을 주입(DI)합니다.

### 2. ⚙️ Core (`/Core`)
- **`IAudioService`, `IFXService`, `ILeaderboardService`, `IScoreService`, `ICombatService`**:
  - 모듈 간의 직결합을 방지하고 테스트 및 모킹(Mocking)이 용이하도록 인터페이스를 정의합니다.
- **`CombatEvents`, `GameUIEvents`, `GameTime`**:
  - 전투 발생 및 UI 이벤트를 중계하고, 런타임 타임스케일을 제어합니다.

### 3. 🎮 Gameplay (`/Gameplay`)
- **`Ball.cs` & `BallLauncher.cs`**:
  - 볼 물리 연산, 충돌 처리, 연속 발사 제어 및 조준 알고리즘.
- **`TrajectoryPreview.cs`**:
  - 플레이어의 조준에 따른 볼 반사 궤적을 2D 물리 레이캐스트로 실시간 예측/렌더링.
- **`Monster.cs` & `GridField.cs`**:
  - 몬스터 체력, 피격 반응, 그리드 기반 턴 이동 및 라인 침범 검사.
- **`CombatService.cs` & `RecallZone.cs`**:
  - 데미지 계산 및 발사된 볼의 하단 회수 처리.

### 4. 🧠 Manager (`/Manager`)
- **`GameManager.cs` & `IGameFlow.cs`**:
  - 전체 게임 상태 머신(State Machine: Ready, Aiming, Shooting, TurnEnd, GameOver 등) 관리.
- **`WaveManager.cs` & `WaveSpawnSequencer.cs`**:
  - 웨이브 데이터 기반 몬스터 스폰 시퀀싱 및 턴 진행 조율.
- **`SkillManager.cs` & `SkillSelectionService.cs`**:
  - 카드 선택 형태의 3택 1 스킬 메커니즘, 액티브/패시브 스킬 획득 및 쿨타임 관리.
- **`JuiceManager.cs`**:
  - 카메라 쉐이크, 피격 역체감(Hit Stop) 등 게임의 손맛(Juice) 연출 총괄.

### 5. 🎨 UI System (`/UI`)
- **View - Controller - Event 분리 구조**:
  - `UIViews/`: 화면에 보여지는 UI 요소 참조 및 애니메이션(`UIView`, `SkillSelectView`, `RankingView` 등)
  - `Controllers/`: View와 런타임 비즈니스 로직 연동 (`SkillSelectController`, `ResultController` 등)
  - `Events/`: UI 신호 전달을 위한 헐거운 결합 이벤트 모음 (`GameplayEvents`, `HomeUIEvents` 등)

### 6. 📊 Data & ScriptableObjects (`/ScriptableObjects`)
- **`GameBalanceData`, `MonsterData`, `WaveTableData`**:
  - 기획 데이터(몬스터 능력치, 웨이브 구성, 게임 밸런스)를 데이터베이스화.
- **`Skills/` (`ActiveSkillData`, `PassiveSkillData`, `FireBallSkillData` 등)**:
  - 다형성을 활용하여 각 스킬의 효과 및 수치를 ScriptableObject로 독립 구성.

### 7. 🏆 Leaderboard (`/Leaderboard`)
- **`LeaderboardServiceFactory.cs`**:
  - 네트워크 환경이나 설정에 따라 PlayFab 기반 리더보드(`PlayFabLeaderboardService`) 또는 로컬 리더보드(`LocalLeaderboardService`)를 동적으로 생성/주입.

### 8. 🛠️ Custom Editor Tools (`/Editor`)
- **`WaveDesignerWindow` (`.cs`, `.uxml`, `.uss`)**:
  - Unity UI Toolkit을 활용하여 몬스터 배치 및 웨이브 구성을 비주얼하게 편집할 수 있는 레벨 디자이너 툴.
- **`AutoBindEditor.cs` / `RequiredDrawer.cs`**:
  - 컴포넌트 자동 바인딩 및 인스펙터 필수 참조 검증 커스텀 에디터 확장.

---

## ✨ Architectural Highlights
1. **Low Coupling & High Cohesion**: `VContainer`와 Interface 기반 설계로 각 시스템 간의 독립성을 보장합니다.
2. **Data-Driven Balance**: 하드코딩을 배제하고 `ScriptableObject` 중심의 데이터 유연성을 확보했습니다.
3. **Robust Event System**: Gameplay 및 UI 레이어가 C# Event / Interface를 통해 소통하여 확장 및 유지보수가 용이합니다.
4. **Tooling Support**: 기획자/디자이너 작업을 돕기 위한 레벨 디자인 및 인스펙터 생산성 툴을 포함합니다.
