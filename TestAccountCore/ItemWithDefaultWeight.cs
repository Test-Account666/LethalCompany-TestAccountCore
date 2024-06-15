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
}