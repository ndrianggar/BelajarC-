﻿using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using Microsoft.IdentityModel.Tokens;
using System;
using System.Collections.Generic;
using System.IdentityModel.Tokens.Jwt;
using System.Linq;
using System.Net.Http;
using System.Security.Claims;
using System.Text;
using System.Threading.Tasks;
using WebApplicationJwt.IdentityAuth;
using WebApplicationJwt.Models;

namespace WebApplicationJwt.Controllers
{

    [Route("api/[controller]")]
    [ApiController]
    public class AuthenticateController : ControllerBase
    {
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly RoleManager<IdentityRole> _roleManager;
        private readonly IConfiguration _configuration;

        public AuthenticateController(UserManager<ApplicationUser> userManager, RoleManager<IdentityRole> roleManager, IConfiguration configuration)
        {
            _userManager = userManager;
            _roleManager = roleManager;
            _configuration = configuration;
        }

        [HttpPost]
        [Route("Register")]
        public async Task<IActionResult> Register([FromBody] RegisterModel model)
        {

            var userExist = await _userManager.FindByNameAsync(model.Username);
            if (userExist != null) return StatusCode(StatusCodes.Status500InternalServerError, new Response { Status = "Error", Message = "User Exitst" });


            ApplicationUser user = new()
            {
                Email = model.Email,
                SecurityStamp = Guid.NewGuid().ToString(),
                UserName = model.Username
            };

            var result = await _userManager.CreateAsync(user, model.Password);
            if (result.Succeeded)
            {
                var errors = new List<string>();

                foreach (var error in result.Errors)
                {
                    errors.Add(error.Description);
                }
                return Ok(new Response { Status = "Succes", Message = $"User created Successfully {model.Username}" });
            }
            return StatusCode(StatusCodes.Status500InternalServerError, new Response { Status = "Error", Message = result.ToString() });
        }

        [HttpPost]
        [Route("Register-admin")]
        public async Task<IActionResult> RegisterAdmin([FromBody] RegisterModel model)
        {
            var userExist = await _userManager.FindByNameAsync(model.Username);
            if (userExist != null)
                return StatusCode(StatusCodes.Status500InternalServerError, new Response { Status = "Error", Message = "User already exist" });

            ApplicationUser user = new()
            {
                Email = model.Email,
                SecurityStamp = Guid.NewGuid().ToString(),
                UserName = model.Username
            };

            var result = await _userManager.CreateAsync(user, model.Password);
            if (!result.Succeeded)
            {
                var errors = new List<string>();

                foreach (var error in result.Errors)
                {
                    errors.Add(error.Description);
                }
                return StatusCode(StatusCodes.Status500InternalServerError, new Response { Status = "Error", Message = string.Join(", ", errors) });
            }

            if (!await _roleManager.RoleExistsAsync(UserRoles.Admin))
                await _roleManager.CreateAsync(new IdentityRole(UserRoles.Admin));

            if (!await _roleManager.RoleExistsAsync(UserRoles.User))
                await _roleManager.CreateAsync(new IdentityRole(UserRoles.User));

            if (!await _roleManager.RoleExistsAsync(UserRoles.Admin))
                await _userManager.AddToRoleAsync(user, UserRoles.Admin);

            return Ok(new Response { Status = "Success", Message = $"User Admin created Successfully {model.Username}" });
        }

        [HttpPost]
        [Route("login")]
        public async Task<IActionResult> Login([FromBody] LoginModel model)
        {
            var user = await _userManager.FindByNameAsync(model.Username);
            if (user != null && await _userManager.CheckPasswordAsync(user, model.Password))
            {
                var userRoles = await _userManager.GetRolesAsync(user);


                var authClaims = new List<Claim>
                    {
                        new Claim(ClaimTypes.Name, user.UserName),
                        new Claim(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString())
                    };

                foreach (var userRole in userRoles)
                {
                    authClaims.Add(new Claim(ClaimTypes.Role, userRole));
                }

                var authSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_configuration["JWT:SecretKey"]));

                var token = new JwtSecurityToken(
                    issuer: _configuration["JWT:ValidIssuer"],
                    audience: _configuration["JWT:ValidAudience"],
                    expires: DateTime.Now.AddHours(3),
                    claims: authClaims,
                    signingCredentials: new SigningCredentials(authSigningKey, SecurityAlgorithms.HmacSha256)
                    );
                return Ok(new { token = new JwtSecurityTokenHandler().WriteToken(token), expiration = token.ValidTo });
            }
            return Unauthorized();
        }

        [HttpPost]
        [Route("change-Password")]
        public async Task<ActionResult> ChangePassword([FromBody] ChangePasswordModel model)
        {
            var user = await _userManager.FindByNameAsync(model.Username);
            if (user == null)
                return StatusCode(StatusCodes.Status404NotFound, new Response { Status = "Error", Message = "User does not exits" });

            if (string.Compare(model.NewPassword, model.ConfirmNewPassword) != 0)
                return StatusCode(StatusCodes.Status400BadRequest, new Response { Status = "Error", Message = "The new password and confirm new password doest not match!" });

            var result = await _userManager.ChangePasswordAsync(user, model.CurrentPassword, model.NewPassword);
            if (!result.Succeeded)
            {
                var errors = new List<string>();

                foreach (var error in result.Errors)
                {
                    errors.Add(error.Description);
                }

                return StatusCode(StatusCodes.Status500InternalServerError, new Response { Status = "error", Message = string.Join(", ", errors) });
            }

            return Ok(new Response { Status = "Succes", Message = "Password Successfully change" });

        }

        [HttpPost]
        [Route("reset-password-admin")]
        public async Task<IActionResult> ResetPassordAdmin([FromBody] ResetPasswordAdminModel model)
        {
            var user = await _userManager.FindByNameAsync(model.Username);
            if (user == null)
                return StatusCode(StatusCodes.Status404NotFound, new Response { Status = "Error", Message = "User does not exits" });

            if (string.Compare(model.NewPassword, model.ConfirmNewPassword) != 0)
                return StatusCode(StatusCodes.Status400BadRequest, new Response { Status = "Error", Message = "The new password and confirm new password doest not match!" });

            var token = await _userManager.GeneratePasswordResetTokenAsync(user);

            var result = await _userManager.ResetPasswordAsync(user, token, model.NewPassword);
            if(!result.Succeeded)
            {
                var errors = new List<string>();
                foreach (var error in result.Errors)
                {
                    errors.Add(error.Description);
                }
                return StatusCode(StatusCodes.Status500InternalServerError, new Response { Status = "Error", Message = string.Join(", ", errors) });
            }

            return Ok(new Response { Status = "Success", Message = "Password succesfully reset" });
        }


        [HttpPost]
        [Route("reset-password-token")]
        public async Task<IActionResult> ResetPasswordToken([FromBody] ResetPasswordToken model)
        {
            var user = await _userManager.FindByNameAsync(model.Username);
            if (user == null)
                return StatusCode(StatusCodes.Status404NotFound, new Response { Status = "Error", Message = "User does not exist" });

            var token = await _userManager.GeneratePasswordResetTokenAsync(user);
            // Best practice send token to user email and generate url, the following only for example
            return Ok(new { token = token });

        }
        

        [HttpPost]
        [Route("reset-password")]
        public async Task<IActionResult> ResetPassword([FromBody] ResetPasswordModel model)
        {
            var user = await _userManager.FindByNameAsync(model.Username);

            if (user == null)
                return StatusCode(StatusCodes.Status404NotFound, new Response { Status = "Error", Message = "User does not exits" });

            if (string.Compare(model.NewPassword, model.ConfirmNewPassword) != 0)
                return StatusCode(StatusCodes.Status400BadRequest, new Response { Status = "Error", Message = "The new password and confirm new password doest not match!" });

            if (string.IsNullOrEmpty(model.Token))
                return StatusCode(StatusCodes.Status400BadRequest, new Response { Status = "Error", Message = "Invalid token" });
            
            var result = await _userManager.ResetPasswordAsync(user, model.Token, model.NewPassword);
            var errors = new List<string>();
            if (!result.Succeeded)
            {
                foreach (var error in result.Errors)
                {
                    errors.Add(error.Description);
                }
                return StatusCode(StatusCodes.Status500InternalServerError, new Response { Status = "Error", Message = string.Join(", ", errors) });
            }

            return Ok(new Response { Status = "Succes", Message = "Password succesfully reseted" });
        }
    }
}