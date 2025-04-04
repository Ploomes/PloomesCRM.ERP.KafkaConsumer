using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PloomesCRMCallbackHub2.Queue.DLL.BasicCommon
{
    internal abstract class ApiInfo
    {
        public const string DdVersion = "2.0"; // For DataDogLog=======
        public const string Version = "01 16.05.2024.11:40"; //For build/version checking 
        public const string ServiceName = "CB2.Queue"; //For build/version checking 
        public const string Env = "prod"; //For build/version checking 
    }
}
