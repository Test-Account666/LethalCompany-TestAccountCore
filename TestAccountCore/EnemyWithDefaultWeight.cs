using System.Collections.Generic;
using UnityEngine;

namespace TestAccountCore;

[CreateAssetMenu(menuName = "ScriptableObjects/EnemyWithDefaultWeight", order = 1)]
public class EnemyWithDefaultWeight : ScriptableObject {
    [SerializeField]
    [Tooltip("The enemy type to register.")]
    [Space(10f)]
    public EnemyType? enemyType;

    [SerializeField]
    [Tooltip("The default spawn weight.")]
    [Space(10f)]
    public int defaultWeight;

    [SerializeField]
    [Tooltip("The terminal info node.")]
    [Space(10f)]
    public TerminalNode infoNode = null!;

    [SerializeField]
    [Tooltip("The terminal keyword.")]
    [Space(10f)]
    public TerminalKeyword keyWord = null!;

    [SerializeField]
    [Tooltip("All network prefabs that are connected to this enemy.")]
    [Space(5F)]
    public List<GameObject> connectedNetworkPrefabs = [
    ];

    public bool isRegistered;
}