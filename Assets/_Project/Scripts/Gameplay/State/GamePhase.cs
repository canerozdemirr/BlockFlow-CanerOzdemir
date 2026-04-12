/// <summary>
/// High-level gameplay state. Read by the drag controller and timer so
/// they stop producing moves once a level has ended, and by UI popups so
/// they only show in terminal states. A full state machine with per-phase
/// classes is overkill at this scale; a flat enum + a tiny observable
/// service (<see cref="GameStateService"/>) does the same job.
/// </summary>
public enum GamePhase
{
    Loading,
    Playing,
    Paused,
    Won,
    Lost
}
