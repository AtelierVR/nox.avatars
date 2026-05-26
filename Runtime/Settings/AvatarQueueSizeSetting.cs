using Nox.CCK.Settings;
using Nox.CCK.Utils;
using UnityEngine;

namespace Nox.Avatars.Runtime.Settings {
	public sealed class AvatarQueueSizeSetting : RangeHandler {
		private const string ConfigKey = "settings.avatar.general.queue";
		internal const int DefaultValue = 3;
		private const int MinValue = 1;
		private const int MaxValue = 16;

		public override string[] GetPath()
			=> new[] { "avatar", "general", "queue" };

		public override int GetOrder() => 0;

		public AvatarQueueSizeSetting() {
			SetRange(MinValue, MaxValue);
			SetStep(1);
			SetValue(Value, false);
			SetLabelKey("settings.entry.avatar.general.queue.label");
			SetValueKey("settings.range.value.integer");
		}

		protected override GameObject GetPrefab()
			=> Main.Instance.CoreAPI.AssetAPI.GetAsset<GameObject>("settings:prefabs/range.prefab");

		internal static int Value {
			get {
				var raw = Mathf.RoundToInt(Config.Load().Get(ConfigKey, DefaultValue));
				return Mathf.Clamp(raw, MinValue, MaxValue);
			}
			set {
				var clamped = Mathf.Clamp(value, MinValue, MaxValue);
				var config = Config.Load();
				config.Set(ConfigKey, clamped);
				config.Save();
			}
		}

		protected override void OnValueChanged(float value) 
			=> Value = Mathf.RoundToInt(value);
	}
}