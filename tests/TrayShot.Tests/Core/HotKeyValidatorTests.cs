using System.Windows.Input;
using TrayShot.Core;
using TrayShot.Infrastructure;
using Xunit;

namespace TrayShot.Tests.Core;

public class HotKeyValidatorTests
{
    [Fact]
    public void Validate_BlocksReservedShortcuts()
    {
        uint ctrlMod = HotKeyManager.MOD_CONTROL;
        uint sKey = (uint)KeyInterop.VirtualKeyFromKey(Key.S);

        var result = HotKeyValidator.Validate(ctrlMod, sKey);

        Assert.False(result.IsValid);
        Assert.Contains("표준 프로그램 단축키", result.Message);
    }

    [Fact]
    public void Validate_BlocksSingleModifierLetterShortcuts()
    {
        uint altMod = HotKeyManager.MOD_ALT;
        uint gKey = (uint)KeyInterop.VirtualKeyFromKey(Key.G);

        var result = HotKeyValidator.Validate(altMod, gKey);

        Assert.False(result.IsValid);
        Assert.Contains("충돌 위험", result.Message);
    }

    [Fact]
    public void Validate_AllowsValidMultiModifierHotKey()
    {
        uint ctrlAltMod = HotKeyManager.MOD_CONTROL | HotKeyManager.MOD_ALT;
        uint sKey = (uint)KeyInterop.VirtualKeyFromKey(Key.S);

        var result = HotKeyValidator.Validate(ctrlAltMod, sKey);

        Assert.True(result.IsValid);
        Assert.Contains("성공적으로 변경되었습니다", result.Message);
    }

    [Fact]
    public void Validate_AllowsFunctionKeysWithoutModifiers()
    {
        uint noMod = 0;
        uint f11Key = (uint)KeyInterop.VirtualKeyFromKey(Key.F11);

        var result = HotKeyValidator.Validate(noMod, f11Key);

        Assert.True(result.IsValid);
    }
}
