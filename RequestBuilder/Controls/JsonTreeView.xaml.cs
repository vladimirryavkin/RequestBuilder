using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Threading;
using Newtonsoft.Json.Linq;
using RequestBuilder.ViewModels;

namespace RequestBuilder.Controls
{
    /// <summary>
    /// Editable tree view for a JSON object. Bind a JObject via DataContext; the control builds its own
    /// editable JsonNodeViewModel tree from it and raises Changed whenever the user edits anything.
    /// Call GetJson() to read the current value back out.
    ///
    /// Keyboard model: every node contributes up to three focusable "stops" (Name, Value, Close - see
    /// JsonStopKind). Tab/Shift+Tab walk the full flattened stop list depth-first ("forward and inward").
    /// Up/Down move to the same stop of the previous/next sibling. Enter and Shift+Enter behave contextually
    /// per stop (see HandleEnter/HandleShiftEnter). A small registry maps (node, stop) to its live element
    /// so navigation between already-rendered stops can focus immediately; brand-new or newly-shown stops
    /// (from an edit) are instead requested via JsonNodeViewModel.PendingFocusStop and claimed once loaded.
    /// </summary>
    public partial class JsonTreeView : UserControl
    {
        private JsonTreeContext context;
        private JsonNodeViewModel rootNode;
        private readonly Dictionary<(JsonNodeViewModel Node, JsonStopKind Stop), FrameworkElement> stopElements = new();

        public event Action Changed;

        public JsonTreeView()
        {
            InitializeComponent();
            DataContextChanged += OnDataContextChanged;
        }

        public JObject GetJson() => rootNode?.ToToken() as JObject;

        private void OnDataContextChanged(object sender, DependencyPropertyChangedEventArgs e)
        {
            stopElements.Clear();
            if (e.NewValue is JObject jObject)
            {
                context = new JsonTreeContext();
                context.Changed += () => Changed?.Invoke();
                rootNode = JsonNodeViewModel.BuildRoot(jObject, context);
                RootPresenter.Content = rootNode;
            }
            else
            {
                context = null;
                rootNode = null;
                RootPresenter.Content = null;
            }
        }

        // ---- Stop registry & focus bookkeeping ----

        // A node's Value stop has several mutually-exclusive possible elements (open brace, bracket,
        // string/number box, checkbox, null box) that all live in the visual tree at once with only one
        // ever Visible at a time. Loaded fires for all of them regardless of visibility, so registration
        // (and re-registration on a Kind change) must only happen for the one that's actually shown -
        // otherwise whichever happened to load last would silently win the registry slot.

        private void Stop_Loaded(object sender, RoutedEventArgs e)
        {
            var fe = sender as FrameworkElement;
            if (fe == null) return;
            // IsVisibleChanged is a plain CLR event (not a RoutedEvent), so it can't be wired via an
            // EventSetter in a Style like Loaded/Unloaded/PreviewKeyDown - subscribe by hand instead.
            // The -= guards against double subscription if Loaded ever fires more than once.
            fe.IsVisibleChanged -= Stop_IsVisibleChanged;
            fe.IsVisibleChanged += Stop_IsVisibleChanged;
            if (fe.IsVisible) Register(fe);
            TryConsumePendingFocus(fe);
        }

        private void Stop_Unloaded(object sender, RoutedEventArgs e)
        {
            var fe = sender as FrameworkElement;
            if (fe == null) return;
            fe.IsVisibleChanged -= Stop_IsVisibleChanged;
            Unregister(fe);
        }

        private void Stop_IsVisibleChanged(object sender, DependencyPropertyChangedEventArgs e)
        {
            if (e.NewValue is not true) return;
            var fe = sender as FrameworkElement;
            Register(fe);
            TryConsumePendingFocus(fe);
        }

        private void Register(FrameworkElement fe)
        {
            if (fe?.DataContext is not JsonNodeViewModel node || fe.Tag is not JsonStopKind stop) return;
            stopElements[(node, stop)] = fe;
        }

        private void Unregister(FrameworkElement fe)
        {
            if (fe?.DataContext is not JsonNodeViewModel node || fe.Tag is not JsonStopKind stop) return;
            if (stopElements.TryGetValue((node, stop), out var registered) && ReferenceEquals(registered, fe))
                stopElements.Remove((node, stop));
        }

        private void TryConsumePendingFocus(FrameworkElement fe)
        {
            if (fe == null || fe.DataContext is not JsonNodeViewModel node || fe.Tag is not JsonStopKind stop) return;
            if (node.PendingFocusStop != stop || !fe.IsVisible) return;
            node.PendingFocusStop = null;
            FocusElement(fe);
        }

        private bool TryFocusExisting(JsonNodeViewModel node, JsonStopKind stop)
        {
            if (node == null || !stopElements.TryGetValue((node, stop), out var fe) || !fe.IsVisible) return false;
            FocusElement(fe);
            return true;
        }

        private static void FocusElement(FrameworkElement fe)
        {
            fe.Dispatcher.BeginInvoke(DispatcherPriority.Input, new Action(() =>
            {
                fe.Focus();
                fe.BringIntoView();
                if (fe is TextBox { IsReadOnly: false } tb) tb.SelectAll();
            }));
        }

        // ---- Keyboard navigation & editing shortcuts ----

        private void Stop_PreviewKeyDown(object sender, KeyEventArgs e)
        {
            if (sender is not FrameworkElement fe || fe.DataContext is not JsonNodeViewModel node || fe.Tag is not JsonStopKind stop)
                return;

            var shift = (Keyboard.Modifiers & ModifierKeys.Shift) != 0;
            switch (e.Key)
            {
                case Key.Enter when shift:
                    HandleShiftEnter(node, stop);
                    e.Handled = true;
                    break;
                case Key.Enter:
                    HandleEnter(node, stop);
                    e.Handled = true;
                    break;
                case Key.Tab:
                    MoveTab(node, stop, shift ? -1 : 1);
                    e.Handled = true;
                    break;
                case Key.Up:
                    MoveSibling(node, stop, -1);
                    e.Handled = true;
                    break;
                case Key.Down:
                    MoveSibling(node, stop, 1);
                    e.Handled = true;
                    break;
            }
        }

        private static void HandleEnter(JsonNodeViewModel node, JsonStopKind stop)
        {
            switch (stop)
            {
                case JsonStopKind.Name when node.HasName:
                    node.AddSiblingStringProperty();
                    break;
                case JsonStopKind.Value when node.IsObject:
                    node.AddChild(JsonNodeKind.String);
                    break;
                case JsonStopKind.Value when node.IsArray:
                    node.InsertCommonObjectAtStart();
                    break;
                case JsonStopKind.Value when node.HasName:
                    node.AddSiblingStringProperty();
                    break;
                case JsonStopKind.Close when node.IsObject && node.IsArrayElement:
                    node.InsertCommonObjectNext();
                    break;
            }
        }

        private static void HandleShiftEnter(JsonNodeViewModel node, JsonStopKind stop)
        {
            if (stop == JsonStopKind.Name || stop == JsonStopKind.Value)
                node.CycleType();
        }

        private void MoveTab(JsonNodeViewModel node, JsonStopKind stop, int direction)
        {
            if (rootNode == null) return;
            var stops = JsonNodeViewModel.GetAllStops(rootNode);
            var idx = stops.FindIndex(s => s.Node == node && s.Stop == stop);
            if (idx == -1) return;
            var nextIdx = idx + direction;
            if (nextIdx < 0 || nextIdx >= stops.Count) return;
            var (nextNode, nextStop) = stops[nextIdx];
            TryFocusExisting(nextNode, nextStop);
        }

        private void MoveSibling(JsonNodeViewModel node, JsonStopKind stop, int direction)
        {
            var parent = node.Parent;
            if (parent == null) return;
            var idx = parent.Children.IndexOf(node);
            var targetIdx = idx + direction;
            if (targetIdx < 0 || targetIdx >= parent.Children.Count) return;
            var target = parent.Children[targetIdx];
            var targetStop = stop == JsonStopKind.Close && !target.IsContainer ? JsonStopKind.Value : stop;
            TryFocusExisting(target, targetStop);
        }

        // ---- Context menu ----

        private void Row_ContextMenuOpening(object sender, ContextMenuEventArgs e)
        {
            if (sender is not FrameworkElement fe || fe.ContextMenu == null) return;
            if (fe.DataContext is not JsonNodeViewModel node)
            {
                e.Handled = true;
                return;
            }

            var menu = fe.ContextMenu;
            menu.Items.Clear();

            if (node.IsObject)
            {
                AddMenuItem(menu, "Add string property", () => node.AddChild(JsonNodeKind.String));
                AddMenuItem(menu, "Add number property", () => node.AddChild(JsonNodeKind.Number));
                AddMenuItem(menu, "Add null property", () => node.AddChild(JsonNodeKind.Null));
                AddMenuItem(menu, "Add boolean property", () => node.AddChild(JsonNodeKind.Boolean));
                AddMenuItem(menu, "Add array", () => node.AddChild(JsonNodeKind.Array));
                menu.Items.Add(new Separator());
                AddMenuItem(menu, "Delete object", node.Delete);
                AddMenuItem(menu, "Clear all values", node.ClearValues);
                if (node.IsArrayElement)
                {
                    menu.Items.Add(new Separator());
                    AddMenuItem(menu, "Copy next", node.CopyNext);
                    AddMenuItem(menu, "Copy to the end", node.CopyToEnd);
                }
            }
            else if (node.IsArray)
            {
                AddMenuItem(menu, "Add string element", () => node.AddChild(JsonNodeKind.String));
                AddMenuItem(menu, "Add number element", () => node.AddChild(JsonNodeKind.Number));
                AddMenuItem(menu, "Add null element", () => node.AddChild(JsonNodeKind.Null));
                AddMenuItem(menu, "Add boolean element", () => node.AddChild(JsonNodeKind.Boolean));
                AddMenuItem(menu, "Add array element", () => node.AddChild(JsonNodeKind.Array));
                AddMenuItem(menu, "Add object element", () => node.AddChild(JsonNodeKind.Object));
                AddMenuItem(menu, "Add common object", () => node.AddCommonObjectChild());
                menu.Items.Add(new Separator());
                AddMenuItem(menu, "Delete array", node.Delete);
                AddMenuItem(menu, "Clear all values", node.ClearValues);
            }
            else
            {
                AddMenuItem(menu, "Delete property", node.Delete);
                menu.Items.Add(new Separator());
                AddMenuItem(menu, "Change to array", () => node.ChangeType(JsonNodeKind.Array));
                AddMenuItem(menu, "Change to object", () => node.ChangeType(JsonNodeKind.Object));
                AddMenuItem(menu, "Change to string", () => node.ChangeType(JsonNodeKind.String));
                AddMenuItem(menu, "Change to number", () => node.ChangeType(JsonNodeKind.Number));
                AddMenuItem(menu, "Change to boolean", () => node.ChangeType(JsonNodeKind.Boolean));
                AddMenuItem(menu, "Change to null", () => node.ChangeType(JsonNodeKind.Null));
            }
        }

        private static void AddMenuItem(ContextMenu menu, string header, Action action)
        {
            var item = new MenuItem { Header = header };
            item.Click += (_, __) => action();
            menu.Items.Add(item);
        }
    }
}
