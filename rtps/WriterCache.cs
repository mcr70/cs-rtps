using System.Collections.Generic;

namespace net.sf.jrtps.rtps
{


	/// <summary>
	/// WriterCache represents writers history cache from the RTPSWriter point of
	/// view. RTPSWriter uses WriterCache to construct Data and HeartBeat messages to
	/// be sent to RTPSReaders.
	/// 
	/// @author mcr70
	/// 
	/// </summary>
	public interface WriterCache<T>
	{
		/// <summary>
		/// Gets the smallest sequence number available in history cache.
		/// </summary>
		/// <returns> seqNumMin </returns>
		long SeqNumMin {get;}

		/// <summary>
		/// Gets the greatest sequence number available in history cache.
		/// </summary>
		/// <returns> seqNumMax </returns>
		long SeqNumMax {get;}

		/// <summary>
		/// Gets all the CacheChanges since given sequence number.
		/// Returned CacheChanges are ordered by sequence numbers.
		/// </summary>
		/// <param name="seqNum"> sequence number to compare </param>
		/// <returns> changes since given seqNum. Returned List is newly allocated. </returns>
		LinkedList<Sample<T>> getSamplesSince(long seqNum);
	}

}