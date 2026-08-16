// This work is licensed under the terms of the MIT license.
// For a copy, see <https://opensource.org/licenses/MIT>.

namespace Ingenium.BuildCli;

/// <summary>
/// Applies default command routing so a bare or unknown invocation runs the Build host.
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
		"self-update",
		"upgrade",
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

	private static readonly HashSet<string> CliValueOptions = new(StringComparer.OrdinalIgnoreCase)
	{
		"-p",
		"--path",
		"--submodule-path",
		"--url",
		"-c",
		"--configuration",
		"-t",
		"--tag",
		"-s",
		"--strategy",
		"--ref",
		"--source",
		"--install-dir",
		"--bin-dir"
	};

	private static readonly HashSet<string> CliFlagOptions = new(StringComparer.OrdinalIgnoreCase)
	{
		"--https",
		"--verbose",
		"-f",
		"--force",
		"-y",
		"--yes",
		"--framework-dependent"
	};

	/// <summary>
	/// Returns <c>true</c> when <paramref name="name"/> is a first-class <c>bld</c> command.
	/// </summary>
	public static bool IsKnownCommand(string? name)
	{
		return !string.IsNullOrWhiteSpace(name) && Commands.Contains(name);
	}

	/// <summary>
	/// Inserts <c>build</c> when no command was supplied, and forwards unknown commands
	/// to the Build submodule as Cake targets.
	/// </summary>
	public static string[] Apply(IReadOnlyList<string> args)
	{
		ArgumentNullException.ThrowIfNull(args);

		if (args.Count == 0)
		{
			return [DefaultCommand];
		}

		var first = args[0];
		if (MetaOptions.Contains(first))
		{
			return ToArray(args);
		}

		if (first.Equals(DefaultCommand, StringComparison.OrdinalIgnoreCase))
		{
			return NormalizeBuildArguments(args);
		}

		if (Commands.Contains(first))
		{
			return ToArray(args);
		}

		if (first.StartsWith('-'))
		{
			return Prepend(DefaultCommand, args);
		}

		return NormalizeBuildArguments(Prepend(DefaultCommand, args));
	}

	private static string[] NormalizeBuildArguments(IReadOnlyList<string> args)
	{
		var result = new List<string> { DefaultCommand };
		var index = 1;

		if (index < args.Count && !args[index].StartsWith('-'))
		{
			result.Add(args[index]);
			index++;
		}

		var ours = new List<string>();
		var cake = new List<string>();
		SplitCliAndCakeArguments(args, index, ours, cake);

		result.AddRange(ours);
		if (cake.Count > 0)
		{
			result.Add("--");
			result.AddRange(cake);
		}

		return result.ToArray();
	}

	private static void SplitCliAndCakeArguments(
		IReadOnlyList<string> args,
		int startIndex,
		List<string> cliArguments,
		List<string> cakeArguments)
	{
		for (var i = startIndex; i < args.Count; i++)
		{
			var arg = args[i];
			if (arg == "--")
			{
				for (var j = i + 1; j < args.Count; j++)
				{
					cakeArguments.Add(args[j]);
				}

				return;
			}

			var name = OptionName(arg);
			if (CliFlagOptions.Contains(name))
			{
				cliArguments.Add(arg);
				continue;
			}

			if (CliValueOptions.Contains(name))
			{
				cliArguments.Add(arg);
				if (!arg.Contains('=') && i + 1 < args.Count)
				{
					cliArguments.Add(args[++i]);
				}

				continue;
			}

			cakeArguments.Add(arg);
		}
	}

	private static string OptionName(string argument)
	{
		var equals = argument.IndexOf('=');
		return equals < 0 ? argument : argument[..equals];
	}

	private static string[] Prepend(string command, IReadOnlyList<string> args)
	{
		var routed = new string[args.Count + 1];
		routed[0] = command;
		for (var i = 0; i < args.Count; i++)
		{
			routed[i + 1] = args[i];
		}

		return routed;
	}

	private static string[] ToArray(IReadOnlyList<string> args)
	{
		return args as string[] ?? args.ToArray();
	}
}
