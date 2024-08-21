public class CustomEndpointBehavior : IEndpointBehavior
{
    public void AddBindingParameters(ServiceEndpoint endpoint, BindingParameterCollection bindingParameters)
    {
    }

    public void ApplyClientBehavior(ServiceEndpoint endpoint, ClientRuntime clientRuntime)
    {
        clientRuntime.ClientMessageInspectors.Add(new CustomMessageInspector());
    }

    public void ApplyDispatchBehavior(ServiceEndpoint endpoint, EndpointDispatcher endpointDispatcher)
    {
    }

    public void Validate(ServiceEndpoint endpoint)
    {
    }
}

public class CustomMessageInspector : IClientMessageInspector
{
    private readonly string _logFilePath = "wcf_log.txt";

    public void AfterReceiveReply(ref Message reply, object correlationState)
    {
        LogToFile("Response: " + reply.ToString());
    }

    public object BeforeSendRequest(ref Message request, IClientChannel channel)
    {
        LogToFile("Request: " + request.ToString());

        return null;
    }

    private void LogToFile(string message)
    {
        File.AppendAllText(_logFilePath, message + Environment.NewLine);
    }
}

public class Program
{
    static void Main(string[] args)
    {
        var client = new YourServiceClient();

        // Add custom behavior to log requests
        client.Endpoint.EndpointBehaviors.Add(new CustomEndpointBehavior());

        // Make a service call
        var result = client.YourServiceMethod();

        // Optionally, close the client
        client.Close();
    }
}