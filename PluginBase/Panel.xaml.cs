using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;

namespace CodeA
{
    /// <summary>
    /// Panel.xaml 的交互逻辑
    /// </summary>
    public partial class Panel : UserControl
    {
        public Counter Counter { get; set; }

        public Panel()
        {
            InitializeComponent();
        }

        private void UserControl_Loaded(object sender, RoutedEventArgs e)
        {
            Counter.ValueChanged += Set;
            Set(sender, e);
        }

        private void Set(object sender, EventArgs e)
        {
            Dispatcher.Invoke(() =>
            {
                txtFirght.Text = string.Format("{0}/36", Counter.Fight);
                txtWin.Text = string.Format("{0}/6", Counter.RankS);
                txtBoss.Text = string.Format("{0}/24", Counter.EnterBoss);
                txtBossWin.Text = string.Format("{0}/12", Counter.WinBoss);
                pgsFirght.Value = Counter.Fight;
                pgsWin.Value = Counter.RankS;
                pgsBoss.Value = Counter.EnterBoss;
                pgsBossWin.Value = Counter.WinBoss;
            });
        }

        private void UserControl_Unloaded(object sender, RoutedEventArgs e)
        {
            Counter.ValueChanged -= Set;
        }
    }
}
