using Microsoft.AspNetCore.Identity;
//using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.IdentityModel.Tokens;
using MongoDB.Driver;
using SampleSite.Identity;
using System;
using System.Collections.Generic;
using System.IdentityModel.Tokens.Jwt;
using System.Linq;
using System.Security.Claims;
using System.Text;
using System.Threading.Tasks;
using TestSite.Models;
//using ToqaPOC.Data;
//using ToqaPOC.Model;
using ToqaPOC.ViewModels;
using MongoDB.Driver.Linq;
using Newtonsoft.Json;

namespace ToqaPOC.Helper
{
    public class GenerateToken
    {
        private readonly IConfiguration configuration;
        private readonly UserManager<ApplicationUser> userManager;
        private readonly IMongoCollection<RefreshToken> _tokens;
        private readonly IMongoDatabaseSettings settings;
        //IMongoQueryable queryDbCollection = dbCollection.AsQueryable();
        IQueryable<RefreshToken> tokens { get; }
        public GenerateToken()
        {
        }
        public GenerateToken(IConfiguration _configuration, UserManager<ApplicationUser> _userManager, IMongoDatabaseSettings _settings)
        {
            configuration = _configuration;
            userManager = _userManager;
            settings = _settings;
            var client = new MongoClient(settings.ConnectionString);
            var database = client.GetDatabase(settings.DatabaseName);

            _tokens = database.GetCollection<RefreshToken>(settings.TokensCollectionName);
            //IMongoQueryable queryDbCollection = _tokens.AsQueryable();
        }

        public IQueryable<RefreshToken> Tokens => _tokens.AsQueryable();
        public async Task<AuthResult> GenerateJSONWebToken(ApplicationUser userInfo)
        {
            var user = await userManager.FindByNameAsync(userInfo.UserName);
            var securityKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(configuration["JWT:Secret"]));
            var credentials = new SigningCredentials(securityKey, SecurityAlgorithms.HmacSha256);
            var userRoles = await userManager.GetRolesAsync(user);
            var claims = new List<Claim>
            {
                new Claim(JwtRegisteredClaimNames.Sub, userInfo.UserName),
                new Claim(JwtRegisteredClaimNames.Email, userInfo.UserName),
                new Claim(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString()),
                new Claim("Age",userInfo.Age.ToString())
            };
            foreach (var userRole in userRoles)
            {
                claims.Add(new Claim(ClaimTypes.Role, userRole));
                //claims.Add(new Claim(ClaimTypes,user.Age.ToString()));
            }
            var Token = new JwtSecurityToken(
                configuration["JWT:ValidIssuer"],
                configuration["JWT:ValidAudience"],
                claims,
                expires: DateTime.UtcNow.AddSeconds(30),
                signingCredentials: credentials);
            var jwtToken = new JwtSecurityTokenHandler().WriteToken(Token);

            var refreshToken = new RefreshToken()
            {
                JwtId = Token.Id,
                IsUsed = false,
                IsRevoked = false,
                UserId = user.Id.ToString(),
                AddedDate = DateTime.UtcNow,
                ExpiryDate = DateTime.UtcNow.AddMonths(6),
                Token = RandomString(64) + Guid.NewGuid()
            };

            //var mongoClient = new MongoClient("");
            //var collection = mongoClient.GetDatabase("").GetCollection<RefreshToken>("RefreshTokens");
            await _tokens.InsertOneAsync(refreshToken);

            return new AuthResult()
            {
                Token = jwtToken,
                Success = true,
                RefreshToken = refreshToken.Token
            };
            //return new JwtSecurityTokenHandler().WriteToken(Token);
        }

        private string RandomString(int length)
        {
            var random = new Random();
            var chars = "ABCDEFGHIJKLMNOPQRSTUVWXYZ0123456789";
            return new string(Enumerable.Repeat(chars, length)
                .Select(x => x[random.Next(x.Length)]).ToArray());
        }

        public async Task<AuthResult> UpdateAndGenerateToken(TokenRequest tokenRequest)
        {
            try
            {
                //var storedToken = await applicationDbContext.RefreshTokens.FirstOrDefaultAsync(x => x.Token == tokenRequest.RefreshToken);
                var storedToken = await _tokens.Find(RefreshToken => RefreshToken.Token == tokenRequest.RefreshToken).FirstOrDefaultAsync();
                // update current token 

                var used = storedToken.IsUsed = true;
                await _tokens.ReplaceOneAsync(RefreshToken => RefreshToken.IsUsed== used ,storedToken);
                // Generate a new token
                var dbUser = await userManager.FindByIdAsync(storedToken.UserId);
                GenerateToken generateToken = new GenerateToken(configuration, userManager, settings);
                return await generateToken.GenerateJSONWebToken(dbUser);
            }
            catch (Exception ex)
            {
                if (ex.Message.Contains("Lifetime validation failed. The token is expired."))
                {

                    return new AuthResult()
                    {
                        Success = false,
                        Errors = new List<string>() {
                            "Token has expired please re-login"
                        }
                    };

                }
                else
                {
                    return new AuthResult()
                    {
                        Success = false,
                        Errors = new List<string>() {
                            "Something went wrong."
                        }
                    };
                }
            }
        }
    }
}
