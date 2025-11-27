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

            // Đảm bảo tệp database được tạo và schema được thiết lập
            InitializeDatabase();
        }

        // Phương thức để đảm bảo các bảng CSDL đã được tạo
        public void InitializeDatabase()
        {
            // Sử dụng một kết nối mới chỉ để kiểm tra và tạo schema
            try
            {
                using (var connection = new SqliteConnection(_connectionString))
                {
                    connection.Open();

                    // Lệnh SQL để kiểm tra xem bảng 'TaiKhoan' đã tồn tại chưa.
                    // Nếu chưa, nó sẽ chạy toàn bộ script tạo bảng (ví dụ mẫu, bạn có thể thay bằng logic kiểm tra phức tạp hơn)
                    using (var command = new SqliteCommand("SELECT name FROM sqlite_master WHERE type='table' AND name='TaiKhoan';", connection))
                    {
                        var result = command.ExecuteScalar();

                        if (result == null)
                        {
                            // Nếu bảng chưa tồn tại, ta chạy toàn bộ script schema.
                            Console.WriteLine("Cơ sở dữ liệu chưa được khởi tạo. Cần chạy script SQL.");
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                // Xử lý lỗi kết nối hoặc truy vấn
                Console.WriteLine($"Lỗi khởi tạo Database: {ex.Message}");
                // Trong ứng dụng WPF, bạn có thể hiển thị MessageBox
                System.Windows.MessageBox.Show($"Lỗi kết nối CSDL: {ex.Message}", "Lỗi Database", System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Error);
            }
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