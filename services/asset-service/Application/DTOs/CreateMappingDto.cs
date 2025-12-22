// Application/DTOs/CreateMappingDto.cs
using System;
using System.Collections.Generic;

namespace Application.DTOs
{
    public class CreateMappingDto
    {
        public Guid AssetId { get; set; }
        public Guid DeviceId { get; set; }
        public Guid DevicePortId { get; set; }

        // NEW: list of registers the frontend selected for mapping
        public List<RegisterMappingDto> Registers { get; set; } = new List<RegisterMappingDto>();
    }

    public class RegisterMappingDto
    {
        // Use int if your register addresses are integers (40005 etc.)
        public int RegisterAddress { get; set; }

        // The signal type id selected in frontend
        public Guid SignalTypeId { get; set; }
        public Guid registerId { get; set; }
    }



}
