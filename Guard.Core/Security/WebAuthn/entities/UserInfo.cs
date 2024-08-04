using System.Runtime.InteropServices;

// Based on https://github.com/dbeinder/Yoq.WindowsWebAuthn - Copyright (c) 2019 David Beinder - MIT License

namespace Guard.Core.Security.WebAuthn.entities
{
    [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode)]
    internal class RawUserInfo
    {
        internal const int MaxUserIdBytes = 64;

        // Version of this structure, to allow for modifications in the future.
        // This field is required and should be set to CURRENT_VERSION above.
        protected int StructVersion = 1;

        // Identifier for the User. This field is required.
        public int UserIdBytes;
        public IntPtr UserId;

        // Contains a detailed name for this account, such as "john.p.smith@example.com".
        public required string Name;

        // Optional URL that can be used to retrieve an image containing the user's current avatar,
        // or a data URI that contains the image data.
        public string? IconUrl;

        // For User: Contains the friendly name associated with the user account by the Relying Party, such as "John P. Smith".
        public required string DisplayName;

        internal UserInfo MarshalToPublic()
        {
            var info = new UserInfo
            {
                Name = Name,
                IconUrl = IconUrl,
                DisplayName = DisplayName,
                UserId = new byte[UserIdBytes]
            };
            if (UserIdBytes != 0 && UserId != IntPtr.Zero)
                Marshal.Copy(UserId, info.UserId, 0, UserIdBytes);
            return info;
        }
    }

    [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode)]
    internal class RawUserInfoOut : RawUserInfo, IDisposable
    {
        public RawUserInfoOut() { }

        [System.Diagnostics.CodeAnalysis.SetsRequiredMembersAttribute]
        public RawUserInfoOut(UserInfo user)
        {
            if (user.UserId.Length > MaxUserIdBytes)
                throw new ArgumentException($"UserId needs to be <= {MaxUserIdBytes} bytes");

            UserId = Marshal.AllocHGlobal(user.UserId.Length);
            Marshal.Copy(user.UserId, 0, UserId, user.UserId.Length);
            Name = user.Name;
            DisplayName = user.DisplayName;
            IconUrl = user.IconUrl;
            UserIdBytes = user.UserId.Length;
        }

        ~RawUserInfoOut() => FreeMemory();

        protected void FreeMemory() => Helper.SafeFreeHGlobal(ref UserId);

        public void Dispose()
        {
            FreeMemory();
            GC.SuppressFinalize(this);
        }
    }

    public class UserInfo
    {
        // Identifier for the User. This field is required.
        public required byte[] UserId;

        // Contains a detailed name for this account, such as "john.p.smith@example.com".
        public required string Name;

        // Optional URL that can be used to retrieve an image containing the user's current avatar,
        // or a data URI that contains the image data.
        public string? IconUrl;

        // For User: Contains the friendly name associated with the user account by the Relying Party, such as "John P. Smith".
        public required string DisplayName;
    }
}
