using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MediaAnalyzer
{
    public class MediaAnalyzerAsset
    {
        public string Uniqueness { get; set; }
        public string JobName { get; set; }
        public string OutputAssetName { get; set; }
        public string InputAssetName { get; set; }
        public string TransformName { get; set; }
    }
}
