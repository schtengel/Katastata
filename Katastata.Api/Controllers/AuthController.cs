using Katastata.Api.Contracts;
using Katastata.Api.Data;
using Katastata.Api.Helpers;
using Katastata.Api.Models;
using Microsoft.AspNetCore.Mvc;

namespace Katastata.Api.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class AuthController : ControllerBase
    {
        private readonly AppDbContext _db;

        public AuthController(AppDbContext db) => _db = db;

        [HttpPost("register")]
        public IActionResult Register([FromBody] AuthRequest request)
        {
            if (_db.Users.Any(u => u.Username == request.Username))
            {
                return Ok(new AuthResponse { Success = false, Message = "Такой пользователь уже существует." });
            }

            var user = new User
            {
                Username = request.Username,
                PasswordHash = PasswordHelper.HashPassword(request.Password),
                PCName = request.PcName
            };

            _db.Users.Add(user);
            _db.SaveChanges();

            return Ok(new AuthResponse { Success = true, UserId = user.Id });
        }

        [HttpPost("login")]
        public IActionResult Login([FromBody] AuthRequest request)
        {
            var hash = PasswordHelper.HashPassword(request.Password);
            var user = _db.Users.FirstOrDefault(u => u.Username == request.Username && u.PasswordHash == hash);
            if (user == null)
            {
                return Ok(new AuthResponse { Success = false, Message = "Неверный логин или пароль." });
            }

            return Ok(new AuthResponse { Success = true, UserId = user.Id });
        }
    }
}
