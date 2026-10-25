using System;
using Nox.Terminal;
using Logger = Nox.CCK.Utils.Logger;

namespace Nox.Avatars.Runtime.Commands {
	/// <summary>
	/// Registers the avatar terminal commands. Mirrors the other mods' command hosts: the commands are
	/// registered when <c>Main</c> initializes and unregistered when it is disposed.
	/// </summary>
	public class AvatarCommands : IDisposable {
		internal static ITerminalAPI TerminalAPI
			=> Main.Instance?.CoreAPI.ModAPI
				.GetMod("terminal")
				?.GetInstance<ITerminalAPI>();

		private readonly (uint, ICommand)[] _list = {
			(0, new AvatarCommand())
		};

		public AvatarCommands() {
			if (TerminalAPI == null) {
				Logger.LogWarning("Terminal API is not available, the avatar commands are not registered.");
				return;
			}

			for (var i = 0; i < _list.Length; i++)
				_list[i] = (TerminalAPI.Register(_list[i].Item2), _list[i].Item2);
		}

		public void Dispose() {
			if (TerminalAPI == null)
				return;

			for (var i = 0; i < _list.Length; i++)
				if (_list[i].Item1 != uint.MaxValue)
					TerminalAPI.Unregister(_list[i].Item1);
		}
	}
}
