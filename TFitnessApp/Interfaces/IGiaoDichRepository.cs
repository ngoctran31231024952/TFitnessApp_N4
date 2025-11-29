using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TFitnessApp.Entities;

namespace TFitnessApp.Interfaces
{
    public interface IGiaoDichRepository
    {
        // Lấy tất cả giao dịch
        List<GiaoDich> GetAll();

        // Bạn có thể thêm các phương thức khác ở đây:
        // Task<GiaoDich> GetByIdAsync(string maGd);
        // int Create(GiaoDich obj);
    }
}
