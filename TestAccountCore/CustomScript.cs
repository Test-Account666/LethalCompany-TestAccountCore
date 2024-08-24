using BepInEx.Configuration;
using UnityEngine;

namespace TestAccountCore;

public abstract class CustomScript : ScriptableObject {
    public abstract void Initialize(ConfigFile? configFile);
}