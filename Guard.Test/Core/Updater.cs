namespace Guard.Test.Core
{
    public class Updater
    {
        [Fact]
        public void CheckUrl()
        {
            Assert.Equal(
                "https://2faguard.app/api/update",
                Guard.Core.Installation.Updater.updateApiUrl
            );
        }
    }
}
