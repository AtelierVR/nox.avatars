using System;
using System.IO;
using System.Linq;
using System.Threading;
using Cysharp.Threading.Tasks;
using Newtonsoft.Json;
using Nox.CCK.Avatars;
using Nox.CCK.Convertors;
using Nox.CCK.Network.Assets;
using Nox.CCK.Utils;
using Nox.Network.Assets;
using UnityEngine;
using UnityEngine.Events;
using Logger = Nox.CCK.Utils.Logger;

namespace Nox.Avatars.Runtime.Network {
	/// <summary>
	/// Avatars on a node: the entity routes (<c>/avatars</c>) and the bundle an avatar is loaded from,
	/// both driven through the generic asset pipeline (<see cref="IAssetsAPI"/>) so avatars share its
	/// release/file/cache handling with every other asset type.
	/// </summary>
	public class Network {
		/// <summary>Invoked when an avatar is fetched from the server.</summary>
		private readonly UnityEvent<Avatar> _fetchEvent = new();

		/// <summary>The avatars collection served by the node.</summary>
		public static AssetEndpoint Endpoint
			=> AvatarsEndpoint.Endpoint;

		/// <summary>Asset pipeline exposed by the "network" mod, shared by every asset type.</summary>
		private static IAssetsAPI Assets
			=> Main.AssetsAPI;

		private void InvokeFetch(Avatar avatar) {
			if (avatar == null)
				return;
			_fetchEvent.Invoke(avatar);
			Main.Instance.CoreAPI.EventAPI.Emit("avatar_fetch", avatar);
		}

		private (string, string) Optimize(Identifier ide) {
			var crt = Main.UserAPI?.Current?.Server;
			if (!string.IsNullOrEmpty(crt))
				return ide.IsLocal(crt)
					? (ide.ToShortString(false), crt)
					: (ide.ToShortString(), crt);
			return (ide.ToShortString(), ide.Server);
		}

		/// <summary>Filters of an avatar search, mapped onto the generic collection filters.</summary>
		private static AssetSearchRequest ToAssetSearchRequest(SearchRequest data)
			=> new() {
				Query  = data.Query,
				Offset = data.Offset,
				Limit  = data.Limit
			};

		public async UniTask<Avatar> Fetch(Identifier ide, CancellationToken cancellationToken = default) {
			var (id, address) = Optimize(ide);
			if (address == Identifier.LOCAL_SERVER) {
				Logger.LogError($"Cannot fetch avatar {ide} from {address}");
				return null;
			}

			var assets = Assets;

			if (assets == null) {
				Logger.LogError("Cannot fetch avatar: the asset pipeline is not available.");
				return null;
			}

			var avatar = await assets.Fetch<Avatar>(address, Endpoint, id, token: cancellationToken);

			if (avatar == null)
				Logger.LogError($"Failed to fetch avatar {ide} from {address}");

			InvokeFetch(avatar);
			return avatar;
		}

		public async UniTask<SearchResponse> Search(SearchRequest data, CancellationToken cancellationToken = default) {
			var address = data.Server ?? Main.UserAPI?.Current?.Server;
			if (string.IsNullOrEmpty(address)) {
				Logger.LogError("Cannot search avatars: no server address provided.");
				return null;
			}

			var assets = Assets;

			if (assets == null) {
				Logger.LogError("Cannot search avatars: the asset pipeline is not available.");
				return null;
			}

			var response = await assets.Search<AssetSearchResponse<Avatar>>(
				address,
				Endpoint,
				ToAssetSearchRequest(data),
				cancellationToken
			);

			if (response == null) {
				Logger.LogError($"Failed to search avatars from {address}");
				return null;
			}

			foreach (var avatar in response.Items)
				InvokeFetch(avatar);

			var result = SearchResponse.From(response);

			if (result != null)
				result.Request = data;

			return result;
		}

		public async UniTask<Avatar> Create(AvatarCreateRequest data, string server, CancellationToken cancellationToken = default) {
			var address = server ?? Main.UserAPI?.Current?.Server;
			if (string.IsNullOrEmpty(address)) {
				Logger.LogError("Cannot create avatar: no server address provided.");
				return null;
			}

			var assets = Assets;

			if (assets == null) {
				Logger.LogError("Cannot create avatar: the asset pipeline is not available.");
				return null;
			}

			var avatar = await assets.Create<Avatar>(address, Endpoint, data, cancellationToken);

			if (avatar == null)
				Logger.LogError($"Failed to create avatar on {address}");

			InvokeFetch(avatar);
			return avatar;
		}

		public async UniTask<Avatar> Update(Identifier ide, AvatarUpdateRequest form, CancellationToken cancellationToken = default) {
			var (id, address) = Optimize(ide);
			if (address == Identifier.LOCAL_SERVER) {
				Logger.LogError($"Cannot update avatar {ide} from {address}");
				return null;
			}

			var assets = Assets;

			if (assets == null) {
				Logger.LogError("Cannot update avatar: the asset pipeline is not available.");
				return null;
			}

			var avatar = await assets.Update<Avatar>(address, Endpoint, id, form, cancellationToken);

			if (avatar == null)
				Logger.LogError($"Failed to update avatar {ide} from {address}");

			InvokeFetch(avatar);
			return avatar;
		}

		public async UniTask<bool> Delete(Identifier ide, CancellationToken cancellationToken = default) {
			var (id, address) = Optimize(ide);
			if (address == Identifier.LOCAL_SERVER) {
				Logger.LogError($"Cannot delete avatar {ide} from {address}");
				return false;
			}

			var assets = Assets;

			if (assets == null) {
				Logger.LogError("Cannot delete avatar: the asset pipeline is not available.");
				return false;
			}

			return await assets.Delete(address, Endpoint, id, cancellationToken);
		}

		/// <summary>
		/// Release an avatar is loaded from. A <c>v</c> query on the identifier pins the release by
		/// name (the version the avatar was published with); without it, the release the asset points
		/// at is used (<c>auto</c> resolving to the newest one).
		/// </summary>
		public async UniTask<IAssetRelease> ResolveRelease(Identifier ide, CancellationToken cancellationToken = default) {
			var (id, address) = Optimize(ide);
			if (address == Identifier.LOCAL_SERVER) {
				Logger.LogError($"Cannot resolve the release of avatar {ide} from {address}");
				return null;
			}

			var assets = Assets;

			if (assets == null) {
				Logger.LogError("Cannot resolve the avatar release: the asset pipeline is not available.");
				return null;
			}

			var version = ide.GetVersion();

			var release = version == AvatarIdentifierExtensions.DefaultVersion
				? await assets.FetchPreferredRelease(address, Endpoint, id, cancellationToken)
				: await assets.FetchRelease(address, Endpoint, id, version.ToString(), true, cancellationToken);

			if (release == null)
				Logger.LogWarning($"No release found for avatar {ide} (version {version}).");

			return release;
		}

		/// <summary>
		/// Bundle an avatar is loaded from: the file of its release that matches the current platform
		/// and engine, or <c>null</c> when the avatar has no compatible variant.
		/// </summary>
		public async UniTask<IAssetFile> ResolveBundle(Identifier ide, CancellationToken cancellationToken = default) {
			var release = await ResolveRelease(ide, cancellationToken);

			if (release == null)
				return null;

			var file = release.BestFile(PlatformExtensions.CurrentPlatform, EngineExtensions.CurrentEngine);

			if (file == null)
				Logger.LogError(
					$"No compatible bundle for avatar {ide}: release {release.Name ?? release.Id.ToString()} has no file for "
					+ $"{PlatformExtensions.CurrentPlatform.GetPlatformName()} on {EngineExtensions.CurrentEngine.GetEngineName()}."
				);

			return file;
		}

		/// <summary>
		/// Adds an image to an avatar, converting the texture to a temporary PNG first.
		/// </summary>
		public async UniTask<bool> AddImage(Identifier ide, Texture2D texture, Action<float> onProgress = null, CancellationToken cancellationToken = default) {
			if (!texture) {
				Logger.LogError($"Cannot add an image to avatar {ide}: texture is null.");
				return false;
			}

			var (id, address) = Optimize(ide);
			if (address == Identifier.LOCAL_SERVER) {
				Logger.LogError($"Cannot add an image to avatar {ide} from {address}");
				return false;
			}

			var assets = Assets;

			if (assets == null) {
				Logger.LogError("Cannot add an avatar image: the asset pipeline is not available.");
				return false;
			}

			byte[] data;

			try {
				data = texture.EncodeToPNG();

				if (data == null || data.Length == 0)
					throw new Exception("Encoded image data is null or empty.");
			} catch (Exception ex) {
				Logger.LogError(new Exception($"Failed to encode texture for avatar {ide}", ex));
				return false;
			}

			var path = Path.Combine(Application.temporaryCachePath, $"{Guid.NewGuid():N}.png");

			try {
				await File.WriteAllBytesAsync(path, data, cancellationToken);
				onProgress?.Invoke(0f);

				var avatar = await assets.AddImage(
					address,
					Endpoint,
					id,
					path,
					onProgress == null ? null : (ratio, _) => onProgress(ratio),
					cancellationToken
				);

				if (avatar == null) {
					Logger.LogError($"Failed to add an image to avatar {ide} on {address}");
					return false;
				}

				onProgress?.Invoke(1f);
				return true;
			} catch (Exception ex) {
				Logger.LogError(new Exception($"Failed to add an image to avatar {ide} on {address}", ex));
				return false;
			} finally {
				try {
					if (File.Exists(path))
						File.Delete(path);
				} catch (Exception ex) {
					Logger.LogWarning($"Failed to remove the temporary image '{path}': {ex.Message}");
				}
			}
		}

		[Serializable]
		public class Favorites : IFavorites {
			[JsonIgnore]
			public string Key { get; set; }
			[JsonProperty("label")]
			public string Label { get; set; }
			[JsonProperty("values"), JsonConverter(typeof(ArrayConverter<StringToIdentifierConverter>))]
			#pragma warning disable UAC1001
			public Identifier[] Values { get; set; }
			#pragma warning restore UAC1001
		}

		public async UniTask<Favorites> FetchFavorites(uint group = 0, bool pub = true) {
			var key   = $"{(pub ? "public." : "")}favorites.avatars.{group}";
			var entry = await Main.Instance.TableAPI.Get(key);
			if (entry == null)
				return new Favorites {
					Key    = key,
					Label  = null,
					Values = Array.Empty<Identifier>()
				};
			var result = JsonConvert.DeserializeObject<Favorites>(entry.AsString);
			result.Key = entry.Key;
			return result;
		}

		/// <summary>
		/// Search every favorite group of avatars, public and private, and stop at the first group containing the given identifier.
		/// </summary>
		/// <param name="identifier">Identifier of the avatar to look for.</param>
		/// <returns>The key of the first group containing the avatar, or <c>null</c> when it is not a favorite.</returns>
		public async UniTask<string> FindFavoriteGroup(Identifier identifier) {
			var page = await Main.Instance.TableAPI.List(0u, 100u, filter: "*favorites.avatars*");

			while (page?.Items != null) {
				var references = page.Items
					.Where(r => r?.Key != null && IsFavoriteAvatarKey(r.Key))
					.OrderBy(r => IsPublicKey(r.Key) ? 0 : 1)
					.ThenBy(r => FavoriteGroup(r.Key));

				foreach (var reference in references) {
					var favorites = await FetchFavoritesByKey(reference.Key);
					if (favorites?.Values?.Any(v => v.Equals(identifier)) == true)
						return reference.Key;
				}

				if (!page.HasNext())
					break;
				page = await page.Next();
			}

			return null;
		}

		private static bool IsFavoriteAvatarKey(string key)
			=> key.StartsWith("favorites.avatars.", StringComparison.Ordinal)
				|| key.StartsWith("public.favorites.avatars.", StringComparison.Ordinal);

		private static bool IsPublicKey(string key)
			=> key.StartsWith("public.", StringComparison.Ordinal);

		private async UniTask<Favorites> FetchFavoritesByKey(string key) {
			var entry = await Main.Instance.TableAPI.Get(key);
			if (entry == null)
				return null;
			var result = JsonConvert.DeserializeObject<Favorites>(entry.AsString);
			result.Key = entry.Key;
			return result;
		}

		private static int FavoriteGroup(string key) {
			var separator = key.LastIndexOf('.');
			return separator >= 0 && int.TryParse(key.Substring(separator + 1), out var group)
				? group
				: int.MaxValue;
		}

		public async UniTask<Favorites> AddFavorite(Identifier identifier, uint group = 0, bool pub = true)
			=> await AddFavorites(new[] { identifier }, group, pub);

		public async UniTask<Favorites> AddFavorites(Identifier[] identifier, uint group = 0, bool pub = true) {
			var e = await FetchFavorites(group, pub);
			e.Values = identifier
				.Concat(e.Values)
				.Distinct()
				.ToArray();

			var entry = await Main.Instance.TableAPI.Set(
				e.Key,
				JsonConvert.SerializeObject(e),
				"application/json+favorite"
			);

			if (entry == null)
				Logger.LogError("Failed to add favorites: entry not found.");
			return e;
		}

		public async UniTask<Favorites> RemoveFavorite(Identifier identifier, uint group = 0, bool pub = true)
			=> await RemoveFavorites(new[] { identifier }, group, pub);

		public async UniTask<Favorites> RemoveFavorites(Identifier[] identifier, uint group = 0, bool pub = true) {
			var e = await FetchFavorites(group, pub);
			e.Values = e.Values
				.Where(i => !identifier.Contains(i))
				.ToArray();

			var entry = await Main.Instance.TableAPI.Set(
				e.Key,
				JsonConvert.SerializeObject(e),
				"application/json+favorite"
			);

			if (entry == null)
				Logger.LogError("Failed to remove favorites: entry not found.");
			return e;
		}
	}
}
