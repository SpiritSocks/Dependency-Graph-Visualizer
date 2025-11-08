using DependencyGraphVisualization.Models;
using System.Text.Json;
using System.Xml;
using System.Xml.Serialization;


namespace DependencyGraphVisualization
{
    internal class Program
    {
        static async Task Main(string[] args)
        {
            List<string> xmlFiles = GetAllXmlFiles();

            foreach ( var filePath in xmlFiles )
            {
                Console.WriteLine($"\nProcessing file: {Path.GetFileName(filePath)}");

                Dictionary<string, string?> dataPair = new();

                try
                {
                    using (XmlReader reader = XmlReader.Create(filePath))
                    {
                        XmlSerializer serializer = new XmlSerializer(typeof(DependecyData));

                        var data = (DependecyData)serializer.Deserialize(reader)!;

                        dataPair.Add("Name", data.Name);
                        dataPair.Add("URL", data.Url);
                        dataPair.Add("Test-mode", data.TestMode.ToString());
                        dataPair.Add("Version", data.Version);
                        dataPair.Add("Test-file-name", data.TestFile);
                    }
                }
                catch (XmlException ex)
                {
                    Console.WriteLine($"XML Error: {ex.Message}");
                    Console.WriteLine($"Line: {ex.LineNumber}, Position: {ex.LinePosition}");
                }
                catch (InvalidOperationException ex)
                {
                    Console.WriteLine($"Deserialization error: {ex.Message}");
                    if (ex.InnerException != null)
                    {
                        Console.WriteLine($"Inner exception: {ex.InnerException.Message}");
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Error: {ex.Message}");
                }


                foreach (var pair in dataPair)
                {
                    if (string.IsNullOrEmpty(pair.Value))
                    {
                        throw new FormatException("Value cannot be null or empty");
                    }

                    Console.WriteLine($"{pair.Key}: {pair.Value}");
                }

                Console.WriteLine();


                // === Этап 2 === //

                string? reportUrl = dataPair["URL"];
                string? version = dataPair["Version"];

                if (string.IsNullOrWhiteSpace(reportUrl))
                {
                    Console.WriteLine("URL not specified");
                    return;
                }

                string packageName = reportUrl.TrimEnd('/').Split('/').Last();
                version ??= "latest";

                try
                {
                    var deps = await FetchDependencyAsync(packageName, version);

                    if (deps.Count == 0)
                    {
                        Console.WriteLine("Package has not dependencies");
                    }
                    else
                    {
                        foreach (var dep in deps)
                        {
                            Console.WriteLine($" - {dep}");
                        }
                    }
                }
                catch (Exception)
                {
                    Console.WriteLine("Error!");
                    continue;
                }


                // === Этап 3 === //
                bool testMode = bool.Parse(dataPair["Test-mode"]!);
                string? testFile = dataPair["Test-file-name"];
                string filter = dataPair.ContainsKey("Filter") ? dataPair["Filter"] ?? "" : "";

                if (testMode)
                {
                    Console.WriteLine("\n=== Test mode enabled ===");

                    if (string.IsNullOrWhiteSpace(testFile))
                    {
                        Console.WriteLine("Test file name not specified in XML.");
                        continue;
                    }

                    string testFilePath = Path.Combine(GetProjectRoot(), "TestFiles", testFile);

                    if (!File.Exists(testFilePath))
                    {
                        Console.WriteLine($"Test file '{testFile}' not found at {testFilePath}");
                        continue;
                    }

                    Console.WriteLine($"Loading dependencies from: {testFilePath}");
                    var graph = LoadDependencies(testFilePath);

                    Console.WriteLine("\nDFS traversal (iterative):");
                    DFS_Iterative(dataPair["Name"]!, graph, filter);
                }
                else
                {
                    Console.WriteLine("\nTest mode is OFF — skipping stage 3.");
                }

            }
        }

        public static List<string> GetAllXmlFiles()
        {
            string xmlDir = Path.Combine(GetProjectRoot(), "XML_Files");

            if (!Directory.Exists(xmlDir))
            {
                Console.WriteLine("Directory not found");
                return new List<string>();
            }

            var xmlFiles = Directory.GetFiles(xmlDir, "*.xml").ToList();

            Console.WriteLine("Found XML files: ");

            foreach (var file in xmlFiles)
            {
                Console.WriteLine($" - {Path.GetFileName(file)}");
            }

            return xmlFiles;
        }


        public static string GetProjectRoot()
        {
            string currentDir = Directory.GetCurrentDirectory();
            DirectoryInfo directory = new DirectoryInfo(currentDir);

            while (directory != null && !directory.GetFiles("*.csproj").Any())
            {
                directory = directory.Parent!;
            }

            return directory?.FullName ?? currentDir;
        }

        public static async Task<List<string>> FetchDependencyAsync(string packageName, string version)
        {
            using var client = new HttpClient();
            var url = $"https://registry.npmjs.org/{packageName}/{version}";

            var response = await client.GetStringAsync(url);
            using var jsonDoc = JsonDocument.Parse(response);

            var deps = new List<string>();
            if (jsonDoc.RootElement.TryGetProperty("dependencies", out var depsProp))
            {
                Console.WriteLine("Dependencies: ");
                foreach (var dep in depsProp.EnumerateObject())
                {
                    deps.Add(dep.Name);
                }
            }

            return deps;
        }

        public static Dictionary<string, List<string>> LoadDependencies (string path)
        {
            var dependencies = new Dictionary<string, List<string>>();
            foreach (var line in File.ReadLines (path))
            {

                if (string.IsNullOrWhiteSpace(line) || !line.Contains(":")) continue;

                var parts = line.Split(':', 2);
                var package = parts[0].Trim();
                var deps = parts[1].Split(' ', StringSplitOptions.RemoveEmptyEntries)
                    .Select(x => x.Trim())
                    .ToList();

                dependencies[package] = deps;
            }

            return dependencies;
        }


        public static void DFS_Iterative(string startPackage, Dictionary<string, List<string>> graph, string filter)
        {
            var visited = new HashSet<string>();
            var stack = new Stack<string>();
            stack.Push(startPackage);

            while (stack.Count > 0)
            {
                var current = stack.Pop();

                // фильтрация по подстроке
                if (current.Contains(filter, StringComparison.OrdinalIgnoreCase))
                    continue;

                if (!visited.Add(current))
                {
                    Console.WriteLine($"[!] Цикл обнаружен у {current}");
                    continue;
                }

                Console.WriteLine(current);

                if (graph.TryGetValue(current, out var deps))
                {
                    foreach (var dep in deps)
                    {
                        if (!visited.Contains(dep))
                            stack.Push(dep);
                    }
                }
            }
        }

    }
}
