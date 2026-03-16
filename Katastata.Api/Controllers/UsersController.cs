using Katastata.Api.Data;
using Microsoft.AspNetCore.Mvc;

namespace Katastata.Api.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class UsersController : ControllerBase
    {
        private readonly AppDbContext _db;
        public UsersController(AppDbContext db) => _db = db;

        [HttpDelete("{id:int}")]
        public IActionResult Delete(int id)
        {
            var user = _db.Users.FirstOrDefault(u => u.Id == id);
            if (user == null) return NotFound();

            _db.Users.Remove(user);
            _db.SaveChanges();
            return NoContent();
        }
    }
}
