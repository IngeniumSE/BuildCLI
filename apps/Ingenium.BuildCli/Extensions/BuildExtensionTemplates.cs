// This work is licensed under the terms of the MIT license.
// For a copy, see <https://opensource.org/licenses/MIT>.

namespace Ingenium.BuildCli.Extensions;

/// <summary>
/// Source templates for a Build extension project.
/// </summary>
public static class BuildExtensionTemplates
{
	/// <summary>
	/// Returns <c>build-extensions/Directory.Build.props</c> content.
	/// </summary>
	public static string DirectoryBuildProps(string submodulePath)
	{
		var import = $"../{ToGitPath(submodulePath)}/apps/Directory.Build.props";
		return $"""
			<Project>
				<Import Project="{import}" />
			</Project>

			""";
	}

	/// <summary>
	/// Returns <c>build-extensions/Directory.Build.targets</c> content.
	/// </summary>
	public static string DirectoryBuildTargets(string submodulePath)
	{
		var import = $"../{ToGitPath(submodulePath)}/apps/Directory.Build.targets";
		return $"""
			<Project>
				<Import Project="{import}" Condition="Exists('{import}')" />
			</Project>

			""";
	}

	/// <summary>
	/// Returns the extension <c>.csproj</c> content.
	/// </summary>
	public static string Project(string submodulePath)
	{
		var reference = $@"..\..\{ToGitPath(submodulePath).Replace('/', '\\')}\apps\Build.Abstractions\Build.Abstractions.csproj";
		return $"""
			<Project Sdk="Microsoft.NET.Sdk">

				<PropertyGroup>
					<TargetFramework>net8.0</TargetFramework>
				</PropertyGroup>

				<ItemGroup>
					<ProjectReference Include="{reference}" />
				</ItemGroup>

			</Project>

			""";
	}

	/// <summary>
	/// Returns a sample Cake task that the Build host will discover.
	/// </summary>
	public static string SampleTask(string projectName)
	{
		var taskName = projectName.EndsWith("BuildExtensions", StringComparison.Ordinal)
			? projectName[..^"BuildExtensions".Length]
			: projectName;

		if (string.IsNullOrEmpty(taskName))
		{
			taskName = "Sample";
		}

		return $$"""
			// This work is licensed under the terms of the MIT license.
			// For a copy, see <https://opensource.org/licenses/MIT>.

			namespace {{projectName}}
			{
				using Build;

				using Cake.Frosting;

				/// <summary>
				/// A sample task discovered by the Ingenium Build host.
				/// </summary>
				[TaskName("{{taskName}}")]
				public sealed class SampleTask : BuildTask
				{
					public SampleTask(BuildServices services)
						: base(services)
					{
					}

					/// <inheritdoc />
					protected override void RunCore(BuildContext context)
					{
						context.Log.Information("Hello from the {{projectName}} build extension.");
					}
				}
			}

			""";
	}

	private static string ToGitPath(string path)
	{
		return path.Replace('\\', '/').Trim('/');
	}
}
