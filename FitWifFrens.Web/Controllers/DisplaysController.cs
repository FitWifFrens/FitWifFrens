using FitWifFrens.Data;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SixLabors.Fonts;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.Drawing.Processing;
using SixLabors.ImageSharp.Formats.Bmp;
using SixLabors.ImageSharp.PixelFormats;
using SixLabors.ImageSharp.Processing;
using System.Globalization;

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
            var font = family.CreateFont(20, FontStyle.Bold);

            const float displayWidth = 264F;
            const float displayHeight = 176F;

            using var image = new Image<L8>((int)displayWidth, (int)displayHeight, new L8(byte.MaxValue));

            var startTime = _timeProvider.GetUtcNow().DateTime.StartOfWeek(DayOfWeek.Monday).SpecifyUtcKind();

            //var stringBuilder = new StringBuilder();

            var userMetricProviderValues = await _dataContext.UserMetricProviderValues.Where(umpv => umpv.UserId == userDisplay.UserId && umpv.Time > startTime).ToListAsync();

            //var runningMinutes = Math.Round(userProviderMetricValues.Where(upmv => upmv.MetricName == "Running" && upmv.MetricType == MetricType.Minutes).Sum(upmv => upmv.Value), 0);
            //var workoutMinutes = Math.Round(userProviderMetricValues.Where(upmv => upmv.MetricName == "Workout" && upmv.MetricType == MetricType.Minutes).Sum(upmv => upmv.Value), 0);

            //stringBuilder.Append($"Running: {runningMinutes} mins\n\nWorkout: {workoutMinutes} mins");

            //var userProviderWeightValues = userProviderMetricValues.Where(upmv => upmv.MetricName == "Weight" && upmv.MetricType == MetricType.Value).OrderBy(upmv => upmv.Time).ToList();

            //if (userProviderWeightValues.Count >= 2)
            //{
            //    var weightChange = Math.Round(userProviderWeightValues.Last().Value - userProviderWeightValues.First().Value, 1);
            //    stringBuilder.Append($"\n\nWeight: {weightChange} kg");
            //}

            const int metricCount = 5;
            const float daysInWeek = 7F;

            const float displayMargin = 6F;

            const float metricHeight = (displayHeight - (displayMargin * 2)) / metricCount;

            const float titleWidth = 30F;
            const float valueWidth = 50F;
            const float chartWidth = (displayWidth - (displayMargin * 2)) - titleWidth - valueWidth;

            const float titleNudgeX = 8F;
            const float titleNudgeY = 8F;

            const float valueNudgeX = 4F;
            const float valueNudgeY = 8F;

            List<PointF> CreateChart(List<(int Day, float Value)> valueByDay, PointF position, float width, float height)
            {
                var widthPerDay = width / daysInWeek;
                var heightPerValue = height / valueByDay.Sum(v => v.Value);

                var points = new List<PointF>
                {
                    new PointF(position.X, position.Y + height)
                };

                var valueSum = 0F;
                foreach (var (day, value) in valueByDay.OrderBy(v => v.Day))
                {
                    valueSum += value;

                    points.Add(new PointF(position.X + (day * widthPerDay), (position.Y + height) - (valueSum * heightPerValue)));
                }

                return points;
            }

            //var points = CreateChart(new List<(int Day, float Value)>
            //{
            //    (1, 20),
            //    (2, 20),
            //    (3, 20),
            //    (4, 20),
            //    (5, 20),
            //    (6, 20),
            //    (7, 20),
            //}, new PointF(0, 0), 264, 50);

            image.Mutate(x =>
            {
                x.Configuration.SetGraphicsOptions(g =>
                {
                    g.Antialias = false;
                });

#if DEBUG
                x.DrawLine(Color.Gray, 1, new PointF(displayMargin, displayMargin), new PointF(displayMargin, displayHeight - displayMargin));
                x.DrawLine(Color.Gray, 1, new PointF(displayMargin + titleWidth, displayMargin), new PointF(displayMargin + titleWidth, displayHeight - displayMargin));
                x.DrawLine(Color.Gray, 1, new PointF(displayMargin + titleWidth + chartWidth, displayMargin), new PointF(displayMargin + titleWidth + chartWidth, displayHeight - displayMargin));
                x.DrawLine(Color.Gray, 1, new PointF(displayMargin + titleWidth + chartWidth + valueWidth, displayMargin), new PointF(displayMargin + titleWidth + chartWidth + valueWidth, displayHeight - displayMargin));

                for (int i = 0; i <= metricCount; i++)
                {
                    x.DrawLine(Color.Gray, 1, new PointF(displayMargin, displayMargin + (metricHeight * i)), new PointF(displayWidth - displayMargin, displayMargin + (metricHeight * i)));
                }
#endif

                var userMetricProviderExerciseValues = userMetricProviderValues.Where(umpv => umpv.MetricName == "Exercise" && umpv.MetricType == MetricType.Minutes).OrderBy(upmv => upmv.Time).ToList();

                if (userMetricProviderExerciseValues.Any())
                {
                    var exercisePoints = userMetricProviderExerciseValues.Select(umpv => ((int)(umpv.Time - startTime).TotalDays, (float)umpv.Value)).ToList();
                    var chartPoints = CreateChart(exercisePoints, new PointF(displayMargin + titleWidth, displayMargin), chartWidth, metricHeight);

                    var runningMinutes = Math.Round(userMetricProviderExerciseValues.Sum(upmv => upmv.Value), 0);

                    x.DrawText("E", font, Color.Black, new PointF(displayMargin + titleNudgeX, displayMargin + titleNudgeY));

                    x.DrawLine(Color.Black, 2, chartPoints.ToArray());

                    x.DrawText(runningMinutes.ToString(CultureInfo.InvariantCulture), font, Color.Black, new PointF(displayMargin + valueNudgeX + titleWidth + chartWidth, displayMargin + valueNudgeY));
                }


                var userMetricProviderRunningValues = userMetricProviderValues.Where(umpv => umpv.MetricName == "Running" && umpv.MetricType == MetricType.Minutes).OrderBy(upmv => upmv.Time).ToList();

                if (userMetricProviderRunningValues.Any())
                {
                    var runningPoints = userMetricProviderRunningValues.Select(umpv => ((int)(umpv.Time - startTime).TotalDays, (float)umpv.Value)).ToList();
                    var chartPoints = CreateChart(runningPoints, new PointF(displayMargin + titleWidth, displayMargin + (metricHeight * 1)), chartWidth, metricHeight);

                    var runningMinutes = Math.Round(userMetricProviderRunningValues.Sum(upmv => upmv.Value), 0);

                    x.DrawText("R", font, Color.Black, new PointF(displayMargin + titleNudgeX, displayMargin + titleNudgeY + (metricHeight * 1)));

                    x.DrawLine(Color.Black, 2, chartPoints.ToArray());

                    x.DrawText(runningMinutes.ToString(CultureInfo.InvariantCulture), font, Color.Black, new PointF(displayMargin + valueNudgeX + titleWidth + chartWidth, displayMargin + valueNudgeY + (metricHeight * 1)));
                }



                //x.DrawLine(Color.Black, 2, points.ToArray());

                //x.DrawText(stringBuilder.ToString(), font, Color.Black, new PointF(10, 10));
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
