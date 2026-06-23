using System;
using System.Linq;
using Nox.CCK.Search;
using Nox.Search;
using UnityEngine;

namespace Nox.Avatars.Runtime.Search {
	public class SearchHandler : IHandler {
		public string GetId()
			=> Main.Instance.CoreAPI.ModMetadata.GetId();

		public string GetTitleKey()
			=> "avatar.search.title";

		public string[] GetTitleArguments()
			=> Array.Empty<string>();

		public string GetPlaceholderKey()
			=> "avatar.search.placeholder";

		public string[] GetPlaceholderArguments()
			=> Array.Empty<string>();

		public Texture2D GetIcon()
			=> Main.Instance.CoreAPI.AssetAPI
				.GetAsset<Texture2D>("ui:icons/avatar.png");

		public string GetDescriptionKey()
			=> "avatar.search.description";

		public string[] GetDescriptionArguments()
			=> Array.Empty<string>();

		public IWorker[] GetWorkers()
			=> SearchHelper.ServersBy("avatar")
				.Select(s => new SearchWorker { Title = s.Title, ServerAddress = s.Address })
				.ToArray<IWorker>();
	}
}