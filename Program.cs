using DependencyGraphVisualization.Models;
using System.Xml;
using System.Xml.Serialization;


namespace DependencyGraphVisualization
{
    internal class Program
    {
        static void Main(string[] args)
        {
            Dictionary<string, string?> dataPair = new Dictionary<string, string?>();

            string filePath = Path.Combine(GetProjectRoot(), "XML_Files", "TestDependencyFile.xml");

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

    }
}
