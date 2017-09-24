namespace net.sf.jrtps.rtps
{
	/// <summary>
	/// Enumeration for different kind of changes made to an instance.
	/// 
	/// @author mcr70
	/// </summary>
	public enum ChangeKind
	{
		/// <summary>
		/// Writer updates an instance.
		/// </summary>
		WRITE,
		/// <summary>
		/// Writer disposes an instance.
		/// </summary>
		DISPOSE,
		/// <summary>
		/// Writer unregisters an instance.
		/// </summary>
		UNREGISTER
	}
}