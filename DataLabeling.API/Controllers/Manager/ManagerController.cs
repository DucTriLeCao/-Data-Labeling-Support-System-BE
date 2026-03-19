using DataLabeling.Application.Interfaces;
using DataLabeling.Domain.DTOs.Manager;
using DataLabeling.Domain.DTOs.Auth;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace DataLabeling.API.Controllers.Manager;

[ApiController]
[Route("api/manager")]
// Authorize Managers and Admins (Managers primarily, Admins override)
[Authorize(Roles = $"{UserRole.Manager},{UserRole.Admin}")]
public class ManagerController : ControllerBase
{
    private readonly IManagerService _managerService;

    public ManagerController(IManagerService managerService)
    {
        _managerService = managerService;
    }

    #region Projects
    [HttpGet("projects")]
    public async Task<IActionResult> GetProjects([FromQuery] int pageNumber = 1, [FromQuery] int pageSize = 20, [FromQuery] string? status = null)
    {
        var result = await _managerService.GetProjectsAsync(pageNumber, pageSize, status);
        return Ok(result);
    }

    [HttpGet("projects/{id}")]
    public async Task<IActionResult> GetProjectById([FromRoute] long id)
    {
        var project = await _managerService.GetProjectByIdAsync(id);
        if (project == null) return NotFound($"Project {id} not found.");
        return Ok(project);
    }

    [HttpPost("projects")]
    public async Task<IActionResult> CreateProject([FromBody] CreateProjectDto request)
    {
        try
        {
            var p = await _managerService.CreateProjectAsync(request);
            return CreatedAtAction(nameof(GetProjectById), new { id = p.Id }, p);
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { Message = ex.Message });
        }
    }

    [HttpPut("projects/{id}")]
    public async Task<IActionResult> UpdateProject([FromRoute] long id, [FromBody] UpdateProjectDto request)
    {
        try
        {
            var p = await _managerService.UpdateProjectAsync(id, request);
            return Ok(p);
        }
        catch (KeyNotFoundException ex)
        {
            return NotFound(ex.Message);
        }
    }
    #endregion

    #region Datasets
    [HttpGet("projects/{projectId}/datasets")]
    public async Task<IActionResult> GetDatasets([FromRoute] long projectId, [FromQuery] int pageNumber = 1, [FromQuery] int pageSize = 20, [FromQuery] string? status = null)
    {
        var result = await _managerService.GetDatasetsAsync(projectId, pageNumber, pageSize, status);
        return Ok(result);
    }

    [HttpPost("projects/{projectId}/datasets")]
    public async Task<IActionResult> CreateDataset([FromRoute] long projectId, [FromBody] CreateDatasetDto request)
    {
        try
        {
            var ds = await _managerService.CreateDatasetAsync(projectId, request);
            return Ok(ds);
        }
        catch (KeyNotFoundException ex)
        {
            return NotFound(ex.Message);
        }
    }

    [HttpPut("datasets/{id}")]
    public async Task<IActionResult> UpdateDataset([FromRoute] long id, [FromBody] UpdateDatasetDto request)
    {
        try
        {
            var ds = await _managerService.UpdateDatasetAsync(id, request);
            return Ok(ds);
        }
        catch (KeyNotFoundException ex)
        {
            return NotFound(ex.Message);
        }
    }

    [HttpGet("datasets/{datasetId}/items")]
    public async Task<IActionResult> GetDataItems([FromRoute] long datasetId, [FromQuery] int pageNumber = 1, [FromQuery] int pageSize = 50, [FromQuery] string? status = null)
    {
        var result = await _managerService.GetDataItemsAsync(datasetId, pageNumber, pageSize, status);
        return Ok(result);
    }

    [HttpPost("datasets/{datasetId}/items")]
    [Consumes("multipart/form-data")]
    public async Task<IActionResult> CreateDataItem([FromRoute] long datasetId, IFormFile file)
    {
        if (file == null || file.Length == 0)
            return BadRequest(new { Message = "No file uploaded." });

        try
        {
            var di = await _managerService.CreateDataItemAsync(datasetId, file.OpenReadStream(), file.FileName);
            return Ok(di);
        }
        catch (KeyNotFoundException ex)
        {
            return NotFound(ex.Message);
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { Message = ex.Message });
        }
    }
    #endregion

    #region Labels
    [HttpGet("projects/{projectId}/labels")]
    public async Task<IActionResult> GetLabels([FromRoute] long projectId, [FromQuery] int pageNumber = 1, [FromQuery] int pageSize = 100)
    {
        var result = await _managerService.GetLabelsAsync(projectId, pageNumber, pageSize);
        return Ok(result);
    }

    [HttpPost("projects/{projectId}/labels")]
    public async Task<IActionResult> CreateLabel([FromRoute] long projectId, [FromBody] CreateLabelDto request)
    {
        try
        {
            var lbl = await _managerService.CreateLabelAsync(projectId, request);
            return Ok(lbl);
        }
        catch (KeyNotFoundException ex)
        {
            return NotFound(ex.Message);
        }
    }

    [HttpPut("labels/{id}")]
    public async Task<IActionResult> UpdateLabel([FromRoute] long id, [FromBody] UpdateLabelDto request)
    {
        try
        {
            var lbl = await _managerService.UpdateLabelAsync(id, request);
            return Ok(lbl);
        }
        catch (KeyNotFoundException ex)
        {
            return NotFound(ex.Message);
        }
    }

    [HttpDelete("labels/{id}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> DeleteLabel([FromRoute] long id)
    {
        try
        {
            var success = await _managerService.DeleteLabelAsync(id);
            if (!success) return NotFound($"Label {id} not found.");
            return NoContent();
        }
        catch(InvalidOperationException ex)
        {
            return BadRequest(new { Message = ex.Message });
        }
    }
    #endregion

    #region Task Assignment & Tracking
    [HttpPost("datasets/{datasetId}/assign")]
    public async Task<IActionResult> AssignDataset([FromRoute] long datasetId, [FromBody] AssignDatasetDto request)
    {
        try
        {
            await _managerService.AssignDatasetAsync(datasetId, request);
            return Ok(new { Message = "Assigned successfully." });
        }
        catch (KeyNotFoundException ex)
        {
            return NotFound(ex.Message);
        }
    }

    [HttpGet("datasets/{datasetId}/progress")]
    public async Task<IActionResult> GetDatasetProgress([FromRoute] long datasetId)
    {
        try
        {
            var progress = await _managerService.GetDatasetProgressAsync(datasetId);
            return Ok(progress);
        }
        catch (KeyNotFoundException ex)
        {
            return NotFound(ex.Message);
        }
    }
    #endregion

    #region Quality Overview
    [HttpGet("projects/{projectId}/quality-overview")]
    public async Task<IActionResult> GetQualityOverview([FromRoute] long projectId)
    {
        try
        {
            var overview = await _managerService.GetQualityOverviewAsync(projectId);
            return Ok(overview);
        }
        catch (KeyNotFoundException ex)
        {
            return NotFound(ex.Message);
        }
    }
    #endregion

    #region Export Data
    [HttpPost("projects/{projectId}/export")]
    public async Task<IActionResult> CreateExportJob([FromRoute] long projectId, [FromBody] ExportRequestDto request)
    {
        try
        {
            // Extract the user id of the person requesting the export
            long userId = 0;
            var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier);
            if (userIdClaim != null) long.TryParse(userIdClaim.Value, out userId);

            var job = await _managerService.CreateExportJobAsync(projectId, request, userId);
            return Ok(job);
        }
        catch (KeyNotFoundException ex)
        {
            return NotFound(ex.Message);
        }
    }

    [HttpGet("projects/{projectId}/export-jobs")]
    public async Task<IActionResult> GetExportJobs([FromRoute] long projectId)
    {
        var jobs = await _managerService.GetExportJobsAsync(projectId);
        return Ok(jobs);
    }
    #endregion
}
