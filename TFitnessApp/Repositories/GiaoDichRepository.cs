using Microsoft.Data.Sqlite;
using System;
using System.Collections.Generic;
using System.Configuration;
using TFitnessApp.Database;
using TFitnessApp.Interfaces;
using TFitnessApp.Entities;
using System.Windows;
using System.Globalization; // Bổ sung để xử lý Parse DateTime an toàn

namespace TFitnessApp.Repositories
{
    /// <summary>
    /// Repository triển khai logic truy cập dữ liệu Giao Dịch bằng SQLite
    /// </summary>
    public class GiaoDichRepository : IGiaoDichRepository
    {
        // Khai báo lại _dbAccess nếu cần thiết cho các lệnh khác, nhưng GetAll() đã được refactor để tự xử lý connection.
        // private readonly DbAcess _dbAccess; 

        public GiaoDichRepository()
        {
            // _dbAccess = new DbAcess(); 
        }

        /// <summary>
        /// Lấy tất cả các giao dịch từ cơ sở dữ liệu.
        /// </summary>
        public List<GiaoDich> GetAll()
        {
            var giaoDichList = new List<GiaoDich>();

            // Lấy chuỗi kết nối an toàn
            string connectionString = ConfigurationManager.ConnectionStrings["SQLiteConnection"]?.ConnectionString;

            if (string.IsNullOrEmpty(connectionString))
            {
                MessageBox.Show("Lỗi cấu hình: Chuỗi kết nối SQLiteConnection không tồn tại.", "Lỗi DB", MessageBoxButton.OK, MessageBoxImage.Error);
                return giaoDichList;
            }

            // Câu lệnh SQL: Đảm bảo tên cột khớp với Entity
            string sql = "SELECT MaGD, MaHV, MaGoi, MaTK, TongTien, DaThanhToan, SoTienNo, NgayGD, TrangThai FROM GiaoDich ORDER BY NgayGD DESC;";

            using (var connection = new SqliteConnection(connectionString))
            {
                try
                {
                    connection.Open();

                    using (var command = new SqliteCommand(sql, connection))
                    {
                        using (var reader = command.ExecuteReader())
                        {
                            while (reader.Read())
                            {
                                // Khởi tạo đối tượng GiaoDich
                                GiaoDich giaoDich = new GiaoDich
                                {
                                    MaGD = reader.GetString(reader.GetOrdinal("MaGD")),
                                    MaHV = reader.GetString(reader.GetOrdinal("MaHV")),
                                    MaGoi = reader.GetString(reader.GetOrdinal("MaGoi")),
                                    MaTK = reader.GetString(reader.GetOrdinal("MaTK")),

                                    TongTien = reader.GetDecimal(reader.GetOrdinal("TongTien")),
                                    DaThanhToan = reader.GetDecimal(reader.GetOrdinal("DaThanhToan")),
                                    SoTienNo = reader.GetDecimal(reader.GetOrdinal("SoTienNo")),

                                    // SỬA LỖI: Gọi phương thức an toàn để chuyển đổi NgayGD
                                    NgayGD = ParseDateTimeSafely(reader, "NgayGD"),

                                    TrangThai = reader.GetString(reader.GetOrdinal("TrangThai")),
                                    IsSelected = false // Thuộc tính giả định
                                };
                                giaoDichList.Add(giaoDich);
                            }
                        }
                    }
                }
                catch (Exception ex)
                {
                    // Thông báo lỗi chi tiết nếu có lỗi kết nối/SQL hoặc lỗi không xử lý được
                    System.Diagnostics.Debug.WriteLine($"LỖI TRUY VẤN DB: {ex.Message} | SQL: {sql}");
                    // Thay đổi thông báo lỗi để hiển thị lỗi thực tế hơn
                    MessageBox.Show($"Lỗi truy vấn cơ sở dữ liệu: {ex.Message}", "Lỗi DB", MessageBoxButton.OK, MessageBoxImage.Error);
                    return new List<GiaoDich>();
                }
            }
            return giaoDichList;
        }

        /// <summary>
        /// Phương thức hỗ trợ để chuyển đổi DateTime an toàn.
        /// Xử lý trường hợp SQLite lưu trữ DateTime dưới dạng TEXT không chuẩn.
        /// </summary>
        private DateTime ParseDateTimeSafely(SqliteDataReader reader, string columnName)
        {
            int ordinal = reader.GetOrdinal(columnName);
            if (reader.IsDBNull(ordinal))
            {
                return DateTime.MinValue; // Trả về giá trị nhỏ nhất nếu null
            }

            // 1. Thử đọc trực tiếp. Phương thức này sẽ thành công nếu DB lưu đúng định dạng ISO 8601 hoặc số.
            try
            {
                return reader.GetDateTime(ordinal);
            }
            catch (InvalidCastException)
            {
                // 2. Nếu thất bại, có thể DB đang lưu dưới dạng TEXT. Ta đọc chuỗi và thử Parse.
                string dateString = reader.GetString(ordinal);

                // Cố gắng chuyển đổi chuỗi sang DateTime bằng TryParse (Generic Parse).
                // Sử dụng CultureInfo.InvariantCulture để xử lý các định dạng chuẩn quốc tế.
                if (DateTime.TryParse(dateString, CultureInfo.InvariantCulture, DateTimeStyles.None, out DateTime result))
                {
                    return result;
                }

                // THỬ VỚI CÁC ĐỊNH DẠNG KHÁC NHAU VÀ VĂN HÓA KHÔNG ĐỔI (InvariantCulture)
                string[] formats = new string[] {
                    "yyyy-MM-dd HH:mm:ss.fff", // ISO 8601 có milliseconds
                    "yyyy-MM-dd HH:mm:ss",     // ISO 8601 phổ biến nhất
                    "dd-MM-yyyy HH:mm:ss",     // Định dạng thường dùng ở VN (Có thời gian)
                    "dd/MM/yyyy HH:mm:ss",     // Định dạng / thường dùng
                    "MM/dd/yyyy HH:mm:ss",     // Định dạng Mỹ phổ biến
                    "dd-MM-yyyy",              // Chỉ ngày
                    "dd/MM/yyyy",              // Chỉ ngày
                    "M/d/yyyy H:mm:ss",        // Định dạng ngắn gọn
                    "M/d/yyyy"
                };

                // Thử ParseExact với tập hợp các định dạng đã mở rộng.
                if (DateTime.TryParseExact(dateString, formats, CultureInfo.InvariantCulture, DateTimeStyles.None, out result))
                {
                    return result;
                }

                // THỬ VỚI VĂN HÓA HIỆN TẠI (CurrentCulture) - Thường là VN
                // Thử lại ParseExact với văn hóa hiện tại, để bắt các định dạng do người dùng nhập vào.
                if (DateTime.TryParse(dateString, CultureInfo.CurrentCulture, DateTimeStyles.None, out result))
                {
                    return result;
                }

                // Nếu mọi cách đều thất bại, ném ra lỗi FormatException.
                throw new FormatException($"Chuỗi '{dateString}' trong cột '{columnName}' không được nhận dạng là định dạng DateTime hợp lệ.");
            }
            catch (Exception ex)
            {
                // Bắt các lỗi khác (ví dụ: lỗi đọc GetOrdinal)
                throw new Exception($"Lỗi khi đọc cột {columnName}: {ex.Message}");
            }
        }
    }
}