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
                        dataPair.Add("Output-file-name", data.OutputFile);
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

    }
}
