namespace Updater.HashZip.ZIPLib
{
	internal sealed class InflateManager
	{
		private const int PRESET_DICT = 32;

		private const int Z_DEFLATED = 8;

		private const int METHOD = 0;

		private const int FLAG = 1;

		private const int DICT4 = 2;

		private const int DICT3 = 3;

		private const int DICT2 = 4;

		private const int DICT1 = 5;

		private const int DICT0 = 6;

		private const int BLOCKS = 7;

		private const int CHECK4 = 8;

		private const int CHECK3 = 9;

		private const int CHECK2 = 10;

		private const int CHECK1 = 11;

		private const int DONE = 12;

		private const int BAD = 13;

		internal int mode;

		internal ZlibCodec _codec;

		internal int method;

		internal long[] was = new long[1];

		internal long need;

		internal int marker;

		private bool _handleRfc1950HeaderBytes = true;

		internal int wbits;

		internal InflateBlocks blocks;

		private static byte[] mark = new byte[4]
		{
			0,
			0,
			255,
			255
		};

		internal bool HandleRfc1950HeaderBytes
		{
			get
			{
				return _handleRfc1950HeaderBytes;
			}
			set
			{
				_handleRfc1950HeaderBytes = value;
			}
		}

		public InflateManager()
		{
		}

		public InflateManager(bool expectRfc1950HeaderBytes)
		{
			_handleRfc1950HeaderBytes = expectRfc1950HeaderBytes;
		}

		internal int Reset()
		{
			_codec.TotalBytesIn = (_codec.TotalBytesOut = 0L);
			_codec.Message = null;
			mode = ((!HandleRfc1950HeaderBytes) ? 7 : 0);
			blocks.Reset(null);
			return 0;
		}

		internal int End()
		{
			if (blocks != null)
			{
				blocks.Free();
			}
			blocks = null;
			return 0;
		}

		internal int Initialize(ZlibCodec codec, int w)
		{
			_codec = codec;
			_codec.Message = null;
			blocks = null;
			if (w < 8 || w > 15)
			{
				End();
				throw new ZlibException("Bad window size.");
			}
			wbits = w;
			blocks = new InflateBlocks(codec, HandleRfc1950HeaderBytes ? this : null, 1 << w);
			Reset();
			return 0;
		}

		internal int Inflate(FlushType flush)
		{
			int num = (int)flush;
			if (_codec.InputBuffer == null)
			{
				throw new ZlibException("InputBuffer is null. ");
			}
			num = ((num == 4) ? (-5) : 0);
			int num2 = -5;
			while (true)
			{
				switch (mode)
				{
				case 0:
					if (_codec.AvailableBytesIn == 0)
					{
						return num2;
					}
					num2 = num;
					_codec.AvailableBytesIn--;
					_codec.TotalBytesIn++;
					if (((method = _codec.InputBuffer[_codec.NextIn++]) & 0xF) != 8)
					{
						mode = 13;
						_codec.Message = $"unknown compression method (0x{method:X2})";
						marker = 5;
						break;
					}
					if ((method >> 4) + 8 > wbits)
					{
						mode = 13;
						_codec.Message = $"invalid window size ({(method >> 4) + 8})";
						marker = 5;
						break;
					}
					mode = 1;
					goto case 1;
				case 1:
				{
					if (_codec.AvailableBytesIn == 0)
					{
						return num2;
					}
					num2 = num;
					_codec.AvailableBytesIn--;
					_codec.TotalBytesIn++;
					int num3 = _codec.InputBuffer[_codec.NextIn++] & 0xFF;
					if (((method << 8) + num3) % 31 != 0)
					{
						mode = 13;
						_codec.Message = "incorrect header check";
						marker = 5;
						break;
					}
					if ((num3 & 0x20) == 0)
					{
						mode = 7;
						break;
					}
					mode = 2;
					goto case 2;
				}
				case 2:
					if (_codec.AvailableBytesIn == 0)
					{
						return num2;
					}
					num2 = num;
					_codec.AvailableBytesIn--;
					_codec.TotalBytesIn++;
					need = (((_codec.InputBuffer[_codec.NextIn++] & 0xFF) << 24) & -16777216);
					mode = 3;
					goto case 3;
				case 3:
					if (_codec.AvailableBytesIn == 0)
					{
						return num2;
					}
					num2 = num;
					_codec.AvailableBytesIn--;
					_codec.TotalBytesIn++;
					need += ((long)((_codec.InputBuffer[_codec.NextIn++] & 0xFF) << 16) & 0xFF0000L);
					mode = 4;
					goto case 4;
				case 4:
					if (_codec.AvailableBytesIn == 0)
					{
						return num2;
					}
					num2 = num;
					_codec.AvailableBytesIn--;
					_codec.TotalBytesIn++;
					need += ((long)((_codec.InputBuffer[_codec.NextIn++] & 0xFF) << 8) & 0xFF00L);
					mode = 5;
					goto case 5;
				case 5:
					if (_codec.AvailableBytesIn == 0)
					{
						return num2;
					}
					num2 = num;
					_codec.AvailableBytesIn--;
					_codec.TotalBytesIn++;
					need += ((long)_codec.InputBuffer[_codec.NextIn++] & 0xFFL);
					_codec._Adler32 = need;
					mode = 6;
					return 2;
				case 6:
					mode = 13;
					_codec.Message = "need dictionary";
					marker = 0;
					return -2;
				case 7:
					num2 = blocks.Process(num2);
					switch (num2)
					{
					case -3:
						mode = 13;
						marker = 0;
						continue;
					case 0:
						num2 = num;
						break;
					}
					if (num2 != 1)
					{
						return num2;
					}
					num2 = num;
					blocks.Reset(was);
					if (!HandleRfc1950HeaderBytes)
					{
						mode = 12;
						break;
					}
					mode = 8;
					goto case 8;
				case 8:
					if (_codec.AvailableBytesIn == 0)
					{
						return num2;
					}
					num2 = num;
					_codec.AvailableBytesIn--;
					_codec.TotalBytesIn++;
					need = (((_codec.InputBuffer[_codec.NextIn++] & 0xFF) << 24) & -16777216);
					mode = 9;
					goto case 9;
				case 9:
					if (_codec.AvailableBytesIn == 0)
					{
						return num2;
					}
					num2 = num;
					_codec.AvailableBytesIn--;
					_codec.TotalBytesIn++;
					need += ((long)((_codec.InputBuffer[_codec.NextIn++] & 0xFF) << 16) & 0xFF0000L);
					mode = 10;
					goto case 10;
				case 10:
					if (_codec.AvailableBytesIn == 0)
					{
						return num2;
					}
					num2 = num;
					_codec.AvailableBytesIn--;
					_codec.TotalBytesIn++;
					need += ((long)((_codec.InputBuffer[_codec.NextIn++] & 0xFF) << 8) & 0xFF00L);
					mode = 11;
					goto case 11;
				case 11:
					if (_codec.AvailableBytesIn == 0)
					{
						return num2;
					}
					num2 = num;
					_codec.AvailableBytesIn--;
					_codec.TotalBytesIn++;
					need += ((long)_codec.InputBuffer[_codec.NextIn++] & 0xFFL);
					if ((int)was[0] != (int)need)
					{
						mode = 13;
						_codec.Message = "incorrect data check";
						marker = 5;
						break;
					}
					mode = 12;
					goto case 12;
				case 12:
					return 1;
				case 13:
					throw new ZlibException($"Bad state ({_codec.Message})");
				default:
					throw new ZlibException("Stream error.");
				}
			}
		}

		internal int SetDictionary(byte[] dictionary)
		{
			int start = 0;
			int num = dictionary.Length;
			if (mode != 6)
			{
				throw new ZlibException("Stream error.");
			}
			if (Adler.Adler32(1L, dictionary, 0, dictionary.Length) != _codec._Adler32)
			{
				return -3;
			}
			_codec._Adler32 = Adler.Adler32(0L, null, 0, 0);
			if (num >= 1 << wbits)
			{
				num = (1 << wbits) - 1;
				start = dictionary.Length - num;
			}
			blocks.SetDictionary(dictionary, start, num);
			mode = 7;
			return 0;
		}

		internal int Sync()
		{
			if (mode != 13)
			{
				mode = 13;
				marker = 0;
			}
			int num;
			if ((num = _codec.AvailableBytesIn) == 0)
			{
				return -5;
			}
			int num2 = _codec.NextIn;
			int num3 = marker;
			while (num != 0 && num3 < 4)
			{
				num3 = ((_codec.InputBuffer[num2] != mark[num3]) ? ((_codec.InputBuffer[num2] == 0) ? (4 - num3) : 0) : (num3 + 1));
				num2++;
				num--;
			}
			_codec.TotalBytesIn += num2 - _codec.NextIn;
			_codec.NextIn = num2;
			_codec.AvailableBytesIn = num;
			marker = num3;
			if (num3 != 4)
			{
				return -3;
			}
			long totalBytesIn = _codec.TotalBytesIn;
			long totalBytesOut = _codec.TotalBytesOut;
			Reset();
			_codec.TotalBytesIn = totalBytesIn;
			_codec.TotalBytesOut = totalBytesOut;
			mode = 7;
			return 0;
		}

		internal int SyncPoint(ZlibCodec z)
		{
			return blocks.SyncPoint();
		}
	}
}
