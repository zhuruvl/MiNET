// friendly name

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using MiNET.Utils;

namespace MiNET.Net
{
	public class ConnectedPackage : Package<ConnectedPackage>
	{
		private DatagramHeader _datagramHeader;
		public Int24 _datagramSequenceNumber; // uint 24

		public Reliability _reliability;
		public Int24 _reliableMessageNumber;

		public Int24 _sequencingIndex;
		public Int24 _orderingIndex;
		public byte _orderingChannel;

		public bool _hasSplit;
		public int _splitPacketCount;
		public short _splitPacketId;
		public int _splitPacketIndex;


		public int MessageLength { get; set; }
		public List<Package> Messages { get; set; }
		public byte[] Buffer { get; set; }

		public ConnectedPackage()
		{
			Messages = new List<Package>();
		}

		protected override void EncodePackage()
		{
			_buffer.Position = 0;

			byte[] encodedMessage = Buffer;
			if (Buffer == null)
			{
				encodedMessage = Messages.First().Encode();
			}

			MessageLength = encodedMessage.Length;

			// Datagram header

			if (_datagramHeader == null) _datagramHeader = new DatagramHeader(0x8c);
			if (_splitPacketCount > 1 && _splitPacketIndex > 0)
			{
				Write((byte) 0x8c);
			}
			else
			{
				Write((byte) 0x84);
			}

			Write(_datagramSequenceNumber);

			// foreach message

			// Message header

			byte rely = (byte) _reliability;
			Write((byte) ((rely << 5) | (_hasSplit ? Convert.ToByte("00010000", 2) : 0x00)));
			Write((short) (MessageLength*8)); // length

			if (_reliability == Reliability.Reliable
			    || _reliability == Reliability.ReliableOrdered
			    || _reliability == Reliability.ReliableSequenced
			    || _reliability == Reliability.ReliableWithAckReceipt
			    || _reliability == Reliability.ReliableOrderedWithAckReceipt
				)
			{
				Write(_reliableMessageNumber);
			}

			if (_reliability == Reliability.UnreliableSequenced
			    || _reliability == Reliability.ReliableOrdered
			    || _reliability == Reliability.ReliableSequenced
			    || _reliability == Reliability.ReliableOrderedWithAckReceipt
				)
			{
				Write(_orderingIndex);
				Write(_orderingChannel);
			}

			if (_hasSplit)
			{
				Write(_splitPacketCount);
				Write(_splitPacketId);
				Write(_splitPacketIndex);
			}

			// Message body

			Write(encodedMessage);
		}

		protected override void DecodePackage()
		{
			Messages = new List<Package>();

			_buffer.Position = 0;

			_datagramHeader = new DatagramHeader(ReadByte());
			_datagramSequenceNumber = ReadLittle();

			while (_buffer.Position != _buffer.Length)
			{
				byte flags = ReadByte();


				_reliability = (Reliability) ((flags & Convert.ToByte("011100000", 2)) >> 5);
				int hasSplitPacket = ((flags & Convert.ToByte("00010000", 2)) >> 0);

				short dataBitLength = ReadShort();

				if (_reliability == Reliability.Reliable
				    || _reliability == Reliability.ReliableSequenced
				    || _reliability == Reliability.ReliableOrdered
					)
				{
					_reliableMessageNumber = ReadLittle();
				}
				else
				{
					_reliableMessageNumber = -1;
				}

				if (_reliability == Reliability.UnreliableSequenced
				    || _reliability == Reliability.ReliableSequenced
					)
				{
					_sequencingIndex = ReadLittle();
				}

				if (_reliability == Reliability.UnreliableSequenced
				    || _reliability == Reliability.ReliableSequenced
				    || _reliability == Reliability.ReliableOrdered
				    || _reliability == Reliability.ReliableOrderedWithAckReceipt
					)
				{
					_orderingIndex = ReadLittle();
					_orderingChannel = ReadByte(); // flags
				}
				else
				{
					_orderingChannel = 0;
				}

				if (hasSplitPacket != 0)
				{
					_splitPacketCount = ReadInt();
					_splitPacketId = ReadShort();
					_splitPacketIndex = ReadInt();
				}
				else
				{
					_splitPacketCount = 0;
				}

				// Slurp the payload
				MessageLength = (int) Math.Ceiling((((double) dataBitLength)/8));

				byte[] internalBuffer = ReadBytes(MessageLength);

				if (hasSplitPacket != 0)
				{
					Messages.Add(new SplitPartPackage(internalBuffer[0], internalBuffer));
					return;
				}

				Messages.Add(PackageFactory.CreatePackage(internalBuffer[0], internalBuffer) ?? new UnknownPackage(internalBuffer[0], internalBuffer));
				if (MessageLength != internalBuffer.Length) Debug.WriteLine("Missmatch of requested lenght, and actual read lenght");
			}
		}
	}
}