using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CodeA
{
    [Serializable]
    public class FileModel
    {
        public DateTime Date { get; set; }
        public int Fight { get; set; }
        public int RankS { get; set; }
        public int EnterBoss { get; set; }
        public int WinBoss { get; set; }
    }
}
