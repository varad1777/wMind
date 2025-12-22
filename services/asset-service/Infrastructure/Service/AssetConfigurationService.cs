using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Application.DTOs;
using Application.Interface;
using Infrastructure.DBs;
using Microsoft.EntityFrameworkCore;
using Domain.Entities;

namespace Infrastructure.Service
{
    public class AssetConfigurationService : IAssetConfiguration
    {
        private readonly DBContext _db;

        public AssetConfigurationService(DBContext db)
        {
            _db = db;
        }

        //public async Task AddConfiguration(AssetConfigurationDto Dto)
        //{
        //    var asset = await _db.Assets.Include(a => a.AssetConfigurations)
        //                                .FirstOrDefaultAsync(a => a.AssetId == Dto.AssetId);

        //    if (asset == null)
        //        throw new Exception("Asset not found");

        //    if (asset.Level <=2)
        //        throw new Exception("Configuration Can be added Only On Machines");

        //    if (asset.AssetConfigurations.Any())
        //        throw new Exception("Asset Configuration Exists");

        //    try
        //    {
        //        //Adding ll the signals id coming fro the post to the configuration table
        //        var configs = Dto.Signals
        // ?           .Select(signalId => new AssetConfiguration
        //            {
        //                AssetId = Dto.AssetId,
        //                SignaTypeID = signalId
        //            })
        //            .ToList();

        //        await _db.AssetConfigurations.AddRangeAsync(configs);
        //        await _db.SaveChangesAsync();
        //    }
        //    catch (Exception ex)
        //    {
        //        throw new Exception("Error adding asset configurations", ex);
        //    }
        //}

        public async Task AddConfiguration(AssetConfigurationDto Dto)
        {
            var asset = await _db.Assets
                .Include(a => a.AssetConfigurations)
                .FirstOrDefaultAsync(a => a.AssetId == Dto.AssetId);

            if (asset == null)
                throw new Exception("Asset not found");

            if (asset.Level <= 2)
                throw new Exception("Configuration can be added only on Machines");

            //pehle se jo signal configured hai
            var existingSignals = asset.AssetConfigurations
                .Select(c => c.SignaTypeID)
                .ToHashSet();

            //duplicate check karega
            var duplicate = Dto.Signals
                .Where(s => existingSignals.Contains(s))
                .ToList();

            if (duplicate.Any())
                throw new Exception("These signals are already configured: " + string.Join(",", duplicate));

            try
            {
                //sirf naye wale
                var newConfigs = Dto.Signals
                    .Where(s => !existingSignals.Contains(s)) 
                    .Select(signalId => new AssetConfiguration
                    {
                        AssetId = Dto.AssetId,
                        SignaTypeID = signalId
                    })
                    .ToList();

                await _db.AssetConfigurations.AddRangeAsync(newConfigs);
                await _db.SaveChangesAsync();
            }
            catch (Exception ex)
            {
                throw new Exception("Error adding asset configurations", ex);
            }
        }



        //get the which signals are connetcde on asset by asset id 
        public async Task<List<SiganDetailsDto>> GetSignalDetailByAssetID(Guid Id)
        {
            var asset = await _db.Assets.Include(a => a.AssetConfigurations)
                                       .FirstOrDefaultAsync(a => a.AssetId == Id);

            if (asset == null)
                throw new Exception("Invalid Asset ID");

            try
            {
                var Details = await _db.AssetConfigurations
                    .Where(ac => ac.AssetId == Id)
                    .Include(a => a.SignalType)
                    .Select(a => new SiganDetailsDto
                    {
                        AssetConfigID = a.AssetConfigId,
                        SignalTypeID = a.SignaTypeID,
                        SignalName = a.SignalType.SignalName,
                        SignalUnit = a.SignalType.SignalUnit,
                        RegsiterAdress=a.SignalType.DefaultRegisterAdress

                    }).ToListAsync();

                return Details;
            }catch(Exception Ex)
            {
                throw new Exception("Error Loading the Signal Detail", Ex);
            }
        }


        public async Task EditSignalsOnAsset(Guid ConfigId, UpdateAssetConfigurationDto dto)
        {
            var Config = await _db.AssetConfigurations.FindAsync(ConfigId);

            if (Config == null)
                throw new Exception("Invalid Configuration");

            try
            {
                Config.SignaTypeID = dto.SignalTypeID;

                await _db.SaveChangesAsync();

            }
            catch(Exception ex)
            {
                throw new Exception("Some Error Occured While Editing the Signal", ex);
            }
        }

        public async Task DeleteSigalOnAsset(Guid ConfigId)
        {
            var Config = await _db.AssetConfigurations.FindAsync(ConfigId);

            if (Config == null)
                throw new Exception("Invalid Configuration");

            try
            {
                 _db.AssetConfigurations.Remove(Config);
                await _db.SaveChangesAsync();
            }catch(Exception ex)
            {
                throw new Exception("Error Occured while Deleting ", ex);
            }

        }


        public async Task<List<SignalTypes>> GetSiganlsToCoonfigure()
        {
            try
            {
                var Signals = await _db.SignalTypes.ToListAsync();

                return Signals;
            }catch(Exception ex)
            {
                throw new Exception("Some Error OCcured", ex);
            }
        }


    }
}
