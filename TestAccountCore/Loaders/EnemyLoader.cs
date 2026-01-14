using System;
using System.Collections.Generic;
using BepInEx.Configuration;
using Dawn;

namespace TestAccountCore.Loaders;

public static class EnemyLoader {
    internal static void RegisterAllEnemies(List<EnemyWithDefaultWeight> enemiesWithDefaultWeight, ConfigFile? configFile) {
        if (configFile is null) return;

        enemiesWithDefaultWeight.ForEach(enemy => RegisterEnemy(enemy, configFile));
    }

    private static void RegisterEnemy(EnemyWithDefaultWeight enemy, ConfigFile? configFile) {
        if (configFile is null) return;
        if (enemy.enemyType is null) throw new NullReferenceException("EnemyType cannot be null!");

        var enemyName = enemy.enemyType.enemyName;
        var section = $"{enemyName} - Enemy";

        var canItemSpawn = configFile.Bind(section, "1. Enabled", true,
            $"If false, {enemyName} will not be registered. This is different from a spawn weight of 0!");

        if (!canItemSpawn.Value) return;

        TestAccountCore.Logger.LogInfo($"Registering enemy {enemyName}...");

        var configMoonRarity = configFile.Bind(section, "2. Moon Spawn Weight",
            $"Vanilla:{enemy.defaultWeight}, Modded:{enemy.defaultWeight}",
            $"Defines the spawn weight per moon. e.g. Assurance:{enemy.defaultWeight}");

        var spawnRateByCustomLevelType = configMoonRarity.Value.ParseConfig(enemyName);

        var namespacedKey = NamespacedKey<DawnEnemyInfo>.From("testaccountcore", "enemy" + enemyName.ToLower());

        DawnLib.DefineEnemy(namespacedKey, enemy.enemyType, enemyBuilder => {
            enemyBuilder.CreateBestiaryNode(enemy.infoNode.displayText).CreateNameKeyword(enemy.keyWord.word);
            enemyBuilder.DefineInside(insideBuilder => { insideBuilder.SetWeights(SetWeights); });
        });

        DawnLib.RegisterNetworkPrefab(enemy.enemyType.enemyPrefab);
        enemy.connectedNetworkPrefabs.ForEach(DawnLib.RegisterNetworkPrefab);

        enemy.isRegistered = true;

        TestAccountCore.Logger.LogInfo($"Fully registered enemy {enemyName}!");
        return;

        void SetWeights(WeightTableBuilder<DawnMoonInfo> weightBuilder) {
            foreach (var (moon, weight) in spawnRateByCustomLevelType) {
                if (moon is null) continue;

                if (moon.Equals("all")) {
                    weightBuilder.SetGlobalWeight(weight);
                    continue;
                }

                var foundKey = NamespacedKey.ForceParse(moon, true);
                if (foundKey == null!) {
                    TestAccountCore.Logger.LogError($"Could not parse key {moon} for enemy {enemyName}");
                    continue;
                }

                if (foundKey is not NamespacedKey<DawnMoonInfo> moonKey) {
                    weightBuilder.AddTagWeight(foundKey, weight);
                    continue;
                }

                weightBuilder.AddWeight(moonKey, weight);
            }

            weightBuilder.Build();
        }
    }
}