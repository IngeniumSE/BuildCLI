// This work is licensed under the terms of the MIT license.
// For a copy, see <https://opensource.org/licenses/MIT>.

namespace Ingenium.BuildCli;

/// <summary>
/// Applies default command routing so a bare invocation runs <c>build</c>.
/// </summary>
public static class CommandLineDefaults
{
	/// <summary>
	/// The command used when the user does not specify one.
	/// </summary>
	public const string DefaultCommand = "build";

	private static readonly HashSet<string> Commands = new(StringComparer.OrdinalIgnoreCase)
	{
		"init",
		"update",
		"status",
		"tags",
		"repair",
		"build",
		"extension"
	};

	private static readonly HashSet<string> MetaOptions = new(StringComparer.OrdinalIgnoreCase)
	{
		"-h",
		"--help",
		"-v",
		"--version"
	};

	/// <summary>
	/// Inserts <c>build</c> when no command was supplied.
	/// </summary>
	public static string[] Apply(IReadOnlyList<string> args)
	{
		ArgumentNullException.ThrowIfNull(args);

		if (args.Count == 0)
		{
			return [DefaultCommand];
		}

		var first = args[0];
		if (MetaOptions.Contains(first) || Commands.Contains(first))
		{
			return args as string[] ?? args.ToArray();
		}

		if (first.StartsWith('-'))
		{
			var routed = new string[args.Count + 1];
			routed[0] = DefaultCommand;
			for (var i = 0; i < args.Count; i++)
			{
				routed[i + 1] = args[i];
			}

			return routed;
		}

		return args as string[] ?? args.ToArray();
	}
}
