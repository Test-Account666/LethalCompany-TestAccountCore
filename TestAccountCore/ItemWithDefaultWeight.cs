using System.Collections.Generic;
using UnityEngine;

namespace TestAccountCore;

[CreateAssetMenu(menuName = "ScriptableObjects/ItemWithDefaultWeight", order = 1)]
public class ItemWithDefaultWeight : ScriptableObject {
    [SerializeField]
    [Tooltip("The item properties.")]
    [Space(10f)]
    public Item? item;

    [SerializeField]
    [Tooltip("The default spawn weight of this item.")]
    [Space(5F)]
    public int defaultWeight;

    [SerializeField]
    [Tooltip("All network prefabs that are connected to this item.")]
    [Space(5F)]
    public List<GameObject> connectedNetworkPrefabs = [];

    [SerializeField]
    [Tooltip("Alternative item properties, which are registered without any spawn weight.")]
    [Space(10f)]
    public List<Item> alternativeItems = [];

    public bool isRegistered;
}