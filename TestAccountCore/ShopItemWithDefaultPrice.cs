using System.Collections.Generic;
using UnityEngine;

namespace TestAccountCore;

[CreateAssetMenu(menuName = "ScriptableObjects/ShopItemWithDefaultPrice", order = 1)]
public class ShopItemWithDefaultPrice : ScriptableObject {
    [SerializeField]
    [Tooltip("The item properties.")]
    [Space(10f)]
    public Item? item;

    [SerializeField]
    [Tooltip("The default price of this item.")]
    [Space(5F)]
    public int defaultPrice;

    [SerializeField]
    [Tooltip("All network prefabs that are connected to this item.")]
    [Space(5F)]
    public List<GameObject> connectedNetworkPrefabs = [
    ];

    public bool isRegistered;
}