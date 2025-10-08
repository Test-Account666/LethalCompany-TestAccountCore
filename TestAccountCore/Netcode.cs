using System;
using System.Reflection;
using UnityEngine;

namespace TestAccountCore;

public static class Netcode {
    public static void ExecuteNetcodePatcher(Assembly assembly) {
        var types = assembly.GetTypes();
        foreach (var type in types) {
            try {
                var methods = type.GetMethods(BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.Static);
                foreach (var method in methods) {
                    var attributes = method.GetCustomAttributes(typeof(RuntimeInitializeOnLoadMethodAttribute), false);
                    if (attributes.Length <= 0) continue;

                    method.Invoke(null, null);
                }
            } catch (Exception) {
                // Probably missing dependencies.
            }
        }
    }
}