using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CodeA.Model
{
    [Serializable]
    public class FileModel
    {
        public DateTime Date { get; set; }

        public int Fight { get; set; }
        public int RankS { get; set; }
        public int EnterBoss { get; set; }
        public int WinBoss { get; set; }

        public int Support20 { get; set; }
        public int Ro { get; set; }
        public int I { get; set; }
    }
}
