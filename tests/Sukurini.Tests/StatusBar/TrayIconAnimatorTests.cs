using System;
using System.Drawing;
using Sukurini.StatusBar;
using Xunit;

namespace Sukurini.Tests.StatusBar;

public class TrayIconAnimatorTests
{
    [Fact]
    public void TrayIconAnimator_RendersInitialFrameAndTransitionsState()
    {
        Icon? lastRenderedIcon = null;
        using var animator = new TrayIconAnimator(icon => lastRenderedIcon = icon);

        Assert.NotNull(lastRenderedIcon);
        Assert.Equal(AnimationState.Idle, animator.CurrentState);

        animator.PlayPulse();
        Assert.Equal(AnimationState.Pulse, animator.CurrentState);

        animator.Stop();
        Assert.Equal(AnimationState.Idle, animator.CurrentState);
    }
}
