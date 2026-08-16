// This work is licensed under the terms of the MIT license.
// For a copy, see <https://opensource.org/licenses/MIT>.

namespace Ingenium.BuildCli.SelfUpdate;

/// <summary>
/// Builds the <c>dotnet publish</c> argument list used to install <c>bld</c>.
/// </summary>
public static class SelfUpdatePublishArguments
{
	/// <summary>
	/// Creates publish arguments that match <c>scripts/install.sh</c> and <c>scripts/install.ps1</c>.
	/// </summary>
	public static IReadOnlyList<string> Create(
		string projectPath,
		string runtimeIdentifier,
		string outputDirectory,
		bool frameworkDependent)
	{
		ArgumentException.ThrowIfNullOrWhiteSpace(projectPath);
		ArgumentException.ThrowIfNullOrWhiteSpace(runtimeIdentifier);
		ArgumentException.ThrowIfNullOrWhiteSpace(outputDirectory);

		var arguments = new List<string>
		{
			"publish",
			projectPath,
			"-c",
			"Release",
			"-r",
			runtimeIdentifier,
			"-o",
			outputDirectory,
			"--nologo"
		};

		if (frameworkDependent)
		{
			arguments.Add("--self-contained");
			arguments.Add("false");
			return arguments;
		}

		arguments.Add("--self-contained");
		arguments.Add("true");
		arguments.Add("-p:PublishSingleFile=true");
		arguments.Add("-p:IncludeNativeLibrariesForSelfExtract=true");
		return arguments;
	}
}
