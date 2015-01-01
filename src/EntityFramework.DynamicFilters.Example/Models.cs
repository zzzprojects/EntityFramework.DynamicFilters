using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace EntityFramework.DynamicFilters.Example
{
    public interface ISoftDelete
    {
        bool IsDeleted { get; set; }
    }

    public class Account
    {
        [Key]
        [Required]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public Guid ID { get; set; }

        public string UserName { get; set; }

        public ICollection<BlogEntry> BlogEntries { get; set; }

        /// <summary>
        /// Column used to verify handling of Entity properties mapped to different conceptual property names.
        /// </summary>
        [Column("RemappedDBProp")]
        public bool RemappedEntityProp { get; set; }
    }

    //  Tests issue with TPH inheritance causing duplicate annotation names being added to the model conventions
    public class DerivedAccount : Account
    {
    }

    public class BlogEntry : ISoftDelete
    {
        [Key]
        [Required]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public Guid ID { get; set; }

        public Account Account { get; set; }
        public Guid AccountID { get; set; }

        public string Body { get; set; }

        public bool IsDeleted { get; set; }

        public int? IntValue { get; set; }

        public string StringValue { get; set; }
    }
}
