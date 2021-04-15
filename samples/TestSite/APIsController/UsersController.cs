using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using MongoDB.Driver;
using SampleSite.Identity;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using TestSite.Models;

namespace TestSite.APIsController
{
    [Authorize(Roles = "Admin")]
    [Route("api/[controller]")]
    [ApiController]
    public class UsersController : ControllerBase
    {
        private readonly IMongoCollection<ApplicationUser> _users;
        private readonly IMongoDatabaseSettings settings;
        public UsersController(IMongoDatabaseSettings _settings)
        {
            settings = _settings;
            var client = new MongoClient(settings.ConnectionString);
            var database = client.GetDatabase(settings.DatabaseName);

            _users = database.GetCollection<ApplicationUser>("Users");
        }
        [HttpGet]
        [Route("GetUsers")]
        public IEnumerable<ApplicationUser> Get()
        {
            return _users.Find(User => true).ToList();
        }
    }
}
