using System.Runtime.Serialization;

public static class ResultExtensions
{
    public static Result Check(this Result result)
    {
        if (!result.Success)
        {
            var error = result.GetError();
            PreserveStackTrace(error);
            throw error;
        }
        return result;
    }

    public static Result<T> Check<T>(this Result<T> result) where T : class
    {
        if (!result.Success)
        {
            var error = result.GetError();
            PreserveStackTrace(error);
            throw error;
        }
        return result;
    }

    /// <summary>
    /// Preserves the StackTrace, so that the Exception can be catched and re-thrown over multiple layers.
    /// </summary>
    /// <remarks>https://stackoverflow.com/a/2085377</remarks>
    private static void PreserveStackTrace(Exception exception)
    {
        var streamingContext = new StreamingContext(StreamingContextStates.CrossAppDomain);
        var objectManager = new ObjectManager(null, streamingContext);
        var serializationInfo = new SerializationInfo(exception.GetType(), new FormatterConverter());

        exception.GetObjectData(serializationInfo, streamingContext);
        objectManager.RegisterObject(exception, 1, serializationInfo);
        objectManager.DoFixups();
    }
}
