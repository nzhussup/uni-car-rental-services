using CarService.Common;

namespace CarService.Services;

public interface IMessageProducer
{
    Task SendMaintenanceInfoMessageAsync(MaintainanceStartInfo message);

    Task SendCarInfoMessageAsync(CarInfo message);
}