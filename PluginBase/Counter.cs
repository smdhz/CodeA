using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using Grabacr07.KanColleWrapper;
using Grabacr07.KanColleWrapper.Models.Raw;

namespace CodeA
{
    public class Counter
    {
        public Counter(KanColleProxy proxy)
        {
            proxy.api_req_sortie_battleresult.TryParse<kcsapi_battleresult>().Subscribe(x => Battle(x.Data));
            proxy.api_port.TryParse<kcsapi_port>().Subscribe(x => Port(x.Data));
        }

        private bool OntheWay = false;
        public int Fright { get; private set; }
        public int RankS { get; private set; }
        public int EnterBoss { get; private set; }

        private string[] Bosses = { "" };

        private void Battle(kcsapi_battleresult data)
        {
            if (!OntheWay)
            {
                OntheWay = true;
                Fright++;
            }
            if (data.api_win_rank == "S")
                RankS++;
            if (Bosses.Contains(data.api_enemy_info.api_deck_name))
                EnterBoss++;
        }

        private void Port(kcsapi_port data)
        {
            OntheWay = false;
        }
    }
}
