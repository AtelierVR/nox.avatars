#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using UnityEngine.Events;
using UnityEngine.UIElements;

namespace Nox.Avatars.Editor {
	public static class AvatarNotificationHelper {
		public static readonly UnityEvent<AvatarNotification[]> OnNotificationsChanged = new();
		public static readonly List<AvatarNotification>         Notifications          = new();

		public static bool Allowed
			=> !Notifications.Exists(n => n.Type == NotificationType.Error);

		public static void Add(AvatarNotification notification) {
			Notifications.Add(notification);
			OnNotificationsChanged?.Invoke(Notifications.ToArray());
		}

		public static void Remove(AvatarNotification notification) {
			Notifications.Remove(notification);
			OnNotificationsChanged?.Invoke(Notifications.ToArray());
		}

		public static void Remove(string uid) {
			foreach (var notification in GetMany(uid))
				Notifications.Remove(notification);
			OnNotificationsChanged?.Invoke(Notifications.ToArray());
		}

		public static AvatarNotification Get(string uid)
			=> Notifications.Find(n => n.Uid == uid);

		public static List<AvatarNotification> GetMany(string uid)
			=> uid.StartsWith("*")
				? Notifications.FindAll(n => n.Uid.EndsWith(uid[1..]))
				: uid.EndsWith("*")
					? Notifications.FindAll(n => n.Uid.StartsWith(uid[..^1]))
					: Notifications.FindAll(n => n.Uid == uid);

		public static bool Has(string uid)
			=> GetMany(uid).Count > 0;

		public static void Clear() {
			Notifications.Clear();
			OnNotificationsChanged?.Invoke(Notifications.ToArray());
		}

		public static void Set(AvatarNotification notification) {
			Notifications.Remove(notification);
			Notifications.Add(notification);
			OnNotificationsChanged?.Invoke(Notifications.ToArray());
		}
	}

	public class AvatarNotification {
		public string           Uid;
		public NotificationType Type;
		public string[]         Content;
		public AvatarAction[]   Actions;

		public AvatarNotification(string uid, NotificationType type, string[] content, AvatarAction[] actions = null) {
			Uid     = uid;
			Type    = type;
			Content = content;
			Actions = actions ?? Array.Empty<AvatarAction>();
		}

		public override string ToString()
			=> $"{GetType()}[Uid={Uid}, Type={Type}, Content={Content}]";
	}

	public class AvatarAction {
		public readonly string[]   Content;
		public readonly UnityEvent Action = new();

		public AvatarAction(string[] content, UnityAction callback = null) {
			Content = content;
			if (callback != null)
				Action.AddListener(callback);
		}
	}

	public enum NotificationType {
		Success,
		Warning,
		Error,
		Info
	}
}
#endif