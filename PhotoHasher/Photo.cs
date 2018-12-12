using System;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Imaging;

namespace PhotoHasher
{
	public class Photo
	{
		public string Path { get; set; }
		private uint? hash;

		public Photo(Bitmap img)
		{
			this.GetHash(img);
			this.Path = System.IO.Path.GetTempFileName();
			this.IsValidPhoto = true;
			this.Size = img.Size.Width * img.Size.Height;

		}

		public Photo(string path, byte detailSize = 8)
		{
			try
			{
				using (var img = Image.FromFile(path) as Bitmap)
				{
					this.GetHash(img, detailSize);
					this.Path = path;
					this.IsValidPhoto = true;
					this.Size = img.Size.Width * img.Size.Height;
				}

			}
			catch (Exception)
			{
				this.IsValidPhoto = false;
			}

		}

		public int Size { get; private set; }

		public bool IsValidPhoto { get; private set; }


		public static Bitmap ResizeAndCropImage(Image image, int size)
		{
			int shortestDim = Math.Min(image.Width, image.Height);
			int xOffset = (image.Width - shortestDim) / 2;
			int yOffset = (image.Height - shortestDim) / 2;

			var destRect = new Rectangle(0, 0, size, size);
			var destImage = new Bitmap(size, size);

			destImage.SetResolution(image.HorizontalResolution, image.VerticalResolution);

			using (var graphics = Graphics.FromImage(destImage))
			{
				graphics.CompositingMode = CompositingMode.SourceCopy;
				graphics.InterpolationMode = InterpolationMode.NearestNeighbor;
				graphics.PixelOffsetMode = PixelOffsetMode.HighSpeed;

				using (var wrapMode = new ImageAttributes())
				{
					wrapMode.SetWrapMode(WrapMode.TileFlipXY);
					graphics.DrawImage(image, destRect, xOffset, yOffset, shortestDim, shortestDim, GraphicsUnit.Pixel, wrapMode);
				}
			}

			return destImage;
		}

		public override int GetHashCode()
		{
			return (int)(hash.Value);
		}

		public unsafe uint GetHash(Bitmap img, byte detailSize = 8)
		{
			if (detailSize < 4 || detailSize > 64)
				throw new InvalidOperationException("detailSize must be between 4 and 64");

			uint detailSizeSquared = (uint)(detailSize * detailSize);
			byte pixelsPerBit = Math.Max((byte)(detailSizeSquared / 32), (byte)1);

			if (hash.HasValue)
				return hash.Value;

			byte[] grayScaleImage = new byte[detailSizeSquared];

			uint averageValue = 0;
			uint finalHash = 0;

			using (var bmp = ResizeAndCropImage(img, detailSize))
			{
				BitmapData bmpd = bmp.LockBits(new Rectangle(0, 0, detailSize, detailSize),
									   ImageLockMode.ReadOnly,
									   bmp.PixelFormat);
				byte bitsPerPixel = (byte)Image.GetPixelFormatSize(bmpd.PixelFormat);

				byte* scan0 = (byte*)bmpd.Scan0.ToPointer();

				for (int y = 0; y < detailSize; y++)
				{
					for (int x = 0; x < detailSize; x++)
					{
						byte* data = scan0 + y * bmpd.Stride + x * bitsPerPixel / 8;

						byte grayTone = (byte)((data[0] * 0.3) + (data[1] * 0.59) + (data[2] * 0.11));
						grayScaleImage[x + y * detailSize] = grayTone;
						averageValue += grayTone;

					}
				}
				bmp.UnlockBits(bmpd);
			}
			averageValue /= detailSizeSquared;

			for (int k = 0; k < detailSizeSquared; k++)
			{
				if (grayScaleImage[k] >= averageValue)
					finalHash |= (1U << (32 - k / pixelsPerBit));
			}

			Console.Write('.');

			hash = finalHash;
			return finalHash;
		}




	}
}
