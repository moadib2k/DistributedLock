using Microsoft.Extensions.Options;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MRB.DistributedLock.Repository
{
    /// <summary>
    /// Options class 
    /// </summary>
    public class DynamoDbRepositoryOptions
    {
        /// <summary>
        /// The name of the table to use for locking
        /// </summary>
        [Required(ErrorMessage ="TableName is required")]
        public string TableName { get; set; } = String.Empty;
    }
}
