// This work is licensed under the terms of the MIT license.
// For a copy, see <https://opensource.org/licenses/MIT>.

using Ingenium.BuildCli.Submodule;

namespace Ingenium.BuildCli.Tests;

public sealed class RepairStrategyParserTests
{
	[Theory]
	[InlineData("stash", RepairStrategy.Stash)]
	[InlineData("reset", RepairStrategy.Reset)]
	[InlineData("head", RepairStrategy.Reset)]
	[InlineData("reinit", RepairStrategy.Reinit)]
	[InlineData("re-init", RepairStrategy.Reinit)]
	[InlineData("reinitialize", RepairStrategy.Reinit)]
	public void TryParse_AcceptsAliases(string value, RepairStrategy expected)
	{
		Assert.True(RepairStrategyParser.TryParse(value, out var strategy));
		Assert.Equal(expected, strategy);
	}

	[Fact]
	public void TryParse_RejectsUnknownValues()
	{
		Assert.False(RepairStrategyParser.TryParse("explode", out _));
		Assert.False(RepairStrategyParser.TryParse(" ", out _));
	}
}
