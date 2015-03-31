using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using System.ComponentModel.Composition;
using Grabacr07.KanColleViewer.Composition;

namespace CodeA
{
    [Export(typeof(IToolPlugin))]
    [ExportMetadata("Title", "CodeA")]
    [ExportMetadata("Description", "周常计数器")]
    [ExportMetadata("Version", "2.0")]
    [ExportMetadata("Author", "Mystic Monkey")]
    public class Base : IToolPlugin
    {
        private Counter counter = new Counter(Grabacr07.KanColleWrapper.KanColleClient.Current.Proxy);

        public string ToolName
        {
            get { return "CodeA"; }
        }

        public object GetToolView()
        {
            return new Panel() { DataContext = counter };
        }

        public object GetSettingsView()
        {
            return null;
        }
    }
}
