using SkiaSharp;

namespace LoGeCuiMobile.Services
{
    public static class ImageCompressionHelper
    {
        public static byte[] CompressJpeg(
            byte[] input,
            int maxWidth = 1600,
            int jpegQuality = 75)
        {
            using var inputStream = new SKMemoryStream(input);
            using var codec = SKCodec.Create(inputStream);
            using var bitmap = SKBitmap.Decode(codec);

            if (bitmap == null)
                throw new InvalidOperationException("Impossible de décoder l'image.");

            var width = bitmap.Width;
            var height = bitmap.Height;

            if (width > maxWidth)
            {
                var ratio = (float)maxWidth / width;
                var newW = maxWidth;
                var newH = (int)(height * ratio);

                using var resized = bitmap.Resize(
                    new SKImageInfo(newW, newH),
                    SKFilterQuality.Medium);

                using var image = SKImage.FromBitmap(resized);
                using var data = image.Encode(
                    SKEncodedImageFormat.Jpeg,
                    jpegQuality);

                return data.ToArray();
            }
            else
            {
                using var image = SKImage.FromBitmap(bitmap);
                using var data = image.Encode(
                    SKEncodedImageFormat.Jpeg,
                    jpegQuality);

                return data.ToArray();
            }
        }
    }
}

