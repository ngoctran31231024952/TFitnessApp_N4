using System;
using System.IO;
using System.Windows;
using System.ComponentModel;
using Microsoft.Data.Sqlite;
using System.Collections.Generic;
using TFitnessApp;
using System.Windows.Controls;
using TFitnessApp.Database;

namespace TFitnessApp.Windows
{
    // Lớp Window2 dùng để xem và chỉnh sửa chi tiết giao dịch
    public partial class Window2 : Window, INotifyPropertyChanged
    {
        #region Trường Dữ liệu Nội bộ
        private string _ChuoiKetNoi;
        private readonly DbAccess _dbAccess;
        private string _maGD;
        private string _maHV;
        private string _hoTenHocVien;
        private string _maGoi;
        private string _tenGoiTap;
        private string _maTK;
        private string _hoTenNhanVien;
        private decimal _tongTien;
        private decimal _daThanhToan;
        private decimal _soTienNo;
        private string _phuongThuc;
        private string _trangThai;
        private DateTime _ngayGD;

        // Biến để kiểm tra xem có đang trong chế độ chỉnh sửa không
        private bool _isEditMode = false;
        #endregion

        #region Thuộc tính Binding (Public Properties)
        public string MaGD
        {
            get => _maGD;
            set { _maGD = value; OnPropertyChanged(nameof(MaGD)); }
        }

        public string MaHV
        {
            get => _maHV;
            set { _maHV = value; OnPropertyChanged(nameof(MaHV)); }
        }

        public string HoTenHocVien
        {
            get => _hoTenHocVien;
            set { _hoTenHocVien = value; OnPropertyChanged(nameof(HoTenHocVien)); }
        }

        public string MaGoi
        {
            get => _maGoi;
            set { _maGoi = value; OnPropertyChanged(nameof(MaGoi)); }
        }

        public string TenGoiTap
        {
            get => _tenGoiTap;
            set { _tenGoiTap = value; OnPropertyChanged(nameof(TenGoiTap)); }
        }

        public string MaTK
        {
            get => _maTK;
            set { _maTK = value; OnPropertyChanged(nameof(MaTK)); }
        }

        public string HoTenNhanVien
        {
            get => _hoTenNhanVien;
            set { _hoTenNhanVien = value; OnPropertyChanged(nameof(HoTenNhanVien)); }
        }

        public decimal TongTien
        {
            get => _tongTien;
            set { _tongTien = value; OnPropertyChanged(nameof(TongTien)); }
        }

        public decimal DaThanhToan
        {
            get => _daThanhToan;
            set
            {
                if (_daThanhToan == value) return; // Tránh cập nhật đệ quy
                _daThanhToan = value;
                OnPropertyChanged(nameof(DaThanhToan));
                TinhTrangThaiThanhToan(); // Tự động tính lại số tiền nợ và trạng thái
            }
        }

        public decimal SoTienNo
        {
            get => _soTienNo;
            // Chỉ cho phép set nội bộ qua hàm TinhTrangThaiThanhToan()
            set { _soTienNo = value; OnPropertyChanged(nameof(SoTienNo)); }
        }

        public string PhuongThuc
        {
            get => _phuongThuc;
            set { _phuongThuc = value; OnPropertyChanged(nameof(PhuongThuc)); }
        }

        public string TrangThai
        {
            get => _trangThai;
            // Chỉ cho phép set nội bộ qua hàm TinhTrangThaiThanhToan()
            set { _trangThai = value; OnPropertyChanged(nameof(TrangThai)); }
        }

        public DateTime NgayGD
        {
            get => _ngayGD;
            set { _ngayGD = value; OnPropertyChanged(nameof(NgayGD)); }
        }

        public bool IsEditMode
        {
            get => _isEditMode;
            set { _isEditMode = value; OnPropertyChanged(nameof(IsEditMode)); }
        }
        #endregion

        #region Khởi tạo và Tải dữ liệu
        // Constructor nhận đối tượng MoDonDuLieuGiaoDich (từ DataGrid)
        public Window2(MoDonDuLieuGiaoDich transaction)
        {
            InitializeComponent();

            // Khởi tạo đối tượng DbAccess
            _dbAccess = new DbAccess();
            // Lấy chuỗi kết nối
            _ChuoiKetNoi = _dbAccess._ChuoiKetNoi;

            this.DataContext = this;

            // Gán dữ liệu cơ bản từ transaction (từ DataGrid)
            GanDuLieuTuTransaction(transaction);

            // Tải thông tin bổ sung (PhuongThuc, HoTenNhanVien) từ database
            TaiThongTinBoSung();

            // Khởi tạo chế độ xem (mặc định là chế độ chỉ đọc)
            SetEditMode(false);
        }

        // Gán các trường dữ liệu từ đối tượng MoDonDuLieuGiaoDich
        private void GanDuLieuTuTransaction(MoDonDuLieuGiaoDich transaction)
        {
            MaGD = transaction.MaGD;
            MaHV = transaction.MaHV;
            HoTenHocVien = transaction.HoTen;
            MaGoi = transaction.MaGoi;
            TenGoiTap = transaction.TenGoi;
            MaTK = transaction.MaTK;
            TongTien = transaction.TongTien;
            DaThanhToan = transaction.DaThanhToan;
            SoTienNo = transaction.SoTienNo;
            TrangThai = transaction.TrangThai;
            NgayGD = transaction.NgayGD;
        }

        // Tải thông tin bổ sung (PhuongThuc, HoTenNhanVien) từ database
        private void TaiThongTinBoSung()
        {
            try
            {
                using (SqliteConnection conn = DbAccess.CreateConnection())
                {
                    conn.Open();

                    // Query lấy Phương thức thanh toán và Họ tên nhân viên
                    string query = @"
                        SELECT 
                            gd.PhuongThuc,
                            tk.HoTen as HoTenNhanVien
                        FROM GiaoDich gd
                        LEFT JOIN TaiKhoan tk ON gd.MaTK = tk.MaTK
                        WHERE gd.MaGD = @MaGD";

                    using (var command = new SqliteCommand(query, conn))
                    {
                        command.Parameters.AddWithValue("@MaGD", MaGD);

                        using (var reader = command.ExecuteReader())
                        {
                            if (reader.Read())
                            {
                                // Gán dữ liệu (xử lý DBNull)
                                PhuongThuc = reader["PhuongThuc"] != DBNull.Value ? reader["PhuongThuc"].ToString() : "";
                                HoTenNhanVien = reader["HoTenNhanVien"] != DBNull.Value ? reader["HoTenNhanVien"].ToString() : "";
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Lỗi khi tải thông tin bổ sung: {ex.Message}");
            }
        }
        #endregion

        #region Xử lý Logic và UI

        // Phương thức tính toán số tiền nợ VÀ Trạng thái thanh toán (được lấy từ Window1)
        private void TinhTrangThaiThanhToan()
        {
            // 1. Tính số tiền nợ
            SoTienNo = TongTien - DaThanhToan;

            // 2. Tính trạng thái thanh toán
            if (TongTien <= 0)
            {
                // Nếu chưa có gói tập
                TrangThai = "Chưa Thanh Toán";
            }
            else if (SoTienNo <= 0 && TongTien > 0)
            {
                // Nếu số tiền nợ bằng 0 (hoặc âm, tức là trả thừa) -> Đã thanh toán
                TrangThai = "Đã Thanh Toán";
            }
            else if (TongTien > 0 && DaThanhToan == 0)
            {
                // Nếu số tiền đã thanh toán bằng 0 (và có tổng tiền) -> Chưa thanh toán
                TrangThai = "Chưa Thanh Toán";
            }
            else if (SoTienNo > 0 && SoTienNo < TongTien)
            {
                // Nếu số tiền nợ lớn hơn 0 và bé hơn tổng tiền -> Trả một phần
                TrangThai = "Trả Một Phần";
            }
            else
            {
                // Trường hợp khác
                TrangThai = "Chưa Xác Định";
            }
        }

        // Thiết lập chế độ chỉnh sửa cho các control
        private void SetEditMode(bool isEdit)
        {
            IsEditMode = isEdit;

            // DaThanhToan (txtDaThanhToan) có thể chỉnh sửa
            if (txtDaThanhToan != null)
                txtDaThanhToan.IsEnabled = isEdit;

            // Cập nhật nút "Sửa" thành "Lưu" hoặc ngược lại
            if (btnSua != null)
            {
                var stackPanel = btnSua.Content as StackPanel;
                if (stackPanel != null)
                {
                    var textBlock = stackPanel.Children[1] as TextBlock;
                    if (textBlock != null)
                        textBlock.Text = isEdit ? "Lưu" : "Sửa";
                }
            }
        }

        // Đóng cửa sổ
        private void btnClose_Click(object sender, RoutedEventArgs e)
        {
            this.Close();
        }

        // Sự kiện khi nhấn nút "Sửa" / "Lưu"
        private void btnSua_Click(object sender, RoutedEventArgs e)
        {
            if (!IsEditMode)
            {
                // Chuyển sang chế độ chỉnh sửa
                SetEditMode(true);
            }
            else
            {
                // Lưu thay đổi 
                try
                {
                    // Validate dữ liệu
                    if (!decimal.TryParse(txtDaThanhToan.Text, out decimal daThanhToanMoi))
                    {
                        MessageBox.Show("Số tiền đã thanh toán không hợp lệ!", "Lỗi",
                            MessageBoxButton.OK, MessageBoxImage.Error);
                        return;
                    }

                    if (daThanhToanMoi < 0)
                    {
                        MessageBox.Show("Số tiền đã thanh toán không được âm!", "Lỗi",
                            MessageBoxButton.OK, MessageBoxImage.Error);
                        return;
                    }

                    if (daThanhToanMoi > TongTien)
                    {
                        MessageBox.Show("Số tiền đã thanh toán không thể lớn hơn tổng tiền!", "Lỗi",
                            MessageBoxButton.OK, MessageBoxImage.Error);
                        return;
                    }

                    // 1. Cập nhật giá trị DaThanhToan
                    DaThanhToan = daThanhToanMoi;

                    // 2. Tự động tính toán số tiền nợ và Trạng thái mới nhất
                    TinhTrangThaiThanhToan();

                    // 3. Cập nhật Ngày giao dịch là thời điểm lưu
                    NgayGD = DateTime.Now;

                    using (SqliteConnection conn = DbAccess.CreateConnection())
                    {
                        conn.Open();
                        string query = @"
                            UPDATE GiaoDich 
                            SET NgayGD = @NgayGD, TrangThai = @TrangThai, DaThanhToan = @DaThanhToan, SoTienNo = @SoTienNo, PhuongThuc = @PhuongThuc
                            WHERE MaGD = @MaGD";

                        using (var command = new SqliteCommand(query, conn))
                        {
                            // Lưu ngày giao dịch với định dạng dd/MM/yyyy HH:mm 
                            command.Parameters.AddWithValue("@NgayGD", NgayGD.ToString("dd/MM/yyyy HH:mm"));
                            command.Parameters.AddWithValue("@TrangThai", TrangThai);
                            command.Parameters.AddWithValue("@DaThanhToan", DaThanhToan);
                            command.Parameters.AddWithValue("@SoTienNo", SoTienNo);
                            command.Parameters.AddWithValue("@PhuongThuc", PhuongThuc);
                            command.Parameters.AddWithValue("@MaGD", MaGD);

                            int rowsAffected = command.ExecuteNonQuery();
                            if (rowsAffected > 0)
                            {
                                MessageBox.Show("Cập nhật giao dịch thành công!");
                                SetEditMode(false);
                            }
                            else
                            {
                                MessageBox.Show("Cập nhật giao dịch thất bại!");
                            }
                        }
                    }
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Lỗi khi cập nhật giao dịch: {ex.Message}");
                }
            }
        }

        // Sự kiện khi nhấn nút "Xóa"
        private void btnXoa_Click(object sender, RoutedEventArgs e)
        {
            var result = MessageBox.Show($"Bạn có chắc chắn muốn xóa giao dịch {MaGD} không?", "Xác nhận xóa",
                                             MessageBoxButton.YesNo, MessageBoxImage.Warning);

            if (result == MessageBoxResult.Yes)
            {
                // Thực hiện xóa giao dịch khỏi Database
                try
                {
                    using (SqliteConnection conn = DbAccess.CreateConnection())
                    {
                        conn.Open();
                        string query = "DELETE FROM GiaoDich WHERE MaGD = @MaGD";

                        using (var command = new SqliteCommand(query, conn))
                        {
                            command.Parameters.AddWithValue("@MaGD", MaGD);
                            int rowsAffected = command.ExecuteNonQuery();

                            if (rowsAffected > 0)
                            {
                                MessageBox.Show("Xóa giao dịch thành công!");
                                this.Close();
                            }
                            else
                            {
                                MessageBox.Show("Xóa giao dịch thất bại!");
                            }
                        }
                    }
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Lỗi khi xóa giao dịch: {ex.Message}");
                }
            }
        }

        // Sự kiện khi nhấn nút "Xuất" hóa đơn
        private void btnXuat_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                // Tạo thư mục xuất file nếu chưa tồn tại
                string exportDirectory = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Exports");
                if (!Directory.Exists(exportDirectory))
                {
                    Directory.CreateDirectory(exportDirectory);
                }

                string fileName = $"HoaDon_{MaGD}_{DateTime.Now:ddMMyyyy_HHmm}.txt";
                string filePath = Path.Combine(exportDirectory, fileName);

                // Tạo nội dung hóa đơn
                string hoaDonContent = TaoNoiDungHoaDon();

                // Ghi file
                File.WriteAllText(filePath, hoaDonContent);

                MessageBox.Show($"Xuất hóa đơn thành công!\nFile: {filePath}", "Thông báo");
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Lỗi khi xuất hóa đơn: {ex.Message}");
            }
        }

        // Phương thức tạo nội dung hóa đơn (text)
        private string TaoNoiDungHoaDon()
        {
            return $@"HÓA ĐƠN THANH TOÁN TFITNESS
========================================
Mã giao dịch: {MaGD}
Ngày xuất: {DateTime.Now:dd/MM/yyyy HH:mm}

THÔNG TIN HỌC VIÊN:
- Mã học viên: {MaHV}
- Họ tên: {HoTenHocVien}

THÔNG TIN GÓI TẬP:
- Mã gói: {MaGoi}
- Tên gói: {TenGoiTap}

THÔNG TIN THANH TOÁN:
- Tổng tiền: {TongTien:N0} VNĐ
- Đã thanh toán: {DaThanhToan:N0} VNĐ
- Số tiền nợ: {SoTienNo:N0} VNĐ
- Phương thức: {PhuongThuc}
- Trạng thái: {TrangThai}
- Ngày giao dịch: {NgayGD:dd/MM/yyyy HH:mm}

NHÂN VIÊN XỬ LÝ:
- Mã nhân viên: {MaTK}
- Họ tên: {HoTenNhanVien}

========================================
Cảm ơn quý khách đã sử dụng dịch vụ!
TFitness - Nâng tầm thể chất";
        }

        #endregion

        #region Triển khai INotifyPropertyChanged
        public event PropertyChangedEventHandler PropertyChanged;
        // Phương thức gọi sự kiện PropertyChanged khi giá trị thuộc tính thay đổi
        protected virtual void OnPropertyChanged(string propertyName)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
        #endregion
    }
}