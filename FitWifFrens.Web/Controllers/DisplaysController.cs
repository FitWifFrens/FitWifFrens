using Microsoft.AspNetCore.Mvc;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.Drawing;
using SixLabors.ImageSharp.Drawing.Processing;
using SixLabors.ImageSharp.Formats.Bmp;
using SixLabors.ImageSharp.PixelFormats;
using SixLabors.ImageSharp.Processing;

namespace FitWifFrens.Web.Controllers
{
    [ApiController]
    [Route("api/displays")]
    public class DisplaysController : Controller
    {
        [HttpGet("{macAddress}")]
        public async Task<IActionResult> Get(string macAddress)
        {
            using var image = new Image<L8>(264, 176, new L8(byte.MaxValue));

            var star1 = new Star(50, 50, 5, 20, 45);
            image.Mutate(x => x.Fill(Color.Black, star1));

            var star2 = new Star(100, 100, 5, 20, 45);
            image.Mutate(x => x.Fill(Color.White, star2));

            var outputStream = new MemoryStream();
            await image.SaveAsBmpAsync(outputStream, new BmpEncoder
            {
                BitsPerPixel = BmpBitsPerPixel.Pixel8,
                SupportTransparency = false
            });

            outputStream.Seek(0, SeekOrigin.Begin);
            return File(outputStream, "image/bmp");
        }
    }
}
