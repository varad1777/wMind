using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Domain.Entities;
using Infrastructure.DBs;
using Microsoft.EntityFrameworkCore;

namespace Infrastructure.Seeding
{
    public static class SignalTypessSeeder
    {
        public static async Task SeedAsync(DBContext dbContext)
        {
            if (!await dbContext.SignalTypes.AnyAsync())
            {
                var signalTypes = new List<SignalTypes>
                {
                
                    new SignalTypes { SignalName = "Voltage",     SignalUnit = "V",     DefaultRegisterAdress = 40001, MinThreshold = 18, MaxThreshold = 26 },
                    new SignalTypes { SignalName = "Current",     SignalUnit = "A",     DefaultRegisterAdress = 40003, MinThreshold = 0,  MaxThreshold = 50 },
                    new SignalTypes { SignalName = "Temperature", SignalUnit = "°C",    DefaultRegisterAdress = 40005, MinThreshold = -10, MaxThreshold = 80 },
                    new SignalTypes { SignalName = "Frequency",   SignalUnit = "Hz",    DefaultRegisterAdress = 40007, MinThreshold = 45, MaxThreshold = 65 },
                    new SignalTypes { SignalName = "Vibration",   SignalUnit = "mm/s",  DefaultRegisterAdress = 40009, MinThreshold = 0, MaxThreshold = 10 },
                    new SignalTypes { SignalName = "FlowRate",    SignalUnit = "L/min", DefaultRegisterAdress = 40011, MinThreshold = 1, MaxThreshold = 200 },
                    new SignalTypes { SignalName = "RPM",         SignalUnit = "rpm",   DefaultRegisterAdress = 40013, MinThreshold = 100, MaxThreshold = 600 },
                    new SignalTypes { SignalName = "Torque",      SignalUnit = "Nm",    DefaultRegisterAdress = 40015, MinThreshold = 0, MaxThreshold = 500 }
                };

                await dbContext.SignalTypes.AddRangeAsync(signalTypes);
                await dbContext.SaveChangesAsync();
            }
        }
    }
}
