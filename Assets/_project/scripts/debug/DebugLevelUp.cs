using UnityEngine;
using UnityEngine.InputSystem;

/// <summary>
/// Debug tool: Ctrl + Shift → instantly grant enough XP to reach level 20,
/// triggering all level-up card screens in sequence.
/// Attach to any persistent GameObject in the scene.
/// </summary>
public sealed class DebugLevelUp : MonoBehaviour
{
    [SerializeField] private int targetLevel = 20;

    private void Update()
    {
        if (Keyboard.current == null) return;

        bool ctrl  = Keyboard.current.leftCtrlKey.isPressed || Keyboard.current.rightCtrlKey.isPressed;
        bool shift = Keyboard.current.leftShiftKey.wasPressedThisFrame || Keyboard.current.rightShiftKey.wasPressedThisFrame;

        if (ctrl && shift)
            GrantLevels();
    }

    private void GrantLevels()
    {
        var prog = PlayerProgression.Instance;
        if (prog == null) { Debug.LogWarning("[DebugLevelUp] PlayerProgression not found."); return; }

        int levelsNeeded = targetLevel - prog.CurrentLevel;
        if (levelsNeeded <= 0) { Debug.Log($"[DebugLevelUp] Already at level {prog.CurrentLevel}."); return; }

        // Add exactly enough XP to cross one threshold at a time using the
        // actual runtime values — no hardcoded curve constants needed.
        while (prog.CurrentLevel < targetLevel)
        {
            int needed = prog.ExpToNextLevel - prog.CurrentExp;
            prog.AddExp(needed);
        }
        Debug.Log($"[DebugLevelUp] Granted {levelsNeeded} level(s) → now level {prog.CurrentLevel}.");
    }
}
