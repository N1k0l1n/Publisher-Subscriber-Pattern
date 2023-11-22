using MessageBroker.Data;
using MessageBroker.Models;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);


//Database Connection
builder.Services.AddDbContext<AppDbContext>(options =>
{

    options.UseSqlServer(builder.Configuration.GetConnectionString("DevConnection"));
});


var app = builder.Build();



app.UseHttpsRedirection();

//Minimal APi

//Create Topic
app.MapPost("api/topics", async (AppDbContext context, Topic topic) =>
{
    await context.Topics.AddAsync(topic);
    await context.SaveChangesAsync();

    return Results.Created($"api/topics/{topic.Id}", topic);
});


//Return all Topics

app.MapGet("api/topics", async (AppDbContext context) =>
{
    var topics = context.Topics.ToListAsync();

    return Results.Ok(topics);
});



//Publish Message

app.MapPost("api/topics/{id}/messages", async (AppDbContext context, int id, Message message) =>
{
    bool topics = await context.Topics.AnyAsync(t => t.Id == id);
    if (!topics)
        return Results.NotFound("Topic Not Found");

    var subs = context.Subscriptions.Where(s => s.Id == id);

    if (subs.Count() == 0)
        return Results.NotFound("There are no subscriptions for this topic");

    foreach (var sub in subs)
    {
        Message msg = new Message
        {
            TopicMessage = message.TopicMessage,
            SubscriptionId = sub.Id,
            ExpiresAfter = message.ExpiresAfter,
            MessageStatus = message.MessageStatus,
        };

        await context.Messages.AddAsync(msg);
    }
    await context.SaveChangesAsync();

    return Results.Ok("Message has been published!");
});


//Create Subscription

app.MapPost("api/topics/{id}/subscriptions", async (AppDbContext context, int id, Subscription sub) =>
{
    bool topics = await context.Topics.AnyAsync(t => t.Id == id);
    if (!topics)
        return Results.NotFound("Topic Not Found");

    sub.TopicId = id;

    await context.Subscriptions.AddAsync(sub);
    await context.SaveChangesAsync();

    return Results.Created($"api/topics/{id}/subscriptions/{sub.Id}", sub);
});


//Get Subscriber Messages
app.MapGet("api/subscriptions/{id}/messages", async (AppDbContext context, int id) =>
{
    bool subs = await context.Subscriptions.AnyAsync(s => s.Id == id);
    if (!subs)
        return Results.NotFound("Subscription not found");

    var messages = context.Messages.Where(m => m.Id == id && m.MessageStatus != "SENT");

    if (messages.Count() == 0)
        return Results.NotFound("No new Mesasges");

    foreach (var msg in messages)
    {
        msg.MessageStatus = "REQUESTED";
    }

    await context.SaveChangesAsync();

    return Results.Ok(messages);
});

//Acknowledged Messages for Subscriber
//int [] confs = messages ids

app.MapPost("api/subscriptions/{id}/messages", async (AppDbContext context, int id, int[] confs) =>
{
    bool subs = await context.Subscriptions.AnyAsync(s => s.Id == id);
    if (!subs)
        return Results.NotFound("Subscription not found");

    if (confs.Length <= 0)
        return Results.BadRequest();

    int count = 0;

    foreach (int i in confs)
    {
        var msg = context.Messages.FirstOrDefault(m => m.Id == i);

        if (msg != null)
        {
            msg.MessageStatus = "SENT";
            await context.SaveChangesAsync();
            count++;
        }
    }
    return Results.Ok($"Acknowledged {count}/{confs.Length} messages");
});

app.Run();
