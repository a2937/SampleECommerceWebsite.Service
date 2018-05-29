using System;
using System.Collections.Generic;
using System.IdentityModel.Tokens.Jwt;
using System.Linq;
using System.Net;
using System.Security.Claims;
using System.Text;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Microsoft.IdentityModel.Tokens;
using SampleECommerceWebsite.Models.Entities;
using SampleECommerceWebsite.Service.Models;

namespace SampleECommerceWebsite.Service.Controllers
{
    //[Produces("application/json")]
    [Route("api/Auth")]
    public class AuthController : Controller
    {
        private readonly UserManager<Customer> _userManager;
        private readonly SignInManager<Customer> _signInManager;
        private readonly RoleManager<IdentityRole<int>> _roleManager;
        private IPasswordHasher<Customer> _passwordHasher;
        private IConfigurationRoot _configurationRoot;
        private ILogger<AuthController> _logger;

        public AuthController(UserManager<Customer> userManager, SignInManager<Customer> signInManager, RoleManager<IdentityRole<int>> roleManager,
            IPasswordHasher<Customer> passwordHasher, IConfigurationRoot configurationRoot, ILogger<AuthController> logger)
        {
            _userManager = userManager;
            _signInManager = signInManager;
            _roleManager = roleManager;
            _passwordHasher = passwordHasher;
            _configurationRoot = configurationRoot;
            _logger = logger;
        }

        [AllowAnonymous]
        [HttpPost]
        [Route("register")]
        public async Task<IActionResult> Register([FromBody]RegisterViewModel model)
        {
            if(!ModelState.IsValid)
            {
                return BadRequest(); 
            }
            var user = new Customer()
            {
                UserName = model.Email,
                Email = model.Email
            };

            var result = await _userManager.CreateAsync(user, model.Password);
            if(result.Succeeded)
            {
                return Ok(result);
            }

            foreach(var error in result.Errors)
            {
                ModelState.AddModelError("error", error.Description); 
            }
            return BadRequest(result.Errors);
        }

        //[ValidateForm]
        [HttpPost("CreateToken")]
        [Route("token")]
        public async Task<IActionResult> CreateToken([FromBody]LoginViewModel model)
        {
            try
            {
                var user = await _userManager.FindByEmailAsync(model.Email); 
                if(user == null)
                {
                    return Unauthorized();
                }
                if(_passwordHasher.VerifyHashedPassword(user,user.PasswordHash,model.Password) == PasswordVerificationResult.Success)
                {
                    var userClaims = await _userManager.GetClaimsAsync(user);
                    var claims = new[]
                    {
                        new Claim(JwtRegisteredClaimNames.Sub, user.UserName),
                        new Claim(JwtRegisteredClaimNames.Jti,Guid.NewGuid().ToString()),
                        new Claim(JwtRegisteredClaimNames.Email,user.Email)
                    }.Union(userClaims);
                    var symmetricSecurityKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_configurationRoot["JwtSecurityToken:Key"]));
                    var signingCredentials = new SigningCredentials(symmetricSecurityKey, SecurityAlgorithms.HmacSha256);

                    var jwtSecurityToken = new JwtSecurityToken(issuer: _configurationRoot["JwtSecurityToken:Issuer"],
                        audience: _configurationRoot["JwtSecurityToken:Audience"], claims: claims, expires: DateTime.UtcNow.AddMinutes(60),
                        signingCredentials: signingCredentials);
                    return Ok(new
                    {
                        token = new JwtSecurityTokenHandler().WriteToken(jwtSecurityToken), expiration = jwtSecurityToken.ValidTo
                    });
                }
                return Unauthorized(); 
            }
            catch(Exception ex)
            {
                _logger.LogError($"error while creating token: {ex}");
                return StatusCode((int)HttpStatusCode.InternalServerError, "error while creating token"); 
            }
        }
    }
}