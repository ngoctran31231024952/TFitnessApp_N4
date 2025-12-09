using System;
using System.IO;
using Microsoft.Data.Sqlite;

namespace TFitnessApp.Database
{
    public class TruyCapDB
    {
        private static readonly string ChuoiKetNoi;

        static TruyCapDB()
        {
            string duongDanDB = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Database", "Tfitness.db");
            ChuoiKetNoi = $"Data Source={duongDanDB};";
        }
        // Thuộc tính để truy cập chuỗi kết nối
        public string _ChuoiKetNoi => ChuoiKetNoi;
        // Phương thức tạo và mở kết nối
        public static SqliteConnection TaoKetNoi()
        {
            var conn = new SqliteConnection(ChuoiKetNoi);
            conn.Open();
            return conn;
        }
    }
}
