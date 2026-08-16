// This work is licensed under the terms of the MIT license.
// For a copy, see <https://opensource.org/licenses/MIT>.

using System.Globalization;
using System.Text;

namespace Ingenium.BuildCli.Extensions;

/// <summary>
/// Converts repository or user-supplied names into <c>*BuildExtensions</c> project names.
/// </summary>
public static class BuildExtensionNames
{
	/// <summary>
	/// The folder the Build host imports via a wildcard project reference.
	/// </summary>
	public const string FolderName = "build-extensions";

	/// <summary>
	/// Returns a project name that ends with <c>BuildExtensions</c>.
	/// </summary>
	public static string ToProjectName(string name)
	{
		ArgumentException.ThrowIfNullOrWhiteSpace(name);

		var pascal = ToPascalIdentifier(name);
		if (pascal.EndsWith("BuildExtensions", StringComparison.OrdinalIgnoreCase))
		{
			return pascal;
		}

		return pascal + "BuildExtensions";
	}

	/// <summary>
	/// Converts an arbitrary name into a PascalCase identifier.
	/// </summary>
	public static string ToPascalIdentifier(string name)
	{
		var builder = new StringBuilder();
		var startWord = true;
		foreach (var ch in name.Trim())
		{
			if (!char.IsLetterOrDigit(ch))
			{
				startWord = true;
				continue;
			}

			if (builder.Length == 0 && char.IsDigit(ch))
			{
				continue;
			}

			builder.Append(startWord ? char.ToUpper(ch, CultureInfo.InvariantCulture) : ch);
			startWord = false;
		}

		return builder.Length == 0 ? "Repo" : builder.ToString();
	}
}
