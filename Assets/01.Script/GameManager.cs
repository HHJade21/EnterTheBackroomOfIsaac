using System; // System 네임스페이스 사용 (Action, 기본 유틸)
using UnityEngine; // Unity 관련 기본 타입 사용
using UnityEngine.SceneManagement; // 씬 전환을 위한 SceneManager 사용

public class GameManager : MonoBehaviour // 전역 게임 상태를 관리하는 싱글톤 매니저
{
    // [Singleton] 스레드 세이프는 아니지만 Unity 환경에서 충분한 MonoBehaviour 싱글톤 구현 설명
    // - 씬 전환 간에도 파괴되지 않도록 유지
    // - 정적 프로퍼티로 Instance 접근자 제공
    // - 중복 인스턴스가 생기면 제거
    public static GameManager Instance { get; private set; } // 싱글톤 인스턴스 보관

    // [Game State] 전역 게임 상태 관리
    // - Title, Lobby, Dungeon, Pause, GameOver 상태 정의
    // - 현재 상태와 상태 변경 이벤트 제공
    public enum GameState { Title, Lobby, Dungeon, Pause, GameOver } // 게임 상태 열거형
    public GameState CurrentState { get; private set; } // 현재 게임 상태
    public event Action<GameState, GameState> OnGameStateChanged; // 상태 변경 이벤트 (이전, 다음)

    // [Scene Flow] 씬 전환 제어 지점
    // - Title, Lobby, Dungeon 씬 이름 보관
    // - 페이드/로딩 화면 훅과 연계 가능
    // - 초기 부팅 흐름 처리
    [Header("Scenes")] // 인스펙터에서 그룹 헤더 표시
    [SerializeField] private string titleSceneName = "Title"; // 타이틀 씬 이름
    [SerializeField] private string lobbySceneName = "Lobby"; // 로비 씬 이름
    [SerializeField] private string dungeonSceneName = "Dungeon"; // 던전 씬 이름

    // [Player Session] 플레이어 선택 및 런 데이터 유지
    // - 선택된 캐릭터 ID
    // - 로그라이크 시드
    // - 런 시작/종료, 리셋 메서드 제공
    public int SelectedCharacterId { get; private set; } = -1; // 선택된 캐릭터 ID
    public int CurrentRunSeed { get; private set; } // 현재 런 시드 값
    public bool IsRunActive { get; private set; } // 런 진행 여부

    // [Services] 공유 서비스 참조 보관 (Awake에서 초기화)
    // - SceneLoader 등 헬퍼 사용 가능
    private SceneLoader sceneLoader; // 필요 시 씬 로더 사용 (기본은 SceneManager)

    // [Events] 도메인 이벤트 정의
    // - 런 시작/종료, 게임오버 등
    public event Action OnRunStarted; // 런 시작 시 호출
    public event Action OnRunEnded; // 런 종료 시 호출
    public event Action OnGameOver; // 게임오버 시 호출

    [Header("Audio")] // 오디오 관련 설정 헤더
    public AudioClip titleBGM; // 타이틀에서 재생할 BGM 클립
    public AudioClip battleBGM; // 전투용 BGM (확장용)
    public AudioClip restBGM; // 휴식/로비 BGM (확장용)

    private AudioSource bgmSource; // BGM 재생용 오디오 소스

    // [Lifecycle] 부트스트랩 루틴
    // - 서비스 초기화
    // - 이벤트 구독/해제 준비
    // - 초기 씬/상태 로드
    private void Awake() // 유니티 라이프사이클: 오브젝트 생성 시 호출
    {
        if (Instance != null && Instance != this) // 이미 인스턴스가 존재하고 자신이 아니면
        {
            Destroy(gameObject); // 중복 오브젝트 제거
            return; // 이후 로직 중단
        }

        Instance = this; // 싱글톤 인스턴스 등록
        DontDestroyOnLoad(gameObject); // 씬 전환 시 파괴되지 않게 설정
        InitializeServices(); // 서비스 초기화 호출

        // BGM 재생용 AudioSource 설정
        bgmSource = gameObject.AddComponent<AudioSource>(); // AudioSource 컴포넌트 추가
        bgmSource.loop = true; // 루프 재생
        bgmSource.playOnAwake = false; // 시작 시 자동 재생 비활성화

        // 씬 로드 완료 이벤트 구독 (씬에 따라 BGM 제어)
        SceneManager.sceneLoaded += HandleSceneLoaded; // 씬 로드시 콜백 등록
    }

    private void Start() // 유니티 라이프사이클: 첫 프레임 전에 호출
    {
        // 기본적으로 Title로 시작 (부트스트랩이 따로 상태를 정하지 않은 경우)
        if (string.IsNullOrEmpty(SceneManager.GetActiveScene().name) || SceneManager.GetActiveScene().name == "SampleScene") // 초기씬 감지
        {
            LoadTitle(); // 타이틀 로드
        }
        else
        {
            // 특정 씬에서 바로 실행되는 경우(예: Title), BGM 상태를 현 씬 기준으로 보정
            HandleSceneLoaded(SceneManager.GetActiveScene(), LoadSceneMode.Single); // 현재 씬 기준 처리
        }
    }

    private void InitializeServices() // 공유 서비스 초기화
    {
        // 가벼운 서비스만 여기서 등록 (무거운 작업은 지양)
        sceneLoader = new SceneLoader(); // 씬 로더 인스턴스 생성
    }

    // [State Machine] 상태 전환 메서드
    public void SetState(GameState next) // 다음 상태로 전환
    {
        if (next == CurrentState) return; // 동일 상태면 무시
        var prev = CurrentState; // 이전 상태 저장
        CurrentState = next; // 현재 상태 갱신
        OnGameStateChanged?.Invoke(prev, next); // 상태 변경 이벤트 호출
    }

    // [Scene API] 씬 전환 인터페이스
    public void LoadTitle() // 타이틀 씬 로드
    {
        SetState(GameState.Title); // 상태를 Title로 설정
        LoadSceneByName(titleSceneName); // 타이틀 씬 로드 호출
        PlayTitleBGM(); // 타이틀 BGM 재생
    }

    public void LoadLobby() // 로비 씬 로드
    {
        SetState(GameState.Lobby); // 상태를 Lobby로 설정
        LoadSceneByName(lobbySceneName); // 로비 씬 로드 호출
        StopBGM(); // 기존 BGM 정지
    }

    public void LoadDungeon() // 던전 씬 로드
    {
        SetState(GameState.Dungeon); // 상태를 Dungeon으로 설정
        LoadSceneByName(dungeonSceneName); // 던전 씬 로드 호출
        StopBGM(); // 타이틀 BGM 정지
    }

    private void LoadSceneByName(string sceneName) // 공통 씬 로드 함수
    {
        if (string.IsNullOrWhiteSpace(sceneName)) return; // 씬 이름이 비어 있으면 무시

        // SceneLoader로 페이드/로딩 UI를 다루고 싶다면 여기에서 교체
        // 현재는 단순하게 SceneManager로 즉시 전환
        SceneManager.LoadScene(sceneName); // 동기 씬 로드 실행
    }

    // [Session API] 런/세션 관련 메서드
    public void SetSelectedCharacter(int characterId) // 선택 캐릭터 설정
    {
        SelectedCharacterId = characterId; // ID 저장
    }

    public void StartRun(int? seed = null) // 런 시작
    {
        if (IsRunActive) return; // 이미 진행 중이면 무시
        CurrentRunSeed = seed ?? UnityEngine.Random.Range(int.MinValue, int.MaxValue); // 시드 결정
        IsRunActive = true; // 런 상태 on
        OnRunStarted?.Invoke(); // 런 시작 이벤트 호출
    }

    public void EndRun() // 런 종료
    {
        if (!IsRunActive) return; // 진행 중이 아니면 무시
        IsRunActive = false; // 런 상태 off
        OnRunEnded?.Invoke(); // 런 종료 이벤트 호출
    }

    public void TriggerGameOver() // 게임오버 트리거
    {
        SetState(GameState.GameOver); // 상태를 GameOver로 설정
        OnGameOver?.Invoke(); // 게임오버 이벤트 호출
        EndRun(); // 진행 중인 런 종료
    }

    private void PlayTitleBGM() // 타이틀 BGM 재생
    {
        if (bgmSource == null) return; // 오디오 소스 없으면 중단
        if (titleBGM == null) return; // 클립이 없으면 중단
        if (bgmSource.clip == titleBGM && bgmSource.isPlaying) return; // 이미 재생 중이면 무시
        bgmSource.clip = titleBGM; // 오디오 소스에 타이틀 클립 할당
        bgmSource.volume = 1f; // 볼륨 설정 (필요 시 인스펙터 노출 가능)
        bgmSource.Play(); // 재생 시작
    }

    private void StopBGM() // 현재 BGM 정지
    {
        if (bgmSource == null) return; // 소스 없으면 중단
        if (!bgmSource.isPlaying) return; // 재생 중이 아니면 중단
        bgmSource.Stop(); // 재생 정지
        bgmSource.clip = null; // 클립 해제 (상태 초기화)
    }

    private void HandleSceneLoaded(Scene scene, LoadSceneMode mode) // 씬 로드 완료 콜백
    {
        if (scene.name == titleSceneName) // 타이틀 씬이면
        {
            PlayTitleBGM(); // 타이틀 BGM 재생
        }
        else // 그 외 씬이면
        {
            StopBGM(); // BGM 정지
        }
    }
}
