using Nox.CCK.Audio;
using UnityEngine;
using UnityEngine.Audio;

namespace Nox.Avatars.Runtime {
	/// <summary>
	/// Routes every <see cref="AudioSource"/> in an avatar (that isn't already routed
	/// to another mixer group, e.g. the avatar's voice) to the "avatar" channel's
	/// dedicated <see cref="AudioMixerGroup"/>. Avatar volume is then controlled by
	/// that mixer track, leaving <see cref="AudioSource.volume"/> free for content.
	/// </summary>
	public sealed class AvatarAudioGroup : MonoBehaviour {
		private static ChannelRegister Register 
            => Main.AvatarRegister;

		private void OnEnable() {
			Apply();
		}

		/// <summary>
		/// Route every (non-routed) <see cref="AudioSource"/> under this avatar to the
		/// avatar channel's mixer group. Sources already assigned to another mixer
		/// (e.g. voice) are left untouched.
		/// </summary>
		public void Apply() {
			var group = Register?.MixerGroup;
			if (group == null)
				return;

			var sources = GetComponentsInChildren<AudioSource>(true);
			for (int i = 0; i < sources.Length; i++) {
				var source = sources[i];
				if (source == null || source.outputAudioMixerGroup != null)
					continue;
				source.outputAudioMixerGroup = group;
			}
		}
	}
}