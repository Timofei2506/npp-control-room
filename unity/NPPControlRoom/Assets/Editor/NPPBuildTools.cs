#if UNITY_EDITOR
using System;
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
            // Никаких диалогов: в batchmode (GitHub Actions) вопрос «сохранить сцену?»
            // означает зависшую сборку. Сцена пустая — всё содержимое строит Bootstrap.
            Scene s = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            EditorSceneManager.SaveScene(s, ScenePath);
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
            // Mono2x собирается на Linux-раннере GitHub Actions без нативных тулчейнов.
            PlayerSettings.SetScriptingBackend(NamedBuildTarget.Standalone, ScriptingImplementation.Mono2x);
        }

        /// <summary>
        /// Сборка для GitHub Actions (game-ci/unity-builder, buildMethod):
        ///   -buildPath <папка>  — куда положить билд (по умолчанию build/StandaloneWindows64).
        /// Бросает исключение при ошибке, чтобы шаг Actions покраснел.
        /// </summary>
        public static void BuildCI()
        {
            // game-ci/unity-builder передаёт -customBuildPath — уже полный путь к .exe.
            // Если метода вызвали вручную — берём -buildPath или значение по умолчанию.
            string exe = ArgValue("-customBuildPath", null);
            if (string.IsNullOrEmpty(exe))
            {
                string dir = ArgValue("-buildPath", "Build/Windows");
                exe = Path.Combine(dir, "NPPControlRoom.exe");
            }
            if (!exe.EndsWith(".exe", StringComparison.OrdinalIgnoreCase)) exe += ".exe";
            Directory.CreateDirectory(Path.GetDirectoryName(exe));
            Prepare();
            var opts = new BuildPlayerOptions
            {
                scenes = new[] { ScenePath },
                locationPathName = exe,
                target = BuildTarget.StandaloneWindows64,
                options = BuildOptions.None
            };

            BuildReport report = BuildPipeline.BuildPlayer(opts);
            if (report.summary.result != BuildResult.Succeeded)
                throw new Exception("[NPP] Сборка не удалась: " + report.summary.result);

            Debug.Log("[NPP] CI build OK: " + exe + " (" + (report.summary.totalSize / (1024 * 1024)) + " МБ)");

            // game-ci CLI ищет в логе ровно эту строку (UnityBuildValidation.validateBuild).
            // Unity 6 печатает "Build succeeded" без "!", из-за чего валидатор ложно валит
            // успешную сборку. Печатаем маркер сами — только при BuildResult.Succeeded.
            Debug.Log("Build succeeded!");
        }

        static string ArgValue(string name, string fallback)
        {
            string[] args = Environment.GetCommandLineArgs();
            for (int i = 0; i < args.Length - 1; i++)
                if (args[i] == name) return args[i + 1];
            return fallback;
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

    /// <summary>
    /// Гарантирует, что перед ЛЮБОЙ сборкой (GitHub Actions, Unity Build Automation,
    /// локальный билд) есть сцена и она включена в Build Settings — независимо от того,
    /// успел ли отработать [InitializeOnLoad] AutoSetup.
    /// </summary>
    public class NPPPreBuild : IPreprocessBuildWithReport
    {
        public int callbackOrder { get { return -1000; } }

        public void OnPreprocessBuild(BuildReport report)
        {
            Debug.Log("[NPP] Pre-build: подготовка проекта для " + report.summary.platform);
            NPPBuildTools.Prepare();
        }
    }
}
#endif
