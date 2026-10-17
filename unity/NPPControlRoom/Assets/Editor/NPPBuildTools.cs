#if UNITY_EDITOR
using System.IO;
using UnityEditor;
using UnityEditor.Build;
using UnityEditor.Build.Reporting;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace NPP.EditorTools
{
    /// <summary>
    /// Автонастройка проекта (сцена, Build Settings, имя продукта) и сборка под Windows.
    /// Меню: NPP ▶ ...
    /// </summary>
    [InitializeOnLoad]
    public static class NPPBuildTools
    {
        const string ScenePath = "Assets/Scenes/Main.unity";
        const string BuildDir = "Build/Windows";

        static NPPBuildTools()
        {
            EditorApplication.delayCall += AutoSetup;
        }

        static void AutoSetup()
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode) return;
            if (!File.Exists(ScenePath)) CreateMainScene();
            EnsureBuildSettings();
            ApplyPlayerSettings();
        }

        [MenuItem("NPP/Подготовить проект (сцена + build settings)")]
        public static void Prepare()
        {
            if (!File.Exists(ScenePath)) CreateMainScene();
            EnsureBuildSettings();
            ApplyPlayerSettings();
            Debug.Log("[NPP] Проект подготовлен. Сцена: " + ScenePath);
        }

        static void CreateMainScene()
        {
            Directory.CreateDirectory("Assets/Scenes");
            Scene active = EditorSceneManager.GetActiveScene();
            if (string.IsNullOrEmpty(active.path) && active.rootCount == 0 &&
                !active.isDirty)
            {
                EditorSceneManager.SaveScene(active, ScenePath);
            }
            else
            {
                EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo();
                Scene s = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
                EditorSceneManager.SaveScene(s, ScenePath);
            }
            AssetDatabase.Refresh();
            Debug.Log("[NPP] Создана сцена " + ScenePath);
        }

        static void EnsureBuildSettings()
        {
            var scenes = EditorBuildSettings.scenes;
            foreach (var s in scenes)
                if (s.path == ScenePath) return;

            var list = new System.Collections.Generic.List<EditorBuildSettingsScene>(scenes);
            list.Insert(0, new EditorBuildSettingsScene(ScenePath, true));
            EditorBuildSettings.scenes = list.ToArray();
        }

        static void ApplyPlayerSettings()
        {
            PlayerSettings.companyName = "NPP Simulation";
            PlayerSettings.productName = "NPP Control Room";
            PlayerSettings.bundleVersion = "0.1.0";
            PlayerSettings.fullScreenMode = FullScreenMode.Windowed;
            PlayerSettings.defaultScreenWidth = 1600;
            PlayerSettings.defaultScreenHeight = 900;
            PlayerSettings.resizableWindow = true;
            PlayerSettings.runInBackground = true;
            PlayerSettings.colorSpace = ColorSpace.Linear;
        }

        [MenuItem("NPP/Собрать Windows x64")]
        public static void BuildWindows()
        {
            Prepare();

            string exe = Path.Combine(BuildDir, "NPPControlRoom.exe");
            Directory.CreateDirectory(BuildDir);

            var opts = new BuildPlayerOptions
            {
                scenes = new[] { ScenePath },
                locationPathName = exe,
                target = BuildTarget.StandaloneWindows64,
                options = BuildOptions.None
            };

            BuildReport report = BuildPipeline.BuildPlayer(opts);
            if (report.summary.result == BuildResult.Succeeded)
                Debug.Log("[NPP] Сборка успешна: " + exe + " (" + (report.summary.totalSize / (1024 * 1024)) + " МБ)");
            else
                Debug.LogError("[NPP] Сборка не удалась: " + report.summary.result);
        }
    }
}
#endif
