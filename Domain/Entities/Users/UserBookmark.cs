using Domain.Entities.Words;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Domain.Entities.Users
{
    public class UserBookmark
    {
        public long Id { get; set; }

        [Required]
        public Guid UserId { get; set; }
        [ForeignKey(nameof(UserId))]
        public User? User { get; set; }

        [Required]
        public long WordId { get; set; }
        [ForeignKey(nameof(WordId))]
        public Word? Word { get; set; }
    }
}
