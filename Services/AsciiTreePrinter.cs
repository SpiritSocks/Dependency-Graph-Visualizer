using System;
using System.Collections.Generic;

namespace DependencyGraphVisualization.Services
{

    public static class AsciiTreePrinter
    {
        public static void Print(string root,
            IDictionary<string, List<string>> graph,
            int maxDepth = int.MaxValue,
            Func<string, bool>? excludePredicate = null)
        {
            void Walk(string node, string prefix, bool last, int depth, HashSet<string> path)
            {
                if (depth > maxDepth) return;
                if (excludePredicate?.Invoke(node) == true) return;

                Console.Write(prefix);
                Console.Write(last ? "└─ " : "├─ ");
                if (path.Contains(node))
                {
                    Console.WriteLine($"{node}  (cycle)");
                    return;
                }
                Console.WriteLine(node);

                path.Add(node);
                if (!graph.TryGetValue(node, out var children) || children.Count == 0)
                {
                    path.Remove(node);
                    return;
                }

                for (int i = 0; i < children.Count; i++)
                {
                    var child = children[i];
                    var isLast = i == children.Count - 1;
                    Walk(child, prefix + (last ? "   " : "│  "), isLast, depth + 1, path);
                }
                path.Remove(node);
            }

            Console.WriteLine(root);
            if (graph.TryGetValue(root, out var kids))
            {
                for (int i = 0; i < kids.Count; i++)
                {
                    Walk(kids[i], "", i == kids.Count - 1, 1, new HashSet<string> { root });
                }
            }
        }
    }
}