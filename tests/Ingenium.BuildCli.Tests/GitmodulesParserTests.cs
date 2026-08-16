// This work is licensed under the terms of the MIT license.
// For a copy, see <https://opensource.org/licenses/MIT>.

using Ingenium.BuildCli.Git;

namespace Ingenium.BuildCli.Tests;

public sealed class GitmodulesParserTests
{
	[Fact]
	public void Parse_ReadsNamedSubmoduleEntries()
	{
		const string contents = """
			[submodule "build"]
				path = build
				url = git@github.com:IngeniumSE/Build.git
				branch = main

			[submodule "docs"]
				path = docs/vendor
				url = https://example.com/docs.git
			""";

		var entries = GitmodulesParser.Parse(contents);

		Assert.Equal(2, entries.Count);
		Assert.Equal("build", entries[0].Name);
		Assert.Equal("build", entries[0].Path);
		Assert.Equal("git@github.com:IngeniumSE/Build.git", entries[0].Url);
		Assert.Equal("main", entries[0].Branch);
		Assert.Equal("docs/vendor", entries[1].Path);
	}

	[Fact]
	public void Parse_IgnoresCommentsAndUnknownKeys()
	{
		const string contents = """
			# generated
			[submodule "Build"]
				path = Build
				url = https://github.com/IngeniumSE/Build.git
				ignore = dirty
			""";

		var entries = GitmodulesParser.Parse(contents);

		Assert.Single(entries);
		Assert.Equal("Build", entries[0].Path);
		Assert.Equal("https://github.com/IngeniumSE/Build.git", entries[0].Url);
	}

	[Fact]
	public void ParseFile_ReturnsEmptyWhenMissing()
	{
		var entries = GitmodulesParser.ParseFile(Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString("N"), ".gitmodules"));

		Assert.Empty(entries);
	}
}
