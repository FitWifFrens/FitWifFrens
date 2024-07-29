﻿using FitWifFrens.Data;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SixLabors.Fonts;
using SixLabors.ImageSharp;
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
            var titleFont = family.CreateFont(30, FontStyle.Bold);
            var valueFont = family.CreateFont(22, FontStyle.Bold);

            const float displayWidth = 264F;
            const float displayHeight = 176F;

            const DayOfWeek startDayOfWeek = DayOfWeek.Monday;
            const int metricCount = 4;
            const int weeksToDisplay = 4;
            const int daysInWeek = 7;
            const int daysToDisplay = weeksToDisplay * daysInWeek;


            using var image = new Image<L8>((int)displayWidth, (int)displayHeight, new L8(byte.MaxValue));

            var time = _timeProvider.GetUtcNow().DateTime.ConvertTimeFromUtc();
            var startTime = time.StartOfWeek(startDayOfWeek).ConvertTimeToUtc().AddDays(daysInWeek).AddDays(-daysToDisplay);

            var userMetricProviderValues = await _dataContext.UserMetricProviderValues.Where(umpv => umpv.UserId == userDisplay.UserId && umpv.Time > startTime).ToListAsync();

            startTime = startTime.ConvertTimeFromUtc();
            userMetricProviderValues.ForEach(umpv => umpv.Time = umpv.Time.ConvertTimeFromUtc());

            const float displayMargin = 6F;

            const float titleWidth = 30F;

            const float metricHeight = (displayHeight - (displayMargin * 2)) / metricCount;

            const float weekWidth = (displayWidth - (displayMargin * 2) - titleWidth) / weeksToDisplay;

            const float titleNudgeX1 = 7F;
            const float titleNudgeX2 = 4F;
            const float titleNudgeX3 = 1F;
            const float titleNudgeY = 7F;

            const float valueNudgeX = 4F;
            const float valueNudgeY = 11F;

            var metricsToShow = new List<(string MetricTitle, float TitleNudgeX, string MetricName, string ProviderName, MetricType MetricType)>
            {
                new("E", titleNudgeX1, "Exercise", "Withings", MetricType.Minutes),
                new("R", titleNudgeX1, "Running", "Withings", MetricType.Minutes),
                new("G", titleNudgeX2, "Workout", "Withings", MetricType.Minutes),
                new("W", titleNudgeX3, "Weight", "Withings", MetricType.Value),
            };

            image.Mutate(x =>
            {
                x.Configuration.SetGraphicsOptions(g =>
                {
                    g.Antialias = false;
                });

#if DEBUG
                x.DrawLine(Color.Gray, 1, new PointF(displayMargin, displayMargin), new PointF(displayMargin, displayHeight - displayMargin));
                x.DrawLine(Color.Gray, 1, new PointF(displayMargin + titleWidth, displayMargin), new PointF(displayMargin + titleWidth, displayHeight - displayMargin));

                for (var i = 0; i <= weeksToDisplay; i++)
                {
                    x.DrawLine(Color.Gray, 1, new PointF(displayMargin + titleWidth + (weekWidth * i), displayMargin), new PointF(displayMargin + titleWidth + (weekWidth * i), displayHeight - displayMargin));
                }

                for (var i = 0; i <= metricCount; i++)
                {
                    x.DrawLine(Color.Gray, 1, new PointF(displayMargin, displayMargin + (metricHeight * i)), new PointF(displayWidth - displayMargin, displayMargin + (metricHeight * i)));
                }
#endif

                for (var m = 0; m < metricCount && m < metricsToShow.Count; m++)
                {
                    var (metricTitle, titleNudgeX, metricName, providerName, metricType) = metricsToShow[m];

                    var userMetricProviderValuesByWeek = userMetricProviderValues
                        .Where(umpv => umpv.MetricName == metricName && umpv.ProviderName == providerName && umpv.MetricType == metricType)
                        .GroupBy(umpv => umpv.Time.StartOfWeek(startDayOfWeek))
                        .ToDictionary(g => g.Key, g => g.ToList());

                    if (userMetricProviderValuesByWeek.Any())
                    {
                        x.DrawText(metricTitle, titleFont, Color.Black, new PointF(displayMargin + titleNudgeX, displayMargin + (metricHeight * m) + titleNudgeY));

                        for (var w = 0; w < weeksToDisplay; w++)
                        {
                            var startOfWeekTime = startTime.AddDays(w * daysInWeek);

                            if (userMetricProviderValuesByWeek.TryGetValue(startOfWeekTime, out var userMetricProviderWeekValues))
                            {
                                if (metricType == MetricType.Minutes)
                                {
                                    var value = Math.Round(userMetricProviderWeekValues.Sum(umpv => umpv.Value), 0);

                                    x.DrawText(value.ToString("F0"), valueFont, Color.Black, new PointF(displayMargin + titleWidth + (weekWidth * w) + valueNudgeX, displayMargin + (metricHeight * m) + valueNudgeY));
                                }
                                else if (metricType == MetricType.Value)
                                {
                                    var value = Math.Round(userMetricProviderWeekValues.Average(umpv => umpv.Value), 1);

                                    x.DrawText(value.ToString("F1"), valueFont, Color.Black, new PointF(displayMargin + titleWidth + (weekWidth * w) + valueNudgeX, displayMargin + (metricHeight * m) + valueNudgeY));
                                }
                                else
                                {
                                    throw new ArgumentOutOfRangeException(nameof(metricType), metricType, "98a11f6f-2580-4750-9a88-2ebd1f1a6b9c");
                                }
                            }
                        }
                    }
                }
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
