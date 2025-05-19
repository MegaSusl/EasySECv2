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
        }

        /// <summary>
        /// ��� ��������� Shell.Current.GoToAsync("GenericEditPage?id=123")
        /// ���� ����� ��������� id � ������� ���������� � ��������� ������������ ������.
        /// </summary>
        public string ItemId
        {
            set
            {
                if (long.TryParse(value, out var id))
                    _ = _vm.LoadExistingAsync(id);
            }
        }

        void BuildFields()
        {
            FieldsHost.Children.Clear();

            // vm.Fields ��������� IList � �������� PropertyInfo
            foreach (var o in _vm.Fields)
            {
                if (o is not PropertyInfo pi)
                    continue;

                var attr = pi.GetCustomAttribute<EditableAttribute>();
                if (attr == null)
                    continue;

                // 1) Label
                var lbl = new Label { Text = attr.Label };
                lbl.Style = (Style)Application.Current.Resources["FormLabelStyle"];

                // 2) Control
                View ctrl;
                if (attr.ControlType == "Picker")
                {
                    var picker = new Picker { Title = attr.Label };
                    picker.Style = (Style)Application.Current.Resources["FormPickerStyle"];

                    // lookup: ���� �������� vm, �������� "Groups", "Orientations" � �.�.
                    var lookupProp = _vm.GetType().GetProperty(pi.Name + "s");
                    if (lookupProp != null)
                    {
                        var rawItems = lookupProp.GetValue(_vm) as IEnumerable;
                        if (rawItems != null)
                        {
                            IList itemsList = rawItems as IList
                                ?? rawItems.Cast<object>().ToList();
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

                // 3) ������
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

                // 4) ��������� � FieldsHost
                FieldsHost.Children.Add(wrapper);
            }

            // 5) ������ ���������/������
            var buttons = new HorizontalStackLayout
            {
                Spacing = 12,
                HorizontalOptions = LayoutOptions.Center
            };
            var save = new Button { Text = "���������" };
            save.SetBinding(Button.CommandProperty, nameof(_vm.SaveCommand));
            var cancel = new Button { Text = "������" };
            cancel.SetBinding(Button.CommandProperty, nameof(_vm.CancelCommand));
            buttons.Children.Add(save);
            buttons.Children.Add(cancel);
            FieldsHost.Children.Add(buttons);

            // 6) ��� �������� VM ������������ �����
            _vm.CloseRequested += ok =>
            {
                MainThread.BeginInvokeOnMainThread(async () =>
                    await Shell.Current.GoToAsync(".."));
            };
        }
    }
}
