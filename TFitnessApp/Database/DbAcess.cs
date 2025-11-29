using System;
using System.Configuration;
using Microsoft.Data.Sqlite; 

namespace TFitnessApp.Database
{
    // Đổi từ internal sang public để các lớp Repository khác có thể sử dụng
    public class DbAcess
    {
        // Khai báo biến lưu trữ chuỗi kết nối
        private readonly string _connectionString;

        // Constructor: Khởi tạo lớp và lấy chuỗi kết nối từ App.config
        public DbAcess()
        {
            // Lấy chuỗi kết nối có tên "SQLiteConnection" từ App.config
            // Lưu ý: Cần thêm Package NuGet System.Configuration.ConfigurationManager
            // và đảm bảo nó được tham chiếu đúng.
            _connectionString = ConfigurationManager.ConnectionStrings["SQLiteConnection"].ConnectionString;
        }

        // Phương thức chung để thực thi các lệnh INSERT/UPDATE/DELETE
        public int ExecuteNonQuery(string sql, params SqliteParameter[] parameters)
        {
            using (var connection = new SqliteConnection(_connectionString))
            {
                connection.Open();
                using (var command = new SqliteCommand(sql, connection))
                {
                    if (parameters != null)
                    {
                        command.Parameters.AddRange(parameters);
                    }
                    return command.ExecuteNonQuery(); // Trả về số dòng bị ảnh hưởng
                }
            }
        }

        // Phương thức chung để lấy một giá trị đơn (ví dụ: đếm số lượng)
        public object ExecuteScalar(string sql, params SqliteParameter[] parameters)
        {
            using (var connection = new SqliteConnection(_connectionString))
            {
                connection.Open();
                using (var command = new SqliteCommand(sql, connection))
                {
                    if (parameters != null)
                    {
                        command.Parameters.AddRange(parameters);
                    }
                    return command.ExecuteScalar();
                }
            }
        }

        // Phương thức chung để đọc dữ liệu (sẽ được sử dụng trong Repository cụ thể)
        // Phương thức này có thể được sử dụng trong lớp Repository chi tiết hơn
        public SqliteDataReader ExecuteReader(string sql, SqliteConnection connection, params SqliteParameter[] parameters)
        {
            using (var command = new SqliteCommand(sql, connection))
            {
                if (parameters != null)
                {
                    command.Parameters.AddRange(parameters);
                }
                // CommandBehavior.CloseConnection đảm bảo connection được đóng khi reader bị đóng
                return command.ExecuteReader(System.Data.CommandBehavior.CloseConnection);
            }
        }
    }
}