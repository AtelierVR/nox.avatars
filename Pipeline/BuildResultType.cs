namespace Nox.Avatars.Pipeline {
	[System.Flags]
	public enum BuildResultType {
		Success,
		AlreadyBuilding,
		EditorCompiling,
		EditorPlaying,
		UnsupportedTarget,
		InvalidTarget,
		InvalidGameObject,
		Failed = AlreadyBuilding | EditorCompiling | EditorPlaying,
	}
}