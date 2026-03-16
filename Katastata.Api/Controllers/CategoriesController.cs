using Katastata.Api.Data;
using Katastata.Api.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Katastata.Api.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class CategoriesController : ControllerBase
    {
        private readonly AppDbContext _db;
        public CategoriesController(AppDbContext db) => _db = db;

        [HttpGet]
        public ActionResult<List<Category>> GetAll() => _db.Categories.OrderBy(c => c.Name).ToList();

        [HttpGet("exists/{name}")]
        public ActionResult<bool> Exists(string name) => _db.Categories.Any(c => c.Name == name);

        [HttpPost]
        public IActionResult Create([FromBody] Category category)
        {
            _db.Categories.Add(new Category { Name = category.Name });
            _db.SaveChanges();
            return Ok();
        }

        [HttpDelete("{id:int}")]
        public IActionResult Delete(int id)
        {
            var hasPrograms = _db.Programs.Any(p => p.CategoryId == id);
            if (hasPrograms) return BadRequest("Category is not empty");

            var category = _db.Categories.FirstOrDefault(c => c.Id == id);
            if (category == null) return NotFound();

            _db.Categories.Remove(category);
            _db.SaveChanges();
            return NoContent();
        }
    }
}
