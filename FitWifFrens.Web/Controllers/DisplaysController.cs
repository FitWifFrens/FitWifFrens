﻿using FitWifFrens.Data;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SixLabors.Fonts;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.Drawing;
using SixLabors.ImageSharp.Drawing.Processing;
using SixLabors.ImageSharp.Formats.Bmp;
using SixLabors.ImageSharp.PixelFormats;
using SixLabors.ImageSharp.Processing;
using System.Text;

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

            var stringBuilder = new StringBuilder();

            var userProviderMetricValues = await _dataContext.UserMetricProviderValues.Where(umpv => umpv.UserId == userDisplay.UserId && umpv.Time > startTime).ToListAsync();

            var runningMinutes = Math.Round(userProviderMetricValues.Where(upmv => upmv.MetricName == "Running" && upmv.MetricType == MetricType.Minutes).Sum(upmv => upmv.Value), 0);
            var workoutMinutes = Math.Round(userProviderMetricValues.Where(upmv => upmv.MetricName == "Workout" && upmv.MetricType == MetricType.Minutes).Sum(upmv => upmv.Value), 0);

            stringBuilder.Append($"Running: {runningMinutes} mins\n\nWorkout: {workoutMinutes} mins");

            var userProviderWeightValues = userProviderMetricValues.Where(upmv => upmv.MetricName == "Weight" && upmv.MetricType == MetricType.Value).OrderBy(upmv => upmv.Time).ToList();

            if (userProviderWeightValues.Count >= 2)
            {
                var weightChange = Math.Round(userProviderWeightValues.Last().Value - userProviderWeightValues.First().Value, 1);
                stringBuilder.Append($"\n\nWeight: {weightChange} kg");
            }


            image.Mutate(x =>
            {
                x.Configuration.SetGraphicsOptions(g =>
                {
                    g.Antialias = false;
                });
                x.Fill(Color.Black, star);
                x.DrawText(stringBuilder.ToString(), font, Color.Black, new PointF(10, 10));
            });


            var outputStream = new MemoryStream();

            var bmpEncoder = new BmpEncoder
            {
                BitsPerPixel = BmpBitsPerPixel.Pixel8,
                SupportTransparency = false
            };

            await image.SaveAsBmpAsync(outputStream, bmpEncoder);

            outputStream.Seek(0, SeekOrigin.Begin);
            return File(outputStream, "image/bmp");
        }
    }
}
