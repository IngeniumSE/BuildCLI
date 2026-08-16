// This work is licensed under the terms of the MIT license.
// For a copy, see <https://opensource.org/licenses/MIT>.

namespace Ingenium.BuildCli.Git;

/// <summary>
/// A single submodule recorded in <c>.gitmodules</c>.
/// </summary>
/// <param name="Name">The submodule name.</param>
/// <param name="Path">The path relative to the repository root.</param>
/// <param name="Url">The remote URL, when present.</param>
/// <param name="Branch">The tracked branch, when present.</param>
public sealed record GitmoduleEntry(string Name, string Path, string? Url, string? Branch);
