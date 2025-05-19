using System;
using System.Collections;
using System.Linq;              // <-- для Cast<>
using System.Reflection;
using EasySECv2.Attributes;
using EasySECv2.Models;
using EasySECv2.Services;
using EasySECv2.ViewModels;
using Microsoft.Maui.Controls;
using Microsoft.Maui.Dispatching; // для MainThread

namespace EasySECv2.Views
{
    [QueryProperty(nameof(ItemId), "id")]
    public partial class GenericEditPage : ContentPage
    {
        public GenericEditPage(GenericEditViewModel<Student> vm)
        {
            InitializeComponent();
            BindingContext = vm;
            BuildFields();
        }

        public string ItemId
        {
            set
            {
                if (long.TryParse(value, out var id)
                    && BindingContext is ILoadableViewModel loader)
                {
                    _ = loader.LoadExistingAsync(id);
                }
            }
        }

        void BuildFields()
        {
            var viewModel = BindingContext;
            if (viewModel == null) return;

            var fieldsProp = viewModel.GetType().GetProperty("Fields");
            if (fieldsProp == null) return;

            var fields = fieldsProp.GetValue(viewModel) as IEnumerable;
            if (fields == null) return;

            foreach (PropertyInfo pi in fields)
            {
                var attr = pi.GetCustomAttribute<EditableAttribute>();
                if (attr == null) continue;

                // 1) Label
                var lbl = new Label { Text = attr.Label };
                lbl.Style = (Style)Application.Current.Resources["FormLabelStyle"];

                // 2) Контрол
                View ctrl;
                if (attr.ControlType == "Picker")
                {
                    var picker = new Picker { Title = attr.Label };
                    picker.Style = (Style)Application.Current.Resources["FormPickerStyle"];

                    // ищем lookup-коллекцию: имя свойства + "s"
                    var lookupProp = viewModel.GetType().GetProperty(pi.Name + "s");
                    if (lookupProp != null)
                    {
                        var rawItems = lookupProp.GetValue(viewModel) as IEnumerable;
                        if (rawItems != null)
                        {
                            // приводим к IList; иначе материализуем в List<object>
                            IList itemsList = rawItems as IList
                                ?? rawItems.Cast<object>().ToList();
                            picker.ItemsSource = itemsList;
                            picker.ItemDisplayBinding = new Binding("name");
                        }
                    }

                    picker.SetBinding(Picker.SelectedItemProperty,
                        new Binding($"Item.{pi.Name}", mode: BindingMode.TwoWay));
                    ctrl = picker;
                }
                else
                {
                    var entry = new Entry();
                    entry.Style = (Style)Application.Current.Resources["FormEntryStyle"];
                    entry.SetBinding(Entry.TextProperty,
                        new Binding($"Item.{pi.Name}", mode: BindingMode.TwoWay));
                    ctrl = entry;
                }

                // 3) Обёртываем Label+Control во Frame
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

                // 4) Добавляем на страницу
                FieldsHost.Children.Add(wrapper);
            }

            // 5) Кнопки Сохранить/Отмена
            var buttons = new HorizontalStackLayout
            {
                Spacing = 12,
                HorizontalOptions = LayoutOptions.Center
            };
            var save = new Button { Text = "Сохранить" };
            save.SetBinding(Button.CommandProperty, "SaveCommand");
            var cancel = new Button { Text = "Отмена" };
            cancel.SetBinding(Button.CommandProperty, "CancelCommand");
            buttons.Children.Add(save);
            buttons.Children.Add(cancel);
            FieldsHost.Children.Add(buttons);

            // 6) Подписка на закрытие
            var closeEv = viewModel.GetType().GetEvent("CloseRequested");
            if (closeEv != null)
            {
                closeEv.AddEventHandler(viewModel, new Action<bool>(ok =>
                {
                    MainThread.BeginInvokeOnMainThread(async () =>
                        await Shell.Current.GoToAsync("..")
                    );
                }));
            }
        }
    }
}
