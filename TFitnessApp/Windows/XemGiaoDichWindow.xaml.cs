using System;
using System.IO;
using System.Windows;
using System.ComponentModel;
using Microsoft.Data.Sqlite;
using System.Collections.Generic;
using TFitnessApp;
using System.Windows.Controls;

namespace TFitnessApp.Windows
{
    public partial class Window2 : Window, INotifyPropertyChanged
    {
        private string _chuoiKetNoi;
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
            set { _daThanhToan = value; OnPropertyChanged(nameof(DaThanhToan)); TinhSoTienNo(); }
        }

        public decimal SoTienNo
        {
            get => _soTienNo;
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

        // Constructor nhận đối tượng MoDonDuLieuGiaoDich đầy đủ
        public Window2(MoDonDuLieuGiaoDich transaction)
        {
            InitializeComponent();

            // Khởi tạo chuỗi kết nối
            string duongDanDB = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Database", "TFitness.db");
            _chuoiKetNoi = $"Data Source={duongDanDB};";

            this.DataContext = this;

            // Gán dữ liệu trực tiếp từ transaction
            GanDuLieuTuTransaction(transaction);

            // Tải thông tin bổ sung từ database
            TaiThongTinBoSung();

            // Khởi tạo chế độ xem
            SetEditMode(false);
        }

        // Constructor nhận mã giao dịch (nếu cần)
        public Window2(string maGD)
        {
            InitializeComponent();

            string duongDanDB = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Database", "TFitness.db");
            _chuoiKetNoi = $"Data Source={duongDanDB};";

            this.DataContext = this;
            TaiThongTinGiaoDich(maGD);
            SetEditMode(false);
        }

        private void GanDuLieuTuTransaction(MoDonDuLieuGiaoDich transaction)
        {
            // Gán tất cả dữ liệu có sẵn từ transaction
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

        private void TaiThongTinBoSung()
        {
            try
            {
                using (var connection = new SqliteConnection(_chuoiKetNoi))
                {
                    connection.Open();

                    // Chỉ lấy thông tin Phương thức và Họ tên nhân viên từ database
                    string query = @"
                        SELECT 
                            gd.PhuongThuc,
                            tk.HoTen as HoTenNhanVien
                        FROM GiaoDich gd
                        LEFT JOIN TaiKhoan tk ON gd.MaTK = tk.MaTK
                        WHERE gd.MaGD = @MaGD";

                    using (var command = new SqliteCommand(query, connection))
                    {
                        command.Parameters.AddWithValue("@MaGD", MaGD);

                        using (var reader = command.ExecuteReader())
                        {
                            if (reader.Read())
                            {
                                PhuongThuc = reader["PhuongThuc"].ToString();
                                HoTenNhanVien = reader["HoTenNhanVien"].ToString();
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

        private void TaiThongTinGiaoDich(string maGD)
        {
            try
            {
                using (var connection = new SqliteConnection(_chuoiKetNoi))
                {
                    connection.Open();

                    // Lấy thông tin giao dịch đầy đủ
                    string query = @"
                        SELECT gd.*, hv.HoTen as HoTenHocVien, gt.TenGoi as TenGoiTap, tk.HoTen as HoTenNhanVien
                        FROM GiaoDich gd
                        LEFT JOIN HocVien hv ON gd.MaHV = hv.MaHV
                        LEFT JOIN GoiTap gt ON gd.MaGoi = gt.MaGoi
                        LEFT JOIN TaiKhoan tk ON gd.MaTK = tk.MaTK
                        WHERE gd.MaGD = @MaGD";

                    using (var command = new SqliteCommand(query, connection))
                    {
                        command.Parameters.AddWithValue("@MaGD", maGD);

                        using (var reader = command.ExecuteReader())
                        {
                            if (reader.Read())
                            {
                                MaGD = reader["MaGD"].ToString();
                                MaHV = reader["MaHV"].ToString();
                                HoTenHocVien = reader["HoTenHocVien"].ToString();
                                MaGoi = reader["MaGoi"].ToString();
                                TenGoiTap = reader["TenGoiTap"].ToString();
                                MaTK = reader["MaTK"].ToString();
                                HoTenNhanVien = reader["HoTenNhanVien"].ToString();
                                TongTien = Convert.ToDecimal(reader["TongTien"]);
                                DaThanhToan = Convert.ToDecimal(reader["DaThanhToan"]);
                                SoTienNo = Convert.ToDecimal(reader["SoTienNo"]);
                                PhuongThuc = reader["PhuongThuc"].ToString();
                                TrangThai = reader["TrangThai"].ToString();
                                NgayGD = Convert.ToDateTime(reader["NgayGD"]);
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Lỗi khi tải thông tin giao dịch: {ex.Message}");
            }
        }

        private void TinhSoTienNo()
        {
            SoTienNo = TongTien - DaThanhToan;
        }

        private void SetEditMode(bool isEdit)
        {
            IsEditMode = isEdit;

            // Cập nhật trạng thái các control có thể chỉnh sửa
            if (dpNgayGD != null)
                dpNgayGD.IsEnabled = isEdit;

            if (cbTrangThai != null)
                cbTrangThai.IsEnabled = isEdit;

            if (txtDaThanhToan != null)
                txtDaThanhToan.IsEnabled = isEdit;

            // Cập nhật nút
            if (btnSua != null)
            {
                var stackPanel = btnSua.Content as StackPanel;
                if (stackPanel != null)
                {
                    var textBlock = stackPanel.Children[1] as TextBlock;
                    if (textBlock != null)
                        textBlock.Text = isEdit ? "LƯU" : "SỬA";
                }
            }
        }

        private void btnClose_Click(object sender, RoutedEventArgs e)
        {
            this.Close();
        }

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

                    if (daThanhToanMoi > TongTien)
                    {
                        MessageBox.Show("Số tiền đã thanh toán không thể lớn hơn tổng tiền!", "Lỗi",
                            MessageBoxButton.OK, MessageBoxImage.Error);
                        return;
                    }

                    // Cập nhật giá trị
                    DaThanhToan = daThanhToanMoi;
                    TrangThai = cbTrangThai.SelectedItem?.ToString();
                    NgayGD = dpNgayGD.SelectedDate ?? NgayGD;

                    // Tính toán số tiền nợ
                    TinhSoTienNo();

                    using (var connection = new SqliteConnection(_chuoiKetNoi))
                    {
                        connection.Open();
                        string query = @"
                            UPDATE GiaoDich 
                            SET NgayGD = @NgayGD, TrangThai = @TrangThai, DaThanhToan = @DaThanhToan, SoTienNo = @SoTienNo
                            WHERE MaGD = @MaGD";

                        using (var command = new SqliteCommand(query, connection))
                        {
                            command.Parameters.AddWithValue("@NgayGD", NgayGD.ToString("yyyy-MM-dd"));
                            command.Parameters.AddWithValue("@TrangThai", TrangThai);
                            command.Parameters.AddWithValue("@DaThanhToan", DaThanhToan);
                            command.Parameters.AddWithValue("@SoTienNo", SoTienNo);
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

        private void btnXoa_Click(object sender, RoutedEventArgs e)
        {
            var result = MessageBox.Show($"Bạn có chắc chắn muốn xóa giao dịch {MaGD} không?", "Xác nhận xóa",
                                       MessageBoxButton.YesNo, MessageBoxImage.Warning);

            if (result == MessageBoxResult.Yes)
            {
                try
                {
                    using (var connection = new SqliteConnection(_chuoiKetNoi))
                    {
                        connection.Open();
                        string query = "DELETE FROM GiaoDich WHERE MaGD = @MaGD";

                        using (var command = new SqliteCommand(query, connection))
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

                string fileName = $"HoaDon_{MaGD}_{DateTime.Now:yyyyMMdd_HHmmss}.txt";
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

        private string TaoNoiDungHoaDon()
        {
            return $@"HÓA ĐƠN THANH TOÁN TFITNESS
========================================
Mã giao dịch: {MaGD}
Ngày xuất: {DateTime.Now:dd/MM/yyyy HH:mm:ss}

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
- Ngày giao dịch: {NgayGD:dd/MM/yyyy}

NHÂN VIÊN XỬ LÝ:
- Mã nhân viên: {MaTK}
- Họ tên: {HoTenNhanVien}

========================================
Cảm ơn quý khách đã sử dụng dịch vụ!
TFitness - Nâng tầm thể chất";
        }

        // Sự kiện khi giá trị đã thanh toán thay đổi
        private void txtDaThanhToan_TextChanged(object sender, TextChangedEventArgs e)
        {
            if (IsEditMode && decimal.TryParse(txtDaThanhToan.Text, out decimal daThanhToanMoi))
            {
                SoTienNo = TongTien - daThanhToanMoi;
            }
        }

        public event PropertyChangedEventHandler PropertyChanged;
        protected virtual void OnPropertyChanged(string propertyName)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}