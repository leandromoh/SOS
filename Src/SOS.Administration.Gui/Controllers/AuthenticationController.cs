﻿using System;
using System.IdentityModel.Tokens.Jwt;
using System.Text;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.IdentityModel.Tokens;

namespace SOS.Administration.Gui.Controllers
{
    public class UserModel
    {
        public string UserName { get; set; }
        public string Password { get; set; }
    }
    [Route("[controller]")]
    [ApiController]
    public class AuthenticationController : ControllerBase
    {
        private AuthenticationConfiguration _config;

        public AuthenticationController(AuthenticationConfiguration authConfig)
        {
            _config = authConfig;
        }
        [AllowAnonymous]
        [HttpPost]
        [Route("login")]
        public IActionResult Login([FromBody] UserModel login)
        {
            IActionResult response = Unauthorized();
            var user = AuthenticateUser(login);

            if (user != null)
            {
                var tokenString = GenerateJSONWebToken(user);
                response = Ok(new { token = tokenString });
            }

            return response;
        }

        private string GenerateJSONWebToken(UserModel userInfo)
        {
            var securityKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_config.SecretKey));
            var credentials = new SigningCredentials(securityKey, SecurityAlgorithms.HmacSha256);

            var token = new JwtSecurityToken(_config.Issuer,
              _config.Issuer,
              null,
              expires: DateTime.Now.AddMinutes(120),
              signingCredentials: credentials);

            return new JwtSecurityTokenHandler().WriteToken(token);
        }

        private UserModel AuthenticateUser(UserModel login)
        {
            UserModel user = null;

            if (login.UserName == "admin" && login.Password == _config.SecretPassword)
            {
                user = new UserModel { UserName = "admin" };
            }
            return user;
        }
    }
}
