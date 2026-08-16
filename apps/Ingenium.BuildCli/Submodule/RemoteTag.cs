// This work is licensed under the terms of the MIT license.
// For a copy, see <https://opensource.org/licenses/MIT>.

namespace Ingenium.BuildCli.Submodule;

/// <summary>
/// A tag advertised by a git remote.
/// </summary>
/// <param name="Name">The tag name, without the <c>refs/tags/</c> prefix.</param>
/// <param name="Commit">The commit the tag points at.</param>
public sealed record RemoteTag(string Name, string Commit);
