using System.ComponentModel.DataAnnotations;

namespace Application.DTOs
{
    public class UpdateAssetDto
    {
        //[Required]
        //[Range(1, int.MaxValue, ErrorMessage = "Asset ID must be valid.")]
        //public Guid AssetId { get; set; }

        //[Range(0, int.MaxValue, ErrorMessage = "OldParentId must be valid.")]
        //public Guid OldParentId { get; set; }

        //[Range(0, int.MaxValue, ErrorMessage = "NewParentId must be valid.")]
        //public Guid NewParentId { get; set; }

        //// Old name not required, no validation needed
        //public string? OldName { get; set; }

        //[StringLength(100, MinimumLength = 3,
        //    ErrorMessage = "New name must be between 3 and 100 characters.")]
        //[RegularExpression("^[A-Za-z0-9 _.-]+$",
        //    ErrorMessage = "New name contains invalid characters.")]
        //public string? NewName { get; set; }

        public Guid AssetId { get; set; }
        public string NewName { get; set; }
    }
}
