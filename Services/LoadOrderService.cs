using System;
using System.Collections.Generic;

namespace DependencyGraphVisualization.Services
{

    public class LoadOrderService
    {
        private readonly IDictionary<string, List<string>> _graph;

        public LoadOrderService(IDictionary<string, List<string>> graph)
        {
            _graph = graph;
        }

        public (List<string> order, List<List<string>> cycles) GetLoadOrder(
            string root,
            int maxDepth = int.MaxValue,
            Func<string, bool>? excludePredicate = null)
        {
            var visited = new Dictionary<string, int>();
            var order = new List<string>();
            var cycles = new List<List<string>>();

            void Dfs(string node, int depth, Stack<string> stack)
            {
                if (depth > maxDepth) return;
                if (excludePredicate?.Invoke(node) == true) return;

                if (!visited.ContainsKey(node)) visited[node] = 0;

                if (visited[node] == 1)
                {

                    var cycle = new List<string>();
                    bool take = false;
                    foreach (var s in stack)
                    {
                        if (s == node) take = true;
                        if (take) cycle.Add(s);
                    }
                    cycle.Reverse();
                    cycles.Add(cycle);
                    return;
                }
                if (visited[node] == 2) return;

                visited[node] = 1;
                stack.Push(node);

                if (_graph.TryGetValue(node, out var childs))
                {
                    foreach (var c in childs)
                        Dfs(c, depth + 1, stack);
                }

                stack.Pop();
                visited[node] = 2;


                if (!order.Contains(node))
                    order.Add(node);
            }

            Dfs(root, 0, new Stack<string>());

            return (order, cycles);
        }
    }
}
