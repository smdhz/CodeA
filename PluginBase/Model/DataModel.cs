using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CodeA.Model
{
    [Serializable]
    public class DataModel
    {
        public DateTime LastUpdate { get; set; }

        public List<int> Supports { get; set; } = new List<int>();
        public List<int> Carriers { get; set; } = new List<int>();

        public List<string> Bosses { get; set; } = new List<string>();
    }
}
