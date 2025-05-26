using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using EasySECv2.Attributes;
using EasySECv2.Services;
using Microsoft.Maui.Controls;
using Microsoft.Maui.Dispatching;

namespace EasySECv2.Views
{
    [QueryProperty(nameof(ItemId), "id")]
    public partial class GenericEditPage : ContentPage
    {
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

                var lbl = new Label { Text = attr.Label };
                lbl.Style = (Style)Application.Current.Resources["FormLabelStyle"];

                View ctrl;
                if (attr.ControlType == "Picker")
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
                }
                else
                {
                    var entry = new Entry();
                    entry.Style = (Style)Application.Current.Resources["FormEntryStyle"];
                    entry.SetBinding(Entry.TextProperty,
                        new Binding($"Item.{pi.Name}", BindingMode.TwoWay));
                    ctrl = entry;
                }

                if (pi.Name.Equals("id", StringComparison.OrdinalIgnoreCase))
                {
                    if (ctrl is Entry e) e.IsReadOnly = true;
                    else if (ctrl is Picker p) p.IsEnabled = false;
                }

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

            _vm.CloseRequested += ok =>
            {
                MainThread.BeginInvokeOnMainThread(async () =>
                    await Shell.Current.GoToAsync(".."));
            };
        }
    }
}
