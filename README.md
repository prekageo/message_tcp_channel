## Message TCP channel for .NET

Recently, I had the task to develop a simple server / client application that should work over the Internet. The communication needs were very simple and I had to use as much as possible lightweight components. So, I had to develop a simple TCP server / client that would handle the communication. For simplicity’s sake I decided to serialize the messages transmitted to XML, although this could easily change to something less verbose and more efficient. A better alternative would be to use Google’s [Protocol Buffers](https://developers.google.com/protocol-buffers/).

The component is written in Visual Basic .NET.

Using the component is rather simple. The following example in C# demonstrates a client interacting with a server:

```csharp
class Program
{
  static void Main(string[] args)
  {
    var server = new MyServer();
    server.StartServer();
    var client = new MessageTcpClient<S2C, C2S>(new TcpClient("127.0.0.1", 8000));
    client.Close += (client_) =>
    {
      Console.WriteLine("Server closed connection.");
    };

    client.MessageReceived += (client_, msg) =>
    {
      Console.WriteLine("Server send message: {0}", msg.FromServer);
    };
    client.SendMessage(new C2S() { FromClient = "abc" });
    Thread.Sleep(1000);
  }
}

class MyServer : MessageServer<C2S, S2C>
{
  protected override void OnNewClient(MessageTcpClient<C2S, S2C> client)
  {
    client.Close += (client_) =>
    {
      Console.WriteLine("Client closed connection.");
    };

    client.MessageReceived += (client_, msg) =>
    {
      Console.WriteLine("Client send message: {0}", msg.FromClient);
      client.SendMessage(new S2C() { FromServer = "def" });
      client.Disconnect();
    };
  }
}

public class C2S
{
  public string FromClient;
}

public class S2C
{
  public string FromServer;
}
```
