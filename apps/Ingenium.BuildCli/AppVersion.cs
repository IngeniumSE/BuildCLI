// This work is licensed under the terms of the MIT license.
// For a copy, see <https://opensource.org/licenses/MIT>.

using System.Reflection;

namespace Ingenium.BuildCli;

/// <summary>
/// Exposes the current CLI version from the assembly metadata.
/// </summary>
public static class AppVersion
{
	/// <summary>
	/// Gets the informational version, falling back to the assembly version.
	/// </summary>
	public static string Current
	{
		get
		{
			var assembly = typeof(AppVersion).Assembly;
			var informational = assembly
				.GetCustomAttribute<AssemblyInformationalVersionAttribute>()
				?.InformationalVersion;

			if (!string.IsNullOrWhiteSpace(informational))
			{
				var plus = informational.IndexOf('+');
				return plus >= 0 ? informational[..plus] : informational;
			}

			return assembly.GetName().Version?.ToString() ?? "0.0.0";
		}
	}
}
