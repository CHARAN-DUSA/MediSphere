using MediSphere.Application.DTOs.Department;
using MediSphere.Application.Interfaces;
using MediSphere.Domain.Entities;
using MediSphere.Domain.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace MediSphere.Application.Services;

public class DepartmentService : IDepartmentService
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly ICacheService _cacheService;

    public DepartmentService(IUnitOfWork unitOfWork, ICacheService cacheService)
    {
        _unitOfWork = unitOfWork;
        _cacheService = cacheService;
    }

    public async Task<IEnumerable<DepartmentDto>> GetAllDepartmentsAsync()
    {
        var cacheKey = "medisphere:departments:all";
        var cached = await _cacheService.GetAsync<List<DepartmentDto>>(cacheKey);
        if (cached != null)
        {
            return cached;
        }

        var departments = await _unitOfWork.Repository<Department>().Query()
            .Include(d => d.Doctors)
            .Where(d => d.IsActive)
            .ToListAsync();

        var result = departments.Select(d => new DepartmentDto
        {
            Id = d.Id, 
            Name = d.Name, 
            Description = d.Description,
            IconUrl = d.IconUrl, 
            DoctorCount = d.Doctors.Count(doc => doc.IsActive)
        }).ToList();

        await _cacheService.SetAsync(cacheKey, result, TimeSpan.FromMinutes(30));
        return result;
    }

    public async Task<DepartmentDto?> GetDepartmentByIdAsync(int id)
    {
        var cacheKey = $"medisphere:department:{id}";
        var cached = await _cacheService.GetAsync<DepartmentDto>(cacheKey);
        if (cached != null)
        {
            return cached;
        }

        var d = await _unitOfWork.Repository<Department>().Query()
            .Include(x => x.Doctors).FirstOrDefaultAsync(x => x.Id == id && x.IsActive);

        if (d == null) return null;

        var result = new DepartmentDto
        {
            Id = d.Id, 
            Name = d.Name, 
            Description = d.Description,
            IconUrl = d.IconUrl, 
            DoctorCount = d.Doctors.Count(doc => doc.IsActive)
        };

        await _cacheService.SetAsync(cacheKey, result, TimeSpan.FromMinutes(30));
        return result;
    }

    public async Task<DepartmentDto> CreateDepartmentAsync(CreateDepartmentDto dto)
    {
        var dept = new Department { Name = dto.Name, Description = dto.Description, IconUrl = dto.IconUrl };
        await _unitOfWork.Repository<Department>().AddAsync(dept);
        await _unitOfWork.SaveChangesAsync();

        await InvalidateDepartmentCachesAsync(dept.Id);

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

        await InvalidateDepartmentCachesAsync(id);

        return (await GetDepartmentByIdAsync(id))!;
    }

    public async Task DeleteDepartmentAsync(int id)
    {
        var dept = await _unitOfWork.Repository<Department>().GetByIdAsync(id)
            ?? throw new KeyNotFoundException("Department not found.");
        dept.IsActive = false; dept.UpdatedAt = DateTime.UtcNow;
        await _unitOfWork.Repository<Department>().UpdateAsync(dept);
        await _unitOfWork.SaveChangesAsync();

        await InvalidateDepartmentCachesAsync(id);
    }

    private async Task InvalidateDepartmentCachesAsync(int id)
    {
        await _cacheService.RemoveAsync("medisphere:departments:all");
        await _cacheService.RemoveAsync($"medisphere:department:{id}");
        await _cacheService.RemoveByPrefixAsync("medisphere:doctors:list:");
        await _cacheService.RemoveByPrefixAsync("medisphere:home:");
        await _cacheService.RemoveAsync("medisphere:admin:dashboard");
    }
}
