using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using Cysharp.Threading.Tasks;
using Nox.Avatars.Menus;
using Nox.Avatars.Parameters;
using Nox.UI;
using UnityEngine;
using Logger = Nox.CCK.Utils.Logger;

namespace Nox.Avatars.Runtime.radial {
	/// <summary>
	/// Convertisseur entre le menu de l'avatar (Avatars.Menus : <see cref="IMenuModule"/>,
	/// <see cref="IMenuEntry"/>, <see cref="IToggleEntry"/>) et les pages du menu radial
	/// de nox.ui (<see cref="IRadialPage"/> / <see cref="IRadialElement"/>).
	/// <para>
	/// La racine du radial est un hub ("Parameters") qui ouvre le menu de l'avatar :
	/// le premier niveau du IMenu racine est affiché à <see cref="ParametersPath"/>,
	/// puis chaque sous-menu est adressé par son id (<c>"/parameters/&lt;id&gt;/..."</c>).
	/// Les changements de page sont exposés via <see cref="IPageAction"/> et exécutés par
	/// nox.ui (le generator navigue via le menu). L'élément de navigation (Close/Back)
	/// est aussi ajouté par nox.ui.
	/// </para>
	/// </summary>
	public static class AvatarMenuConverter {
		/// <summary>Chemin racine du radial avatar.</summary>
		public const string RootPath = "/";

		/// <summary>Chemin du menu des paramètres (page du IMenu racine de l'avatar).</summary>
		public const string ParametersPath = "/parameters";

		/// <summary>Clé de traduction de l'entrée "Parameters" du hub.</summary>
		public const string ParametersLabelKey = "radial.parameters";

		/// <summary>
		/// Clé spéciale "value" : renvoie directement le premier argument (texte
		/// déjà traduit fourni par les entrées du IMenu de l'avatar).
		/// </summary>
		public const string ValueLabelKey = "value";

		private static string[] LabelFor(string text)
			=> new[] { ValueLabelKey, text };

		/// <summary>Page hub : une seule entrée "Parameters" qui ouvre le menu de l'avatar.</summary>
		public static IRadialPage CreateHub()
			=> new Page(RootPath, new IRadialElement[] {
				new Element(new[] { ParametersLabelKey }, null, new PageAction(ParametersPath)),
			});

		/// <summary>Page du menu de l'avatar correspondant à un chemin (sous "/parameters").</summary>
		public static IRadialPage ToRadialPage(IMenuEntry root, string path)
			=> ToRadialPage(root, path, ParametersPath);

		/// <summary>Page du menu de l'avatar à un chemin sous le chemin de base du menu.</summary>
		public static IRadialPage ToRadialPage(IMenuEntry root, string path, string basePath) {
			var menu   = Resolve(root, path, basePath);
			var content = BuildContent(menu, path);
			return new Page(path, content);
		}

		private static IRadialElement[] BuildContent(IMenuEntry menu, string pagePath) {
			if (menu == null || menu.Entries == null)
				return Array.Empty<IRadialElement>();

			var list = new List<IRadialElement>();
			foreach (var entry in menu.Entries) {
				if (entry == null)
					continue;
				list.Add(ToElement(pagePath, entry));
			}
			return list.ToArray();
		}

		private static IRadialElement ToElement(string pagePath, IEntry entry) {
			// Sous-menu → changement de page (IPageAction exécutée par nox.ui).
			if (entry is IMenuEntry) {
				var childPath = pagePath + "/" + entry.Id;
				return new Element(LabelFor(entry.Label), entry.Icon, new PageAction(childPath));
			}

			// Toggle → bascule du paramètre associé.
			if (entry is IToggleEntry toggle)
				return new Element(LabelFor(entry.Label), entry.Icon, new ToggleAction(toggle.Parameter));

			// Autre entrée : simple affichage (non cliquable).
			return new Element(LabelFor(entry.Label), entry.Icon, null);
		}

		/// <summary>
		/// Résout la page demandée depuis le menu racine : le premier niveau est au
		/// chemin <paramref name="basePath"/>, les sous-menus sont ensuite adressés
		/// par leurs ids.
		/// </summary>
		private static IMenuEntry Resolve(IMenuEntry root, string path, string basePath) {
			if (root == null)
				return null;
			if (path == basePath)
				return root;
			if (string.IsNullOrEmpty(basePath) || !path.StartsWith(basePath + "/"))
				return null;

			var menu = root;
			foreach (var segment in path[(basePath.Length + 1)..].Split('/', StringSplitOptions.RemoveEmptyEntries)) {
				if (!int.TryParse(segment, out var id))
					return null;
				menu = FindSubmenu(menu, id);
				if (menu == null)
					return null;
			}
			return menu;
		}

		private static IMenuEntry FindSubmenu(IMenuEntry menu, int id) {
			if (menu?.Entries == null)
				return null;
			foreach (var entry in menu.Entries)
				if (entry is IMenuEntry submenu && submenu.Id == id)
					return submenu;
			return null;
		}

		/// <summary>Page radiale du menu avatar.</summary>
		private sealed class Page : IRadialPage {
			public Page(string path, IRadialElement[] content) {
				Key     = path;
				Context = new object[] { path };
				Content = content;
			}

			public string Key { get; }

			public object[] Context { get; }

			public IRadialMenu Menu { get; }

			public IRadialElement[] Content { get; }

			public void OnOpen(IRadialPage lastPage) { }

			public void OnRestore(IRadialPage lastPage) { }

			public void OnRefresh() { }

			public void OnRemove() { }

			public void OnDisplay(IRadialPage lastPage) { }

			public void OnHide(IRadialPage nextPage) { }
		}

		/// <summary>Élément radial (libellé clé + arguments, icône + action).</summary>
		private sealed class Element : IRadialElement {
			public Element(string[] label, Sprite icon, IRadialElementAction action) {
				Label  = label;
				Icon   = UniTask.FromResult(icon);
				Action = action;
			}

			public string[] Label { get; }

			public UniTask<Sprite> Icon { get; }

			public IRadialElementAction Action { get; }
		}

		/// <summary>
		/// Changement de page (sous-menu). Le chemin est exposé ; l'exécution est
		/// laissée à nox.ui (RadialGenerator navigue via le menu).
		/// </summary>
		private sealed class PageAction : IPageAction {
			public string Path { get; }

			public PageAction(string path) 
				=> Path = path;
		}

		/// <summary>Bascule d'un paramètre de l'avatar (booléen pour l'instant).</summary>
		private sealed class ToggleAction : ITriggerAction {
			private readonly string _parameter;

			public ToggleAction(string parameter) {
				_parameter = parameter;
			}

			public async UniTask Execute(CancellationToken cancellationToken = default) {
				var parameter = GetParameter(_parameter);
				if (parameter == null) {
					Logger.LogWarning($"[avatar] paramètre introuvable : {_parameter}");
					return;
				}

				if (parameter.GetValueType() == ParameterType.Bool) {
					var current = parameter.Get() is bool b && b;
					parameter.Set(!current);
					Logger.LogDebug($"[avatar] {_parameter} = {!current}");
					return;
				}

				Logger.LogDebug($"[avatar] {_parameter} = {parameter.Get()} (toggle spécifique à venir)");
			}

			private static IParameter GetParameter(string name)
				=> Client.CurrentAvatar?.Descriptor?.GetModules<IParameterModule>().FirstOrDefault()?.GetParameter(name);
		}
	}
}
