namespace net.sf.jrtps.rtps
{
	using Data = net.sf.jrtps.message.Data;
	using Guid = net.sf.jrtps.types.Guid;
	using Time = net.sf.jrtps.types.Time;


	/// <summary>
	/// ReaderCache represents history cache from the RTPSReader point of view.
	/// RTPSReader uses ReaderCache to pass samples coming from network to DDS layer. 
	/// 
	/// @author mcr70
	/// </summary>
	/// @param <T> Type of the ReaderCache  </param>
	public interface ReaderCache<T>
	{
		/// <summary>
		/// Notifies implementing class that a new set of changes is coming from RTPS layer.
		/// </summary>
		/// <param name="id"> Id of the message that caused this invocation </param>
		void changesBegin(int id);

		/// <summary>
		/// Adds a new change to ReaderCache. It is the responsibility of the implementing class
		/// to decide whether or not this Sample is actually made available to applications or not.
		/// </summary>
		/// <param name="id"> id </param>
		/// <param name="writerGuid"> Guid of the writer </param>
		/// <param name="data"> Data </param>
		/// <param name="timestamp"> Time </param>
		void addChange(int id, Guid writerGuid, Data data, Time timestamp);

		/// <summary>
		/// Notifies implementing class that there are no more samples coming from the RTPS layer.
		/// I.e. the whole RTPS message has been processed.
		/// </summary>
		/// <param name="id"> Id of the message that caused this invocation </param>
		void changesEnd(int id);
	}

}