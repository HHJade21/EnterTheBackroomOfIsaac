using UnityEngine;

public class GameManager : MonoBehaviour
{
    // [Singleton] Implement thread-safe MonoBehaviour singleton pattern
    // - Ensure single instance persists across scenes (DontDestroyOnLoad)
    // - Provide public static accessor (Instance)
    // - Guard against duplicates on scene load

    // [Game State] Track global game state
    // - Title, Lobby, Dungeon, Pause, GameOver enums
    // - Current state property and state change event

    // [Scene Flow] Centralize scene transitions
    // - Load Title, Lobby, Dungeon scenes
    // - Expose async load with fade and loading screen hooks
    // - Handle initial boot flow and cleanup between scenes

    // [Player Session] Persist selected character and run data
    // - Selected character ID / data
    // - Seed for roguelike run
    // - Methods to start/end a run, reset state

    // [Services] Hold references to shared services (registered on Awake)
    // - SceneLoader, AudioService, SaveService (if any)

    // [Events] Define and raise domain events
    // - OnGameStateChanged, OnRunStarted, OnRunEnded, OnGameOver

    // [Lifecycle] Wire bootstrapping
    // - Initialize services
    // - Subscribe/unsubscribe to events
    // - Load initial scene/state
}
