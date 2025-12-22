using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Application.DTOs;
using Domain.Entities;


namespace Application.Interface
{
    public interface IAssetConfiguration
    {
        Task AddConfiguration(AssetConfigurationDto Dto);
        Task<List<SiganDetailsDto>> GetSignalDetailByAssetID(Guid Id);

        Task EditSignalsOnAsset(Guid ConfigId, UpdateAssetConfigurationDto dto);

        Task DeleteSigalOnAsset(Guid ConfigId);

        Task<List<SignalTypes>> GetSiganlsToCoonfigure();
    }
}
