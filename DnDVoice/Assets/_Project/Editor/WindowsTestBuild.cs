using System;
using System.IO;
using System.IO.Compression;
using System.Linq;
using UnityEditor;
using UnityEditor.Build.Reporting;
using UnityEngine;

namespace DndProximityVoice.Editor
{
    public static class WindowsReleaseBuild
    {
        private const string MenuPath = "D&D Proximity Voice/Build Windows V2 Preview";
        private const string ExecutableName = "DnD Proximity Voice.exe";

        [MenuItem(MenuPath, priority = 10)]
        public static void Build()
        {
            try
            {
                var scenes = EditorBuildSettings.scenes
                    .Where(scene => scene.enabled)
                    .Select(scene => scene.path)
                    .ToArray();

                if (scenes.Length == 0)
                {
                    EditorUtility.DisplayDialog(
                        "D&D Proximity Voice",
                        "Nessuna scena è abilitata nelle Build Settings.",
                        "OK");
                    return;
                }

                var buildName = "DnDProximityVoice-Windows-V2-PREVIEW";
                var projectRoot = Path.GetFullPath(Path.Combine(Application.dataPath, ".."));
                var buildsRoot = Path.Combine(projectRoot, "Builds");
                var buildDirectory = Path.Combine(buildsRoot, buildName);
                var executablePath = Path.Combine(buildDirectory, ExecutableName);
                var zipPath = Path.Combine(buildsRoot, buildName + ".zip");

                Directory.CreateDirectory(buildDirectory);
                var report = BuildPipeline.BuildPlayer(new BuildPlayerOptions
                {
                    scenes = scenes,
                    locationPathName = executablePath,
                    target = BuildTarget.StandaloneWindows64,
                    targetGroup = BuildTargetGroup.Standalone,
                    options = BuildOptions.None
                });

                if (report.summary.result != BuildResult.Succeeded)
                {
                    EditorUtility.DisplayDialog(
                        "Build non riuscita",
                        $"Unity ha interrotto la build con risultato: {report.summary.result}.\n" +
                        "Controlla la Console per i dettagli.",
                        "OK");
                    return;
                }

                WriteReleaseInstructions(buildDirectory);
                CreateZip(buildDirectory, zipPath);

                Debug.Log(
                    $"Build Windows V2 Preview completata: {zipPath} " +
                    $"({report.summary.totalSize / (1024f * 1024f):0.0} MB non compressi).");
                EditorUtility.RevealInFinder(zipPath);
                EditorUtility.DisplayDialog(
                    "Build pronta",
                    "Il file ZIP è pronto per essere inviato al tuo amico.\n\n" + zipPath,
                    "OK");
            }
            catch (Exception exception)
            {
                Debug.LogException(exception);
                EditorUtility.DisplayDialog(
                    "Build non riuscita",
                    "Si è verificato un errore durante la preparazione della build. " +
                    "Controlla la Console di Unity.",
                    "OK");
            }
        }

        [MenuItem(MenuPath, validate = true)]
        private static bool ValidateBuild()
        {
            return !EditorApplication.isPlayingOrWillChangePlaymode &&
                   !EditorApplication.isCompiling &&
                   !BuildPipeline.isBuildingPlayer;
        }

        private static void WriteReleaseInstructions(string buildDirectory)
        {
            const string instructions =
                "D&D PROXIMITY VOICE - V2 PREVIEW\r\n" +
                "===================================\r\n\r\n" +
                "1. Estrai completamente il file ZIP in una cartella.\r\n" +
                "2. Avvia 'DnD Proximity Voice.exe'.\r\n" +
                "3. Se Windows SmartScreen compare, usa 'Ulteriori informazioni' e poi " +
                "'Esegui comunque'. L'applicazione non è ancora firmata digitalmente.\r\n" +
                "4. Accedi con il tuo account Discord.\r\n" +
                "5. Scegli TAVOLO 2D. Il World Builder 3D non è ancora disponibile.\r\n" +
                "6. Inserisci il codice sessione ricevuto dal Dungeon Master e premi ENTRA.\r\n" +
                "7. Premi ATTIVA VOCE e consenti l'accesso al microfono.\r\n" +
                "8. Controlla che in basso compaia 'Mappa sincronizzata'.\r\n" +
                "9. Usa cuffie o auricolari per evitare eco e ritorni audio.\r\n\r\n" +
                "Questa è una preview V2 e non sostituisce la Build 1.0 stabile.\r\n" +
                "Non spostare soltanto il file EXE: deve restare insieme alla cartella " +
                "'DnD Proximity Voice_Data' e agli altri file estratti.\r\n";

            File.WriteAllText(Path.Combine(buildDirectory, "LEGGIMI - V2 PREVIEW.txt"), instructions);
        }

        private static void CreateZip(string sourceDirectory, string zipPath)
        {
            if (File.Exists(zipPath))
            {
                File.Delete(zipPath);
            }

            var sourceRoot = Path.GetFullPath(sourceDirectory)
                .TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar) +
                Path.DirectorySeparatorChar;

            using (var zipStream = new FileStream(zipPath, FileMode.CreateNew, FileAccess.Write))
            using (var archive = new ZipArchive(zipStream, ZipArchiveMode.Create))
            {
                foreach (var filePath in Directory.GetFiles(sourceRoot, "*", SearchOption.AllDirectories))
                {
                    var entryName = filePath.Substring(sourceRoot.Length)
                        .Replace(Path.DirectorySeparatorChar, '/');
                    var entry = archive.CreateEntry(
                        entryName,
                        System.IO.Compression.CompressionLevel.Optimal);
                    using (var input = new FileStream(filePath, FileMode.Open, FileAccess.Read, FileShare.Read))
                    using (var output = entry.Open())
                    {
                        input.CopyTo(output);
                    }
                }
            }
        }
    }
}
