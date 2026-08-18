using System.Collections.ObjectModel;
using System.Globalization;
using Newtonsoft.Json.Linq;

namespace RequestBuilder.ViewModels
{
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

        /// <summary>
        /// Set by editing operations that create or retype a node, naming which of its stops the view
        /// should grab keyboard focus for once that stop's element loads or becomes visible. Not used for
        /// navigating between already-visible stops (Tab/Shift+Tab/Up/Down) - those focus directly.
        /// </summary>
        public JsonStopKind? PendingFocusStop { get; set; }

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

        // ---- Keyboard traversal ----

        /// <summary>
        /// Every focusable stop in this node's subtree, in the exact order the tree renders them: a
        /// container's own opening brace/bracket comes before its children, which come before its closing
        /// brace/bracket ("forward and inward" - this is the Tab order). Recomputed on demand since the
        /// tree shape changes constantly under editing; cheap enough at realistic JSON sizes.
        /// </summary>
        public static List<(JsonNodeViewModel Node, JsonStopKind Stop)> GetAllStops(JsonNodeViewModel root)
        {
            var list = new List<(JsonNodeViewModel Node, JsonStopKind Stop)>();
            Collect(root, list);
            return list;
        }

        private static void Collect(JsonNodeViewModel node, List<(JsonNodeViewModel Node, JsonStopKind Stop)> list)
        {
            if (node.HasName) list.Add((node, JsonStopKind.Name));
            list.Add((node, JsonStopKind.Value));
            if (node.IsContainer)
            {
                foreach (var child in node.Children)
                    Collect(child, list);
                list.Add((node, JsonStopKind.Close));
            }
        }

        // ---- Editing operations (invoked from context menu items and keyboard shortcuts) ----

        public JsonNodeViewModel AddChild(JsonNodeKind kind)
        {
            if (!IsContainer) return null;
            var childName = IsObject ? GenerateUniquePropertyName() : null;
            var child = new JsonNodeViewModel(Context, kind, childName, this);
            Children.Add(child);
            child.PendingFocusStop = child.HasName ? JsonStopKind.Name : JsonStopKind.Value;
            NotifyChanged();
            return child;
        }

        /// <summary>
        /// Object-property Enter flow: commits the current property and adds a new blank string property
        /// right after it in the same parent object, focused on its name so the key can be typed next.
        /// </summary>
        public JsonNodeViewModel AddSiblingStringProperty()
        {
            if (Parent == null || Parent.Kind != JsonNodeKind.Object) return null;
            var idx = Parent.Children.IndexOf(this);
            var sibling = new JsonNodeViewModel(Context, JsonNodeKind.String, Parent.GenerateUniquePropertyName(), Parent);
            Parent.Children.Insert(idx + 1, sibling);
            sibling.PendingFocusStop = JsonStopKind.Name;
            Parent.NotifyChanged();
            return sibling;
        }

        /// <summary>Array-only: adds a new object element (append) scaffolded from the array's common property shape.</summary>
        public JsonNodeViewModel AddCommonObjectChild()
        {
            if (!IsArray) return null;
            var common = BuildCommonObject();
            Children.Add(common);
            NotifyChanged();
            return common;
        }

        /// <summary>Array-only ('[' Enter): inserts a new common-shaped object as the array's first element.</summary>
        public JsonNodeViewModel InsertCommonObjectAtStart()
        {
            if (!IsArray) return null;
            var common = BuildCommonObject();
            Children.Insert(0, common);
            common.PendingFocusStop = JsonStopKind.Value;
            NotifyChanged();
            return common;
        }

        /// <summary>Object-array-element-only ('}' Enter): inserts a new common-shaped object right after this one.</summary>
        public JsonNodeViewModel InsertCommonObjectNext()
        {
            if (!IsArrayElement || Kind != JsonNodeKind.Object) return null;
            var idx = Parent.Children.IndexOf(this);
            var common = Parent.BuildCommonObject();
            Parent.Children.Insert(idx + 1, common);
            common.PendingFocusStop = JsonStopKind.Value;
            Parent.NotifyChanged();
            return common;
        }

        /// <summary>
        /// Array-only: builds (without inserting) an object scaffolded with the properties common to every
        /// other object element already in this array (names + types, default/empty values) - a ready-made
        /// shape matching the array's existing objects rather than a blank {}.
        /// </summary>
        private JsonNodeViewModel BuildCommonObject()
        {
            var objectSiblings = Children.Where(c => c.IsObject).ToList();
            var common = new JsonNodeViewModel(Context, JsonNodeKind.Object, null, this);

            if (objectSiblings.Count > 0)
            {
                var commonNames = objectSiblings
                    .Select(o => new HashSet<string>(o.Children.Select(c => c.Name)))
                    .Aggregate((a, b) => { a.IntersectWith(b); return a; });

                foreach (var propName in objectSiblings[0].Children.Select(c => c.Name).Where(commonNames.Contains))
                {
                    var template = objectSiblings[0].Children.First(c => c.Name == propName);
                    common.Children.Add(new JsonNodeViewModel(Context, template.Kind, propName, common,
                        template.Kind == JsonNodeKind.Number ? "0" : ""));
                }
            }

            return common;
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
            PendingFocusStop = JsonStopKind.Value;
            NotifyChanged();
        }

        /// <summary>Shift+Enter flow: string -> number -> null -> boolean -> object -> array -> (back to) string.</summary>
        public void CycleType()
        {
            if (IsRoot) return;
            ChangeType(Kind switch
            {
                JsonNodeKind.String => JsonNodeKind.Number,
                JsonNodeKind.Number => JsonNodeKind.Null,
                JsonNodeKind.Null => JsonNodeKind.Boolean,
                JsonNodeKind.Boolean => JsonNodeKind.Object,
                JsonNodeKind.Object => JsonNodeKind.Array,
                JsonNodeKind.Array => JsonNodeKind.String,
                _ => JsonNodeKind.String,
            });
        }

        public void CopyNext()
        {
            if (!IsArrayElement) return;
            var idx = Parent.Children.IndexOf(this);
            var clone = DeepClone(Parent);
            Parent.Children.Insert(idx + 1, clone);
            clone.PendingFocusStop = JsonStopKind.Value;
            NotifyChanged();
        }

        public void CopyToEnd()
        {
            if (!IsArrayElement) return;
            var clone = DeepClone(Parent);
            Parent.Children.Add(clone);
            clone.PendingFocusStop = JsonStopKind.Value;
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
