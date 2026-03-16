using Katastata.Api.Data;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Katastata.Api.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class StatisticsController : ControllerBase
    {
        private readonly AppDbContext _db;
        public StatisticsController(AppDbContext db) => _db = db;

        [HttpGet("{userId:int}")]
        public IActionResult GetByUser(int userId)
        {
            var stats = _db.Statistics
                .Where(st => st.UserId == userId)
                .Include(st => st.Program)
                .OrderByDescending(st => st.TotalTime)
                .ToList();

            return Ok(stats.Select(st => new
            {
                st.Id,
                st.UserId,
                st.ProgramId,
                st.TotalTime,
                st.LastLaunch,
                Program = st.Program == null ? null : new { st.Program.Id, st.Program.Name, st.Program.Path, st.Program.CategoryId }
            }));
        }
    }
}
