using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Xml.Linq;
using DependencyGraphVisualization.Export;
using DependencyGraphVisualization.Services;

namespace DependencyGraphVisualization
{
    internal class Program
    {
        static void Main(string[] args)
        {
            Console.OutputEncoding = System.Text.Encoding.UTF8;
            Console.WriteLine("=== Dependency Graph Visualizer (Вариант 27) ===");

            // Определяем имя конфигурационного файла
            string configName = args.Length > 0 ? args[0] : "config";
            string configPath;

            if (File.Exists($"XML_Files/{configName}.xml"))
                configPath = $"XML_Files/{configName}.xml";
            else if (File.Exists($"{configName}.xml"))
                configPath = $"{configName}.xml";
            else
                configPath = "config.xml";

            if (!File.Exists(configPath))
            {
                Console.WriteLine($"Не найден файл конфигурации: {configPath}");
                Console.WriteLine("Пример запуска: dotnet run ExpressApp");
                return;
            }

            Console.WriteLine($"Используется конфигурация: {configPath}");

            // Чтение XML
            var xdoc = XDocument.Load(configPath);

            var cfg = new
            {
                Package = xdoc.Root?.Element("Name")?.Value ?? "",
                RepositoryUrl = xdoc.Root?.Element("Url")?.Value ?? "",
                UseTestRepo = (xdoc.Root?.Element("TestMode")?.Value ?? "false")
                    .Equals("true", StringComparison.OrdinalIgnoreCase),
                Version = xdoc.Root?.Element("Version")?.Value ?? "",
                TestFile = xdoc.Root?.Element("TestFile")?.Value ?? "",
                ExcludeSubstring = xdoc.Root?.Element("ExcludeSubstring")?.Value ?? "",
                MaxDepth = int.TryParse(xdoc.Root?.Element("MaxDepth")?.Value, out var md) ? md : int.MaxValue,
                PrintLoadOrder = (xdoc.Root?.Element("PrintLoadOrder")?.Value ?? "false")
                    .Equals("true", StringComparison.OrdinalIgnoreCase),
                PrintAsciiTree = (xdoc.Root?.Element("PrintAsciiTree")?.Value ?? "false")
                    .Equals("true", StringComparison.OrdinalIgnoreCase),
                PlantUmlOut = xdoc.Root?.Element("PlantUmlOut")?.Value ?? "",
                SvgOut = xdoc.Root?.Element("SvgOut")?.Value ?? ""
            };

            if (string.IsNullOrWhiteSpace(cfg.Package))
            {
                Console.WriteLine("Ошибка: в конфигурации не указано имя пакета (<Name>).");
                return;
            }

            Console.WriteLine($"Пакет: {cfg.Package}");
            Console.WriteLine($"Репозиторий: {cfg.RepositoryUrl}");
            Console.WriteLine();

            // Построение тестового графа зависимостей (заглушка)
            var graph = BuildGraphStub(cfg.Package);

            Console.WriteLine($"Граф зависимостей сформирован. Количество узлов: {graph.Count}");

            Func<string, bool>? excludePredicate = null;
            if (!string.IsNullOrWhiteSpace(cfg.ExcludeSubstring))
                excludePredicate = (s) => s.Contains(cfg.ExcludeSubstring, StringComparison.OrdinalIgnoreCase);

            // Этап 4. Дополнительные операции
            if (cfg.PrintLoadOrder)
            {
                Console.WriteLine("\n=== Этап 4: Порядок загрузки зависимостей ===");
                var service = new LoadOrderService(graph);
                var (order, cycles) = service.GetLoadOrder(cfg.Package, cfg.MaxDepth, excludePredicate);

                foreach (var p in order)
                    Console.WriteLine(" • " + p);

                if (cycles.Count > 0)
                {
                    Console.WriteLine("\nОбнаружены циклы зависимостей:");
                    foreach (var cyc in cycles)
                        Console.WriteLine("   - " + string.Join(" -> ", cyc) + " -> " + cyc[0]);
                }

                Directory.CreateDirectory("out");
                File.WriteAllLines("out/load_order.txt", order);
                Console.WriteLine("Результат сохранён в out/load_order.txt");
            }

            // Этап 5. Визуализация
            if (cfg.PrintAsciiTree)
            {
                Console.WriteLine("\n=== Этап 5: ASCII-дерево зависимостей ===");
                AsciiTreePrinter.Print(cfg.Package, graph, cfg.MaxDepth, excludePredicate);
            }

            if (!string.IsNullOrWhiteSpace(cfg.PlantUmlOut))
            {
                Console.WriteLine("\n=== Этап 5: Генерация PlantUML ===");
                var puml = PlantUmlExporter.ToPlantUml(graph, cfg.Package);
                PlantUmlExporter.SavePuml(cfg.PlantUmlOut, puml);
                Console.WriteLine($"Файл PlantUML сохранён: {cfg.PlantUmlOut}");

                if (!string.IsNullOrWhiteSpace(cfg.SvgOut))
                {
                    if (PlantUmlExporter.TryRenderSvg(cfg.PlantUmlOut, cfg.SvgOut, out var err))
                        Console.WriteLine($"SVG-файл сохранён: {cfg.SvgOut}");
                    else
                        Console.WriteLine($"Ошибка генерации SVG через PlantUML: {err}");
                }
            }

            Console.WriteLine("\n=== Выполнение завершено ===");
        }

        private static Dictionary<string, List<string>> BuildGraphStub(string root)
        {
            return new Dictionary<string, List<string>>
            {
                [root] = new List<string> { "musl", "libc-utils", "busybox" },
                ["musl"] = new List<string> { "libgcc" },
                ["busybox"] = new List<string> { "libc-utils" },
                ["libc-utils"] = new List<string>(),
                ["libgcc"] = new List<string>()
            };
        }
    }
}
