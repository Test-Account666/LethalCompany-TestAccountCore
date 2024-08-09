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
    [Tooltip("The hazard namde.")]
    [Space(10f)]
    public string? hazardName;
}