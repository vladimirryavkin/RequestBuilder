using System.Collections.ObjectModel;
using System.Globalization;
using Newtonsoft.Json.Linq;

namespace RequestBuilder.ViewModels
{
    public enum JsonNodeKind { Object, Array, String, Number, Boolean, Null }

    /// <summary>Shared state for one JsonTreeView instance: current selection and an edit notification.</summary>
    public class JsonTreeContext
    {
        public event Action Changed;

        public JsonNodeViewModel SelectedNode { get; private set; }

        public void Select(JsonNodeViewModel node)
        {
            if (ReferenceEquals(SelectedNode, node)) return;
            var previous = SelectedNode;
            SelectedNode = node;
            previous?.RaiseIsSelectedChanged();
            node?.RaiseIsSelectedChanged();
        }

        public void NotifyChanged() => Changed?.Invoke();
    }

    /// <summary>
    /// One node of an editable JSON tree (object, array, or scalar). Mirrors a JToken but owns its own
    /// editable state so the UI never has to fight Newtonsoft.Json.Linq's own change semantics (e.g.
    /// JProperty.Name is read-only). Call ToToken()/BuildRoot() to convert to/from a real JObject.
    /// </summary>
    public class JsonNodeViewModel : BaseViewModel
    {
        private JsonNodeKind kind;
        private string name;
        private string stringValue;
        private bool boolValue;
        private bool isLast = true;
        private bool focusRequested;
        private bool scrollRequested;

        public JsonTreeContext Context { get; }
        public JsonNodeViewModel Parent { get; }
        public ObservableCollection<JsonNodeViewModel> Children { get; } = new();

        public JsonNodeViewModel(JsonTreeContext context, JsonNodeKind kind, string name, JsonNodeViewModel parent,
            string initialStringValue = "", bool initialBoolValue = false)
        {
            Context = context;
            this.kind = kind;
            this.name = name;
            Parent = parent;
            stringValue = kind == JsonNodeKind.Number && string.IsNullOrEmpty(initialStringValue) ? "0" : (initialStringValue ?? "");
            boolValue = initialBoolValue;
            Children.CollectionChanged += (_, __) => RefreshChildPositions();
        }

        public bool IsRoot => Parent == null;

        /// <summary>True while this node is a named property of an Object (as opposed to root or an array element).</summary>
        public bool HasName => Parent != null && Parent.Kind == JsonNodeKind.Object;

        public bool IsArrayElement => Parent != null && Parent.Kind == JsonNodeKind.Array;

        public JsonNodeKind Kind
        {
            get => kind;
            private set
            {
                if (kind == value) return;
                kind = value;
                OnPropertyChanged();
                OnPropertyChanged(nameof(IsObject));
                OnPropertyChanged(nameof(IsArray));
                OnPropertyChanged(nameof(IsString));
                OnPropertyChanged(nameof(IsNumber));
                OnPropertyChanged(nameof(IsBoolean));
                OnPropertyChanged(nameof(IsNull));
                OnPropertyChanged(nameof(IsContainer));
                OnPropertyChanged(nameof(ShowLeafComma));
                OnPropertyChanged(nameof(ShowClosingComma));
            }
        }

        public bool IsObject => Kind == JsonNodeKind.Object;
        public bool IsArray => Kind == JsonNodeKind.Array;
        public bool IsString => Kind == JsonNodeKind.String;
        public bool IsNumber => Kind == JsonNodeKind.Number;
        public bool IsBoolean => Kind == JsonNodeKind.Boolean;
        public bool IsNull => Kind == JsonNodeKind.Null;
        public bool IsContainer => IsObject || IsArray;

        public string Name
        {
            get => name;
            set
            {
                if (name == value) return;
                name = value;
                OnPropertyChanged();
                NotifyChanged();
            }
        }

        public string StringValue
        {
            get => stringValue;
            set
            {
                var v = value ?? "";
                if (stringValue == v) return;
                stringValue = v;
                OnPropertyChanged();
                NotifyChanged();
            }
        }

        public bool BoolValue
        {
            get => boolValue;
            set
            {
                if (boolValue == value) return;
                boolValue = value;
                OnPropertyChanged();
                NotifyChanged();
            }
        }

        public bool IsSelected => Context.SelectedNode == this;
        internal void RaiseIsSelectedChanged() => OnPropertyChanged(nameof(IsSelected));

        public bool IsLast
        {
            get => isLast;
            private set
            {
                if (isLast == value) return;
                isLast = value;
                OnPropertyChanged();
                OnPropertyChanged(nameof(ShowLeafComma));
                OnPropertyChanged(nameof(ShowClosingComma));
            }
        }

        public bool ShowLeafComma => !IsContainer && !IsLast;
        public bool ShowClosingComma => IsContainer && !IsLast;

        /// <summary>Set on a freshly added/edited node so the view can grab keyboard focus once its editor loads.</summary>
        public bool FocusRequested
        {
            get => focusRequested;
            set { focusRequested = value; OnPropertyChanged(); }
        }

        /// <summary>Set on a freshly appended array element so the view can scroll it into view once it loads.</summary>
        public bool ScrollRequested
        {
            get => scrollRequested;
            set { scrollRequested = value; OnPropertyChanged(); }
        }

        private void RefreshChildPositions()
        {
            for (int i = 0; i < Children.Count; i++)
                Children[i].IsLast = i == Children.Count - 1;
        }

        private void NotifyChanged() => Context.NotifyChanged();

        // ---- Tree construction ----

        public static JsonNodeViewModel BuildRoot(JObject source, JsonTreeContext context)
        {
            var root = new JsonNodeViewModel(context, JsonNodeKind.Object, null, null);
            foreach (var prop in source.Properties())
                root.Children.Add(BuildFromToken(prop.Value, prop.Name, root, context));
            return root;
        }

        private static JsonNodeViewModel BuildFromToken(JToken token, string name, JsonNodeViewModel parent, JsonTreeContext context)
        {
            switch (token)
            {
                case JObject o:
                    var objNode = new JsonNodeViewModel(context, JsonNodeKind.Object, name, parent);
                    foreach (var prop in o.Properties())
                        objNode.Children.Add(BuildFromToken(prop.Value, prop.Name, objNode, context));
                    return objNode;
                case JArray a:
                    var arrNode = new JsonNodeViewModel(context, JsonNodeKind.Array, name, parent);
                    foreach (var item in a)
                        arrNode.Children.Add(BuildFromToken(item, null, arrNode, context));
                    return arrNode;
                case JValue v:
                    return BuildFromValue(v, name, parent, context);
                default:
                    return new JsonNodeViewModel(context, JsonNodeKind.String, name, parent, token?.ToString() ?? "");
            }
        }

        private static JsonNodeViewModel BuildFromValue(JValue v, string name, JsonNodeViewModel parent, JsonTreeContext context)
        {
            switch (v.Type)
            {
                case JTokenType.Integer:
                    return new JsonNodeViewModel(context, JsonNodeKind.Number, name, parent,
                        Convert.ToInt64(v.Value, CultureInfo.InvariantCulture).ToString(CultureInfo.InvariantCulture));
                case JTokenType.Float:
                    return new JsonNodeViewModel(context, JsonNodeKind.Number, name, parent,
                        Convert.ToDouble(v.Value, CultureInfo.InvariantCulture).ToString(CultureInfo.InvariantCulture));
                case JTokenType.Boolean:
                    return new JsonNodeViewModel(context, JsonNodeKind.Boolean, name, parent, "", (bool)v.Value);
                case JTokenType.Null:
                case JTokenType.Undefined:
                    return new JsonNodeViewModel(context, JsonNodeKind.Null, name, parent);
                default:
                    return new JsonNodeViewModel(context, JsonNodeKind.String, name, parent, v.Value?.ToString() ?? "");
            }
        }

        public JToken ToToken()
        {
            switch (Kind)
            {
                case JsonNodeKind.Object:
                    var obj = new JObject();
                    foreach (var child in Children)
                        obj[child.Name ?? ""] = child.ToToken();
                    return obj;
                case JsonNodeKind.Array:
                    var arr = new JArray();
                    foreach (var child in Children)
                        arr.Add(child.ToToken());
                    return arr;
                case JsonNodeKind.String:
                    return new JValue(StringValue ?? "");
                case JsonNodeKind.Number:
                    return new JValue(ParseNumber(StringValue));
                case JsonNodeKind.Boolean:
                    return new JValue(BoolValue);
                default:
                    return JValue.CreateNull();
            }
        }

        private static object ParseNumber(string s)
        {
            if (long.TryParse(s, NumberStyles.Integer, CultureInfo.InvariantCulture, out var l)) return l;
            if (double.TryParse(s, NumberStyles.Float, CultureInfo.InvariantCulture, out var d)) return d;
            return 0L;
        }

        // ---- Editing operations (invoked from context menu items) ----

        public JsonNodeViewModel AddChild(JsonNodeKind kind)
        {
            if (!IsContainer) return null;
            var childName = IsObject ? GenerateUniquePropertyName() : null;
            var child = new JsonNodeViewModel(Context, kind, childName, this);
            Children.Add(child);
            child.FocusRequested = true;
            NotifyChanged();
            return child;
        }

        private string GenerateUniquePropertyName()
        {
            var existing = new HashSet<string>(Children.Select(c => c.Name));
            const string baseName = "newProperty";
            if (!existing.Contains(baseName)) return baseName;
            var i = 1;
            while (existing.Contains(baseName + i)) i++;
            return baseName + i;
        }

        public void Delete()
        {
            if (IsRoot)
            {
                Children.Clear();
                NotifyChanged();
                return;
            }
            if (Context.SelectedNode == this) Context.Select(null);
            Parent.Children.Remove(this);
            NotifyChanged();
        }

        public void ClearValues()
        {
            foreach (var child in Children)
                child.ClearOwnValue();
            NotifyChanged();
        }

        private void ClearOwnValue()
        {
            switch (Kind)
            {
                case JsonNodeKind.String:
                    stringValue = "";
                    OnPropertyChanged(nameof(StringValue));
                    break;
                case JsonNodeKind.Number:
                    stringValue = "0";
                    OnPropertyChanged(nameof(StringValue));
                    break;
                case JsonNodeKind.Boolean:
                    boolValue = false;
                    OnPropertyChanged(nameof(BoolValue));
                    break;
                case JsonNodeKind.Array:
                    Children.Clear();
                    break;
                case JsonNodeKind.Object:
                    foreach (var child in Children)
                        child.ClearOwnValue();
                    break;
            }
        }

        public void ChangeType(JsonNodeKind newKind)
        {
            Kind = newKind;
            switch (newKind)
            {
                case JsonNodeKind.String:
                    stringValue = "";
                    break;
                case JsonNodeKind.Number:
                    stringValue = "0";
                    break;
                case JsonNodeKind.Boolean:
                    boolValue = false;
                    break;
                case JsonNodeKind.Object:
                case JsonNodeKind.Array:
                    Children.Clear();
                    break;
            }
            OnPropertyChanged(nameof(StringValue));
            OnPropertyChanged(nameof(BoolValue));
            NotifyChanged();
        }

        public void CopyNext()
        {
            if (!IsArrayElement) return;
            var idx = Parent.Children.IndexOf(this);
            var clone = DeepClone(Parent);
            Parent.Children.Insert(idx + 1, clone);
            Context.Select(clone);
            NotifyChanged();
        }

        public void CopyToEnd()
        {
            if (!IsArrayElement) return;
            var clone = DeepClone(Parent);
            Parent.Children.Add(clone);
            Context.Select(clone);
            clone.ScrollRequested = true;
            NotifyChanged();
        }

        private JsonNodeViewModel DeepClone(JsonNodeViewModel newParent)
        {
            var clone = new JsonNodeViewModel(Context, Kind, Name, newParent, StringValue, BoolValue);
            foreach (var child in Children)
                clone.Children.Add(child.DeepClone(clone));
            return clone;
        }
    }
}
