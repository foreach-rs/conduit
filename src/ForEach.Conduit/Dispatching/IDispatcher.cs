namespace ForEach.Conduit.Dispatching;

/// <summary>
/// Composite dispatcher interface. Inject <see cref="IDispatcher"/> when you need full access,
/// or inject a narrower interface (<see cref="ICommandDispatcher"/>, <see cref="IQueryDispatcher"/>,
/// <see cref="IEventPublisher"/>) to communicate intent and keep dependencies minimal.
/// </summary>
public interface IDispatcher : ICommandDispatcher, IQueryDispatcher, IEventPublisher
{
}