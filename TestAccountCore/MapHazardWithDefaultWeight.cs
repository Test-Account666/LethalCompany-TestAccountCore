using UnityEngine;

namespace TestAccountCore;

[CreateAssetMenu(menuName = "ScriptableObjects/MapHazardWithDefaultWeight", order = 1)]
public class MapHazardWithDefaultWeight : ScriptableObject {
    [SerializeField]
    [Tooltip("The hazard to spawn.")]
    [Space(10f)]
    public GameObject? spawnableMapObject;

    [SerializeField]
    [Tooltip("The amount to spawn.")]
    [Space(10f)]
    public int amount;

    [SerializeField]
    [Tooltip("The amount to spawn.")]
    [Space(10f)]
    public string? spawnCurve;

    [SerializeField]
    [Tooltip("The hazard name.")]
    [Space(10f)]
    public string? hazardName;

    [SerializeField]
    [Space(10f)]
    public bool spawnFacingAwayFromWall;

    [SerializeField]
    [Space(10f)]
    public bool spawnFacingWall;

    [SerializeField]
    [Space(10f)]
    public bool spawnWithBackToWall;

    [SerializeField]
    [Space(10f)]
    public bool spawnWithBackFlushAgainstWall;

    [SerializeField]
    [Space(10f)]
    public bool requireDistanceBetweenSpawns;

    [SerializeField]
    [Space(10f)]
    public bool disallowSpawningNearEntrances;
}