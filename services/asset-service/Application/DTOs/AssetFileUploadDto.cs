using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Application.DTOs
{

    public class AssetUploadRequest
    {
        public List<AssetFileUploadDto> Assets { get; set; }
    }
    public class AssetFileUploadDto
    {
        public string AssetName { get; set; }
        public string? ParentName { get; set; }
        public int Level { get; set; }

    }
}
