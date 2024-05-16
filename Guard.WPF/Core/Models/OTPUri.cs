using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Guard.WPF.Core.Icons;

namespace Guard.WPF.Core.Models
{
    internal enum OtpUriType
    {
        TOTP,
        HOTP
    }

    internal class OTPUri
    {
        internal OtpUriType? Type { get; set; }
        internal string? Issuer { get; set; }
        internal string? Secret { get; set; }
        internal string? Account { get; set; }
        internal TOTPAlgorithm? Algorithm { get; set; }
        internal int? Digits { get; set; }
        internal int? Period { get; set; }
    }
}
