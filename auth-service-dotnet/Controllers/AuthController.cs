using auth_service_dotnet.Services.Interface;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity.Data;
using Microsoft.AspNetCore.Mvc;
using auth_service_dotnet.Dto;
using static auth_service_dotnet.Dto.AuthDto;
namespace auth_service_dotnet.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class AuthController : ControllerBase
    {
        private readonly IJwtService _jwt;

        public AuthController(IJwtService jwt)
        {
            _jwt = jwt;
        }

        [HttpPost("login")]
        public IActionResult Login([FromBody] AuthDto.LoginRequest req)
        {
            // Add null check
            if (req == null)
            {
                return BadRequest("Login request cannot be null");
            }

            // TEMP: Hardcoded user (will be replaced later)
            if (req.Username != "admin" || req.Password != "password")
                return Unauthorized();

            var token = _jwt.GenerateToken(req.Username, "Admin");

            var response = new AuthDto.AuthResponse
            {
                Token = token
            };

            return Ok(response);
        }
    }
}
