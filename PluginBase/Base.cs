using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using System.ComponentModel.Composition;
using Grabacr07.KanColleViewer.Composition;

namespace CodeA
{
    [Export(typeof(IPlugin))]
    [Export(typeof(ITool))]
    [ExportMetadata("Guid", "320fdf37-1e13-40a5-b5ae-6d7dcd35936b")]
    [ExportMetadata("Title", "CodeA")]
    [ExportMetadata("Description", "周常计数器")]
    [ExportMetadata("Version", "2.0")]
    [ExportMetadata("Author", "Mystic Monkey")]
    public class Base : IPlugin, ITool
    {
        private Counter counter = new Counter(Grabacr07.KanColleWrapper.KanColleClient.Current.Proxy);

        public string Name => "CodeA";

        public object View => new Panel() { DataContext = counter };

        public void Initialize() { }
    }
}
