using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using System;
using System.Collections.Generic;
using System.IdentityModel.Tokens.Jwt;
using System.Linq;
using System.Net.Http.Headers;
using System.Threading.Tasks;

namespace TestSite.APIsController
{
    [Route("api/[controller]")]
    [ApiController]
    public class GetAgeFromToken : ControllerBase
    {
        [HttpGet]
        [Route("GetAgeFromToken")]
        public string GetAgeToken()
        {
            string token = HttpContext.Request.Headers["Authorization"];
            if (AuthenticationHeaderValue.TryParse(token, out var headerValue))
            {
                // we have a valid AuthenticationHeaderValue that has the following details:

                var scheme = headerValue.Scheme;
                var parameter = headerValue.Parameter;

                // scheme will be "Bearer"
                // parmameter will be the token itself.
                var stream = parameter;
                var handler = new JwtSecurityTokenHandler();
                var jsonToken = handler.ReadToken(stream);
                var tokenS = jsonToken as JwtSecurityToken;
                var jti = tokenS.Claims.First(claim => claim.Type == "Age").Value;
                string word = "The Age is : ";
                var result = word + jti;
                return result;
            }
            return "Error";
        }
    }
}
