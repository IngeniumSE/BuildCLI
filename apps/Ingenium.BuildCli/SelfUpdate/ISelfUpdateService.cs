// This work is licensed under the terms of the MIT license.
// For a copy, see <https://opensource.org/licenses/MIT>.

namespace Ingenium.BuildCli.SelfUpdate;

/// <summary>
/// Republishes and reinstalls the <c>bld</c> CLI.
/// </summary>
public interface ISelfUpdateService
{
	/// <summary>
	/// Clones or uses a local checkout, publishes <c>bld</c>, and replaces the installed binary.
	/// </summary>
	Task<SelfUpdateResult> UpdateAsync(SelfUpdateRequest request, CancellationToken cancellationToken = default);
}
