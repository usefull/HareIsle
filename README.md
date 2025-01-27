# HareIsle
IPC .NET library powered by RabbitMQ. Provides RPC query, broadcast, and queue processing functionality.
## Using
Install NuGet package:
```
dotnet add package HareIsle
```
Add using derective:
```cs
using RabbitMQ.Client;
using Usefull.HareIsle;
```
Create RabbitMq connection factory:
```cs
ConnectionFactory connectionFactory = new()
{
    Uri = new Uri("amqp://user:pass@rabbithost:5672/"),
    ConsumerDispatchConcurrency = 10,
    AutomaticRecoveryEnabled = true,
    TopologyRecoveryEnabled = true,
    RequestedHeartbeat = TimeSpan.FromSeconds(10)
};
```
### Scenario: broadcasting
Create the broadcast message handler:
```cs
// RabbitMq connection
using var connection = await connectionFactory.CreateConnectionAsync();
// Message handler
using var handler = new BroadcastHandler<SomeMessage>(
  "Handler actor ID",  // ID of the actor on whose behalf the message is processed
  connection,          // RabbitMq connection
  "Broadcast ID",      // ID of the actor on whose behalf messages are broadcast
  message =>           // handling function           
  {
    // do some handling
  });
Console.ReadKey();    // message listening stops when press a key
```
Publish the broadcast message:
```cs
// Create the broadcast publisher
using var emitter = new Emitter("Broadcast ID", connection);
// Publish the message
await emitter.BroadcastAsync(new SomeMessage
{
    Message = "some text",
    Value = 123
});
```
### Scenario: queue handling
Create the queue handler:
```cs
// RabbitMq connection
using var connection = await connectionFactory.CreateConnectionAsync();
// Message handler
using var handler = new QueueHandler<SomeMessage>(
  "Handler actor ID",  // ID of the actor on whose behalf the message is processed
  connection,          // RabbitMq connection
  "QueueName",         // queue name
  5,                   // concurrency factor
  message =>           // handling function           
  {
    // do some handling
  });
Console.ReadKey();    // queue listening stops when press a key
```
Place the message in the queue:
```cs
using var emitter = new Emitter("SomeActorID", connection);  // create the emmiter
await emitter.DeclareQueueAsync("QueueName");  // declare the queue
await emitter.EnqueueAsync("QueueName", new SomeMessage  // send the message
{
    Text = "some text"
});
```
### Scenario: RPC
Create the RPC handler:
```cs
// RabbitMq connection
using var connection = await connectionFactory.CreateConnectionAsync();
// RPC handler
using var handler = new RpcHandler<Request, Response>(
  "Actor ID",          // ID of the actor on whose behalf the RPC is processed
  connection,          // RabbitMq connection
  5,                   // concurrency factor
  reques =>            // RPC handling function           
  {
    // do some handling
  });
Console.ReadKey();    // message listening stops when press a key
```
Make the RPC and get a response:
```cs
using var rpcClient = new RpcClient("ClientID", connection);
var request = new Request
{
    Value = 123,
    Text = "some text"
};
var response = await rpcClient.CallAsync<Request, Response>("Actor ID", request);
```
