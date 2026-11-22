namespace ERP.WorkflowwServices.API.Services;

public sealed class EmailDeliveryException(string message, Exception? innerException = null)
    : Exception(message, innerException)
{
}
