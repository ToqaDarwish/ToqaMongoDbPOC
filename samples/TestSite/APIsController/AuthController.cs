using AspNetCore.Identity.Mongo.Model;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using Microsoft.IdentityModel.Tokens;
using SampleSite.Identity;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using TestSite.Models;
using ToqaPOC.Helper;
using ToqaPOC.ViewModels;

namespace TestSite.APIsController
{
    [Route("api/[controller]/[action]")]
    [ApiController]
    public class AuthController : ControllerBase
    {

        private readonly UserManager<ApplicationUser> userManager;
        private readonly RoleManager<MongoRole> roleManager;
        private readonly IConfiguration configuration;
        private readonly IMongoDatabaseSettings settings;
        private readonly TokenValidationParameters tokenValidationParams;

        public AuthController(UserManager<ApplicationUser> _userManager, RoleManager<MongoRole> _roleManager, IConfiguration _configuration, IMongoDatabaseSettings _settings, TokenValidationParameters _tokenValidationParams)
        {
            userManager = _userManager;
            roleManager = _roleManager;
            configuration = _configuration;
            settings = _settings;
            tokenValidationParams = _tokenValidationParams;
        }

        // GET: api/<AuthController>
        [HttpGet]
        public IEnumerable<string> Test()
        {
            return new string[] { "Api is working !!" };
        }

        [HttpPost]
        [Route("Login")]
        public async Task<IActionResult> Login([FromBody] LoginRequest model)
        {
            var user = await userManager.FindByNameAsync(model.username);
            IActionResult response = Unauthorized();
            if (user != null && await userManager.CheckPasswordAsync(user, model.password))
            {
                GenerateToken generateToken = new GenerateToken(configuration, userManager, settings);
                var tokenString = generateToken.GenerateJSONWebToken(user);
                response = Ok(new { token = tokenString.Result });
                return response;
            }
            else
            {
                return Unauthorized();
            }
        }

        [HttpPost]
        [Route("Register")]
        public async Task<ActionResult> Register([FromBody] RegisterRequest model)
        {
            var userExist = await userManager.FindByNameAsync(model.UserName);
            //if (userExist != null)
            //    return StatusCode(StatusCodes.Status500InternalServerError, new Response { Status = "Error", Massage = "User Already Exist" });
            ApplicationUser user = new ApplicationUser()
            {
                Email = model.Email,
                SecurityStamp = Guid.NewGuid().ToString(),
                UserName = model.UserName
            };
            var result = await userManager.CreateAsync(user, model.Password);
            if (!result.Succeeded)
            {
                return StatusCode(StatusCodes.Status500InternalServerError, new Response { Status = "Error", Massage = "User Creation Failed" });
            }
            return Ok(new Response { Status = "Success", Massage = "User Created Successfully" });
        }

        [HttpPost]
        [Route("RegisterAdmin")]
        public async Task<ActionResult> RegisterAdmin([FromBody] RegisterRequest model)
        {
            var userExist = await userManager.FindByNameAsync(model.UserName);
            if (userExist != null)
                return StatusCode(StatusCodes.Status500InternalServerError, new Response { Status = "Error", Massage = "User Already Exist" });
            ApplicationUser user = new ApplicationUser()
            {
                Email = model.Email,
                SecurityStamp = Guid.NewGuid().ToString(),
                UserName = model.UserName
            };
            var result = await userManager.CreateAsync(user, model.Password);
            if (!result.Succeeded)
            {
                return StatusCode(StatusCodes.Status500InternalServerError, new Response { Status = "Error", Massage = "User Creation Failed" });
            }
            if (!await roleManager.RoleExistsAsync("Admin"))
                await roleManager.CreateAsync(new MongoRole("Admin"));
            if (!await roleManager.RoleExistsAsync("User"))
                await roleManager.CreateAsync(new MongoRole("User"));
            if (await roleManager.RoleExistsAsync("Admin"))
            {
                await userManager.AddToRoleAsync(user, "Admin");
            }

            return Ok(new Response { Status = "Success", Massage = "User Created Successfully" });
        }

        [HttpPost]
        [Route("RefreshToken")]
        public async Task<IActionResult> RefreshToken([FromBody] TokenRequest tokenRequest)
        {
            if (ModelState.IsValid)
            {
                GenerateToken generateToken = new GenerateToken(configuration, userManager, settings);
                var result = await generateToken.UpdateAndGenerateToken(tokenRequest);

                if (result == null)
                {
                    return BadRequest(new AuthResult()
                    {
                        Errors = new List<string>() {
                            "Invalid tokens"
                        },
                        Success = false
                    });
                }

                return Ok(result);
            }

            return BadRequest(new AuthResult()
            {
                Errors = new List<string>() {
                    "Invalid payload"
                },
                Success = false
            });
        }


    }
}
