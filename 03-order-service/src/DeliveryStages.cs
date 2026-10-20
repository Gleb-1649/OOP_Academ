namespace OrderDesk;

public enum OrderStatus { Preparing, InTransit, Delivered }
public enum OrderKind { Standard, Special }

internal interface IOrderState
{
    OrderStatus Status { get; }
    void EnsureEditable();
    void StartDelivery(CustomerOrder order);
    void CompleteDelivery(CustomerOrder order);
}

internal sealed class PreparingState : IOrderState
{
    public OrderStatus Status => OrderStatus.Preparing;
    public void EnsureEditable() { }
    public void StartDelivery(CustomerOrder order)
    {
        order.LockPrice();
        order.SetState(new InTransitState());
    }
    public void CompleteDelivery(CustomerOrder order) =>
        throw new InvalidOperationException("Сначала передайте заказ в доставку.");
}

internal sealed class InTransitState : IOrderState
{
    public OrderStatus Status => OrderStatus.InTransit;
    public void EnsureEditable() =>
        throw new InvalidOperationException("Заказ уже в доставке, состав менять нельзя.");
    public void StartDelivery(CustomerOrder order) =>
        throw new InvalidOperationException("Заказ уже передан в доставку.");
    public void CompleteDelivery(CustomerOrder order) => order.SetState(new DeliveredState());
}

internal sealed class DeliveredState : IOrderState
{
    public OrderStatus Status => OrderStatus.Delivered;
    public void EnsureEditable() =>
        throw new InvalidOperationException("Выполненный заказ нельзя менять.");
    public void StartDelivery(CustomerOrder order) =>
        throw new InvalidOperationException("Выполненный заказ нельзя отправить повторно.");
    public void CompleteDelivery(CustomerOrder order) =>
        throw new InvalidOperationException("Заказ уже выполнен.");
}
