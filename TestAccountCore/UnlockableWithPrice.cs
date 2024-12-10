using LethalLib.Modules;
using UnityEngine;

namespace TestAccountCore;

[CreateAssetMenu(menuName = "ScriptableObjects/UnlockableWithPrice", order = 1)]
public class UnlockableWithPrice : ScriptableObject {
    [SerializeField]
    [Tooltip("The unlockable type.")]
    [Space(10f)]
    public StoreType storeType;

    [SerializeField]
    [Tooltip("The unlockable name.")]
    [Space(10f)]
    public string? unlockableName;

    [SerializeField]
    [Tooltip("The unlockable price.")]
    [Space(10f)]
    public int price;

    [SerializeField]
    [Tooltip("The unlockable prefab.")]
    [Space(10f)]
    public GameObject? spawnPrefab;
}