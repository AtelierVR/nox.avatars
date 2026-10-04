using System;
using Nox.CCK.Avatars;
using Nox.CCK.Utils;

namespace Nox.Avatars.Pipeline {
	/// <summary>
	/// Ce qu'un appelant demande à <see cref="Builder.Build"/> : un avatar, une destination. Le nombre
	/// de variants vient des plateformes du descriptor, et le répertoire de travail est créé par le build.
	/// </summary>
	public class BuildData {
		/// <summary>
		/// Avatar à construire. Ses <see cref="AvatarDescriptor.Targets"/> donnent un variant par
		/// plateforme (vide = plateforme courante). Le build ne le modifie jamais.
		/// </summary>
		public AvatarDescriptor Descriptor;

		/// <summary>Dossier de destination des bundles.</summary>
		public string OutputPath;

		/// <summary>Plateforme du variant en cours de construction (positionnée par le builder).</summary>
		public Platform Target;

		/// <summary>Nom du bundle en cours de construction (positionné par le builder).</summary>
		public string Filename;

		/// <summary>Répertoire de travail temporaire (positionné par le builder).</summary>
		public string TempPath;

		public Action<float, string> ProgressCallback = (_, _) => { };
	}
}
