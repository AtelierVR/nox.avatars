using Nox.Avatars.Menus;
using Nox.UI;

namespace Nox.Avatars.Runtime.radial {
	/// <summary>
	/// Fournisseur des pages radiales de l'avatar. Récupère le menu racine de
	/// l'avatar (<see cref="IMenuModule.Menu"/>, SDK Avatars.Menus) et le convertit
	/// en page radiale nox.ui via <see cref="AvatarMenuConverter"/> (les sous-menus
	/// sont adressés par leur id : "/&lt;id&gt;/..."). L'élément de navigation
	/// (Close/Back) est ajouté par nox.ui.
	/// </summary>
	public static class AvatarRadialPage {
		/// <summary>Chemin racine du menu radial de l'avatar.</summary>
		public const string RootPath = "/";

		/// <summary>
		/// Crée la page radiale correspondant à un chemin. Renvoie toujours une page
		/// (le contenu peut être vide → l'élément Close/Back est tout de même ajouté
		/// par nox.ui).
		/// </summary>
		public static IRadialPage Create(string path) {
			if (string.IsNullOrEmpty(path))
				return null;
			path = path.TrimEnd('/');
			if (path.Length == 0)
				path = RootPath;

			// Racine : hub qui ouvre le menu des paramètres (premier niveau du IMenu
			// de l'avatar, servi à "/parameters").
			if (path == RootPath)
				return AvatarMenuConverter.CreateHub();

			return AvatarMenuConverter.ToRadialPage(GetRootMenu(), path);
		}

		/// <summary>Menu racine de l'avatar courant (premier IMenuModule avec un menu).</summary>
		private static IMenuEntry GetRootMenu() {
			var modules = Client.CurrentAvatar?.Descriptor?.GetModules<IMenuModule>();
			if (modules == null)
				return null;
			foreach (var module in modules) {
				var menu = module?.Menu;
				if (menu != null)
					return menu;
			}
			return null;
		}
	}
}
