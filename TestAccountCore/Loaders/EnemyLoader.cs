using System;
using System.Collections.Generic;
using BepInEx.Configuration;
using LethalLib.Modules;

namespace TestAccountCore.Loaders;

public static class EnemyLoader {
    internal static void RegisterAllEnemies(List<EnemyWithDefaultWeight> enemiesWithDefaultWeight, ConfigFile? configFile) {
        if (configFile is null) return;

        enemiesWithDefaultWeight.ForEach(enemy => RegisterEnemy(enemy, configFile));
    }

    private static void RegisterEnemy(EnemyWithDefaultWeight enemy, ConfigFile? configFile) {
        if (configFile is null) return;

        if (enemy.enemyType is null) throw new NullReferenceException("EnemyType cannot be null!");

        var canItemSpawn = configFile.Bind($"{enemy.enemyType.enemyName}", "1. Enabled", true,
                                           $"If false, {enemy.enemyType.enemyName} will not be registered. This is different from a spawn weight of 0!");

        if (!canItemSpawn.Value) return;

        TestAccountCore.Logger.LogInfo($"Registering enemy {enemy.enemyType.enemyName}...");

        var configMoonRarity = configFile.Bind($"{enemy.enemyType.enemyName}", "2. Moon Spawn Weight",
                                               $"Vanilla:{enemy.defaultWeight}, Modded:{enemy.defaultWeight}",
                                               $"Defines the spawn weight per moon. e.g. Assurance:{enemy.defaultWeight}");

        var parsedConfig = configMoonRarity.Value.ParseConfig(enemy.enemyType.enemyName);

        foreach (var networkPrefab in enemy.connectedNetworkPrefabs) NetworkPrefabs.RegisterNetworkPrefab(networkPrefab);

        NetworkPrefabs.RegisterNetworkPrefab(enemy.enemyType.enemyPrefab);

        Enemies.RegisterEnemy(enemy.enemyType, parsedConfig.spawnRateByLevelType, parsedConfig.spawnRateByCustomLevelType, enemy.infoNode, enemy.keyWord);

        enemy.isRegistered = true;

        TestAccountCore.Logger.LogInfo($"Fully registered enemy {enemy.enemyType.enemyName}!");
    }
}