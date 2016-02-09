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
using System.Net;

namespace CodeA
{
    public class Counter : INotifyPropertyChanged
    {
        private static readonly string filePath = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
            "grabacr.net",
            "KanColleViewer",
            "CodeA.xml");
        private static readonly string dataPath = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
            "grabacr.net",
            "KanColleViewer",
            "CodeA_Enemy.xml");

        private XmlSerializer serializer = new XmlSerializer(typeof(Model.FileModel));
        private FileInfo dataFile;

        // 计数参数
        private Model.DataModel _data;
        public Model.DataModel Data
        {
            get { return _data; }
            private set { SetEvent(nameof(Data)); }
        }

        // 最后一次回母港
        private DateTime LastPort = TimeZoneInfo.ConvertTimeBySystemTimeZoneId(DateTime.Now, "Tokyo Standard Time");

        public Livet.Commands.ViewModelCommand UpdateCommand { get; private set; }

        public Counter(KanColleProxy proxy)
        {
            // 注册订阅
            proxy.api_req_sortie_battleresult.TryParse<kcsapi_battleresult>().Subscribe(x => Battle(x.Data));
            proxy.api_req_combined_battle_battleresult.TryParse<kcsapi_battleresult>().Subscribe(x => Battle(x.Data));
            proxy.api_port.TryParse<kcsapi_port>().Subscribe(x => Port(x.Data));

            UpdateCommand = new Livet.Commands.ViewModelCommand(update);

            // 检查文件
            dataFile = CheckFile();
            if (dataFile.Exists)
            {
                try
                {
                    using (FileStream fs = new FileStream(dataFile.FullName, FileMode.Open))
                    {
                        // 读文件
                        Model.FileModel model = serializer.Deserialize(fs) as Model.FileModel;

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

            // 更新敌数据
            XmlSerializer serial = new XmlSerializer(typeof(Model.DataModel));
            if (File.Exists(dataPath))
            {
                using (FileStream fs = new FileStream(dataPath, FileMode.Open))
                    Data = serial.Deserialize(fs) as Model.DataModel;
            }
            else
            {
                using (Stream rs = System.Reflection.Assembly.GetExecutingAssembly().GetManifestResourceStream("CodeA.Enemy.xml"))
                    Data = serial.Deserialize(rs) as Model.DataModel;
                update();
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
                if (Data.Bosses.Contains(data.api_enemy_info.api_deck_name))
                {
                    EnterBoss++;
                    // BOSS 胜利
                    if (data.api_win_rank != "C" & data.api_win_rank != "D")
                        WinBoss++;
                }
                SetEvent(nameof(Fight), nameof(RankS), nameof(EnterBoss), nameof(WinBoss));
            }

            int acCount = data.api_ship_id.Count(i => Data.Supports.Contains(i)),
                cvCount = data.api_ship_id.Count(i => Data.Carriers.Contains(i));

            // 20补给
            if (misson.Contains(213) & acCount > 0)
            {
                Changed = true;
                Support20 += acCount;
                SetEvent(nameof(Support20));
            }

            // ろ号
            if (misson.Contains(221) & acCount > 0)
            {
                Changed = true;
                Ro += acCount;
                SetEvent(nameof(Ro));
            }

            // い号
            if (misson.Contains(220) & cvCount > 0)
            {
                Changed = true;
                I += cvCount;
                SetEvent(nameof(I));
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
                SetEvent(
                    nameof(Fight),
                    nameof(RankS),
                    nameof(EnterBoss),
                    nameof(WinBoss),
                    nameof(Support20),
                    nameof(Ro),
                    nameof(I));
            }

            // 写文件
            if (Changed)
            {
                Changed = false;
                using (FileStream fs = new FileStream(dataFile.FullName, FileMode.Create))
                    serializer.Serialize(fs, new Model.FileModel()
                    {
                        Date = jpNow,

                        Fight = Fight,
                        RankS = RankS,
                        EnterBoss = EnterBoss,
                        WinBoss = WinBoss,

                        Support20 = Support20,
                        Ro = Ro,
                        I = I
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
        /// 从服务器刷新数据
        /// </summary>
        private async void update()
        {
            try
            {
                HttpWebRequest HttpWReq = (HttpWebRequest)WebRequest.Create("http://mysticmonkey.co.nf/Enemy.xml");
                HttpWebResponse HttpWResp = (HttpWebResponse)await HttpWReq.GetResponseAsync();

                XmlSerializer serial = new XmlSerializer(typeof(Model.DataModel));
                Data = serial.Deserialize(HttpWResp.GetResponseStream()) as Model.DataModel;
                Data.LastUpdate = DateTime.Now;
                using (FileStream fs = new FileStream(dataPath, FileMode.Create))
                    serial.Serialize(fs, Data);
                SetEvent(nameof(Data));
                HttpWResp.Close();
            }
            catch (WebException e)
            {
                System.Windows.MessageBox.Show(
                    "我们好像被河蟹了" + Environment.NewLine + "获取数据时" + e.Message,
                    AppDomain.CurrentDomain.FriendlyName,
                    System.Windows.MessageBoxButton.OK,
                    System.Windows.MessageBoxImage.Warning);
            }
        }

        /// <summary>
        /// 获取合适的记录文件（OneDrive / 本地）
        /// </summary>
        /// <returns>合适的文件</returns>
        private FileInfo CheckFile()
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
