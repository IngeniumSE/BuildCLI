// This work is licensed under the terms of the MIT license.
// For a copy, see <https://opensource.org/licenses/MIT>.

using Ingenium.BuildCli.Submodule;

namespace Ingenium.BuildCli.Tests;

public sealed class TagSelectorTests
{
	[Fact]
	public void ParseLsRemote_PrefersPeeledAnnotatedTags()
	{
		const string output = """
			aaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaa	refs/tags/v1.0.0
			bbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbb	refs/tags/v1.0.0^{}
			cccccccccccccccccccccccccccccccccccccccc	refs/tags/v1.1.0
			""";

		var tags = TagSelector.ParseLsRemote(output);

		Assert.Equal(2, tags.Count);
		Assert.Equal("bbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbb", tags.Single(tag => tag.Name == "v1.0.0").Commit);
		Assert.Equal("cccccccccccccccccccccccccccccccccccccccc", tags.Single(tag => tag.Name == "v1.1.0").Commit);
	}

	[Fact]
	public void SelectLatest_PrefersHighestStableSemVer()
	{
		var tags = new[]
		{
			new RemoteTag("v1.2.0", "a"),
			new RemoteTag("v2.0.0-preview.1", "b"),
			new RemoteTag("v2.0.0", "c"),
			new RemoteTag("not-a-version", "d")
		};

		var latest = TagSelector.SelectLatest(tags);

		Assert.Equal("v2.0.0", latest?.Name);
	}

	[Fact]
	public void SelectLatest_PrefersStableOverPrereleaseOfSameVersion()
	{
		var tags = new[]
		{
			new RemoteTag("1.0.0-preview", "a"),
			new RemoteTag("1.0.0", "b")
		};

		var latest = TagSelector.SelectLatest(tags);

		Assert.Equal("1.0.0", latest?.Name);
	}

	[Fact]
	public void Find_MatchesOptionalLeadingV()
	{
		var tags = new[]
		{
			new RemoteTag("v1.2.3", "abc")
		};

		Assert.Equal("v1.2.3", TagSelector.Find(tags, "1.2.3")?.Name);
		Assert.Equal("v1.2.3", TagSelector.Find(tags, "v1.2.3")?.Name);
	}

	[Fact]
	public void SelectLatest_ReturnsNullForEmptyList()
	{
		Assert.Null(TagSelector.SelectLatest([]));
	}
}
