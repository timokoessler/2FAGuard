using Guard.Core;

namespace Guard.Test.Core
{
    public class CliBridgePolicy
    {
        [Fact]
        public void MissingRegistryValueDisablesDesktopBridge()
        {
            Assert.False(RegistrySettings.IsCliDesktopBridgeEnabled(null));
        }

        [Fact]
        public void DwordOneEnablesDesktopBridge()
        {
            Assert.True(RegistrySettings.IsCliDesktopBridgeEnabled(1));
        }

        [Fact]
        public void InvalidRegistryValueDisablesDesktopBridge()
        {
            Assert.False(RegistrySettings.IsCliDesktopBridgeEnabled("1"));
            Assert.False(RegistrySettings.IsCliDesktopBridgeEnabled(2));
            Assert.False(RegistrySettings.IsCliDesktopBridgeEnabled(0));
        }
    }
}
