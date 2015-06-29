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
using System.ComponentModel;

namespace CodeA
{
    public class Counter : INotifyPropertyChanged
    {
        private static readonly string filePath = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
            "grabacr.net",
            "KanColleViewer",
            "CodeA.xml");
        private XmlSerializer serializer = new XmlSerializer(typeof(FileModel));
        private FileInfo dataFile;

        // 最后一次回母港
        private DateTime LastPort = TimeZoneInfo.ConvertTimeBySystemTimeZoneId(DateTime.Now, "Tokyo Standard Time");

        public Counter(KanColleProxy proxy)
        {
            // 注册订阅
            proxy.api_req_sortie_battleresult.TryParse<kcsapi_battleresult>().Subscribe(x => Battle(x.Data));
            proxy.api_port.TryParse<kcsapi_port>().Subscribe(x => Port(x.Data));

            dataFile = checkFile();
            if (dataFile.Exists)
            {
                try
                {
                    using (FileStream fs = new FileStream(dataFile.FullName, FileMode.Open))
                    {
                        // 读文件
                        FileModel model = serializer.Deserialize(fs) as FileModel;
                        
                        // 比对日期
                        if (model.Date >= GetResetTime())
                        {
                            Fight = model.Fight;
                            RankS = model.RankS;
                            EnterBoss = model.EnterBoss;
                            WinBoss = model.WinBoss;

                            Support20 = model.Support20;
                            Ro = model.Ro;
                            I = model.I;
                        }
                    }
                }
                catch (InvalidOperationException)
                { dataFile.Delete(); }
            }
        }

        private bool Changed = false;   // 写文件

        // あ
        public int Fight { get; private set; }
        public int RankS { get; private set; }
        public int EnterBoss { get; private set; }
        public int WinBoss { get; private set; }

        // 其它
        public int Support20 { get; set; }
        public int Ro { get; set; }
        public int I { get; set; }

        // 常量表
        private readonly string[] Bosses = 
        { 
            "敵主力艦隊", "敵主力部隊", "敵機動部隊", "敵通商破壊主力艦隊",
            "敵通商破壊艦隊", "敵主力打撃群", "敵侵攻中核艦隊",
            "敵北方侵攻艦隊", "敵キス島包囲艦隊", "深海棲艦泊地艦隊", "深海棲艦北方艦隊中枢", "北方増援部隊主力",
            "東方派遣艦隊", "東方主力艦隊", "敵東方中枢艦隊",
            "敵前線司令艦隊", "敵機動部隊本隊", "敵サーモン方面主力艦隊", "敵補給部隊本体", "敵任務部隊本隊",
            "敵回航中空母", "敵攻略部隊本体"
        };

        private readonly int[] Supports = new int[] { 513, 526, 558 };                  // ワ
        private readonly int[] Carriers = new int[] { 510, 523, 560, 512, 525, 528 };   // ヌ、ヲ

        /// <summary>
        /// 战斗时调整计数器
        /// </summary>
        /// <param name="data">战斗结果 (battleresult)</param>
        private void Battle(kcsapi_battleresult data)
        {
            int[] misson =
                KanColleClient.Current.Homeport.Quests.Current.Where(i => i != null).Select(i => i.Id).ToArray();

            // あ号
            if (misson.Contains(214))
            {
                Changed = true;
                Fight++;
                if (data.api_win_rank == "S")
                    RankS++;
                // 进 BOSS
                if (Bosses.Contains(data.api_enemy_info.api_deck_name))
                {
                    EnterBoss++;
                    // BOSS 胜利
                    if (data.api_win_rank != "C" & data.api_win_rank != "D")
                        WinBoss++;
                }
                SetEvent("Fight", "RankS", "EnterBoss", "WinBoss");
            }

            int acCount = data.api_ship_id.Where(i => Supports.Contains(i)).Count(),
                cvCount = data.api_ship_id.Where(i => Carriers.Contains(i)).Count();

            // 20补给
            if (misson.Contains(213) & acCount > 0)
            {
                Changed = true;
                Support20 += acCount;
                SetEvent("Support20");
            }

            // ろ号
            if (misson.Contains(221) & acCount > 0)
            {
                Changed = true;
                Ro += acCount;
                SetEvent("Ro");
            }

            // い号
            if (misson.Contains(220) & cvCount > 0)
            {
                Changed = true;
                I += cvCount;
                SetEvent("I");
            }
        }

        /// <summary>
        /// 回母港时记录信息
        /// </summary>
        /// <param name="data">母港信息 (port)</param>
        private void Port(kcsapi_port data)
        {
            DateTime jpNow = TimeZoneInfo.ConvertTimeBySystemTimeZoneId(DateTime.Now, "Tokyo Standard Time");
            // 处理不关机爱好者
            if (LastPort < GetResetTime() & GetResetTime() < jpNow)
            {
                if (dataFile.Exists)
                    dataFile.Delete();
                Fight = RankS = EnterBoss = WinBoss = Support20 = Ro = I = 0;
                SetEvent("Fight", "RankS", "EnterBoss", "WinBoss", "Support20", "Ro", "I");
            }

            // 写文件
            if (Changed)
            {
                Changed = false;
                using (FileStream fs = new FileStream(dataFile.FullName, FileMode.Create))
                    serializer.Serialize(fs, new FileModel()
                    {
                        Date = jpNow,

                        Fight = this.Fight,
                        RankS = this.RankS,
                        EnterBoss = this.EnterBoss,
                        WinBoss = this.WinBoss,

                        Support20 = this.Support20,
                        Ro = this.Ro,
                        I = this.I
                    });
            }
            LastPort = jpNow;
        }

        /// <summary>
        /// 取重置计数的时间点
        /// </summary>
        /// <returns>周一5时 (UTC+9)</returns>
        private DateTime GetResetTime()
        {
            DateTime now = TimeZoneInfo.ConvertTimeBySystemTimeZoneId(DateTime.Now, "Tokyo Standard Time");

            // 取星期一
            int Offset = Convert.ToInt32(now.DayOfWeek);
            if (Offset == 0)
                Offset = 7;
            Offset--;

            return now.Date.AddDays(-Offset).AddHours(5);
        }

        /// <summary>
        /// 获取合适的记录文件（OneDrive / 本地）
        /// </summary>
        /// <returns>合适的文件</returns>
        private FileInfo checkFile()
        {
            // 尝试 Win7 OneDrive
            string oneDrive = Convert.ToString(Microsoft.Win32.Registry.GetValue(@"HKEY_CURRENT_USER\Software\Microsoft\OneDrive", "UserFolder", null));
            // Win8 OneDrive
            if (string.IsNullOrEmpty(oneDrive))
                oneDrive = Convert.ToString(Microsoft.Win32.Registry.GetValue(@"HKEY_CURRENT_USER\Software\Microsoft\Windows\CurrentVersion\SkyDrive", "UserFolder", null));
            // 使用本地
            if (string.IsNullOrEmpty(oneDrive))
                return new FileInfo(filePath);
            else
                oneDrive += "\\Application Data\\CodeA.xml";

            return new FileInfo(oneDrive);
        }

        public event PropertyChangedEventHandler PropertyChanged;
        /// <summary>
        /// 触发 PropertyChanged 事件
        /// </summary>
        /// <param name="property">发生改变的属性（一个或多个）</param>
        private void SetEvent(params string[] property)
        {
            if (PropertyChanged != null)
                foreach (string i in property)
                    PropertyChanged(this, new PropertyChangedEventArgs(i));
        }
    }
}
