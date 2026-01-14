using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using UnityEngine;

namespace TestAccountCore;

public static class Netcode {
    public static void ExecuteNetcodePatcher(Assembly assembly) {
        var types = new List<Type>();

        try {
            types.AddRange(assembly.GetTypes());
        } catch (ReflectionTypeLoadException exception) {
            types.AddRange(exception.Types.Where(t => t != null));
            // Probably missing dependencies.
        }

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