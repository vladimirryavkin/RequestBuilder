namespace RequestBuilder.ViewModels
{
    /// <summary>Shared state for one JsonTreeView instance: just the edit notification for now.</summary>
    public class JsonTreeContext
    {
        public event Action Changed;

        public void NotifyChanged() => Changed?.Invoke();
    }
}
