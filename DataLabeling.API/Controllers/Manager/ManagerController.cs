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
    public async Task<IActionResult> GetProjects([FromQuery] int pageNumber = 1, [FromQuery] int pageSize = 20, [FromQuery] string? status = null, [FromQuery] string? searchTerm = null)
    {
        var result = await _managerService.GetProjectsAsync(pageNumber, pageSize, status, searchTerm);
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

    [HttpDelete("projects/{id}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> DeleteProject([FromRoute] long id)
    {
        try
        {
            var success = await _managerService.DeleteProjectAsync(id);
            if (!success) return NotFound($"Project {id} not found.");
            return NoContent();
        }
        catch (Exception ex)
        {
            return BadRequest(new { Message = ex.Message });
        }
    }

    [HttpDelete("projects/bulk")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> BulkDeleteProjects([FromBody] List<long> projectIds)
    {
        if (projectIds == null || projectIds.Count == 0)
            return BadRequest(new { Message = "No project IDs provided" });

        var result = await _managerService.BulkDeleteProjectsAsync(projectIds);
        return Ok(result);
    }
    #endregion

    #region Datasets
    [HttpGet("datasets")]
    public async Task<IActionResult> GetDatasets([FromQuery] long projectId, [FromQuery] int pageNumber = 1, [FromQuery] int pageSize = 20, [FromQuery] string? status = null, [FromQuery] string? searchTerm = null)
    {
        var result = await _managerService.GetDatasetsAsync(projectId, pageNumber, pageSize, status, searchTerm);
        return Ok(result);
    }

    [HttpGet("datasets/{id}")]
    public async Task<IActionResult> GetDatasetById([FromRoute] long id)
    {
        var dataset = await _managerService.GetDatasetByIdAsync(id);
        if (dataset == null) return NotFound($"Dataset {id} not found.");
        return Ok(dataset);
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

    [HttpDelete("datasets/{id}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> DeleteDataset([FromRoute] long id)
    {
        try
        {
            var success = await _managerService.DeleteDatasetAsync(id);
            if (!success) return NotFound($"Dataset {id} not found.");
            return NoContent();
        }
        catch (Exception ex)
        {
            return BadRequest(new { Message = ex.Message });
        }
    }

    [HttpDelete("datasets/bulk")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> BulkDeleteDatasets([FromBody] List<long> datasetIds)
    {
        if (datasetIds == null || datasetIds.Count == 0)
            return BadRequest(new { Message = "No dataset IDs provided" });

        var result = await _managerService.BulkDeleteDatasetsAsync(datasetIds);
        return Ok(result);
    }
    #endregion

    #region Data Items
    [HttpGet("data-items")]
    public async Task<IActionResult> GetDataItems([FromQuery] long datasetId, [FromQuery] int pageNumber = 1, [FromQuery] int pageSize = 20, [FromQuery] string? status = null, [FromQuery] string? searchTerm = null)
    {
        var result = await _managerService.GetDataItemsAsync(datasetId, pageNumber, pageSize, status, searchTerm);
        return Ok(result);
    }

    [HttpGet("data-items/{id}")]
    public async Task<IActionResult> GetDataItemById([FromRoute] long id)
    {
        var dataItem = await _managerService.GetDataItemByIdAsync(id);
        if (dataItem == null) return NotFound($"Data item {id} not found.");
        return Ok(dataItem);
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

    [HttpDelete("data-items/{id}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> DeleteDataItem([FromRoute] long id)
    {
        try
        {
            var success = await _managerService.DeleteDataItemAsync(id);
            if (!success) return NotFound($"Data item {id} not found.");
            return NoContent();
        }
        catch (Exception ex)
        {
            return BadRequest(new { Message = ex.Message });
        }
    }

    [HttpDelete("data-items/bulk")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> BulkDeleteDataItems([FromBody] List<long> dataItemIds)
    {
        if (dataItemIds == null || dataItemIds.Count == 0)
            return BadRequest(new { Message = "No data item IDs provided" });

        var result = await _managerService.BulkDeleteDataItemsAsync(dataItemIds);
        return Ok(result);
    }
    #endregion

    #region Labels
    [HttpGet("projects/{projectId}/labels")]
    public async Task<IActionResult> GetLabels([FromRoute] long projectId, [FromQuery] int pageNumber = 1, [FromQuery] int pageSize = 20)
    {
        var result = await _managerService.GetLabelsAsync(projectId, pageNumber, pageSize);
        return Ok(result);
    }

    [HttpGet("labels/{id}")]
    public async Task<IActionResult> GetLabelById([FromRoute] long id)
    {
        var label = await _managerService.GetLabelByIdAsync(id);
        if (label == null) return NotFound($"Label {id} not found.");
        return Ok(label);
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

    [HttpDelete("labels/bulk")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> BulkDeleteLabels([FromBody] List<long> labelIds)
    {
        if (labelIds == null || labelIds.Count == 0)
            return BadRequest(new { Message = "No label IDs provided" });

        var result = await _managerService.BulkDeleteLabelsAsync(labelIds);
        return Ok(result);
    }
    #endregion

    #region Task Assignment & Tracking
    [HttpPost("data-items/assign")]
    public async Task<IActionResult> AssignDataItems([FromBody] AssignDataItemsDto request)
    {
        try
        {
            var success = await _managerService.AssignDataItemsAsync(request.DataItemIds, request.UserId, request.Role);
            return Ok(new { message = "Data items assigned successfully.", dataItemsCount = request.DataItemIds.Count });
        }
        catch (KeyNotFoundException ex)
        {
            return NotFound(new { message = ex.Message });
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
        catch (Exception ex)
        {
            return BadRequest(new { message = ex.Message });
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

    [HttpGet("datasets/{datasetId}/assignments")]
    public async Task<IActionResult> GetDatasetAssignments([FromRoute] long datasetId)
    {
        try
        {
            var assignments = await _managerService.GetDatasetAssignmentsAsync(datasetId);
            return Ok(assignments);
        }
        catch (KeyNotFoundException ex)
        {
            return NotFound(ex.Message);
        }
    }
    #endregion

    #region Quality Overview
    [HttpGet("quality-overview/by-project")]
    public async Task<IActionResult> GetQualityOverviewByProject()
    {
        try
        {
            var overview = await _managerService.GetQualityOverviewByProjectAsync();
            return Ok(overview);
        }
        catch (Exception ex)
        {
            return BadRequest(new { Message = ex.Message });
        }
    }

    [HttpGet("quality-overview/by-dataset")]
    public async Task<IActionResult> GetQualityOverviewByDataset()
    {
        try
        {
            var overview = await _managerService.GetQualityOverviewByDatasetAsync();
            return Ok(overview);
        }
        catch (Exception ex)
        {
            return BadRequest(new { Message = ex.Message });
        }
    }

    [HttpGet("quality-overview/by-dataitem")]
    public async Task<IActionResult> GetQualityOverviewByDataItem()
    {
        try
        {
            var overview = await _managerService.GetQualityOverviewByDataItemAsync();
            return Ok(overview);
        }
        catch (Exception ex)
        {
            return BadRequest(new { Message = ex.Message });
        }
    }

    [HttpGet("quality-overview/by-annotator")]
    public async Task<IActionResult> GetQualityOverviewByAnnotator()
    {
        try
        {
            var overview = await _managerService.GetQualityOverviewByAnnotatorAsync();
            return Ok(overview);
        }
        catch (Exception ex)
        {
            return BadRequest(new { Message = ex.Message });
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
    public async Task<IActionResult> GetExportJobs([FromRoute] long projectId, [FromQuery] int pageNumber = 1, [FromQuery] int pageSize = 20)
    {
        var jobs = await _managerService.GetExportJobsAsync(projectId, pageNumber, pageSize);
        return Ok(jobs);
    }
    #endregion

    #region User Management
    [HttpGet("users")]
    public async Task<IActionResult> GetUsers([FromQuery] int pageNumber = 1, [FromQuery] int pageSize = 20, [FromQuery] string? role = null, [FromQuery] string? status = null)
    {
        var result = await _managerService.GetUsersAsync(pageNumber, pageSize, role, status);
        return Ok(result);
    }

    [HttpGet("users/{id}")]
    public async Task<IActionResult> GetUserById([FromRoute] long id)
    {
        var user = await _managerService.GetUserByIdAsync(id);
        if (user == null) return NotFound($"User {id} not found.");
        return Ok(user);
    }
    #endregion

}
