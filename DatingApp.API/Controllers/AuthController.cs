using System;
using System.Collections.Generic;
using System.IdentityModel.Tokens.Jwt;
using System.Linq;
using System.Security.Claims;
using System.Text;
using System.Threading.Tasks;
using DatingApp.API.Data;
using DatingApp.API.Dtos;
using DatingApp.API.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.IdentityModel.Tokens;

namespace DatingApp.API.Controllers
{
    [Route("api/[controller]/[Action]")]
    [ApiController]
    public class AuthController : ControllerBase
    {
        private readonly IAuthRepository _authRepository;
        private readonly IConfiguration _config;
        public AuthController(IAuthRepository authRepository, IConfiguration config)
        {
            _authRepository = authRepository;
            _config = config;
        }

        [HttpPost]
        public async Task<IActionResult> Register(UserForRegisterDto userForRegisterDto)
        {
            userForRegisterDto.UserName = userForRegisterDto.UserName.ToLower();

            if (await _authRepository.UserExists(userForRegisterDto.UserName))
            {
                ModelState.AddModelError("UserName", "Username already exists");
                //var problemDetails = new ValidationProblemDetails(ModelState)
                //{
                //    Status = StatusCodes.Status400BadRequest,
                //};
                //return BadRequest(problemDetails);
                
                return ValidationProblem(ModelState);
                //return BadRequest("Username already exists");
            }

            User userToCreate = new User
            {
                UserName = userForRegisterDto.UserName
            };

            User createdUser = await _authRepository.Register(userToCreate, userForRegisterDto.Password);

            return StatusCode(StatusCodes.Status201Created);
        }

        [HttpPost]
        public async Task<IActionResult> LogIn(UserForLogInDto userForLogInDto)
        {
            var user = await _authRepository.LogIn(userForLogInDto.UserName.ToLower(), userForLogInDto.Password);

            if (user == null)
            {
                return Unauthorized();
            }

            var claims = new[]
            {
                new Claim(ClaimTypes.NameIdentifier,user.Id.ToString()),
                new Claim(ClaimTypes.Name,user.UserName)
            };

            var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_config.GetSection("AppSettings:Token").Value));

            var cred = new SigningCredentials(key, SecurityAlgorithms.HmacSha512Signature);

            var tokenDecriptor = new SecurityTokenDescriptor
            {
                Subject = new ClaimsIdentity(claims),
                Expires = DateTime.Now.AddDays(1),
                SigningCredentials = cred
            };

            var tokenHandler = new JwtSecurityTokenHandler();

            var token = tokenHandler.CreateToken(tokenDecriptor);

            return Ok(new { token = tokenHandler.WriteToken(token) });
        }
    }
}