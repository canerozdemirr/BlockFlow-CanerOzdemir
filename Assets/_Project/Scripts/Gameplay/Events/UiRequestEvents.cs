/// <summary>
/// Fired when the HUD pause button is pressed. The pause popup subscribes
/// and shows itself. Decouples the HUD from the concrete popup view.
/// </summary>
public readonly struct PauseRequestedEvent { }

/// <summary>
/// View-side intent: restart the currently loaded level. Handled by
/// <c>GameFlowController</c> which owns the <c>LevelRunner</c>.
/// </summary>
public readonly struct LevelRestartRequestedEvent { }

/// <summary>
/// View-side intent: advance progression one step. Handled by
/// <c>GameFlowController</c> which owns the <c>LevelProgressionService</c>.
/// </summary>
public readonly struct LevelAdvanceRequestedEvent { }

/// <summary>
/// View-side intent: load whatever the progression service currently points
/// at. Typically fired after the win popup finishes its "Next" animation.
/// </summary>
public readonly struct LevelLoadCurrentRequestedEvent { }
