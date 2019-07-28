namespace RimBattle
{
	public enum MPPlayerListAction : byte
	{
		List,
		Add,
		Remove,
		Latencies,
		Status
	}

	public enum MPEventType
	{
		Connect,
		Disconnect,
		ListChanged
	}

	public enum MPPlayerStatus : byte
	{
		Simulating,
		Playing,
		Desynced
	}

	public enum MPPlayerType : byte
	{
		Normal,
		Steam,
		Arbiter
	}
}