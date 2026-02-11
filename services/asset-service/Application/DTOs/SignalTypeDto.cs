
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Application.DTOs
{
    public class SignalTypeDto
{
    public Guid SignalTypeID { get; set; }
    public string SignalName { get; set; }
    public string SignalUnit { get; set; }
    public int DefaultRegisterAdress { get; set; }
    public double MinThreshold { get; set; }
    public double MaxThreshold { get; set; }
}
}