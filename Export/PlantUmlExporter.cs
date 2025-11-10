using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Text;

namespace DependencyGraphVisualization.Export
{
    public static class PlantUmlExporter
    {
        public static string ToPlantUml(IDictionary<string, List<string>> graph, string? root = null)
        {

            var sb = new StringBuilder();
            sb.AppendLine("@startuml");
            sb.AppendLine("skinparam dpi 160");
            sb.AppendLine("skinparam linetype ortho");
            sb.AppendLine("skinparam ArrowColor #777777");
            sb.AppendLine("skinparam ArrowThickness 1");

            var nodes = new HashSet<string>(graph.Keys);
            foreach (var kvp in graph)
                foreach (var dep in kvp.Value)
                    nodes.Add(dep);


            foreach (var n in nodes)
            {
                if (root != null && n.Equals(root, StringComparison.OrdinalIgnoreCase))
                    sb.AppendLine($"node \"{n}\" as {San(n)} #LightBlue");
                else
                    sb.AppendLine($"node \"{n}\" as {San(n)}");
            }

            foreach (var (pkg, deps) in graph)
                foreach (var d in deps)
                    sb.AppendLine($"{San(pkg)} --> {San(d)}");

            sb.AppendLine("@enduml");
            return sb.ToString();
        }

        public static void SavePuml(string path, string puml)
        {
            Directory.CreateDirectory(Path.GetDirectoryName(Path.GetFullPath(path))!);
            File.WriteAllText(path, puml, new UTF8Encoding(false));
        }

        public static bool TryRenderSvg(string pumlPath, string svgPath, out string? error)
        {
            try
            {
                var psi = new ProcessStartInfo
                {
                    FileName = "plantuml",
                    Arguments = $"-tsvg \"{pumlPath}\" -o \"{Path.GetDirectoryName(Path.GetFullPath(svgPath))}\"",
                    RedirectStandardError = true,
                    RedirectStandardOutput = true,
                    UseShellExecute = false,
                    CreateNoWindow = true
                };
                using var p = Process.Start(psi)!;
                p.WaitForExit();
                if (p.ExitCode != 0)
                {
                    error = (p.StandardError.ReadToEnd() + "\n" + p.StandardOutput.ReadToEnd()).Trim();
                    return false;
                }
                error = null;
                return true;
            }
            catch (Exception ex)
            {
                error = ex.Message;
                return false;
            }
        }

        private static string San(string name) =>
            "N_" + string.Concat(name.Replace('-', '_').Replace('.', '_').Replace('/', '_'));
    }
}
