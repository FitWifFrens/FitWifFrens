using FitWifFrens.Data;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SixLabors.Fonts;
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
        private readonly DataContext _dataContext;
        private readonly TimeProvider _timeProvider;

        public DisplaysController(DataContext dataContext, TimeProvider timeProvider)
        {
            _dataContext = dataContext;
            _timeProvider = timeProvider;
        }

        [HttpGet("{macAddress}")]
        public async Task<IActionResult> Get(string macAddress)
        {
            var userDisplay = await _dataContext.UserDisplays.SingleOrDefaultAsync(ud => ud.MacAddress == macAddress);

            if (userDisplay == null)
            {
                return NotFound();
            }

            var collection = new FontCollection();
            var family = collection.Add("wwwroot/fonts/Poppins-Regular.ttf");
            var font = family.CreateFont(26, FontStyle.Bold);

            using var image = new Image<L8>(264, 176, new L8(byte.MaxValue));

            var star = new Star(240, 150, 5, 10, 20);

            var startTime = _timeProvider.GetUtcNow().AddDays(-7);

            var userProviderMetricValues = await _dataContext.UserProviderMetricValues.Where(upmv => upmv.UserId == userDisplay.UserId && upmv.Time > startTime).ToListAsync();

            var runningMinutes = Math.Round(userProviderMetricValues.Where(upmv => upmv.MetricName == "Running" && upmv.MetricType == MetricType.Minutes).Sum(upmv => upmv.Value), 0);
            var workoutMinutes = Math.Round(userProviderMetricValues.Where(upmv => upmv.MetricName == "Workout" && upmv.MetricType == MetricType.Minutes).Sum(upmv => upmv.Value), 0);

            var weightChange = Math.Round(
                userProviderMetricValues.Where(upmv => upmv.MetricName == "Weight" && upmv.MetricType == MetricType.Value).OrderBy(upmv => upmv.Time).Last().Value -
                userProviderMetricValues.Where(upmv => upmv.MetricName == "Weight" && upmv.MetricType == MetricType.Value).OrderBy(upmv => upmv.Time).First().Value, 1);

            image.Mutate(x => x.Fill(Color.Black, star).DrawText($"Running: {runningMinutes} mins\n\nWorkout: {workoutMinutes} mins\n\nWeight: {weightChange} kg", font, Color.Black, new PointF(10, 10)));

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
