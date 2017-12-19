using System;
using System.Data.Entity.Infrastructure;
using System.IO;
using System.Runtime.Serialization.Formatters.Binary;

namespace EntityFramework.DynamicFilters
{
    internal class DynamicFilterSerializer : IMetadataAnnotationSerializer
    {
        public object Deserialize(string name, string value)
        {
            using (MemoryStream ms = new MemoryStream(Convert.FromBase64String(value)))
            {
                var bf = new BinaryFormatter();

                return (DynamicFilterDefinition)bf.Deserialize(ms);
            }
        }

        public string Serialize(string name, object value)
        {
            using (MemoryStream ms = new MemoryStream())
            {
                var bf = new BinaryFormatter();
                bf.Serialize(ms, value);

                return Convert.ToBase64String(ms.ToArray());
            }
        }
    }
}