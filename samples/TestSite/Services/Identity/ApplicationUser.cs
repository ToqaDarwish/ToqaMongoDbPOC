using AspNetCore.Identity.Mongo.Model;

namespace SampleSite.Identity
{
    public class ApplicationUser : MongoUser
    {
        public int? Age { get; set; }
    }
}
