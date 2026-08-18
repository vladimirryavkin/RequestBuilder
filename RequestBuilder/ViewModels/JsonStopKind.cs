namespace RequestBuilder.ViewModels
{
    /// <summary>
    /// Which editable "stop" of a node is being referred to - a node contributes a Name stop only when
    /// it's a named object property, always contributes a Value stop (the leaf editor, or the opening
    /// brace/bracket for a container), and contributes a Close stop only when it's a container (the
    /// closing brace/bracket). These are exactly the keyboard focus/tab stops the view renders.
    /// </summary>
    public enum JsonStopKind { Name, Value, Close }
}
