using System;
using System.Collections.Generic;

namespace net.sf.jrtps.rtps
{

	using Data = net.sf.jrtps.message.Data;
	using DataEncapsulation = net.sf.jrtps.message.DataEncapsulation;
	using CoherentSet = net.sf.jrtps.message.parameter.CoherentSet;
	using KeyHash = net.sf.jrtps.message.parameter.KeyHash;
	using ParameterId = net.sf.jrtps.message.parameter.ParameterId;
	using ParameterList = net.sf.jrtps.message.parameter.ParameterList;
	using StatusInfo = net.sf.jrtps.message.parameter.StatusInfo;
	using Guid = net.sf.jrtps.types.Guid;

	using Logger = org.slf4j.Logger;
	using LoggerFactory = org.slf4j.LoggerFactory;

	/// <summary>
	/// Represents a sample of type T.
	/// 
	/// @author mcr70
	/// </summary>
	/// @param <T> Type of Sample </param>
	public class Sample<T> : ICloneable
	{
		private static readonly Logger log = LoggerFactory.getLogger(typeof(Sample));

		private Dictionary<string, object> properties = new Dictionary<string, object>();
		private readonly Guid writerGuid;
		private readonly Marshaller<T> marshaller;
		private readonly long seqNum;
		private readonly long timestamp;
		private readonly long sourceTimeStamp;
		private readonly StatusInfo sInfo;

		private T obj; // Sample contains either T or Data, lazily convert to other when needed.
		private Data data;
		private KeyHash key;

		private DataEncapsulation marshalledData;
		private CoherentSet coherentSet;

		private Sample(Guid writerGuid, Marshaller<T> marshaller, long seqNum, long timestamp, long sourceTimeStamp, StatusInfo sInfo)
		{
			this.writerGuid = writerGuid;
			this.marshaller = marshaller;
			this.seqNum = seqNum;
			this.timestamp = timestamp;
			this.sourceTimeStamp = sourceTimeStamp;
			this.sInfo = sInfo;
		}

		/// <summary>
		/// This constructor is used to create a Sample, that has no content. It is used to pass
		/// only inline QoS parameters to remote reader. For example, indicating an end of coherent set. </summary>
		/// <param name="seqNum"> Sequence number </param>
		public Sample(long seqNum) : this(null, null, seqNum, DateTimeHelperClass.CurrentUnixTimeMillis(), DateTimeHelperClass.CurrentUnixTimeMillis(), (StatusInfo)null)
		{
		}

		/// <summary>
		/// This constructor is used when adding Sample to UDDSWriterCache. </summary>
		/// <param name="writerGuid"> Guid of the writer </param>
		/// <param name="m"> Marshaller used </param>
		/// <param name="seqNum"> Sequence number </param>
		/// <param name="timestamp"> Timestamp of this sample </param>
		/// <param name="kind"> ChangeKind </param>
		/// <param name="obj"> Object of type T </param>
		public Sample(Guid writerGuid, Marshaller<T> m, long seqNum, long timestamp, ChangeKind kind, T obj) : this(writerGuid, m, seqNum, DateTimeHelperClass.CurrentUnixTimeMillis(), timestamp, new StatusInfo(kind))
		{
			this.obj = obj;
		}

		/// <summary>
		/// This constructor is used when adding Sample to UDDSReaderCache. </summary>
		/// <param name="writerGuid"> Guid of the writer </param>
		/// <param name="m"> Marshaller used </param>
		/// <param name="seqNum"> Sequence number </param>
		/// <param name="timestamp"> Timestamp of sample </param>
		/// <param name="sourceTimestamp"> source timestamp of sample </param>
		/// <param name="data"> Data, whose payload is decoded into Object of type T </param>
		public Sample(Guid writerGuid, Marshaller<T> m, long seqNum, long timestamp, long sourceTimestamp, Data data) : this(writerGuid, m, seqNum, timestamp, sourceTimestamp, data.StatusInfo)
		{
			this.data = data;

			if (data.inlineQosFlag())
			{
				ParameterList inlineQos = data.InlineQos;
				if (inlineQos != null)
				{
					coherentSet = (CoherentSet) inlineQos.getParameter(ParameterId.PID_COHERENT_SET);
				}
			}
		}


		/// <summary>
		/// Gets the data associated with this Sample.
		/// </summary>
		/// <returns> data </returns>
		public virtual T Data
		{
			get
			{
				if (obj != default(T))
				{
					return obj;
				}
    
				if (data != null)
				{
					try
					{
						obj = marshaller.unmarshall(data.DataEncapsulation);
					}
					catch (IOException e)
					{
						log.warn("Failed to convert Data submessage to java object", e);
					}
					finally
					{
						data = null; // Try to convert only once
					}
				}
    
				return obj;
			}
		}

		/// <summary>
		/// Gets the timestamp associated with this Sample.
		/// Time stamp can be either local timestamp, or remote writers timestamp,
		/// based on DESTINATION_ORDER QoS policy.
		/// </summary>
		/// <returns> timestamp in milliseconds. </returns>
		public virtual long Timestamp
		{
			get
			{
				return timestamp;
			}
		}

		/// <summary>
		/// Gets the sourceTimestamp associated with this Sample. Returns the timestamp
		/// set by remote writer. If remote writer did not provide timestamp, it has been
		/// set to reception time.
		/// </summary>
		/// <returns> source timestamp in milliseconds </returns>
		public virtual long SourceTimeStamp
		{
			get
			{
				return sourceTimeStamp;
			}
		}

		/// <summary>
		/// Gets the value of disposeFlag of StatusInfo parameter. StatusInfo
		/// parameter is part of Data submessage.
		/// </summary>
		/// <seealso cref= StatusInfo </seealso>
		/// <returns> true, if disposeFlag is set </returns>
		public virtual bool Disposed
		{
			get
			{
				return sInfo.Disposed;
			}
		}

		/// <summary>
		/// Gets the value of unregisterFlag of StatusInfo parameter. StatusInfo
		/// parameter is part of Data submessage.
		/// </summary>
		/// <seealso cref= StatusInfo </seealso>
		/// <returns> true, if unregisterFlag is set </returns>
		public virtual bool Unregistered
		{
			get
			{
				return sInfo.Unregistered;
			}
		}

		/// <summary>
		/// Gets the Guid of the writer that wrote this Sample originally. </summary>
		/// <returns> Guid of the writer </returns>
		public virtual Guid WriterGuid
		{
			get
			{
				return writerGuid;
			}
		}



		/// <summary>
		/// Gets the sequence number of this Sample.
		/// </summary>
		/// <returns> sequence number </returns>
		public virtual long SequenceNumber
		{
			get
			{
				return seqNum;
			}
		}

		/// <summary>
		/// Gets the key of this Sample. Key of the Sample is used to distinguish between
		/// instances, when transmitting Samples over the wire.
		/// </summary>
		/// <returns> Key, or null if this Sample does not have a key. </returns>
		public virtual KeyHash Key
		{
			get
			{
				if (key == null && marshaller != null && marshaller.hasKey())
				{
					T aData = Data;
					key = new KeyHash(marshaller.extractKey(aData));
				}
    
				return key;
			}
		}

		/// <summary>
		/// Get the ChangeKind of this Sample. </summary>
		/// <returns> ChangeKind May be null, if this Sample does not represent a change to an instance. </returns>
		public virtual ChangeKind Kind
		{
			get
			{
				if (sInfo != null)
				{
					return sInfo.Kind;
				}
    
				return null;
			}
		}


		/// <summary>
		/// Gets the DataEncapsulation. </summary>
		/// <returns> DataEncapsulation </returns>
		/// <exception cref="IOException"> </exception>
//JAVA TO C# CONVERTER WARNING: Method 'throws' clauses are not available in .NET:
//ORIGINAL LINE: net.sf.jrtps.message.DataEncapsulation getDataEncapsulation() throws java.io.IOException
		internal virtual DataEncapsulation DataEncapsulation
		{
			get
			{
				if (marshalledData == null && marshaller != null)
				{
					marshalledData = marshaller.marshall(Data);
				}
    
				return marshalledData;
			}
		}

		/// <summary>
		/// Checks whether or not this Sample is associated with a Key. </summary>
		/// <returns> true or false </returns>
		internal virtual bool hasKey()
		{
			if (marshaller != null)
			{
				return marshaller.hasKey();
			}

			return false;
		}

		/// <summary>
		/// Return CoherentSet attribute of this Sample, if it exists. </summary>
		/// <returns> CoherentSet, or null if one has not been set </returns>
		public virtual CoherentSet CoherentSet
		{
			get
			{
				return coherentSet;
			}
			set
			{
				coherentSet = value;
			}
		}


		public override string ToString()
		{
			return "Sample[" + seqNum + "]:" + sInfo;
		}

		public virtual object getProperty(string key)
		{
			return properties[key];
		}
		public virtual void setProperty(string key, object value)
		{
			properties[key] = value;
		}

//JAVA TO C# CONVERTER WARNING: Method 'throws' clauses are not available in .NET:
//ORIGINAL LINE: @Override public Object clone() throws CloneNotSupportedException
		public override object clone()
		{
//JAVA TO C# CONVERTER WARNING: Java wildcard generics have no direct equivalent in .NET:
//ORIGINAL LINE: Sample<?> s = (Sample<?>) super.clone();
			Sample<object> s = (Sample<object>) base.clone();
			s.properties = new Dictionary<>(this.properties);

			return s;
		}
	}

}