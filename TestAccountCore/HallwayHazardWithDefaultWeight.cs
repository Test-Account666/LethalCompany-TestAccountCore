using System.Collections.Generic;
using UnityEngine;

namespace TestAccountCore;

[CreateAssetMenu(menuName = "ScriptableObjects/HallwayHazardWithDefaultWeight", order = 1)]
public class HallwayHazardWithDefaultWeight : ScriptableObject {
    [SerializeField] [Tooltip("The hazard prefab to spawn.")] [Space(10f)]
    public GameObject? hazardPrefab;

    [SerializeField] [Tooltip("The spawn prefab used to spawn the hazard.")] [Space(10f)]
    public GameObject? spawnHazardPrefab;

    [SerializeField] [Tooltip("The hazard name.")] [Space(10f)]
    public string? hazardName;

    [SerializeField] [Tooltip("The default spawn weight of this hazard.")] [Space(5F)]
    public int defaultWeight;

    [SerializeField] [Tooltip("All network prefabs that are connected to this hazard.")] [Space(5F)]
    public List<GameObject> connectedNetworkPrefabs = [
    ];

    public bool isRegistered;
}