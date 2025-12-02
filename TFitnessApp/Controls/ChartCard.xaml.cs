using System.Windows;
using System.Windows.Controls;
using System.Windows.Markup;


namespace TFitnessApp
{
    [ContentProperty("NoiDungTrong")]
    public partial class ChartCard : UserControl
    {
        public ChartCard()
        {
            InitializeComponent();
        }

        // --- 1. TIÊU ĐỀ ---
        public static readonly DependencyProperty TieuDeProperty =
            DependencyProperty.Register("TieuDe", typeof(string), typeof(ChartCard), new PropertyMetadata("Tiêu đề mặc định"));

        public string TieuDe
        {
            get => (string)GetValue(TieuDeProperty);
            set => SetValue(TieuDeProperty, value);
        }

        // --- 2. NỘI DUNG ---
        public static readonly DependencyProperty NoiDungTrongProperty =
            DependencyProperty.Register("NoiDungTrong", typeof(object), typeof(ChartCard));

        public object NoiDungTrong
        {
            get => GetValue(NoiDungTrongProperty);
            set => SetValue(NoiDungTrongProperty, value);
        }

        // --- 3. CÁC CỜ ẨN/HIỆN (VISIBILITY) ---
        public static readonly DependencyProperty HienNutInProperty =
            DependencyProperty.Register("HienNutIn", typeof(bool), typeof(ChartCard), new PropertyMetadata(true));
        public bool HienNutIn { get => (bool)GetValue(HienNutInProperty); set => SetValue(HienNutInProperty, value); }

        public static readonly DependencyProperty HienChonLoaiBDProperty =
            DependencyProperty.Register("HienChonLoaiBD", typeof(bool), typeof(ChartCard), new PropertyMetadata(false));
        public bool HienChonLoaiBD { get => (bool)GetValue(HienChonLoaiBDProperty); set => SetValue(HienChonLoaiBDProperty, value); }

        public static readonly DependencyProperty HienChonChiNhanhProperty =
            DependencyProperty.Register("HienChonChiNhanh", typeof(bool), typeof(ChartCard), new PropertyMetadata(true));
        public bool HienChonChiNhanh { get => (bool)GetValue(HienChonChiNhanhProperty); set => SetValue(HienChonChiNhanhProperty, value); }

        public static readonly DependencyProperty HienBoLocNgayProperty =
            DependencyProperty.Register("HienBoLocNgay", typeof(bool), typeof(ChartCard), new PropertyMetadata(true));
        public bool HienBoLocNgay { get => (bool)GetValue(HienBoLocNgayProperty); set => SetValue(HienBoLocNgayProperty, value); }
    }
}