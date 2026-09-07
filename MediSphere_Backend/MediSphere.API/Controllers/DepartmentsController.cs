using MediSphere.Application.DTOs.Common;
using MediSphere.Application.DTOs.Department;
using MediSphere.Application.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace MediSphere.API.Controllers;

[ApiController]
[Route("api/[controller]")]
public class DepartmentsController : ControllerBase
{
    private readonly IDepartmentService _departmentService;

    public DepartmentsController(IDepartmentService departmentService) => _departmentService = departmentService;

    [HttpGet]
    public async Task<ActionResult<ApiResponse<IEnumerable<DepartmentDto>>>> GetAll()
    {
        var result = await _departmentService.GetAllDepartmentsAsync();
        return Ok(ApiResponse<IEnumerable<DepartmentDto>>.Ok(result));
    }

    [HttpGet("{id}")]
    public async Task<ActionResult<ApiResponse<DepartmentDto>>> GetById(int id)
    {
        var result = await _departmentService.GetDepartmentByIdAsync(id);
        if (result == null) return NotFound(ApiResponse<DepartmentDto>.Fail("Department not found."));
        return Ok(ApiResponse<DepartmentDto>.Ok(result));
    }

    [Authorize(Roles = "Admin")]
    [HttpPost]
    public async Task<ActionResult<ApiResponse<DepartmentDto>>> Create([FromBody] CreateDepartmentDto dto)
    {
        var result = await _departmentService.CreateDepartmentAsync(dto);
        return CreatedAtAction(nameof(GetById), new { id = result.Id }, ApiResponse<DepartmentDto>.Ok(result, "Department created."));
    }

    [Authorize(Roles = "Admin")]
    [HttpPut("{id}")]
    public async Task<ActionResult<ApiResponse<DepartmentDto>>> Update(int id, [FromBody] CreateDepartmentDto dto)
    {
        var result = await _departmentService.UpdateDepartmentAsync(id, dto);
        return Ok(ApiResponse<DepartmentDto>.Ok(result, "Department updated."));
    }

    [Authorize(Roles = "Admin")]
    [HttpDelete("{id}")]
    public async Task<ActionResult<ApiResponse<object>>> Delete(int id)
    {
        await _departmentService.DeleteDepartmentAsync(id);
        return Ok(ApiResponse<object>.Ok(null!, "Department deleted."));
    }
}
