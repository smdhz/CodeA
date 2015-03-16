using System;
using System.IO;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Serialization;

using Grabacr07.KanColleWrapper;
using Grabacr07.KanColleWrapper.Models.Raw;
using Grabacr07.KanColleWrapper.Models;

namespace CodeA
{
    public class Counter
    {
        private static readonly string filePath = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
            "grabacr.net",
            "KanColleViewer",
            "CodeA.xml");
        private XmlSerializer serializer = new XmlSerializer(typeof(FileModel));

        public Counter(KanColleProxy proxy)
        {
            proxy.api_req_sortie_battleresult.TryParse<kcsapi_battleresult>().Subscribe(x => Battle(x.Data));
            proxy.api_port.TryParse<kcsapi_port>().Subscribe(x => Port(x.Data));

            if (new FileInfo(filePath).Exists)
            {
                using (FileStream fs = new FileStream(filePath, FileMode.Open))
                {
                    FileModel model = serializer.Deserialize(fs) as FileModel;
                    int Offset = Convert.ToInt32(DateTime.Today.DayOfWeek);
                    if (Offset == 0)
                        Offset = 7;
                    Offset--;
                    if (model.Date >= DateTime.Today.AddDays(-Offset))
                    {
                        Fight = model.Fight;
                        RankS = model.RankS;
                        EnterBoss = model.EnterBoss;
                        WinBoss = model.WinBoss;
                    }
                }
            }
        }

        private bool OntheWay = false;
        private bool Changed = false;
        private Guid FightID = Guid.Empty;
        public int Fight { get; private set; }
        public int RankS { get; private set; }
        public int EnterBoss { get; private set; }
        public int WinBoss { get; private set; }

        private string[] Bosses = 
        { 
            "敵主力艦隊", "敵主力部隊", "敵機動部隊", "敵通商破壊主力艦隊",
            "敵通商破壊艦隊", "敵主力打撃群", "敵侵攻中核艦隊",
            "敵北方侵攻艦隊", "敵キス島包囲艦隊", "深海棲艦泊地艦隊", "深海棲艦北方艦隊中枢", "北方増援部隊主力",
            "東方派遣艦隊", "東方主力艦隊", "敵東方中枢艦隊",
            "敵前線司令艦隊", "敵機動部隊本隊", "敵サーモン方面主力艦隊", "敵補給部隊本体", "敵任務部隊本隊",
            "敵回航中空母", "敵攻略部隊本体"
        };

        private void Battle(kcsapi_battleresult data)
        {
            // 当前任务
            List<int> quests = new List<int>();
            foreach (Quest i in KanColleClient.Current.Homeport.Quests.Current)
                if (i != null)
                    quests.Add(i.Id);

            if (!OntheWay)
            {
                FightID = Guid.NewGuid();
                OntheWay = true;                    
            }

            // 包含あ号
            if (quests.Contains(214))
            {
                Changed = true;
                Fight++;
                if (data.api_win_rank == "S")
                    RankS++;
                if (Bosses.Contains(data.api_enemy_info.api_deck_name))
                {
                    EnterBoss++;
                    if (data.api_win_rank != "C" & data.api_win_rank != "D")
                        WinBoss++;
                }
                ValueChanged(this, new EventArgs());
            }

            // 记录
            using (LogDataContext db = new LogDataContext())
            {
                ShipLog log = new ShipLog()
                {
                    Time = DateTime.Now,
                    Area = data.api_quest_name,
                    Enemy = data.api_enemy_info.api_deck_name,
                    Rank = data.api_win_rank,
                    Fight = FightID
                };
                if (data.api_get_ship != null)
                    log.Drop = data.api_get_ship.api_ship_name;
                db.ShipLog.InsertOnSubmit(log);
                db.SubmitChanges();
            }
        }

        private void Port(kcsapi_port data)
        {
            OntheWay = false;
            if (!Changed)
                return;
            using (FileStream fs = new FileStream(filePath, FileMode.Create))
                serializer.Serialize(fs, new FileModel()
                {
                    Date = DateTime.Now,
                    Fight = this.Fight,
                    RankS = this.RankS,
                    EnterBoss = this.EnterBoss,
                    WinBoss = this.WinBoss
                });
            Changed = false;
        }

        public event EventHandler ValueChanged = (se, ev) => { };
    }
}
