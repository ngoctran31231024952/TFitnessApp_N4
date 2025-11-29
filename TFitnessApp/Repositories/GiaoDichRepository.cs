using Microsoft.Data.Sqlite;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Configuration; // Cần thiết để sử dụng ConfigurationManager
using TFitnessApp.Database;
using TFitnessApp.Interfaces;
using TFitnessApp.Entities;

namespace TFitnessApp.Repositories
{
    /// <summary>
    /// Repository triển khai logic truy cập dữ liệu Giao Dịch bằng SQLite
    /// </summary>
    public class GiaoDichRepository : IGiaoDichRepository
    {
        private readonly DbAcess _dbAccess;

        // ĐÃ LOẠI BỎ: private readonly string _connectionString;

        public GiaoDichRepository()
        {
            _dbAccess = new DbAcess();
        }

        /// <summary>
        /// Lấy tất cả các giao dịch từ cơ sở dữ liệu.
        /// </summary>
        public List<GiaoDich> GetAll()
        {
            var giaoDichList = new List<GiaoDich>();

            // Lấy chuỗi kết nối từ App.config. Biến cục bộ này được gán giá trị.
            string connectionString = ConfigurationManager.ConnectionStrings["SQLiteConnection"].ConnectionString;

            // BP 1: Kiểm tra chuỗi kết nối.

            // Câu lệnh SQL: Đã thêm MaTK
            string sql = "SELECT MaGD, MaHV, MaGoi, MaTK, TongTien, DaThanhToan, SoTienNo, NgayGD, TrangThai FROM GiaoDich ORDER BY NgayGD DESC;";

            // SỬA LỖI: Dùng biến cục bộ 'connectionString' đã được gán giá trị
            using (var connection = new SqliteConnection(connectionString))
            {
                try
                {
                    // LỖI 'version' XẢY RA TRƯỚC HOẶC TẠI DÒNG NÀY (ArgumentException)
                    connection.Open();

                    using (var command = new SqliteCommand(sql, connection))
                    {
                        using (var reader = command.ExecuteReader())
                        {
                            while (reader.Read())
                            {
                                // BP 2: Kiểm tra dữ liệu đang đọc
                                GiaoDich giaoDich = new GiaoDich
                                {
                                    MaGD = reader.GetString(reader.GetOrdinal("MaGD")),
                                    MaHV = reader.GetString(reader.GetOrdinal("MaHV")),
                                    MaGoi = reader.GetString(reader.GetOrdinal("MaGoi")),
                                    MaTK = reader.GetString(reader.GetOrdinal("MaTK")),

                                    TongTien = reader.GetDecimal(reader.GetOrdinal("TongTien")),
                                    DaThanhToan = reader.GetDecimal(reader.GetOrdinal("DaThanhToan")),
                                    SoTienNo = reader.GetDecimal(reader.GetOrdinal("SoTienNo")),
                                    NgayGD = reader.GetDateTime(reader.GetOrdinal("NgayGD")),
                                    TrangThai = reader.GetString(reader.GetOrdinal("TrangThai"))
                                };
                                giaoDichList.Add(giaoDich);
                            }
                        }
                    }
                }
                catch (Exception ex)
                {
                    // LỖI XẢY RA: Nếu code nhảy vào đây, CÓ LỖI KẾT NỐI/SQL.
                    System.Diagnostics.Debug.WriteLine($"LỖI TRUY VẤN DB: {ex.Message}");
                    return new List<GiaoDich>();
                }
            }

            // BP 3: Kiểm tra kết quả cuối cùng
            return giaoDichList;
        }
    }
}