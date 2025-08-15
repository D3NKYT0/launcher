namespace Updater.HashZip.ZIPLib
{
	internal sealed class Adler
	{
		private static int BASE = 65521;

		private static int NMAX = 5552;

		internal static long Adler32(long adler, byte[] buf, int index, int len)
		{
			if (buf == null)
			{
				return 1L;
			}
			long num = adler & 0xFFFF;
			long num2 = (adler >> 16) & 0xFFFF;
			while (len > 0)
			{
				int num3 = (len < NMAX) ? len : NMAX;
				len -= num3;
				while (num3 >= 16)
				{
					num += (buf[index++] & 0xFF);
					num2 += num;
					num += (buf[index++] & 0xFF);
					num2 += num;
					num += (buf[index++] & 0xFF);
					num2 += num;
					num += (buf[index++] & 0xFF);
					num2 += num;
					num += (buf[index++] & 0xFF);
					num2 += num;
					num += (buf[index++] & 0xFF);
					num2 += num;
					num += (buf[index++] & 0xFF);
					num2 += num;
					num += (buf[index++] & 0xFF);
					num2 += num;
					num += (buf[index++] & 0xFF);
					num2 += num;
					num += (buf[index++] & 0xFF);
					num2 += num;
					num += (buf[index++] & 0xFF);
					num2 += num;
					num += (buf[index++] & 0xFF);
					num2 += num;
					num += (buf[index++] & 0xFF);
					num2 += num;
					num += (buf[index++] & 0xFF);
					num2 += num;
					num += (buf[index++] & 0xFF);
					num2 += num;
					num += (buf[index++] & 0xFF);
					num2 += num;
					num3 -= 16;
				}
				if (num3 != 0)
				{
					do
					{
						num += (buf[index++] & 0xFF);
						num2 += num;
					}
					while (--num3 != 0);
				}
				num %= BASE;
				num2 %= BASE;
			}
			return (num2 << 16) | num;
		}
	}
}
