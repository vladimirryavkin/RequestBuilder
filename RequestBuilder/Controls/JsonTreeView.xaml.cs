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
    /// </summary>
    public partial class JsonTreeView : UserControl
    {
        private JsonTreeContext context;

        public event Action Changed;

        public JsonTreeView()
        {
            InitializeComponent();
            DataContextChanged += OnDataContextChanged;
        }

        public JObject GetJson() => (RootPresenter.Content as JsonNodeViewModel)?.ToToken() as JObject;

        private void OnDataContextChanged(object sender, DependencyPropertyChangedEventArgs e)
        {
            if (e.NewValue is JObject jObject)
            {
                context = new JsonTreeContext();
                context.Changed += () => Changed?.Invoke();
                RootPresenter.Content = JsonNodeViewModel.BuildRoot(jObject, context);
            }
            else
            {
                context = null;
                RootPresenter.Content = null;
            }
        }

        private void Row_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            if (sender is FrameworkElement fe && fe.DataContext is JsonNodeViewModel node)
            {
                node.Context.Select(node);
                e.Handled = true;
            }
        }

        private void Row_Loaded(object sender, RoutedEventArgs e)
        {
            if (sender is not FrameworkElement fe || fe.DataContext is not JsonNodeViewModel node) return;
            if (!node.ScrollRequested) return;
            node.ScrollRequested = false;
            fe.Dispatcher.BeginInvoke(DispatcherPriority.Loaded, new Action(() => fe.BringIntoView()));
        }

        private void Editor_Loaded(object sender, RoutedEventArgs e)
        {
            if (sender is not FrameworkElement fe || fe.DataContext is not JsonNodeViewModel node) return;
            if (!node.FocusRequested) return;
            node.FocusRequested = false;
            fe.Dispatcher.BeginInvoke(DispatcherPriority.Input, new Action(() =>
            {
                fe.Focus();
                if (fe is TextBox tb) tb.SelectAll();
            }));
        }

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
