using System.Collections.Generic;
using TFitnessApp.Entities;

namespace TFitnessApp.Interfaces
{
    public interface IGiaoDichRepository
    {
        List<GiaoDich> GetAll();
    }
}