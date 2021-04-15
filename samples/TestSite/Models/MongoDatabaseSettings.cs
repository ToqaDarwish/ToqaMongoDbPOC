using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace TestSite.Models
{
    public class MongoDatabaseSettings : IMongoDatabaseSettings
    {
        public string TokensCollectionName { get; set; }
        public string ConnectionString { get; set; }
        public string DatabaseName { get; set; }
    }

    public interface IMongoDatabaseSettings
    {
        string TokensCollectionName { get; set; }
        string ConnectionString { get; set; }
        string DatabaseName { get; set; }
    }
    
}
