using System;

namespace Updater.HashZip.ZIPLib
{
	internal sealed class InflateBlocks
	{
		private const int MANY = 1440;

		private static readonly int[] inflate_mask = new int[17]
		{
			0,
			1,
			3,
			7,
			15,
			31,
			63,
			127,
			255,
			511,
			1023,
			2047,
			4095,
			8191,
			16383,
			32767,
			65535
		};

		internal static readonly int[] border = new int[19]
		{
			16,
			17,
			18,
			0,
			8,
			7,
			9,
			6,
			10,
			5,
			11,
			4,
			12,
			3,
			13,
			2,
			14,
			1,
			15
		};

		private const int TYPE = 0;

		private const int LENS = 1;

		private const int STORED = 2;

		private const int TABLE = 3;

		private const int BTREE = 4;

		private const int DTREE = 5;

		private const int CODES = 6;

		private const int DRY = 7;

		private const int DONE = 8;

		private const int BAD = 9;

		internal int mode;

		internal int left;

		internal int table;

		internal int index;

		internal int[] blens;

		internal int[] bb = new int[1];

		internal int[] tb = new int[1];

		internal InflateCodes codes = new InflateCodes();

		internal int last;

		internal ZlibCodec _codec;

		internal int bitk;

		internal int bitb;

		internal int[] hufts;

		internal byte[] window;

		internal int end;

		internal int read;

		internal int write;

		internal object checkfn;

		internal long check;

		internal InfTree inftree = new InfTree();

		internal InflateBlocks(ZlibCodec codec, object checkfn, int w)
		{
			_codec = codec;
			hufts = new int[4320];
			window = new byte[w];
			end = w;
			this.checkfn = checkfn;
			mode = 0;
			Reset(null);
		}

		internal void Reset(long[] c)
		{
			if (c != null)
			{
				c[0] = check;
			}
			if (mode == 4 || mode == 5)
			{
			}
			if (mode == 6)
			{
			}
			mode = 0;
			bitk = 0;
			bitb = 0;
			read = (write = 0);
			if (checkfn != null)
			{
				_codec._Adler32 = (check = Adler.Adler32(0L, null, 0, 0));
			}
		}

		internal int Process(int r)
		{
			int num = _codec.NextIn;
			int num2 = _codec.AvailableBytesIn;
			int num3 = bitb;
			int i = bitk;
			int num4 = write;
			int num5 = (num4 < read) ? (read - num4 - 1) : (end - num4);
			while (true)
			{
				switch (mode)
				{
				case 0:
				{
					for (; i < 3; i += 8)
					{
						if (num2 != 0)
						{
							r = 0;
							num2--;
							num3 |= (_codec.InputBuffer[num++] & 0xFF) << i;
							continue;
						}
						bitb = num3;
						bitk = i;
						_codec.AvailableBytesIn = num2;
						_codec.TotalBytesIn += num - _codec.NextIn;
						_codec.NextIn = num;
						write = num4;
						return Flush(r);
					}
					int num6 = num3 & 7;
					last = (num6 & 1);
					switch (SharedUtils.URShift(num6, 1))
					{
					case 0:
						num3 = SharedUtils.URShift(num3, 3);
						i -= 3;
						num6 = (i & 7);
						num3 = SharedUtils.URShift(num3, num6);
						i -= num6;
						mode = 1;
						break;
					case 1:
					{
						int[] array = new int[1];
						int[] array2 = new int[1];
						int[][] array3 = new int[1][];
						int[][] array4 = new int[1][];
						InfTree.inflate_trees_fixed(array, array2, array3, array4, _codec);
						codes.Init(array[0], array2[0], array3[0], 0, array4[0], 0);
						num3 = SharedUtils.URShift(num3, 3);
						i -= 3;
						mode = 6;
						break;
					}
					case 2:
						num3 = SharedUtils.URShift(num3, 3);
						i -= 3;
						mode = 3;
						break;
					case 3:
						num3 = SharedUtils.URShift(num3, 3);
						i -= 3;
						mode = 9;
						_codec.Message = "invalid block type";
						r = -3;
						bitb = num3;
						bitk = i;
						_codec.AvailableBytesIn = num2;
						_codec.TotalBytesIn += num - _codec.NextIn;
						_codec.NextIn = num;
						write = num4;
						return Flush(r);
					}
					break;
				}
				case 1:
					for (; i < 32; i += 8)
					{
						if (num2 != 0)
						{
							r = 0;
							num2--;
							num3 |= (_codec.InputBuffer[num++] & 0xFF) << i;
							continue;
						}
						bitb = num3;
						bitk = i;
						_codec.AvailableBytesIn = num2;
						_codec.TotalBytesIn += num - _codec.NextIn;
						_codec.NextIn = num;
						write = num4;
						return Flush(r);
					}
					if ((SharedUtils.URShift(~num3, 16) & 0xFFFF) != (num3 & 0xFFFF))
					{
						mode = 9;
						_codec.Message = "invalid stored block lengths";
						r = -3;
						bitb = num3;
						bitk = i;
						_codec.AvailableBytesIn = num2;
						_codec.TotalBytesIn += num - _codec.NextIn;
						_codec.NextIn = num;
						write = num4;
						return Flush(r);
					}
					left = (num3 & 0xFFFF);
					num3 = (i = 0);
					mode = ((left != 0) ? 2 : ((last != 0) ? 7 : 0));
					break;
				case 2:
				{
					if (num2 == 0)
					{
						bitb = num3;
						bitk = i;
						_codec.AvailableBytesIn = num2;
						_codec.TotalBytesIn += num - _codec.NextIn;
						_codec.NextIn = num;
						write = num4;
						return Flush(r);
					}
					if (num5 == 0)
					{
						if (num4 == end && read != 0)
						{
							num4 = 0;
							num5 = ((num4 < read) ? (read - num4 - 1) : (end - num4));
						}
						if (num5 == 0)
						{
							write = num4;
							r = Flush(r);
							num4 = write;
							num5 = ((num4 < read) ? (read - num4 - 1) : (end - num4));
							if (num4 == end && read != 0)
							{
								num4 = 0;
								num5 = ((num4 < read) ? (read - num4 - 1) : (end - num4));
							}
							if (num5 == 0)
							{
								bitb = num3;
								bitk = i;
								_codec.AvailableBytesIn = num2;
								_codec.TotalBytesIn += num - _codec.NextIn;
								_codec.NextIn = num;
								write = num4;
								return Flush(r);
							}
						}
					}
					r = 0;
					int num6 = left;
					if (num6 > num2)
					{
						num6 = num2;
					}
					if (num6 > num5)
					{
						num6 = num5;
					}
					Array.Copy(_codec.InputBuffer, num, window, num4, num6);
					num += num6;
					num2 -= num6;
					num4 += num6;
					num5 -= num6;
					if ((left -= num6) == 0)
					{
						mode = ((last != 0) ? 7 : 0);
					}
					break;
				}
				case 3:
				{
					for (; i < 14; i += 8)
					{
						if (num2 != 0)
						{
							r = 0;
							num2--;
							num3 |= (_codec.InputBuffer[num++] & 0xFF) << i;
							continue;
						}
						bitb = num3;
						bitk = i;
						_codec.AvailableBytesIn = num2;
						_codec.TotalBytesIn += num - _codec.NextIn;
						_codec.NextIn = num;
						write = num4;
						return Flush(r);
					}
					int num6 = table = (num3 & 0x3FFF);
					if ((num6 & 0x1F) > 29 || ((num6 >> 5) & 0x1F) > 29)
					{
						mode = 9;
						_codec.Message = "too many length or distance symbols";
						r = -3;
						bitb = num3;
						bitk = i;
						_codec.AvailableBytesIn = num2;
						_codec.TotalBytesIn += num - _codec.NextIn;
						_codec.NextIn = num;
						write = num4;
						return Flush(r);
					}
					num6 = 258 + (num6 & 0x1F) + ((num6 >> 5) & 0x1F);
					if (blens == null || blens.Length < num6)
					{
						blens = new int[num6];
					}
					else
					{
						for (int j = 0; j < num6; j++)
						{
							blens[j] = 0;
						}
					}
					num3 = SharedUtils.URShift(num3, 14);
					i -= 14;
					index = 0;
					mode = 4;
					goto case 4;
				}
				case 4:
				{
					while (index < 4 + SharedUtils.URShift(table, 10))
					{
						for (; i < 3; i += 8)
						{
							if (num2 != 0)
							{
								r = 0;
								num2--;
								num3 |= (_codec.InputBuffer[num++] & 0xFF) << i;
								continue;
							}
							bitb = num3;
							bitk = i;
							_codec.AvailableBytesIn = num2;
							_codec.TotalBytesIn += num - _codec.NextIn;
							_codec.NextIn = num;
							write = num4;
							return Flush(r);
						}
						blens[border[index++]] = (num3 & 7);
						num3 = SharedUtils.URShift(num3, 3);
						i -= 3;
					}
					while (index < 19)
					{
						blens[border[index++]] = 0;
					}
					bb[0] = 7;
					int num6 = inftree.inflate_trees_bits(blens, bb, tb, hufts, _codec);
					if (num6 != 0)
					{
						r = num6;
						if (r == -3)
						{
							blens = null;
							mode = 9;
						}
						bitb = num3;
						bitk = i;
						_codec.AvailableBytesIn = num2;
						_codec.TotalBytesIn += num - _codec.NextIn;
						_codec.NextIn = num;
						write = num4;
						return Flush(r);
					}
					index = 0;
					mode = 5;
					goto case 5;
				}
				case 5:
				{
					int num6;
					while (true)
					{
						num6 = table;
						if (index >= 258 + (num6 & 0x1F) + ((num6 >> 5) & 0x1F))
						{
							break;
						}
						for (num6 = bb[0]; i < num6; i += 8)
						{
							if (num2 != 0)
							{
								r = 0;
								num2--;
								num3 |= (_codec.InputBuffer[num++] & 0xFF) << i;
								continue;
							}
							bitb = num3;
							bitk = i;
							_codec.AvailableBytesIn = num2;
							_codec.TotalBytesIn += num - _codec.NextIn;
							_codec.NextIn = num;
							write = num4;
							return Flush(r);
						}
						if (tb[0] == -1)
						{
						}
						num6 = hufts[(tb[0] + (num3 & inflate_mask[num6])) * 3 + 1];
						int num7 = hufts[(tb[0] + (num3 & inflate_mask[num6])) * 3 + 2];
						if (num7 < 16)
						{
							num3 = SharedUtils.URShift(num3, num6);
							i -= num6;
							blens[index++] = num7;
							continue;
						}
						int num8 = (num7 == 18) ? 7 : (num7 - 14);
						int num9 = (num7 == 18) ? 11 : 3;
						for (; i < num6 + num8; i += 8)
						{
							if (num2 != 0)
							{
								r = 0;
								num2--;
								num3 |= (_codec.InputBuffer[num++] & 0xFF) << i;
								continue;
							}
							bitb = num3;
							bitk = i;
							_codec.AvailableBytesIn = num2;
							_codec.TotalBytesIn += num - _codec.NextIn;
							_codec.NextIn = num;
							write = num4;
							return Flush(r);
						}
						num3 = SharedUtils.URShift(num3, num6);
						i -= num6;
						num9 += (num3 & inflate_mask[num8]);
						num3 = SharedUtils.URShift(num3, num8);
						i -= num8;
						num8 = index;
						num6 = table;
						if (num8 + num9 > 258 + (num6 & 0x1F) + ((num6 >> 5) & 0x1F) || (num7 == 16 && num8 < 1))
						{
							blens = null;
							mode = 9;
							_codec.Message = "invalid bit length repeat";
							r = -3;
							bitb = num3;
							bitk = i;
							_codec.AvailableBytesIn = num2;
							_codec.TotalBytesIn += num - _codec.NextIn;
							_codec.NextIn = num;
							write = num4;
							return Flush(r);
						}
						num7 = ((num7 == 16) ? blens[num8 - 1] : 0);
						do
						{
							blens[num8++] = num7;
						}
						while (--num9 != 0);
						index = num8;
					}
					tb[0] = -1;
					int[] array5 = new int[1]
					{
						9
					};
					int[] array6 = new int[1]
					{
						6
					};
					int[] array7 = new int[1];
					int[] array8 = new int[1];
					num6 = table;
					num6 = inftree.inflate_trees_dynamic(257 + (num6 & 0x1F), 1 + ((num6 >> 5) & 0x1F), blens, array5, array6, array7, array8, hufts, _codec);
					if (num6 != 0)
					{
						if (num6 == -3)
						{
							blens = null;
							mode = 9;
						}
						r = num6;
						bitb = num3;
						bitk = i;
						_codec.AvailableBytesIn = num2;
						_codec.TotalBytesIn += num - _codec.NextIn;
						_codec.NextIn = num;
						write = num4;
						return Flush(r);
					}
					codes.Init(array5[0], array6[0], hufts, array7[0], hufts, array8[0]);
					mode = 6;
					goto case 6;
				}
				case 6:
					bitb = num3;
					bitk = i;
					_codec.AvailableBytesIn = num2;
					_codec.TotalBytesIn += num - _codec.NextIn;
					_codec.NextIn = num;
					write = num4;
					if ((r = codes.Process(this, r)) != 1)
					{
						return Flush(r);
					}
					r = 0;
					num = _codec.NextIn;
					num2 = _codec.AvailableBytesIn;
					num3 = bitb;
					i = bitk;
					num4 = write;
					num5 = ((num4 < read) ? (read - num4 - 1) : (end - num4));
					if (last == 0)
					{
						mode = 0;
						break;
					}
					mode = 7;
					goto case 7;
				case 7:
					write = num4;
					r = Flush(r);
					num4 = write;
					num5 = ((num4 < read) ? (read - num4 - 1) : (end - num4));
					if (read != write)
					{
						bitb = num3;
						bitk = i;
						_codec.AvailableBytesIn = num2;
						_codec.TotalBytesIn += num - _codec.NextIn;
						_codec.NextIn = num;
						write = num4;
						return Flush(r);
					}
					mode = 8;
					goto case 8;
				case 8:
					r = 1;
					bitb = num3;
					bitk = i;
					_codec.AvailableBytesIn = num2;
					_codec.TotalBytesIn += num - _codec.NextIn;
					_codec.NextIn = num;
					write = num4;
					return Flush(r);
				case 9:
					r = -3;
					bitb = num3;
					bitk = i;
					_codec.AvailableBytesIn = num2;
					_codec.TotalBytesIn += num - _codec.NextIn;
					_codec.NextIn = num;
					write = num4;
					return Flush(r);
				default:
					r = -2;
					bitb = num3;
					bitk = i;
					_codec.AvailableBytesIn = num2;
					_codec.TotalBytesIn += num - _codec.NextIn;
					_codec.NextIn = num;
					write = num4;
					return Flush(r);
				}
			}
		}

		internal void Free()
		{
			Reset(null);
			window = null;
			hufts = null;
		}

		internal void SetDictionary(byte[] d, int start, int n)
		{
			Array.Copy(d, start, window, 0, n);
			read = (write = n);
		}

		internal int SyncPoint()
		{
			return (mode == 1) ? 1 : 0;
		}

		internal int Flush(int r)
		{
			int nextOut = _codec.NextOut;
			int num = read;
			int num2 = ((num <= write) ? write : end) - num;
			if (num2 > _codec.AvailableBytesOut)
			{
				num2 = _codec.AvailableBytesOut;
			}
			if (num2 != 0 && r == -5)
			{
				r = 0;
			}
			_codec.AvailableBytesOut -= num2;
			_codec.TotalBytesOut += num2;
			if (checkfn != null)
			{
				_codec._Adler32 = (check = Adler.Adler32(check, window, num, num2));
			}
			Array.Copy(window, num, _codec.OutputBuffer, nextOut, num2);
			nextOut += num2;
			num += num2;
			if (num == end)
			{
				num = 0;
				if (write == end)
				{
					write = 0;
				}
				num2 = write - num;
				if (num2 > _codec.AvailableBytesOut)
				{
					num2 = _codec.AvailableBytesOut;
				}
				if (num2 != 0 && r == -5)
				{
					r = 0;
				}
				_codec.AvailableBytesOut -= num2;
				_codec.TotalBytesOut += num2;
				if (checkfn != null)
				{
					_codec._Adler32 = (check = Adler.Adler32(check, window, num, num2));
				}
				Array.Copy(window, num, _codec.OutputBuffer, nextOut, num2);
				nextOut += num2;
				num += num2;
			}
			_codec.NextOut = nextOut;
			read = num;
			return r;
		}
	}
}
