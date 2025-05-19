using System;
using System.Collections;
using System.Windows.Input;
using Microsoft.Maui.Controls;

namespace EasySECv2.Controls
{
    public partial class CrudListView : ContentView
    {
        public CrudListView()
        {
            InitializeComponent();
        }

        // 1) ItemsSource
        public static readonly BindableProperty ItemsSourceProperty =
            BindableProperty.Create(
                nameof(ItemsSource),
                typeof(IEnumerable),
                typeof(CrudListView),
                null);
        public IEnumerable ItemsSource
        {
            get => (IEnumerable)GetValue(ItemsSourceProperty);
            set => SetValue(ItemsSourceProperty, value);
        }

        // 2) RowTemplate
        public static readonly BindableProperty RowTemplateProperty =
            BindableProperty.Create(
                nameof(RowTemplate),
                typeof(DataTemplate),
                typeof(CrudListView),
                default(DataTemplate));
        public DataTemplate RowTemplate
        {
            get => (DataTemplate)GetValue(RowTemplateProperty);
            set => SetValue(RowTemplateProperty, value);
        }

        // 3) FilterTemplate + callback
        public static readonly BindableProperty FilterTemplateProperty =
            BindableProperty.Create(
                nameof(FilterTemplate),
                typeof(DataTemplate),
                typeof(CrudListView),
                default(DataTemplate),
                propertyChanged: OnFilterTemplateChanged);
        public DataTemplate FilterTemplate
        {
            get => (DataTemplate)GetValue(FilterTemplateProperty);
            set => SetValue(FilterTemplateProperty, value);
        }

        // 4) HeaderTemplate + callback
        public static readonly BindableProperty HeaderTemplateProperty =
            BindableProperty.Create(
                nameof(HeaderTemplate),
                typeof(DataTemplate),
                typeof(CrudListView),
                default(DataTemplate),
                propertyChanged: OnHeaderTemplateChanged);
        public DataTemplate HeaderTemplate
        {
            get => (DataTemplate)GetValue(HeaderTemplateProperty);
            set => SetValue(HeaderTemplateProperty, value);
        }

        // 5) AddCommand
        public static readonly BindableProperty AddCommandProperty =
            BindableProperty.Create(
                nameof(AddCommand),
                typeof(ICommand),
                typeof(CrudListView),
                null);
        public ICommand AddCommand
        {
            get => (ICommand)GetValue(AddCommandProperty);
            set => SetValue(AddCommandProperty, value);
        }

        // 6) ImportCommand (optional)
        public static readonly BindableProperty ImportCommandProperty =
            BindableProperty.Create(
                nameof(ImportCommand),
                typeof(ICommand),
                typeof(CrudListView),
                null);
        public ICommand ImportCommand
        {
            get => (ICommand)GetValue(ImportCommandProperty);
            set => SetValue(ImportCommandProperty, value);
        }

        // 7) RefreshCommand
        public static readonly BindableProperty RefreshCommandProperty =
            BindableProperty.Create(
                nameof(RefreshCommand),
                typeof(ICommand),
                typeof(CrudListView),
                null);
        public ICommand RefreshCommand
        {
            get => (ICommand)GetValue(RefreshCommandProperty);
            set => SetValue(RefreshCommandProperty, value);
        }

        // 8) EditCommand (for row buttons)
        public static readonly BindableProperty EditCommandProperty =
            BindableProperty.Create(
                nameof(EditCommand),
                typeof(ICommand),
                typeof(CrudListView),
                null);
        public ICommand EditCommand
        {
            get => (ICommand)GetValue(EditCommandProperty);
            set => SetValue(EditCommandProperty, value);
        }

        // 9) DeleteCommand (for row buttons)
        public static readonly BindableProperty DeleteCommandProperty =
            BindableProperty.Create(
                nameof(DeleteCommand),
                typeof(ICommand),
                typeof(CrudListView),
                null);
        public ICommand DeleteCommand
        {
            get => (ICommand)GetValue(DeleteCommandProperty);
            set => SetValue(DeleteCommandProperty, value);
        }

        // 10) SelectedItem (two‐way)
        public static readonly BindableProperty SelectedItemProperty =
            BindableProperty.Create(
                nameof(SelectedItem),
                typeof(object),
                typeof(CrudListView),
                null,
                BindingMode.TwoWay);
        public object SelectedItem
        {
            get => GetValue(SelectedItemProperty);
            set => SetValue(SelectedItemProperty, value);
        }

        // Rebuild filter & header when VM changes
        protected override void OnBindingContextChanged()
        {
            base.OnBindingContextChanged();
            BuildFilter();
            BuildHeader();
        }

        // Called when FilterTemplate changes
        static void OnFilterTemplateChanged(BindableObject bindable, object oldVal, object newVal)
            => ((CrudListView)bindable).BuildFilter();

        // Called when HeaderTemplate changes
        static void OnHeaderTemplateChanged(BindableObject bindable, object oldVal, object newVal)
            => ((CrudListView)bindable).BuildHeader();

        // Instantiate and insert filter UI
        void BuildFilter()
        {
            if (FilterTemplate == null)
            {
                FilterHost.Content = null;
                return;
            }

            var content = FilterTemplate.CreateContent();
            View view = null;

            if (content is View v)
                view = v;
            else if (content is ViewCell cell && cell.View is View cv)
                view = cv;

            if (view == null)
                return;

            view.BindingContext = this.BindingContext;
            FilterHost.Content = view;
        }

        // Instantiate and insert header UI
        void BuildHeader()
        {
            if (HeaderTemplate == null)
            {
                HeaderHost.Content = null;
                return;
            }

            var content = HeaderTemplate.CreateContent();
            View view = null;

            if (content is View v)
                view = v;
            else if (content is ViewCell cell && cell.View is View cv)
                view = cv;

            if (view == null)
                return;

            view.BindingContext = this.BindingContext;
            HeaderHost.Content = view;
        }
    }
}
