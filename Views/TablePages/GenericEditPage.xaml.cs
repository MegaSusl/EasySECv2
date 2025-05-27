using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using EasySECv2.Attributes;
using EasySECv2.Services;
using Microsoft.Maui.Controls;
using Microsoft.Maui.Dispatching;

namespace EasySECv2.Views
{
    [QueryProperty(nameof(ItemId), "id")]
    public partial class GenericEditPage : ContentPage
    {
        // ---------------------------------------------------------------------
        // Supported control types – keep as strings to preserve backward-compat
        // ---------------------------------------------------------------------
        const string CT_PICKER = "Picker";
        const string CT_ENTRY = "Entry";      // default
        const string CT_CHECKBOX = "CheckBox";
        const string CT_DATEPICKER = "DatePicker";

        readonly IGenericEditViewModel _vm;

        public GenericEditPage(IGenericEditViewModel vm)
        {
            InitializeComponent();

            _vm = vm;
            BindingContext = vm;

            BuildFields();
            Title = vm.IsNew ? "Создание новой записи" : "Редактирование";
        }

        public string ItemId
        {
            set
            {
                if (long.TryParse(value, out var id))
                    _ = LoadAndSetTitle(id);
            }
        }

        private async Task LoadAndSetTitle(long id)
        {
            await _vm.LoadExistingAsync(id);
            MainThread.BeginInvokeOnMainThread(() =>
            {
                Title = _vm.IsNew ? "Создание новой записи" : "Редактирование";
            });
        }

        // ---------------------------------------------------------------------
        // ResolveControlType – chooses a control if автор не указал ControlType
        // ---------------------------------------------------------------------
        static string ResolveControlType(PropertyInfo pi, EditableAttribute attr)
        {
            if (!string.IsNullOrWhiteSpace(attr.ControlType))
                return attr.ControlType;

            var type = Nullable.GetUnderlyingType(pi.PropertyType) ?? pi.PropertyType;

            if (type == typeof(bool)) return CT_CHECKBOX;
            if (type == typeof(DateOnly) ||
                type == typeof(DateTime)) return CT_DATEPICKER;
            if (type.IsEnum) return CT_PICKER;

            return CT_ENTRY; // по-умолчанию Entry
        }

        // ---------------------------------------------------------------------
        // BuildFields – динамическая генерация формы
        // ---------------------------------------------------------------------
        void BuildFields()
        {
            FieldsHost.Children.Clear();

            foreach (var o in _vm.Fields)
            {
                if (o is not PropertyInfo pi)
                    continue;

                var attr = pi.GetCustomAttribute<EditableAttribute>();
                if (attr == null)
                    continue;

                // ---------- Заголовок
                var lbl = new Label { Text = attr.Label };
                lbl.Style = (Style)Application.Current.Resources["FormLabelStyle"];

                // ---------- Контрол в зависимости от ControlType
                View ctrl;
                var ctlType = ResolveControlType(pi, attr);

                switch (ctlType)
                {
                    case CT_PICKER:
                        {
                            var picker = new Picker { Title = attr.Label };
                            picker.Style = (Style)Application.Current.Resources["FormPickerStyle"];

                            var lookupProp = _vm.GetType().GetProperty(pi.Name + "s");
                            if (lookupProp != null)
                            {
                                var rawItems = lookupProp.GetValue(_vm) as IEnumerable;
                                if (rawItems != null)
                                {
                                    IList itemsList = rawItems as IList ?? rawItems.Cast<object>().ToList();
                                    picker.ItemsSource = itemsList;
                                    picker.ItemDisplayBinding = new Binding("name");
                                }
                            }

                            picker.SetBinding(Picker.SelectedItemProperty,
                                new Binding($"Item.{pi.Name}", BindingMode.TwoWay));
                            ctrl = picker;
                            break;
                        }

                    case CT_CHECKBOX:
                        {
                            var cb = new CheckBox();
                            if (Application.Current.Resources.TryGetValue("FormCheckBoxStyle", out var s))
                                cb.Style = (Style)s;
                            cb.SetBinding(CheckBox.IsCheckedProperty,
                                new Binding($"Item.{pi.Name}", BindingMode.TwoWay));
                            ctrl = cb;
                            break;
                        }

                    case CT_DATEPICKER:
                        {
                            var dp = new DatePicker();
                            if (Application.Current.Resources.TryGetValue("FormDatePickerStyle", out var s))
                                dp.Style = (Style)s;
                            dp.SetBinding(DatePicker.DateProperty,
                                new Binding($"Item.{pi.Name}", BindingMode.TwoWay));
                            ctrl = dp;
                            break;
                        }

                    default: // Entry
                        {
                            var entry = new Entry();
                            entry.Style = (Style)Application.Current.Resources["FormEntryStyle"];
                            entry.SetBinding(Entry.TextProperty,
                                new Binding($"Item.{pi.Name}", BindingMode.TwoWay));
                            ctrl = entry;
                            break;
                        }
                }

                // ---------- Read-only / disabled для id
                if (pi.Name.Equals("id", StringComparison.OrdinalIgnoreCase))
                {
                    switch (ctrl)
                    {
                        case Entry e: e.IsReadOnly = true; break;
                        case Picker p: p.IsEnabled = false; break;
                        case CheckBox c: c.IsEnabled = false; break;
                        case DatePicker d: d.IsEnabled = false; break;
                    }
                }

                // ---------- Обертка
                var wrapper = new Frame
                {
                    Style = (Style)Application.Current.Resources["FormFieldFrameStyle"],
                    Padding = 0,
                    HasShadow = false
                };
                var stack = new VerticalStackLayout { Spacing = 4, Padding = 8 };
                stack.Children.Add(lbl);
                stack.Children.Add(ctrl);
                wrapper.Content = stack;

                FieldsHost.Children.Add(wrapper);
            }

            // ---------- Кнопки Save / Cancel
            var buttons = new HorizontalStackLayout
            {
                Spacing = 12,
                HorizontalOptions = LayoutOptions.Center
            };
            var save = new Button { Text = "Сохранить" };
            save.SetBinding(Button.CommandProperty, nameof(_vm.SaveCommand));
            var cancel = new Button { Text = "Отмена" };
            cancel.SetBinding(Button.CommandProperty, nameof(_vm.CancelCommand));
            buttons.Children.Add(save);
            buttons.Children.Add(cancel);
            FieldsHost.Children.Add(buttons);

            // ---------- Закрытие страницы
            _vm.CloseRequested += ok =>
            {
                MainThread.BeginInvokeOnMainThread(async () =>
                    await Shell.Current.GoToAsync(".."));
            };
        }
    }
}