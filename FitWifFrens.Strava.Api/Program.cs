var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddCors(o => o.AddDefaultPolicy(x => x.AllowAnyOrigin().AllowAnyHeader().AllowAnyMethod()));

var app = builder.Build();

// Configure the HTTP request pipeline.

app.UseHttpsRedirection();

app.UseCors();

//var summaries = new[]
//{
//    "Freezing", "Bracing", "Chilly", "Cool", "Mild", "Warm", "Balmy", "Hot", "Sweltering", "Scorching"
//};

var id = new[]
{
    "0x2fefb77e01d5019b7b2571b44d8cea84d0e1d83491d93ec3f9bb8871dedd7cdb",
    "0x1a66e7598f54a728f7db97983b226005aca9ecbc44928e519f0244a61303af20",
    "0x035646cc875f3b69fb4fbbb0e8a6f01805a52255d7619a0fddb71cea7ad49960",
    "0x1926cb37beb496f718831774cb00bbf9b94981c3a6e88cce2521eef6bc642a42"
};

app.MapGet("/stravaactivity/{worldId}", (string worldId) =>
{
    var activity =  new StravaActivity
    (
        worldId,
        Random.Shared.Next(2, 7),
        Random.Shared.Next(20, 120)
     );
    return activity;
});

app.Run();

internal record StravaActivity(string Id, int NumberOfActivity, int MinuteOfActivity)
{

}