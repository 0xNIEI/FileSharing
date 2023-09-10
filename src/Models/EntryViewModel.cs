using System.ComponentModel.DataAnnotations;
using System.ComponentModel;

namespace FileSharing.Models
{
    public class EntryViewModel
    {
        public string? guid { get; set; }

        [DisplayName("Custom ID")]
        [Required]
        public string? customId { get; set; }

        public string? validationError { get; set; }

        [DisplayName("Expiration time")]
        public double expiresIn { get; set; }
        
        [DisplayName("Maximum number of downloads")]
        public int maxNumOfDownloads { get; set; }

        public string? originalFileName { get; set; }

        [DisplayName("BroToken™")]
        public string? broToken { get; set; }

        public EntryViewModel()
        {
            //initialize
        }

        public EntryViewModel(Entry entry)
        {
            guid = entry.guid;
            customId = entry.customId;
            originalFileName = entry.originalFileName;
        }
    }
}
