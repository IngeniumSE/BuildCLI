// This work is licensed under the terms of the MIT license.
// For a copy, see <https://opensource.org/licenses/MIT>.

namespace Ingenium.BuildCli.Git;

/// <summary>
/// Optional diagnostic output for git invocations.
/// </summary>
public interface IGitTrace
{
	/// <summary>
	/// Gets or sets a value indicating whether git commands should be written.
	/// </summary>
	bool Enabled { get; set; }

	/// <summary>
	/// Writes a diagnostic line.
	/// </summary>
	void Write(string message);
}
