namespace net.sf.jrtps.rtps
{
	using PublicationData = net.sf.jrtps.builtin.PublicationData;

	/// <summary>
	/// A Listener for writer liveliness protocol.
	/// @author mcr70
	/// </summary>
	public interface WriterLivelinessListener
	{
		/// <summary>
		/// This method is called when writer liveliness is lost. </summary>
		/// <param name="pd"> PublicationData of the writer whose liveliness is lost </param>
		void livelinessLost(PublicationData pd);

		/// <summary>
		/// This method gets called when liveliness is first lost, and then
		/// restored again. </summary>
		/// <param name="pd"> PublicationData of the writer whose liveliness is restored </param>
		void livelinessRestored(PublicationData pd);
	}

}