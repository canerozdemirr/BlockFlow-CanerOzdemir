/// <summary>
/// Named audio cues used throughout the gameplay loop. Keeping the enum
/// tight (no open-ended strings) means a typo in a bus listener becomes a
/// compile error, and every hookup has an obvious place to look.
///
/// Add new entries at the bottom — changing the numeric order would shift
/// any values designers have already serialized into an AudioLibrary asset.
/// </summary>
public enum SfxKey
{
    None = 0,
    DragStart,
    DragEnd,
    BlockStep,
    WallBump,
    BlockGround,
    IceReveal,
    LevelWon,
    LevelLost,
    UiClick
}
