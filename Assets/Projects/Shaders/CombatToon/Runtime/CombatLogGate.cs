using UnityEngine;

/// <summary>
/// Turns Debug.Log off in release (non-development) builds. The combat code logs every
/// attack phase / state change; in a player build each call still formats strings and
/// captures stack traces. Editor and Development builds keep full logging.
/// </summary>
internal static class CombatLogGate
{
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    private static void Init()
    {
#if !UNITY_EDITOR
        if (!Debug.isDebugBuild)
        {
            Debug.unityLogger.filterLogType = LogType.Warning; // keep warnings/errors only
        }
#endif
    }
}
