using MediSphere.Application.DTOs.Department;
using MediSphere.Application.Interfaces;
using MediSphere.Domain.Entities;
using MediSphere.Domain.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace MediSphere.Application.Services;

public class DepartmentService : IDepartmentService
{
    private readonly IUnitOfWork _unitOfWork;

    public DepartmentService(IUnitOfWork unitOfWork) => _unitOfWork = unitOfWork;

    public async Task<IEnumerable<DepartmentDto>> GetAllDepartmentsAsync()
    {
        var departments = await _unitOfWork.Repository<Department>().Query()
            .Include(d => d.Doctors)
            .Where(d => d.IsActive)
            .ToListAsync();
        return departments.Select(d => new DepartmentDto
        {
            Id = d.Id, Name = d.Name, Description = d.Description,
            IconUrl = d.IconUrl, DoctorCount = d.Doctors.Count(doc => doc.IsActive)
        });
    }

    public async Task<DepartmentDto?> GetDepartmentByIdAsync(int id)
    {
        var d = await _unitOfWork.Repository<Department>().Query()
            .Include(x => x.Doctors).FirstOrDefaultAsync(x => x.Id == id && x.IsActive);
        return d == null ? null : new DepartmentDto
        {
            Id = d.Id, Name = d.Name, Description = d.Description,
            IconUrl = d.IconUrl, DoctorCount = d.Doctors.Count(doc => doc.IsActive)
        };
    }

    public async Task<DepartmentDto> CreateDepartmentAsync(CreateDepartmentDto dto)
    {
        var dept = new Department { Name = dto.Name, Description = dto.Description, IconUrl = dto.IconUrl };
        await _unitOfWork.Repository<Department>().AddAsync(dept);
        await _unitOfWork.SaveChangesAsync();
        return (await GetDepartmentByIdAsync(dept.Id))!;
    }

    public async Task<DepartmentDto> UpdateDepartmentAsync(int id, CreateDepartmentDto dto)
    {
        var dept = await _unitOfWork.Repository<Department>().GetByIdAsync(id)
            ?? throw new KeyNotFoundException("Department not found.");
        dept.Name = dto.Name; dept.Description = dto.Description;
        dept.IconUrl = dto.IconUrl; dept.UpdatedAt = DateTime.UtcNow;
        await _unitOfWork.Repository<Department>().UpdateAsync(dept);
        await _unitOfWork.SaveChangesAsync();
        return (await GetDepartmentByIdAsync(id))!;
    }

    public async Task DeleteDepartmentAsync(int id)
    {
        var dept = await _unitOfWork.Repository<Department>().GetByIdAsync(id)
            ?? throw new KeyNotFoundException("Department not found.");
        dept.IsActive = false; dept.UpdatedAt = DateTime.UtcNow;
        await _unitOfWork.Repository<Department>().UpdateAsync(dept);
        await _unitOfWork.SaveChangesAsync();
    }
}
