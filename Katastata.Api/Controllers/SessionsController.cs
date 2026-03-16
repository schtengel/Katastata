using Katastata.Api.Contracts;
using Katastata.Api.Data;
using Katastata.Api.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Katastata.Api.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class SessionsController : ControllerBase
    {
        private readonly AppDbContext _db;
        public SessionsController(AppDbContext db) => _db = db;

        [HttpGet("{userId:int}")]
        public IActionResult GetByUser(int userId)
        {
            var sessions = _db.Sessions
                .Where(s => s.UserId == userId)
                .Include(s => s.Program)
                .ThenInclude(p => p.Category)
                .OrderByDescending(s => s.StartTime)
                .ToList();

            return Ok(sessions.Select(s => new
            {
                s.Id,
                s.UserId,
                s.ProgramId,
                s.StartTime,
                s.EndTime,
                Program = s.Program == null ? null : new
                {
                    s.Program.Id,
                    s.Program.Name,
                    s.Program.Path,
                    s.Program.CategoryId,
                    Category = s.Program.Category == null ? null : new { s.Program.Category.Id, s.Program.Category.Name }
                }
            }));
        }

        [HttpPost]
        public IActionResult Create([FromBody] SessionCreateRequest request)
        {
            var session = new Session
            {
                UserId = request.UserId,
                ProgramId = request.ProgramId,
                StartTime = request.StartTime,
                EndTime = request.EndTime
            };
            _db.Sessions.Add(session);

            var stat = _db.Statistics.FirstOrDefault(s => s.UserId == request.UserId && s.ProgramId == request.ProgramId);
            if (stat == null)
            {
                stat = new Statistics
                {
                    UserId = request.UserId,
                    ProgramId = request.ProgramId,
                    TotalTime = TimeSpan.Zero
                };
                _db.Statistics.Add(stat);
            }

            stat.TotalTime += request.EndTime - request.StartTime;
            stat.LastLaunch = request.StartTime > (stat.LastLaunch ?? DateTime.MinValue)
                ? request.StartTime
                : stat.LastLaunch;

            _db.SaveChanges();
            return Ok();
        }
    }
}
