using System;
using System.Collections;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.Serialization;
using Random = UnityEngine.Random;

namespace TestAccountCore;

[AddComponentMenu("TestAccount666/TestAccountCore/Material Variants")]
public class MaterialVariants : NetworkBehaviour {
    [Tooltip("The variants of the scrap.")]
    public Material[] materialVariants;

    [Space(5f)]
    [Tooltip("The mesh renderers to change the material of. This will use the first material in the array.")]
    public MeshRenderer[] meshRenderers;

    [FormerlySerializedAs("ChangeScanNodeText")]
    [Space(5f)]
    public bool changeScanNodeText;

    [Tooltip("The text to change to when the material is changed.")]
    public string[] scanNodeText;

    [Space(5f)]
    [Tooltip("The scan node properties to change the text of.")]
    public ScanNodeProperties scanNodeProperties;

    [Space(5f)]
    [Tooltip("The currently saved material variant.")]
    public int savedMaterialVariant = -1;

    public bool synced;

    public override void OnNetworkSpawn() => StartCoroutine(WaitAndSync());

    [ServerRpc(RequireOwnership = false)]
    private void SetRendererServerRpc() {
        savedMaterialVariant = savedMaterialVariant != -1
            ? Math.Clamp(savedMaterialVariant, 0, materialVariants.Length - 1)
            : Random.Range(0, materialVariants.Length);

        SetRendererClientRpc(savedMaterialVariant);
    }

    [ClientRpc]
    private void SetRendererClientRpc(int materialVariant) {
        synced = true;

        foreach (var renderer in meshRenderers) {
            renderer.material = materialVariants[materialVariant];

            if (!changeScanNodeText) continue;

            scanNodeProperties.headerText = scanNodeText[materialVariant];
        }
    }

    private IEnumerator WaitAndSync() {
        TestAccountCore.Logger.LogDebug("Starting WaitAndSync");
        yield return new WaitForSeconds(1);
        TestAccountCore.Logger.LogDebug("After wait");
        SetRendererServerRpc();
        TestAccountCore.Logger.LogDebug("RPC called");
        yield return new WaitForEndOfFrame();
        TestAccountCore.Logger.LogDebug("End of frame");
        if (synced) {
            TestAccountCore.Logger.LogDebug("Already synced, breaking out.");
            yield break;
        }

        TestAccountCore.Logger.LogDebug("Not yet synced, retrying...");
        yield return WaitAndSync();
    }
}