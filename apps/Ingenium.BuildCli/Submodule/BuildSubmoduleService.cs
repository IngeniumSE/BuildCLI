// This work is licensed under the terms of the MIT license.
// For a copy, see <https://opensource.org/licenses/MIT>.

using Ingenium.BuildCli.Git;

namespace Ingenium.BuildCli.Submodule;

/// <summary>
/// Orchestrates git submodule operations for the Ingenium Build repository.
/// </summary>
public sealed class BuildSubmoduleService : IBuildSubmoduleService
{
	private readonly IGitClient _git;

	/// <summary>
	/// Initializes a new instance of the <see cref="BuildSubmoduleService"/> class.
	/// </summary>
	public BuildSubmoduleService(IGitClient git)
	{
		_git = git;
	}

	/// <inheritdoc />
	public async Task<BuildSubmoduleChange> InitAsync(BuildSubmoduleRequest request, CancellationToken cancellationToken = default)
	{
		var context = await CreateContextAsync(request, requireRegistered: false, cancellationToken);
		if (context.Entry is not null && !request.Force)
		{
			if (IsInitialized(context))
			{
				throw new BuildCliException(
					$"The Build submodule is already initialized at '{context.RelativePath}'. Use 'buildcli update' to change version.",
					ExitCodes.AlreadyInitialized);
			}

			await EnsureCheckedOutAsync(context, cancellationToken);
			return await CheckoutRefAsync(context, request.Tag, added: false, previousCommit: null, cancellationToken);
		}

		if (context.Entry is not null && request.Force)
		{
			await EnsureCheckedOutAsync(context, cancellationToken);
			return await CheckoutRefAsync(context, request.Tag, added: false, previousCommit: await TryGetHeadAsync(context, cancellationToken), cancellationToken);
		}

		var destination = Path.Combine(context.RepositoryRoot, context.RelativePath);
		if (Directory.Exists(destination) && Directory.EnumerateFileSystemEntries(destination).Any() && !request.Force)
		{
			throw new BuildCliException(
				$"The path '{context.RelativePath}' already exists and is not empty. Use --force to add the submodule anyway.");
		}

		var addArgs = new List<string> { "submodule", "add" };
		if (request.Force)
		{
			addArgs.Add("--force");
		}

		addArgs.Add("--name");
		addArgs.Add(Path.GetFileName(context.RelativePath.TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar)));
		addArgs.Add(context.Url);
		addArgs.Add(ToGitPath(context.RelativePath));

		await _git.RunRequiredAsync(
			context.RepositoryRoot,
			addArgs,
			$"Failed to add the Build submodule from '{context.Url}'.",
			cancellationToken: cancellationToken);

		var initialized = context with { Entry = new GitmoduleEntry(Path.GetFileName(context.RelativePath), context.RelativePath, context.Url, null) };
		return await CheckoutRefAsync(initialized, request.Tag, added: true, previousCommit: null, cancellationToken);
	}

	/// <inheritdoc />
	public async Task<BuildSubmoduleChange> UpdateAsync(BuildSubmoduleRequest request, CancellationToken cancellationToken = default)
	{
		var context = await CreateContextAsync(request, requireRegistered: true, cancellationToken);
		await EnsureCheckedOutAsync(context, cancellationToken);
		var previous = await TryGetHeadAsync(context, cancellationToken);
		return await CheckoutRefAsync(context, request.Tag, added: false, previous, cancellationToken);
	}

	/// <inheritdoc />
	public async Task<BuildSubmoduleStatus> GetStatusAsync(BuildSubmoduleRequest request, CancellationToken cancellationToken = default)
	{
		EnsureGitAvailable();
		var root = await _git.GetRepositoryRootAsync(request.RepositoryPath, cancellationToken);
		var entry = FindBuildEntry(root, request);
		var url = ResolveUrl(request, entry, await TryGetOriginUrlAsync(root, cancellationToken));
		var relativePath = ResolveRelativePath(request, entry);
		var initialized = entry is not null && IsInitialized(new SubmoduleContext(root, relativePath, url, entry));

		string? commit = null;
		IReadOnlyList<string> currentTags = [];
		if (initialized)
		{
			var context = new SubmoduleContext(root, relativePath, url, entry);
			commit = await TryGetHeadAsync(context, cancellationToken);
			currentTags = await GetTagsPointingAtHeadAsync(context, cancellationToken);
		}

		RemoteTag? latest = null;
		try
		{
			var tags = await ListRemoteTagsAsync(url, cancellationToken);
			latest = TagSelector.SelectLatest(tags);
		}
		catch (BuildCliException)
		{
			// Status should still render when the remote is unreachable.
		}

		return new BuildSubmoduleStatus
		{
			RepositoryRoot = root,
			IsRegistered = entry is not null,
			IsInitialized = initialized,
			RelativePath = relativePath,
			Url = url,
			Commit = commit,
			CurrentTags = currentTags,
			LatestTag = latest?.Name,
			LatestCommit = latest?.Commit
		};
	}

	/// <inheritdoc />
	public async Task<IReadOnlyList<RemoteTag>> ListTagsAsync(BuildSubmoduleRequest request, CancellationToken cancellationToken = default)
	{
		EnsureGitAvailable();
		string url;
		try
		{
			var root = await _git.GetRepositoryRootAsync(request.RepositoryPath, cancellationToken);
			var entry = FindBuildEntry(root, request);
			url = ResolveUrl(request, entry, await TryGetOriginUrlAsync(root, cancellationToken));
		}
		catch (BuildCliException ex) when (ex.ExitCode == ExitCodes.NotAGitRepository)
		{
			url = request.Url ?? BuildRepositoryUrls.GetDefault(request.UseHttps);
		}

		return await ListRemoteTagsAsync(url, cancellationToken);
	}

	private async Task<SubmoduleContext> CreateContextAsync(
		BuildSubmoduleRequest request,
		bool requireRegistered,
		CancellationToken cancellationToken)
	{
		EnsureGitAvailable();
		var root = await _git.GetRepositoryRootAsync(request.RepositoryPath, cancellationToken);
		var entry = FindBuildEntry(root, request);
		if (requireRegistered && entry is null)
		{
			throw new BuildCliException(
				"The Build submodule is not registered in this repository. Run 'buildcli init' first.",
				ExitCodes.SubmoduleNotFound);
		}

		var url = ResolveUrl(request, entry, await TryGetOriginUrlAsync(root, cancellationToken));
		var relativePath = ResolveRelativePath(request, entry);
		return new SubmoduleContext(root, relativePath, url, entry);
	}

	private void EnsureGitAvailable()
	{
		if (!_git.IsGitAvailable())
		{
			throw new BuildCliException(
				"git was not found on PATH. Install Git and try again.",
				ExitCodes.GitNotFound);
		}
	}

	private static GitmoduleEntry? FindBuildEntry(string repositoryRoot, BuildSubmoduleRequest request)
	{
		var entries = GitmodulesParser.ParseFile(Path.Combine(repositoryRoot, ".gitmodules"));
		if (entries.Count == 0)
		{
			return null;
		}

		if (!string.IsNullOrWhiteSpace(request.SubmodulePath))
		{
			var requested = ToGitPath(request.SubmodulePath);
			return entries.FirstOrDefault(entry =>
				entry.Path.Equals(requested, StringComparison.OrdinalIgnoreCase)
				|| entry.Name.Equals(requested, StringComparison.OrdinalIgnoreCase));
		}

		var buildEntries = entries.Where(entry => BuildRepositoryUrls.IsBuildRepository(entry.Url)).ToList();
		if (buildEntries.Count == 1)
		{
			return buildEntries[0];
		}

		if (buildEntries.Count > 1)
		{
			return buildEntries.FirstOrDefault(entry =>
					entry.Path.Equals(BuildRepositoryUrls.DefaultPath, StringComparison.OrdinalIgnoreCase))
				?? buildEntries[0];
		}

		return entries.FirstOrDefault(entry =>
			entry.Path.Equals(BuildRepositoryUrls.DefaultPath, StringComparison.OrdinalIgnoreCase)
			|| entry.Path.Equals("Build", StringComparison.Ordinal));
	}

	private static string ResolveRelativePath(BuildSubmoduleRequest request, GitmoduleEntry? entry)
	{
		if (!string.IsNullOrWhiteSpace(request.SubmodulePath))
		{
			return ToGitPath(request.SubmodulePath);
		}

		return entry?.Path ?? BuildRepositoryUrls.DefaultPath;
	}

	private static string ResolveUrl(BuildSubmoduleRequest request, GitmoduleEntry? entry, string? parentRemote)
	{
		if (!string.IsNullOrWhiteSpace(request.Url))
		{
			return request.Url;
		}

		if (!string.IsNullOrWhiteSpace(entry?.Url))
		{
			return entry.Url;
		}

		return BuildRepositoryUrls.InferFromParentRemote(parentRemote, request.UseHttps ? true : null);
	}

	private async Task<string?> TryGetOriginUrlAsync(string repositoryRoot, CancellationToken cancellationToken)
	{
		var result = await _git.RunAsync(repositoryRoot, ["remote", "get-url", "origin"], cancellationToken);
		return result.IsSuccess ? result.StandardOutput.Trim() : null;
	}

	private static bool IsInitialized(SubmoduleContext context)
	{
		var gitDir = Path.Combine(context.AbsolutePath, ".git");
		return File.Exists(gitDir) || Directory.Exists(gitDir);
	}

	private async Task EnsureCheckedOutAsync(SubmoduleContext context, CancellationToken cancellationToken)
	{
		if (IsInitialized(context))
		{
			return;
		}

		await _git.RunRequiredAsync(
			context.RepositoryRoot,
			["submodule", "update", "--init", "--", ToGitPath(context.RelativePath)],
			$"Failed to initialize the Build submodule at '{context.RelativePath}'.",
			cancellationToken: cancellationToken);
	}

	private async Task<BuildSubmoduleChange> CheckoutRefAsync(
		SubmoduleContext context,
		string? requestedRef,
		bool added,
		string? previousCommit,
		CancellationToken cancellationToken)
	{
		await _git.RunRequiredAsync(
			context.AbsolutePath,
			["fetch", "origin", "--tags", "--prune"],
			"Failed to fetch Build submodule tags.",
			cancellationToken: cancellationToken);

		var tags = await ListRemoteTagsAsync(context.Url, cancellationToken);
		string checkoutRef;
		if (string.IsNullOrWhiteSpace(requestedRef))
		{
			var latest = TagSelector.SelectLatest(tags);
			checkoutRef = latest?.Name ?? await GetDefaultRemoteRefAsync(context, cancellationToken);
		}
		else
		{
			var match = TagSelector.Find(tags, requestedRef);
			checkoutRef = match?.Name ?? requestedRef;
		}

		var checkout = await _git.RunAsync(
			context.AbsolutePath,
			["checkout", "--detach", checkoutRef],
			cancellationToken);

		if (!checkout.IsSuccess)
		{
			throw new BuildCliException(
				$"Could not check out '{checkoutRef}' in the Build submodule.{Environment.NewLine}{checkout.ErrorMessage}",
				ExitCodes.RefNotFound);
		}

		await _git.RunRequiredAsync(
			context.RepositoryRoot,
			["add", "--", ToGitPath(context.RelativePath), ".gitmodules"],
			"Failed to stage the Build submodule change.",
			cancellationToken: cancellationToken);

		var commit = await TryGetHeadAsync(context, cancellationToken)
			?? throw new BuildCliException("The Build submodule checkout succeeded but HEAD could not be read.");

		return new BuildSubmoduleChange
		{
			RepositoryRoot = context.RepositoryRoot,
			RelativePath = context.RelativePath,
			Url = context.Url,
			CheckedOutRef = checkoutRef,
			Commit = commit,
			PreviousCommit = previousCommit,
			Added = added
		};
	}

	private async Task<string> GetDefaultRemoteRefAsync(SubmoduleContext context, CancellationToken cancellationToken)
	{
		var result = await _git.RunAsync(context.AbsolutePath, ["rev-parse", "--abbrev-ref", "origin/HEAD"], cancellationToken);
		if (result.IsSuccess)
		{
			var value = result.StandardOutput.Trim();
			if (!string.IsNullOrEmpty(value) && !value.Equals("origin/HEAD", StringComparison.Ordinal))
			{
				return value;
			}
		}

		foreach (var candidate in new[] { "origin/main", "origin/master" })
		{
			var probe = await _git.RunAsync(context.AbsolutePath, ["rev-parse", "--verify", candidate], cancellationToken);
			if (probe.IsSuccess)
			{
				return candidate;
			}
		}

		throw new BuildCliException(
			"The Build repository has no tags and no default branch could be determined.",
			ExitCodes.RefNotFound);
	}

	private async Task<string?> TryGetHeadAsync(SubmoduleContext context, CancellationToken cancellationToken)
	{
		if (!IsInitialized(context))
		{
			return null;
		}

		var result = await _git.RunAsync(context.AbsolutePath, ["rev-parse", "HEAD"], cancellationToken);
		return result.IsSuccess ? result.StandardOutput.Trim() : null;
	}

	private async Task<IReadOnlyList<string>> GetTagsPointingAtHeadAsync(SubmoduleContext context, CancellationToken cancellationToken)
	{
		var result = await _git.RunAsync(context.AbsolutePath, ["tag", "--points-at", "HEAD"], cancellationToken);
		if (!result.IsSuccess)
		{
			return [];
		}

		return result.StandardOutput
			.Split(['\r', '\n'], StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
			.ToArray();
	}

	private async Task<IReadOnlyList<RemoteTag>> ListRemoteTagsAsync(string url, CancellationToken cancellationToken)
	{
		var result = await _git.RunRequiredAsync(
			Environment.CurrentDirectory,
			["ls-remote", "--tags", "--sort=-v:refname", url],
			$"Failed to list tags from '{url}'.",
			cancellationToken: cancellationToken);

		return TagSelector.ParseLsRemote(result.StandardOutput);
	}

	private static string ToGitPath(string path)
	{
		return path.Replace('\\', '/').Trim('/');
	}

	private sealed record SubmoduleContext(
		string RepositoryRoot,
		string RelativePath,
		string Url,
		GitmoduleEntry? Entry)
	{
		public string AbsolutePath => Path.Combine(RepositoryRoot, RelativePath.Replace('/', Path.DirectorySeparatorChar));
	}
}
