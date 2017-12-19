using System;
using System.Collections.Generic;
using System.Data.Entity;
using System.Data.Entity.Infrastructure;
using System.IO;
using System.Linq;
using System.Text;

namespace EntityFramework.DynamicFilters
{
    public class MyDbConfiguration : DbConfiguration
    {
        public MyDbConfiguration() : base()
        {
            this.SetMetadataAnnotationSerializer("DynamicFilter", () => new DynamicFilterSerializer());
            this.SetModelStore(new DefaultDbModelStore(Directory.GetCurrentDirectory()));
        }
    }
}
