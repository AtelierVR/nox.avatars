using System.Collections.Generic;
using Nox.CCK.Settings;
using Nox.CCK.Utils;
using Nox.UI;
using Nox.UI.modals;
using UnityEngine;

namespace Nox.Avatars.Runtime.Settings {
	/// <summary>
	/// Where the savable parameters of a local avatar are stored (see <see cref="ISavableContainer.Save"/>).
	/// <para>
	/// Path: avatar > general > save. <see cref="Local"/> keeps them on this machine only,
	/// <see cref="PreferredCloud"/> puts them in the table of the account when the player is connected, and
	/// falls back to this machine otherwise.
	/// </para>
	/// </summary>
	public sealed class ParametersSaveSetting : DropdownHandler {
		/// <summary>Value of the setting when the parameters stay on this machine.</summary>
		public const string Local = "local";

		/// <summary>Value of the setting when the parameters follow the account (local when offline).</summary>
		public const string PreferredCloud = "preferred_cloud";

		private const string ConfigKey = "settings.avatar.general.save";

		public override string[] Path
			=> new[] { "avatar", "general", "save" };

		public override int Order => 50002;

		public ParametersSaveSetting() {
			SetLabel($"settings.entry.{string.Join(".", Path)}.label");
			SetOptions(GetOptions());
			SetValue(Raw, notify: false);
		}

		private static Dictionary<string, string[]> GetOptions()
			=> new() {
				[Local]          = new[] { $"settings.entry.avatar.general.save.option.{Local}" },
				[PreferredCloud] = new[] { $"settings.entry.avatar.general.save.option.{PreferredCloud}" }
			};

		protected override GameObject GetPrefab()
			=> Main.Instance.CoreAPI.AssetAPI.GetAsset<GameObject>("settings:prefabs/dropdown.prefab");

		protected override IModalBuilder GetModalBuilder(IMenu menu)
			=> Main.Instance.CoreAPI?.ModAPI?.GetMod("ui")?.GetInstance<IUiAPI>()?.MakeModal(menu);

		/// <summary>Whether the parameters stay on this machine, whatever the connection state.</summary>
		internal static bool PrefersLocal
			=> Raw == Local;

		/// <summary>Value of the setting, normalised to one of the two options.</summary>
		private static string Raw {
			get => Config.Load().Get(ConfigKey, PreferredCloud) == Local ? Local : PreferredCloud;
			set {
				var config = Config.Load();
				config.Set(ConfigKey, value);
				config.Save();
			}
		}

		protected override void OnValueChanged(string value)
			=> Raw = value;
	}
}
