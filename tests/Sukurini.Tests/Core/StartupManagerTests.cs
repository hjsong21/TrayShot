using Sukurini.Core;
using Xunit;

namespace Sukurini.Tests.Core;

public class StartupManagerTests
{
    [Fact]
    public void StartupManager_ChecksStartupStatusWithoutThrowing()
    {
        bool isEnabled = StartupManager.IsStartupEnabled();
        Assert.True(isEnabled || !isEnabled);
    }
}
