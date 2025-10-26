using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Serialization;

namespace DependencyGraphVisualization.Models
{
    [XmlRoot("Configuration")]
    public class DependecyData
    {
        public string? Name { get; set; }
        public string? Url { get; set; }
        public bool TestMode { get; set; }
        public string? Version { get; set; }
        public string? OutputFile { get; set; }
    }
}
