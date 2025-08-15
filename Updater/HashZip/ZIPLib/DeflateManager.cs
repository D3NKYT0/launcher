using System;

namespace Updater.HashZip.ZIPLib
{
	internal sealed class DeflateManager
	{
		internal delegate BlockState CompressFunc(FlushType flush);

		internal class Config
		{
			internal int GoodLength;

			internal int MaxLazy;

			internal int NiceLength;

			internal int MaxChainLength;

			internal DeflateFlavor Flavor;

			private static Config[] Table;

			private Config(int goodLength, int maxLazy, int niceLength, int maxChainLength, DeflateFlavor flavor)
			{
				GoodLength = goodLength;
				MaxLazy = maxLazy;
				NiceLength = niceLength;
				MaxChainLength = maxChainLength;
				Flavor = flavor;
			}

			public static Config Lookup(CompressionLevel level)
			{
				return Table[(int)level];
			}

			static Config()
			{
				Table = new Config[10]
				{
					new Config(0, 0, 0, 0, DeflateFlavor.Store),
					new Config(4, 4, 8, 4, DeflateFlavor.Fast),
					new Config(4, 5, 16, 8, DeflateFlavor.Fast),
					new Config(4, 6, 32, 32, DeflateFlavor.Fast),
					new Config(4, 4, 16, 16, DeflateFlavor.Slow),
					new Config(8, 16, 32, 32, DeflateFlavor.Slow),
					new Config(8, 16, 128, 128, DeflateFlavor.Slow),
					new Config(8, 32, 128, 256, DeflateFlavor.Slow),
					new Config(32, 128, 258, 1024, DeflateFlavor.Slow),
					new Config(32, 258, 258, 4096, DeflateFlavor.Slow)
				};
			}
		}

		private const int MEM_LEVEL_MAX = 9;

		private const int MEM_LEVEL_DEFAULT = 8;

		private CompressFunc DeflateFunction;

		private static readonly string[] z_errmsg = new string[10]
		{
			"need dictionary",
			"stream end",
			"",
			"file error",
			"stream error",
			"data error",
			"insufficient memory",
			"buffer error",
			"incompatible version",
			""
		};

		private const int PRESET_DICT = 32;

		private const int INIT_STATE = 42;

		private const int BUSY_STATE = 113;

		private const int FINISH_STATE = 666;

		private const int Z_DEFLATED = 8;

		private const int STORED_BLOCK = 0;

		private const int STATIC_TREES = 1;

		private const int DYN_TREES = 2;

		private const int Z_BINARY = 0;

		private const int Z_ASCII = 1;

		private const int Z_UNKNOWN = 2;

		private const int Buf_size = 16;

		private const int REP_3_6 = 16;

		private const int REPZ_3_10 = 17;

		private const int REPZ_11_138 = 18;

		private const int MIN_MATCH = 3;

		private const int MAX_MATCH = 258;

		private static readonly int MIN_LOOKAHEAD = 262;

		private const int MAX_BITS = 15;

		private const int D_CODES = 30;

		private const int BL_CODES = 19;

		private const int LENGTH_CODES = 29;

		private const int LITERALS = 256;

		private static readonly int L_CODES = 286;

		private static readonly int HEAP_SIZE = 2 * L_CODES + 1;

		private const int END_BLOCK = 256;

		internal ZlibCodec _codec;

		internal int status;

		internal byte[] pending;

		internal int nextPending;

		internal int pendingCount;

		internal sbyte data_type;

		internal sbyte method;

		internal int last_flush;

		internal int w_size;

		internal int w_bits;

		internal int w_mask;

		internal byte[] window;

		internal int window_size;

		internal short[] prev;

		internal short[] head;

		internal int ins_h;

		internal int hash_size;

		internal int hash_bits;

		internal int hash_mask;

		internal int hash_shift;

		internal int block_start;

		private Config config;

		internal int match_length;

		internal int prev_match;

		internal int match_available;

		internal int strstart;

		internal int match_start;

		internal int lookahead;

		internal int prev_length;

		internal CompressionLevel compressionLevel;

		internal CompressionStrategy compressionStrategy;

		internal short[] dyn_ltree;

		internal short[] dyn_dtree;

		internal short[] bl_tree;

		internal Tree l_desc = new Tree();

		internal Tree d_desc = new Tree();

		internal Tree bl_desc = new Tree();

		internal short[] bl_count = new short[16];

		internal int[] heap = new int[2 * L_CODES + 1];

		internal int heap_len;

		internal int heap_max;

		internal sbyte[] depth = new sbyte[2 * L_CODES + 1];

		internal int l_buf;

		internal int lit_bufsize;

		internal int last_lit;

		internal int d_buf;

		internal int opt_len;

		internal int static_len;

		internal int matches;

		internal int last_eob_len;

		internal short bi_buf;

		internal int bi_valid;

		private bool Rfc1950BytesEmitted = false;

		private bool _WantRfc1950HeaderBytes = true;

		internal bool WantRfc1950HeaderBytes
		{
			get
			{
				return _WantRfc1950HeaderBytes;
			}
			set
			{
				_WantRfc1950HeaderBytes = value;
			}
		}

		internal DeflateManager()
		{
			dyn_ltree = new short[HEAP_SIZE * 2];
			dyn_dtree = new short[122];
			bl_tree = new short[78];
		}

		private void _InitializeLazyMatch()
		{
			window_size = 2 * w_size;
			for (int i = 0; i < hash_size; i++)
			{
				head[i] = 0;
			}
			config = Config.Lookup(compressionLevel);
			switch (config.Flavor)
			{
			case DeflateFlavor.Store:
				DeflateFunction = DeflateNone;
				break;
			case DeflateFlavor.Fast:
				DeflateFunction = DeflateFast;
				break;
			case DeflateFlavor.Slow:
				DeflateFunction = DeflateSlow;
				break;
			}
			strstart = 0;
			block_start = 0;
			lookahead = 0;
			match_length = (prev_length = 2);
			match_available = 0;
			ins_h = 0;
		}

		private void _InitializeTreeData()
		{
			l_desc.dyn_tree = dyn_ltree;
			l_desc.stat_desc = StaticTree.static_l_desc;
			d_desc.dyn_tree = dyn_dtree;
			d_desc.stat_desc = StaticTree.static_d_desc;
			bl_desc.dyn_tree = bl_tree;
			bl_desc.stat_desc = StaticTree.static_bl_desc;
			bi_buf = 0;
			bi_valid = 0;
			last_eob_len = 8;
			_InitializeBlocks();
		}

		internal void _InitializeBlocks()
		{
			for (int i = 0; i < L_CODES; i++)
			{
				dyn_ltree[i * 2] = 0;
			}
			for (int j = 0; j < 30; j++)
			{
				dyn_dtree[j * 2] = 0;
			}
			for (int k = 0; k < 19; k++)
			{
				bl_tree[k * 2] = 0;
			}
			dyn_ltree[512] = 1;
			opt_len = (static_len = 0);
			last_lit = (matches = 0);
		}

		internal void pqdownheap(short[] tree, int k)
		{
			int num = heap[k];
			for (int num2 = k << 1; num2 <= heap_len; num2 <<= 1)
			{
				if (num2 < heap_len && _IsSmaller(tree, heap[num2 + 1], heap[num2], depth))
				{
					num2++;
				}
				if (_IsSmaller(tree, num, heap[num2], depth))
				{
					break;
				}
				heap[k] = heap[num2];
				k = num2;
			}
			heap[k] = num;
		}

		internal static bool _IsSmaller(short[] tree, int n, int m, sbyte[] depth)
		{
			short num = tree[n * 2];
			short num2 = tree[m * 2];
			return num < num2 || (num == num2 && depth[n] <= depth[m]);
		}

		internal void scan_tree(short[] tree, int max_code)
		{
			int num = -1;
			int num2 = tree[1];
			int num3 = 0;
			int num4 = 7;
			int num5 = 4;
			if (num2 == 0)
			{
				num4 = 138;
				num5 = 3;
			}
			tree[(max_code + 1) * 2 + 1] = short.MaxValue;
			for (int i = 0; i <= max_code; i++)
			{
				int num6 = num2;
				num2 = tree[(i + 1) * 2 + 1];
				if (++num3 < num4 && num6 == num2)
				{
					continue;
				}
				if (num3 < num5)
				{
					bl_tree[num6 * 2] = (short)(bl_tree[num6 * 2] + num3);
				}
				else if (num6 != 0)
				{
					if (num6 != num)
					{
						bl_tree[num6 * 2]++;
					}
					bl_tree[32]++;
				}
				else if (num3 <= 10)
				{
					bl_tree[34]++;
				}
				else
				{
					bl_tree[36]++;
				}
				num3 = 0;
				num = num6;
				if (num2 == 0)
				{
					num4 = 138;
					num5 = 3;
				}
				else if (num6 == num2)
				{
					num4 = 6;
					num5 = 3;
				}
				else
				{
					num4 = 7;
					num5 = 4;
				}
			}
		}

		internal int build_bl_tree()
		{
			scan_tree(dyn_ltree, l_desc.max_code);
			scan_tree(dyn_dtree, d_desc.max_code);
			bl_desc.build_tree(this);
			int num = 18;
			while (num >= 3 && bl_tree[Tree.bl_order[num] * 2 + 1] == 0)
			{
				num--;
			}
			opt_len += 3 * (num + 1) + 5 + 5 + 4;
			return num;
		}

		internal void send_all_trees(int lcodes, int dcodes, int blcodes)
		{
			send_bits(lcodes - 257, 5);
			send_bits(dcodes - 1, 5);
			send_bits(blcodes - 4, 4);
			for (int i = 0; i < blcodes; i++)
			{
				send_bits(bl_tree[Tree.bl_order[i] * 2 + 1], 3);
			}
			send_tree(dyn_ltree, lcodes - 1);
			send_tree(dyn_dtree, dcodes - 1);
		}

		internal void send_tree(short[] tree, int max_code)
		{
			int num = -1;
			int num2 = tree[1];
			int num3 = 0;
			int num4 = 7;
			int num5 = 4;
			if (num2 == 0)
			{
				num4 = 138;
				num5 = 3;
			}
			for (int i = 0; i <= max_code; i++)
			{
				int num6 = num2;
				num2 = tree[(i + 1) * 2 + 1];
				if (++num3 < num4 && num6 == num2)
				{
					continue;
				}
				if (num3 < num5)
				{
					do
					{
						send_code(num6, bl_tree);
					}
					while (--num3 != 0);
				}
				else if (num6 != 0)
				{
					if (num6 != num)
					{
						send_code(num6, bl_tree);
						num3--;
					}
					send_code(16, bl_tree);
					send_bits(num3 - 3, 2);
				}
				else if (num3 <= 10)
				{
					send_code(17, bl_tree);
					send_bits(num3 - 3, 3);
				}
				else
				{
					send_code(18, bl_tree);
					send_bits(num3 - 11, 7);
				}
				num3 = 0;
				num = num6;
				if (num2 == 0)
				{
					num4 = 138;
					num5 = 3;
				}
				else if (num6 == num2)
				{
					num4 = 6;
					num5 = 3;
				}
				else
				{
					num4 = 7;
					num5 = 4;
				}
			}
		}

		private void put_byte(byte[] p, int start, int len)
		{
			Array.Copy(p, start, pending, pendingCount, len);
			pendingCount += len;
		}

		private void put_byte(byte c)
		{
			pending[pendingCount++] = c;
		}

		internal void put_short(int w)
		{
			put_byte((byte)w);
			put_byte((byte)SharedUtils.URShift(w, 8));
		}

		internal void putShortMSB(int b)
		{
			put_byte((byte)(b >> 8));
			put_byte((byte)b);
		}

		internal void send_code(int c, short[] tree)
		{
			int num = c * 2;
			send_bits(tree[num] & 0xFFFF, tree[num + 1] & 0xFFFF);
		}

		internal void send_bits(int value_Renamed, int length)
		{
			if (bi_valid > 16 - length)
			{
				bi_buf |= (short)((value_Renamed << bi_valid) & 0xFFFF);
				put_short(bi_buf);
				bi_buf = (short)SharedUtils.URShift(value_Renamed, 16 - bi_valid);
				bi_valid += length - 16;
			}
			else
			{
				bi_buf |= (short)((value_Renamed << bi_valid) & 0xFFFF);
				bi_valid += length;
			}
		}

		internal void _tr_align()
		{
			send_bits(2, 3);
			send_code(256, StaticTree.static_ltree);
			bi_flush();
			if (1 + last_eob_len + 10 - bi_valid < 9)
			{
				send_bits(2, 3);
				send_code(256, StaticTree.static_ltree);
				bi_flush();
			}
			last_eob_len = 7;
		}

		internal bool _tr_tally(int dist, int lc)
		{
			pending[d_buf + last_lit * 2] = (byte)SharedUtils.URShift(dist, 8);
			pending[d_buf + last_lit * 2 + 1] = (byte)dist;
			pending[l_buf + last_lit] = (byte)lc;
			last_lit++;
			if (dist == 0)
			{
				dyn_ltree[lc * 2]++;
			}
			else
			{
				matches++;
				dist--;
				dyn_ltree[(Tree._length_code[lc] + 256 + 1) * 2]++;
				dyn_dtree[Tree.d_code(dist) * 2]++;
			}
			if ((last_lit & 0x1FFF) == 0 && compressionLevel > CompressionLevel.Level2)
			{
				int num = last_lit * 8;
				int num2 = strstart - block_start;
				for (int i = 0; i < 30; i++)
				{
					num = (int)(num + dyn_dtree[i * 2] * (5L + (long)Tree.extra_dbits[i]));
				}
				num = SharedUtils.URShift(num, 3);
				if (matches < last_lit / 2 && num < num2 / 2)
				{
					return true;
				}
			}
			return last_lit == lit_bufsize - 1 || last_lit == lit_bufsize;
		}

		internal void send_compressed_block(short[] ltree, short[] dtree)
		{
			int num = 0;
			if (last_lit != 0)
			{
				do
				{
					int num2 = ((pending[d_buf + num * 2] << 8) & 0xFF00) | (pending[d_buf + num * 2 + 1] & 0xFF);
					int num3 = pending[l_buf + num] & 0xFF;
					num++;
					if (num2 == 0)
					{
						send_code(num3, ltree);
						continue;
					}
					int num4 = Tree._length_code[num3];
					send_code(num4 + 256 + 1, ltree);
					int num5 = Tree.extra_lbits[num4];
					if (num5 != 0)
					{
						num3 -= Tree.base_length[num4];
						send_bits(num3, num5);
					}
					num2--;
					num4 = Tree.d_code(num2);
					send_code(num4, dtree);
					num5 = Tree.extra_dbits[num4];
					if (num5 != 0)
					{
						num2 -= Tree.base_dist[num4];
						send_bits(num2, num5);
					}
				}
				while (num < last_lit);
			}
			send_code(256, ltree);
			last_eob_len = ltree[513];
		}

		internal void set_data_type()
		{
			int i = 0;
			int num = 0;
			int num2 = 0;
			for (; i < 7; i++)
			{
				num2 += dyn_ltree[i * 2];
			}
			for (; i < 128; i++)
			{
				num += dyn_ltree[i * 2];
			}
			for (; i < 256; i++)
			{
				num2 += dyn_ltree[i * 2];
			}
			data_type = (sbyte)((num2 <= SharedUtils.URShift(num, 2)) ? 1 : 0);
		}

		internal void bi_flush()
		{
			if (bi_valid == 16)
			{
				put_short(bi_buf);
				bi_buf = 0;
				bi_valid = 0;
			}
			else if (bi_valid >= 8)
			{
				put_byte((byte)bi_buf);
				bi_buf = (short)SharedUtils.URShift(bi_buf, 8);
				bi_valid -= 8;
			}
		}

		internal void bi_windup()
		{
			if (bi_valid > 8)
			{
				put_short(bi_buf);
			}
			else if (bi_valid > 0)
			{
				put_byte((byte)bi_buf);
			}
			bi_buf = 0;
			bi_valid = 0;
		}

		internal void copy_block(int buf, int len, bool header)
		{
			bi_windup();
			last_eob_len = 8;
			if (header)
			{
				put_short((short)len);
				put_short((short)(~len));
			}
			put_byte(window, buf, len);
		}

		internal void flush_block_only(bool eof)
		{
			_tr_flush_block((block_start >= 0) ? block_start : (-1), strstart - block_start, eof);
			block_start = strstart;
			_codec.flush_pending();
		}

		internal BlockState DeflateNone(FlushType flush)
		{
			int num = 65535;
			if (num > pending.Length - 5)
			{
				num = pending.Length - 5;
			}
			while (true)
			{
				if (lookahead <= 1)
				{
					_fillWindow();
					if (lookahead == 0 && flush == FlushType.None)
					{
						return BlockState.NeedMore;
					}
					if (lookahead == 0)
					{
						break;
					}
				}
				strstart += lookahead;
				lookahead = 0;
				int num2 = block_start + num;
				if (strstart == 0 || strstart >= num2)
				{
					lookahead = strstart - num2;
					strstart = num2;
					flush_block_only(eof: false);
					if (_codec.AvailableBytesOut == 0)
					{
						return BlockState.NeedMore;
					}
				}
				if (strstart - block_start >= w_size - MIN_LOOKAHEAD)
				{
					flush_block_only(eof: false);
					if (_codec.AvailableBytesOut == 0)
					{
						return BlockState.NeedMore;
					}
				}
			}
			flush_block_only(flush == FlushType.Finish);
			if (_codec.AvailableBytesOut == 0)
			{
				return (flush == FlushType.Finish) ? BlockState.FinishStarted : BlockState.NeedMore;
			}
			return (flush != FlushType.Finish) ? BlockState.BlockDone : BlockState.FinishDone;
		}

		internal void _tr_stored_block(int buf, int stored_len, bool eof)
		{
			send_bits(eof ? 1 : 0, 3);
			copy_block(buf, stored_len, header: true);
		}

		internal void _tr_flush_block(int buf, int stored_len, bool eof)
		{
			int num = 0;
			int num2;
			int num3;
			if (compressionLevel > CompressionLevel.None)
			{
				if (data_type == 2)
				{
					set_data_type();
				}
				l_desc.build_tree(this);
				d_desc.build_tree(this);
				num = build_bl_tree();
				num2 = SharedUtils.URShift(opt_len + 3 + 7, 3);
				num3 = SharedUtils.URShift(static_len + 3 + 7, 3);
				if (num3 <= num2)
				{
					num2 = num3;
				}
			}
			else
			{
				num2 = (num3 = stored_len + 5);
			}
			if (stored_len + 4 <= num2 && buf != -1)
			{
				_tr_stored_block(buf, stored_len, eof);
			}
			else if (num3 == num2)
			{
				send_bits(2 + (eof ? 1 : 0), 3);
				send_compressed_block(StaticTree.static_ltree, StaticTree.static_dtree);
			}
			else
			{
				send_bits(4 + (eof ? 1 : 0), 3);
				send_all_trees(l_desc.max_code + 1, d_desc.max_code + 1, num + 1);
				send_compressed_block(dyn_ltree, dyn_dtree);
			}
			_InitializeBlocks();
			if (eof)
			{
				bi_windup();
			}
		}

		private void _fillWindow()
		{
			do
			{
				int num = window_size - lookahead - strstart;
				int num2;
				if (num == 0 && strstart == 0 && lookahead == 0)
				{
					num = w_size;
				}
				else if (num == -1)
				{
					num--;
				}
				else if (strstart >= w_size + w_size - MIN_LOOKAHEAD)
				{
					Array.Copy(window, w_size, window, 0, w_size);
					match_start -= w_size;
					strstart -= w_size;
					block_start -= w_size;
					num2 = hash_size;
					int num3 = num2;
					do
					{
						int num4 = head[--num3] & 0xFFFF;
						head[num3] = (short)((num4 >= w_size) ? (num4 - w_size) : 0);
					}
					while (--num2 != 0);
					num2 = w_size;
					num3 = num2;
					do
					{
						int num4 = prev[--num3] & 0xFFFF;
						prev[num3] = (short)((num4 >= w_size) ? (num4 - w_size) : 0);
					}
					while (--num2 != 0);
					num += w_size;
				}
				if (_codec.AvailableBytesIn == 0)
				{
					break;
				}
				num2 = _codec.read_buf(window, strstart + lookahead, num);
				lookahead += num2;
				if (lookahead >= 3)
				{
					ins_h = (window[strstart] & 0xFF);
					ins_h = (((ins_h << hash_shift) ^ (window[strstart + 1] & 0xFF)) & hash_mask);
				}
			}
			while (lookahead < MIN_LOOKAHEAD && _codec.AvailableBytesIn != 0);
		}

		internal BlockState DeflateFast(FlushType flush)
		{
			int num = 0;
			while (true)
			{
				if (lookahead < MIN_LOOKAHEAD)
				{
					_fillWindow();
					if (lookahead < MIN_LOOKAHEAD && flush == FlushType.None)
					{
						return BlockState.NeedMore;
					}
					if (lookahead == 0)
					{
						break;
					}
				}
				if (lookahead >= 3)
				{
					ins_h = (((ins_h << hash_shift) ^ (window[strstart + 2] & 0xFF)) & hash_mask);
					num = (head[ins_h] & 0xFFFF);
					prev[strstart & w_mask] = head[ins_h];
					head[ins_h] = (short)strstart;
				}
				if (num != 0L && ((strstart - num) & 0xFFFF) <= w_size - MIN_LOOKAHEAD && compressionStrategy != CompressionStrategy.HuffmanOnly)
				{
					match_length = longest_match(num);
				}
				bool flag;
				if (match_length >= 3)
				{
					flag = _tr_tally(strstart - match_start, match_length - 3);
					lookahead -= match_length;
					if (match_length <= config.MaxLazy && lookahead >= 3)
					{
						match_length--;
						do
						{
							strstart++;
							ins_h = (((ins_h << hash_shift) ^ (window[strstart + 2] & 0xFF)) & hash_mask);
							num = (head[ins_h] & 0xFFFF);
							prev[strstart & w_mask] = head[ins_h];
							head[ins_h] = (short)strstart;
						}
						while (--match_length != 0);
						strstart++;
					}
					else
					{
						strstart += match_length;
						match_length = 0;
						ins_h = (window[strstart] & 0xFF);
						ins_h = (((ins_h << hash_shift) ^ (window[strstart + 1] & 0xFF)) & hash_mask);
					}
				}
				else
				{
					flag = _tr_tally(0, window[strstart] & 0xFF);
					lookahead--;
					strstart++;
				}
				if (flag)
				{
					flush_block_only(eof: false);
					if (_codec.AvailableBytesOut == 0)
					{
						return BlockState.NeedMore;
					}
				}
			}
			flush_block_only(flush == FlushType.Finish);
			if (_codec.AvailableBytesOut == 0)
			{
				if (flush == FlushType.Finish)
				{
					return BlockState.FinishStarted;
				}
				return BlockState.NeedMore;
			}
			return (flush != FlushType.Finish) ? BlockState.BlockDone : BlockState.FinishDone;
		}

		internal BlockState DeflateSlow(FlushType flush)
		{
			int num = 0;
			while (true)
			{
				if (lookahead < MIN_LOOKAHEAD)
				{
					_fillWindow();
					if (lookahead < MIN_LOOKAHEAD && flush == FlushType.None)
					{
						return BlockState.NeedMore;
					}
					if (lookahead == 0)
					{
						break;
					}
				}
				if (lookahead >= 3)
				{
					ins_h = (((ins_h << hash_shift) ^ (window[strstart + 2] & 0xFF)) & hash_mask);
					num = (head[ins_h] & 0xFFFF);
					prev[strstart & w_mask] = head[ins_h];
					head[ins_h] = (short)strstart;
				}
				prev_length = match_length;
				prev_match = match_start;
				match_length = 2;
				if (num != 0 && prev_length < config.MaxLazy && ((strstart - num) & 0xFFFF) <= w_size - MIN_LOOKAHEAD)
				{
					if (compressionStrategy != CompressionStrategy.HuffmanOnly)
					{
						match_length = longest_match(num);
					}
					if (match_length <= 5 && (compressionStrategy == CompressionStrategy.Filtered || (match_length == 3 && strstart - match_start > 4096)))
					{
						match_length = 2;
					}
				}
				if (prev_length >= 3 && match_length <= prev_length)
				{
					int num2 = strstart + lookahead - 3;
					bool flag = _tr_tally(strstart - 1 - prev_match, prev_length - 3);
					lookahead -= prev_length - 1;
					prev_length -= 2;
					do
					{
						if (++strstart <= num2)
						{
							ins_h = (((ins_h << hash_shift) ^ (window[strstart + 2] & 0xFF)) & hash_mask);
							num = (head[ins_h] & 0xFFFF);
							prev[strstart & w_mask] = head[ins_h];
							head[ins_h] = (short)strstart;
						}
					}
					while (--prev_length != 0);
					match_available = 0;
					match_length = 2;
					strstart++;
					if (flag)
					{
						flush_block_only(eof: false);
						if (_codec.AvailableBytesOut == 0)
						{
							return BlockState.NeedMore;
						}
					}
				}
				else if (match_available != 0)
				{
					if (_tr_tally(0, window[strstart - 1] & 0xFF))
					{
						flush_block_only(eof: false);
					}
					strstart++;
					lookahead--;
					if (_codec.AvailableBytesOut == 0)
					{
						return BlockState.NeedMore;
					}
				}
				else
				{
					match_available = 1;
					strstart++;
					lookahead--;
				}
			}
			if (match_available != 0)
			{
				bool flag = _tr_tally(0, window[strstart - 1] & 0xFF);
				match_available = 0;
			}
			flush_block_only(flush == FlushType.Finish);
			if (_codec.AvailableBytesOut == 0)
			{
				if (flush == FlushType.Finish)
				{
					return BlockState.FinishStarted;
				}
				return BlockState.NeedMore;
			}
			return (flush != FlushType.Finish) ? BlockState.BlockDone : BlockState.FinishDone;
		}

		internal int longest_match(int cur_match)
		{
			int num = config.MaxChainLength;
			int num2 = strstart;
			int num3 = prev_length;
			int num4 = (strstart > w_size - MIN_LOOKAHEAD) ? (strstart - (w_size - MIN_LOOKAHEAD)) : 0;
			int niceLength = config.NiceLength;
			int num5 = w_mask;
			int num6 = strstart + 258;
			byte b = window[num2 + num3 - 1];
			byte b2 = window[num2 + num3];
			if (prev_length >= config.GoodLength)
			{
				num >>= 2;
			}
			if (niceLength > lookahead)
			{
				niceLength = lookahead;
			}
			do
			{
				int num7 = cur_match;
				if (window[num7 + num3] != b2 || window[num7 + num3 - 1] != b || window[num7] != window[num2] || window[++num7] != window[num2 + 1])
				{
					continue;
				}
				num2 += 2;
				num7++;
				while (window[++num2] == window[++num7] && window[++num2] == window[++num7] && window[++num2] == window[++num7] && window[++num2] == window[++num7] && window[++num2] == window[++num7] && window[++num2] == window[++num7] && window[++num2] == window[++num7] && window[++num2] == window[++num7] && num2 < num6)
				{
				}
				int num8 = 258 - (num6 - num2);
				num2 = num6 - 258;
				if (num8 > num3)
				{
					match_start = cur_match;
					num3 = num8;
					if (num8 >= niceLength)
					{
						break;
					}
					b = window[num2 + num3 - 1];
					b2 = window[num2 + num3];
				}
			}
			while ((cur_match = (prev[cur_match & num5] & 0xFFFF)) > num4 && --num != 0);
			if (num3 <= lookahead)
			{
				return num3;
			}
			return lookahead;
		}

		internal int Initialize(ZlibCodec codec, CompressionLevel level)
		{
			return Initialize(codec, level, 15);
		}

		internal int Initialize(ZlibCodec codec, CompressionLevel level, int bits)
		{
			return Initialize(codec, level, bits, 8, CompressionStrategy.Default);
		}

		internal int Initialize(ZlibCodec codec, CompressionLevel level, int bits, CompressionStrategy compressionStrategy)
		{
			return Initialize(codec, level, bits, 8, compressionStrategy);
		}

		internal int Initialize(ZlibCodec codec, CompressionLevel level, int windowBits, int memLevel, CompressionStrategy strategy)
		{
			_codec = codec;
			_codec.Message = null;
			if (windowBits < 9 || windowBits > 15)
			{
				throw new ZlibException("windowBits must be in the range 9..15.");
			}
			if (memLevel < 1 || memLevel > 9)
			{
				throw new ZlibException($"memLevel must be in the range 1.. {9}");
			}
			_codec.dstate = this;
			w_bits = windowBits;
			w_size = 1 << w_bits;
			w_mask = w_size - 1;
			hash_bits = memLevel + 7;
			hash_size = 1 << hash_bits;
			hash_mask = hash_size - 1;
			hash_shift = (hash_bits + 3 - 1) / 3;
			window = new byte[w_size * 2];
			prev = new short[w_size];
			head = new short[hash_size];
			lit_bufsize = 1 << memLevel + 6;
			pending = new byte[lit_bufsize * 4];
			d_buf = lit_bufsize / 2;
			l_buf = 3 * lit_bufsize;
			compressionLevel = level;
			compressionStrategy = strategy;
			method = 8;
			Reset();
			return 0;
		}

		internal void Reset()
		{
			_codec.TotalBytesIn = (_codec.TotalBytesOut = 0L);
			_codec.Message = null;
			pendingCount = 0;
			nextPending = 0;
			Rfc1950BytesEmitted = false;
			status = (WantRfc1950HeaderBytes ? 42 : 113);
			_codec._Adler32 = Adler.Adler32(0L, null, 0, 0);
			last_flush = 0;
			_InitializeTreeData();
			_InitializeLazyMatch();
		}

		internal int End()
		{
			if (status != 42 && status != 113 && status != 666)
			{
				return -2;
			}
			pending = null;
			head = null;
			prev = null;
			window = null;
			return (status == 113) ? (-3) : 0;
		}

		internal int SetParams(CompressionLevel level, CompressionStrategy strategy)
		{
			int result = 0;
			if (compressionLevel != level)
			{
				Config config = Config.Lookup(level);
				if (config.Flavor != this.config.Flavor && _codec.TotalBytesIn != 0)
				{
					result = _codec.Deflate(FlushType.Partial);
				}
				compressionLevel = level;
				this.config = config;
				switch (this.config.Flavor)
				{
				case DeflateFlavor.Store:
					DeflateFunction = DeflateNone;
					break;
				case DeflateFlavor.Fast:
					DeflateFunction = DeflateFast;
					break;
				case DeflateFlavor.Slow:
					DeflateFunction = DeflateSlow;
					break;
				}
			}
			compressionStrategy = strategy;
			return result;
		}

		internal int SetDictionary(byte[] dictionary)
		{
			int num = dictionary.Length;
			int sourceIndex = 0;
			if (dictionary == null || status != 42)
			{
				throw new ZlibException("Stream error.");
			}
			_codec._Adler32 = Adler.Adler32(_codec._Adler32, dictionary, 0, dictionary.Length);
			if (num < 3)
			{
				return 0;
			}
			if (num > w_size - MIN_LOOKAHEAD)
			{
				num = w_size - MIN_LOOKAHEAD;
				sourceIndex = dictionary.Length - num;
			}
			Array.Copy(dictionary, sourceIndex, window, 0, num);
			strstart = num;
			block_start = num;
			ins_h = (window[0] & 0xFF);
			ins_h = (((ins_h << hash_shift) ^ (window[1] & 0xFF)) & hash_mask);
			for (int i = 0; i <= num - 3; i++)
			{
				ins_h = (((ins_h << hash_shift) ^ (window[i + 2] & 0xFF)) & hash_mask);
				prev[i & w_mask] = head[ins_h];
				head[ins_h] = (short)i;
			}
			return 0;
		}

		internal int Deflate(FlushType flush)
		{
			if (_codec.OutputBuffer == null || (_codec.InputBuffer == null && _codec.AvailableBytesIn != 0) || (status == 666 && flush != FlushType.Finish))
			{
				_codec.Message = z_errmsg[4];
				throw new ZlibException($"Something is fishy. [{_codec.Message}]");
			}
			if (_codec.AvailableBytesOut == 0)
			{
				_codec.Message = z_errmsg[7];
				throw new ZlibException("OutputBuffer is full (AvailableBytesOut == 0)");
			}
			int num = last_flush;
			last_flush = (int)flush;
			if (status == 42)
			{
				int num2 = 8 + (w_bits - 8 << 4) << 8;
				int num3 = (int)((compressionLevel - 1) & (CompressionLevel)255) >> 1;
				if (num3 > 3)
				{
					num3 = 3;
				}
				num2 |= num3 << 6;
				if (strstart != 0)
				{
					num2 |= 0x20;
				}
				num2 += 31 - num2 % 31;
				status = 113;
				putShortMSB(num2);
				if (strstart != 0)
				{
					putShortMSB((int)SharedUtils.URShift(_codec._Adler32, 16));
					putShortMSB((int)(_codec._Adler32 & 0xFFFF));
				}
				_codec._Adler32 = Adler.Adler32(0L, null, 0, 0);
			}
			if (pendingCount != 0)
			{
				_codec.flush_pending();
				if (_codec.AvailableBytesOut == 0)
				{
					last_flush = -1;
					return 0;
				}
			}
			else if (_codec.AvailableBytesIn == 0 && (int)flush <= num && flush != FlushType.Finish)
			{
				return 0;
			}
			if (status == 666 && _codec.AvailableBytesIn != 0)
			{
				_codec.Message = z_errmsg[7];
				throw new ZlibException("status == FINISH_STATE && _codec.AvailableBytesIn != 0");
			}
			if (_codec.AvailableBytesIn != 0 || lookahead != 0 || (flush != 0 && status != 666))
			{
				BlockState blockState = DeflateFunction(flush);
				if (blockState == BlockState.FinishStarted || blockState == BlockState.FinishDone)
				{
					status = 666;
				}
				if (blockState == BlockState.NeedMore || blockState == BlockState.FinishStarted)
				{
					if (_codec.AvailableBytesOut == 0)
					{
						last_flush = -1;
					}
					return 0;
				}
				if (blockState == BlockState.BlockDone)
				{
					if (flush == FlushType.Partial)
					{
						_tr_align();
					}
					else
					{
						_tr_stored_block(0, 0, eof: false);
						if (flush == FlushType.Full)
						{
							for (int i = 0; i < hash_size; i++)
							{
								head[i] = 0;
							}
						}
					}
					_codec.flush_pending();
					if (_codec.AvailableBytesOut == 0)
					{
						last_flush = -1;
						return 0;
					}
				}
			}
			if (flush != FlushType.Finish)
			{
				return 0;
			}
			if (!WantRfc1950HeaderBytes || Rfc1950BytesEmitted)
			{
				return 1;
			}
			putShortMSB((int)SharedUtils.URShift(_codec._Adler32, 16));
			putShortMSB((int)(_codec._Adler32 & 0xFFFF));
			_codec.flush_pending();
			Rfc1950BytesEmitted = true;
			return (pendingCount == 0) ? 1 : 0;
		}
	}
}
