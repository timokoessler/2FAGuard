using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TOTPTokenGuard.Core.Models
{
    internal class AuthFileData
    {
        public string? WindowsHelloProtectedKey { get; set; }
        public string? WindowsHelloChallenge { get; set; }
        public string? PasswordProtectedKey { get; set; }
        public string? ProtectedDbKey { get; set; }
    }
}
