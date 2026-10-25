using System;
using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using Nox.Avatars.Controllers;
using Nox.CCK.Users;
using Nox.CCK.Utils;
using Nox.Terminal;

namespace Nox.Avatars.Runtime.Commands {
	/// <summary>
	/// Terminal command <c>avatar load &lt;hash|identifier&gt;</c>: loads an avatar and applies it to the
	/// current user.
	/// <para>
	/// <c>&lt;hash&gt;</c> is the name of a bundle in the avatar cache (see <see cref="AvatarCache"/>);
	/// <c>&lt;identifier&gt;</c> is a server avatar identifier (<c>a:42@server</c>, <c>42@server</c>, …).
	/// For an identifier, the avatar is also saved on the current user account, like the avatar picker
	/// does (<c>IUserAPI.UpdateCurrent</c>).
	/// </para>
	/// </summary>
	public class AvatarCommand : ICommand, IHelper {
		private const string CommandName = "avatar";
		private const string LoadName    = "load";

		public string GetName()
			=> CommandName;

		public string GetDescription()
			=> "Load an avatar for the current user.";

		public string GetShort()
			=> "load an avatar";

		public string GetUsage()
			=> $"{CommandName} {LoadName} <hash|identifier>";

		public string[] AutoComplete(string input, IContext context = null) {
			var value = input?.Trim().ToLowerInvariant() ?? string.Empty;
			var load  = $"{CommandName} {LoadName}";

			if (string.IsNullOrEmpty(value))
				return new[] { CommandName, load };

			if (CommandName.StartsWith(value))
				return new[] { CommandName };

			if (load.StartsWith(value))
				return new[] { load };

			return Array.Empty<string>();
		}

		public UniTask<bool> Execute(string input, IContext context = null)
			=> ExecuteAsync(input, context);

		private async UniTask<bool> ExecuteAsync(string input, IContext context) {
			var args       = ArgsParser.Parse(input);
			var positional = args.Positional;

			if (positional.Length == 0 || !positional[0].Equals(CommandName, StringComparison.OrdinalIgnoreCase))
				return false;

			if (positional.Length < 3 || !positional[1].Equals(LoadName, StringComparison.OrdinalIgnoreCase)) {
				context?.PrintLn(GetUsage());
				return true;
			}

			var token = positional[2].Trim();
			if (string.IsNullOrEmpty(token)) {
				context?.PrintLn(GetUsage());
				return true;
			}

			var controller = Main.Instance?.ControllerAPI?.Current as IControllerAvatar;
			if (controller == null) {
				context?.PrintLn("No avatar controller available. Join a session first.");
				return true;
			}

			// A hash is the name of a cached bundle; everything else is treated as a server identifier.
			if (AvatarCache.InCache(token)) {
				await LoadFromCache(token, controller, context);
				return true;
			}

			await LoadFromIdentifier(token, controller, context);
			return true;
		}

		private static async UniTask LoadFromCache(string hash, IControllerAvatar controller, IContext context) {
			context?.PrintLn($"Loading cached avatar '{hash}'...");

			var runtime = await AvatarLoader.LoadFromCache(
				hash,
				new Dictionary<string, object> {
					["source"] = "avatar-command",
					["local"]  = true,
					["hash"]   = hash
				}
			);

			if (runtime == null) {
				context?.PrintLn($"Failed to load cached avatar '{hash}'.");
				return;
			}

			if (!await controller.SetAvatar(runtime)) {
				await runtime.Dispose();
				context?.PrintLn($"Failed to apply cached avatar '{hash}'.");
				return;
			}

			context?.PrintLn($"Avatar '{hash}' is now active (local cache, not announced to the session).");
		}

		private static async UniTask LoadFromIdentifier(string raw, IControllerAvatar controller, IContext context) {
			Identifier identifier;
			try {
				identifier = Identifier.Parse(raw);
			} catch (Exception exception) {
				context?.PrintLn($"Invalid avatar identifier '{raw}': {exception.Message}");
				return;
			}

			if (!identifier.IsValid()) {
				context?.PrintLn($"Invalid avatar identifier '{raw}'.");
				return;
			}

			context?.PrintLn($"Loading avatar {identifier.ToString()}...");

			// Save it on the account first, like the avatar picker (IUserAPI.UpdateCurrent + SetAvatar).
			var user = Main.UserAPI;
			if (user?.Current != null) {
				try {
					await user.UpdateCurrent(new UpdateCurrentRequest { Avatar = identifier });
				} catch (Exception exception) {
					context?.PrintLn($"Could not save the avatar on the current user: {exception.Message}");
				}
			}

			string lastMessage = null;
			var avatar = await controller.SetAvatar(identifier, (message, progress) => {
				if (context == null || string.IsNullOrEmpty(message) || message == lastMessage)
					return;

				lastMessage = message;
				context.PrintLn(message);
			});

			if (avatar == null) {
				context?.PrintLn($"Failed to load avatar {identifier.ToString()}.");
				return;
			}

			context?.PrintLn($"Avatar {identifier.ToString()} is now active.");
		}
	}
}
