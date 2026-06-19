using MediSphere.Application.DTOs.Department;

namespace MediSphere.Application.Interfaces;

public interface IDepartmentService
{
    Task<IEnumerable<DepartmentDto>> GetAllDepartmentsAsync();
    Task<DepartmentDto?> GetDepartmentByIdAsync(int id);
    Task<DepartmentDto> CreateDepartmentAsync(CreateDepartmentDto dto);
    Task<DepartmentDto> UpdateDepartmentAsync(int id, CreateDepartmentDto dto);
    Task DeleteDepartmentAsync(int id);
}
