using Katastata.Api.Contracts;
using Katastata.Api.Data;
using Katastata.Api.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Katastata.Api.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class ProgramsController : ControllerBase
    {
        private readonly AppDbContext _db;
        public ProgramsController(AppDbContext db) => _db = db;

        [HttpGet]
        public IActionResult GetAll([FromQuery] int userId)
        {
            var programs = _db.Programs
                .Include(p => p.Category)
                .Include(p => p.Statistics.Where(s => s.UserId == userId))
                .ToList();
            return Ok(programs.Select(MapProgram));
        }

        [HttpGet("by-category/{categoryId:int}")]
        public IActionResult ByCategory(int categoryId)
        {
            var programs = _db.Programs.Where(p => p.CategoryId == categoryId).ToList();
            return Ok(programs.Select(MapProgram));
        }

        [HttpPost("ensure")]
        public IActionResult Ensure([FromBody] ProgramSyncRequest request)
        {
            var program = _db.Programs.Include(p => p.Category).FirstOrDefault(p => p.Path == request.Path);
            if (program == null)
            {
                program = new ProgramEntity { Name = request.Name, Path = request.Path, CategoryId = 1 };
                _db.Programs.Add(program);
                _db.SaveChanges();
                _db.Entry(program).Reference(x => x.Category).Load();
            }
            return Ok(MapProgram(program));
        }

        [HttpPost("ensure/bulk")]
        public IActionResult EnsureBulk([FromBody] List<ProgramSyncRequest> requests)
        {
            var existingPaths = _db.Programs.Select(p => p.Path).ToHashSet();
            foreach (var req in requests.Where(r => !existingPaths.Contains(r.Path)))
            {
                _db.Programs.Add(new ProgramEntity { Name = req.Name, Path = req.Path, CategoryId = 1 });
            }

            _db.SaveChanges();
            return Ok();
        }

        [HttpPut("{id:int}")]
        public IActionResult Update(int id, [FromBody] ProgramEntity model)
        {
            var program = _db.Programs.FirstOrDefault(p => p.Id == id);
            if (program == null) return NotFound();
            program.Name = model.Name;
            program.CategoryId = model.CategoryId;
            _db.SaveChanges();
            return NoContent();
        }

        private static object MapProgram(ProgramEntity p) => new
        {
            p.Id,
            p.Name,
            p.Path,
            p.CategoryId,
            Category = p.Category == null ? null : new { p.Category.Id, p.Category.Name },
            Statistics = p.Statistics.Select(s => new
            {
                s.Id,
                s.UserId,
                s.ProgramId,
                s.TotalTime,
                s.LastLaunch
            })
        };
    }
}
